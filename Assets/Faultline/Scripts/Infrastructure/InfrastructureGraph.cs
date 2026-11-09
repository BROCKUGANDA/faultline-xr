namespace Faultline.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    using Faultline.Domain;

    /// <summary>
    /// Result of analysing the blast radius and health impact of a node or edge.
    /// </summary>
    public sealed class ImpactAnalysis
    {
        public string RootNodeId { get; }
        public IReadOnlyList<string> BlastRadius { get; }   // all reachable node ids
        public IReadOnlyList<string> PropagationPath { get; } // ordered traversal path
        public IReadOnlyDictionary<string, double> HealthImpact { get; } // nodeId -> delta
        public bool HasCycle { get; }
        public IReadOnlyList<string> CyclePath { get; }

        public ImpactAnalysis(string rootNodeId,
            IEnumerable<string> blastRadius,
            IEnumerable<string> propagationPath,
            IDictionary<string, double> healthImpact,
            bool hasCycle,
            IEnumerable<string> cyclePath = null)
        {
            RootNodeId = rootNodeId;
            BlastRadius = new ReadOnlyCollection<string>(blastRadius.Distinct().ToList());
            PropagationPath = new ReadOnlyCollection<string>(propagationPath.ToList());
            HealthImpact = new ReadOnlyDictionary<string, double>(healthImpact ?? new Dictionary<string, double>());
            HasCycle = hasCycle;
            CyclePath = new ReadOnlyCollection<string>((cyclePath ?? Enumerable.Empty<string>()).ToList());
        }
    }

    /// <summary>
    /// Directed infrastructure graph of service nodes and dependency edges.
    /// Supports upstream/downstream traversal, impact analysis, cycle
    /// detection, duplicate-edge prevention, and cycle-safe traversal via
    /// iterative DFS/BFS with a visited set.
    /// </summary>
    public sealed class InfrastructureGraph
    {
        private readonly Dictionary<string, ServiceNode> _nodes = new();
        private readonly Dictionary<string, DependencyEdge> _edges = new();
        private readonly Dictionary<string, List<string>> _downstream = new(); // source -> targets
        private readonly Dictionary<string, List<string>> _upstream = new();   // target -> sources

        public IReadOnlyCollection<ServiceNode> Nodes => _nodes.Values;
        public IReadOnlyCollection<DependencyEdge> Edges => _edges.Values;

        /// <summary>
        /// Add a node to the graph. Returns failure if the node id already
        /// exists.
        /// </summary>
        public Faultline.Domain.Result<bool> AddNode(ServiceNode node)
        {
            if (node == null)
            {
                return Faultline.Domain.Result<bool>.Failure("FL-GR-0043", "Node cannot be null.");
            }

            if (_nodes.ContainsKey(node.NodeId))
            {
                return Faultline.Domain.Result<bool>.Failure("FL-GR-0043",
                    $"Node already exists: {node.NodeId}.");
            }

            _nodes[node.NodeId] = node;
            _downstream[node.NodeId] = new List<string>();
            _upstream[node.NodeId] = new List<string>();
            return Faultline.Domain.Result<bool>.Success(true);
        }

        /// <summary>
        /// Add a directed dependency edge. Prevents duplicate edges between
        /// the same source and target. Validates both endpoints exist.
        /// </summary>
        public Faultline.Domain.Result<bool> AddEdge(DependencyEdge edge)
        {
            if (edge == null)
            {
                return Faultline.Domain.Result<bool>.Failure("FL-GR-0043", "Edge cannot be null.");
            }

            if (!_nodes.ContainsKey(edge.SourceNodeId))
            {
                return Faultline.Domain.Result<bool>.Failure("FL-GR-0041",
                    $"Source node not found: {edge.SourceNodeId}.");
            }

            if (!_nodes.ContainsKey(edge.TargetNodeId))
            {
                return Faultline.Domain.Result<bool>.Failure("FL-GR-0041",
                    $"Target node not found: {edge.TargetNodeId}.");
            }

            var edgeKey = $"{edge.SourceNodeId}->{edge.TargetNodeId}";
            if (_edges.ContainsKey(edgeKey))
            {
                return Faultline.Domain.Result<bool>.Failure("FL-GR-0042",
                    $"Duplicate edge from {edge.SourceNodeId} to {edge.TargetNodeId}.");
            }

            _edges[edgeKey] = edge;

            if (edge.Direction == Domain.DependencyDirection.Downstream)
            {
                // Source depends on target: traversal source -> target is downstream
                _downstream[edge.SourceNodeId].Add(edge.TargetNodeId);
                _upstream[edge.TargetNodeId].Add(edge.SourceNodeId);
            }
            else
            {
                // Upstream: target depends on source
                _downstream[edge.TargetNodeId].Add(edge.SourceNodeId);
                _upstream[edge.SourceNodeId].Add(edge.TargetNodeId);
            }

            return Faultline.Domain.Result<bool>.Success(true);
        }

        public bool NodeExists(string nodeId) => _nodes.ContainsKey(nodeId);

        public Faultline.Domain.Result<bool> NodeExistsResult(string nodeId) =>
            _nodes.ContainsKey(nodeId)
                ? Faultline.Domain.Result<bool>.Success(true)
                : Faultline.Domain.Result<bool>.Failure("FL-GR-0043", $"Node not found: {nodeId}.");

        public Faultline.Domain.Result<string> GetNodeStatus(string nodeId)
        {
            if (!_nodes.TryGetValue(nodeId, out var node))
            {
                return Faultline.Domain.Result<string>.Failure("FL-GR-0043", $"Node not found: {nodeId}.");
            }

            return Faultline.Domain.Result<string>.Success(node.Status.ToString());
        }

        public IReadOnlyList<string> GetDependents(string nodeId)
        {
            // Nodes that depend on this node (upstream direction from this node's perspective)
            return _upstream.TryGetValue(nodeId, out var list)
                ? new ReadOnlyCollection<string>(new List<string>(list))
                : new ReadOnlyCollection<string>(new List<string>());
        }

        public IReadOnlyList<string> GetDependencies(string nodeId)
        {
            // Nodes this node depends on (downstream traversal)
            return _downstream.TryGetValue(nodeId, out var list)
                ? new ReadOnlyCollection<string>(new List<string>(list))
                : new ReadOnlyCollection<string>(new List<string>());
        }

        public Faultline.Domain.Result<bool> HasCycleResult()
        {
            var (hasCycle, path) = DetectCycle();
            return Faultline.Domain.Result<bool>.Success(hasCycle);
        }

        public (bool HasCycle, IReadOnlyList<string> CyclePath) DetectCycle()
        {
            var visited = new HashSet<string>();
            var recStack = new HashSet<string>();
            var path = new List<string>();

            foreach (var start in _nodes.Keys)
            {
                if (DetectCycleDfs(start, visited, recStack, path, out var cyclePath))
                {
                    return (true, cyclePath);
                }
            }

            return (false, new List<string>());
        }

        private bool DetectCycleDfs(string node, HashSet<string> visited, HashSet<string> recStack,
            List<string> path, out List<string> cyclePath)
        {
            if (recStack.Contains(node))
            {
                var idx = path.IndexOf(node);
                cyclePath = path.Skip(idx).ToList();
                return true;
            }

            if (visited.Contains(node))
            {
                cyclePath = null;
                return false;
            }

            visited.Add(node);
            recStack.Add(node);
            path.Add(node);

            foreach (var neighbor in GetOutgoing(node))
            {
                if (DetectCycleDfs(neighbor, visited, recStack, path, out var foundPath))
                {
                    cyclePath = foundPath;
                    return true;
                }
            }

            path.RemoveAt(path.Count - 1);
            recStack.Remove(node);
            cyclePath = null;
            return false;
        }

        /// <summary>
        /// Iterative DFS downstream traversal from a start node, returning all
        /// reachable nodes in visitation order. Cycle-safe via visited set.
        /// </summary>
        public IReadOnlyList<string> TraverseDownstream(string startNodeId)
        {
            var result = new List<string>();
            if (!_nodes.ContainsKey(startNodeId))
            {
                return new ReadOnlyCollection<string>(result);
            }

            var visited = new HashSet<string> { startNodeId };
            var stack = new Stack<string>();
            stack.Push(startNodeId);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                result.Add(current);

                foreach (var neighbor in GetOutgoing(current))
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        stack.Push(neighbor);
                    }
                }
            }

            return new ReadOnlyCollection<string>(result);
        }

        /// <summary>
        /// Iterative DFS upstream traversal from a start node, returning all
        /// nodes that depend (directly or transitively) on it. Cycle-safe.
        /// </summary>
        public IReadOnlyList<string> TraverseUpstream(string startNodeId)
        {
            var result = new List<string>();
            if (!_nodes.ContainsKey(startNodeId))
            {
                return new ReadOnlyCollection<string>(result);
            }

            var visited = new HashSet<string> { startNodeId };
            var stack = new Stack<string>();
            stack.Push(startNodeId);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                result.Add(current);

                foreach (var neighbor in GetIncoming(current))
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        stack.Push(neighbor);
                    }
                }
            }

            return new ReadOnlyCollection<string>(result);
        }

        /// <summary>
        /// Analyse the blast radius and health impact of a node. Health impact
        /// decays by downstream distance: the root takes the full hit and each
        /// additional hop contributes less, so nested failures do not sum to
        /// more than catastrophic.
        /// </summary>
        public Faultline.Domain.Result<ImpactAnalysis> AnalyzeImpact(string nodeId)
        {
            if (!_nodes.TryGetValue(nodeId, out var root))
            {
                return Faultline.Domain.Result<ImpactAnalysis>.Failure("FL-GR-0043",
                    $"Node not found: {nodeId}.");
            }

            var (hasCycle, cyclePath) = DetectCycle();

            // Breadth-first downstream walk so each node's distance from the
            // root is known; iterative to stay cycle-safe.
            var distances = new Dictionary<string, int>();
            var blastRadius = new List<string>();
            var queue = new Queue<string>();
            distances[nodeId] = 0;
            queue.Enqueue(nodeId);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                blastRadius.Add(current);

                foreach (var neighbor in GetOutgoing(current))
                {
                    if (!distances.ContainsKey(neighbor))
                    {
                        distances[neighbor] = distances[current] + 1;
                        queue.Enqueue(neighbor);
                    }
                }
            }

            var propagationPath = blastRadius;
            var healthImpact = new Dictionary<string, double>();
            foreach (var nid in blastRadius)
            {
                if (!_nodes.TryGetValue(nid, out var n)) continue;
                double delta = nid == nodeId ? -1.0 : -0.25 / distances[nid];
                healthImpact[nid] = delta;
            }

            return Faultline.Domain.Result<ImpactAnalysis>.Success(
                new ImpactAnalysis(nodeId, blastRadius, propagationPath, healthImpact, hasCycle, cyclePath));
        }

        private IEnumerable<string> GetOutgoing(string nodeId)
        {
            return _downstream.TryGetValue(nodeId, out var list) ? list : Enumerable.Empty<string>();
        }

        private IEnumerable<string> GetIncoming(string nodeId)
        {
            return _upstream.TryGetValue(nodeId, out var list) ? list : Enumerable.Empty<string>();
        }
    }
}
