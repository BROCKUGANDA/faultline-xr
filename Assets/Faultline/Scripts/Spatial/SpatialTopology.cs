namespace Faultline.Spatial
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Faultline.Domain;
    using Faultline.Domain.Errors;

    /// <summary>
    /// Assembles spatial nodes and edges into the infrastructure field
    /// (the 3D spatial representation of the infrastructure graph).
    ///
    /// Responsibilities:
    ///   - Build <see cref="SpatialNode"/> / <see cref="SpatialEdge"/> instances
    ///     from domain entities using <see cref="TopologyLayout"/> positions.
    ///   - Dynamic filtering: show/hide nodes by status or service type.
    ///   - Status materials derived from design-system colour tokens
    ///     (see <see cref="Faultline.DesignSystem.DesignTokens"/>).
    ///   - Handles missing / disconnected graph elements without throwing.
    ///
    /// Pure C# — no UnityEngine. The Presentation layer renders the result.
    /// </summary>
    public sealed class SpatialTopology
    {
        private readonly TopologyLayout _layout = new();
        private readonly Dictionary<string, SpatialNode> _nodes = new(StringComparer.Ordinal);
        private readonly Dictionary<string, SpatialEdge> _edges = new(StringComparer.Ordinal);

        /// <summary>All spatial nodes in the field.</summary>
        public IReadOnlyDictionary<string, SpatialNode> Nodes => _nodes;

        /// <summary>All spatial edges in the field.</summary>
        public IReadOnlyDictionary<string, SpatialEdge> Edges => _edges;

        /// <summary>Current status-material filter (null = show all).</summary>
        public NodeStatusMaterial? StatusFilter { get; private set; }

        /// <summary>Current service-type filter (null = show all).</summary>
        public string ServiceTypeFilter { get; private set; }

        public event EventHandler<TopologyChangedEventArgs> Changed;

        /// <summary>
        /// Build or rebuild the topology from a domain graph.
        /// Missing / disconnected elements are handled gracefully.
        /// </summary>
        public Faultline.Domain.Result<bool> BuildFromGraph(
            Faultline.Infrastructure.InfrastructureGraph graph,
            Faultline.Domain.IInfrastructureGraph queryableGraph)
        {
            if (graph == null)
            {
                return Faultline.Domain.Result<bool>.Failure("FL-GR-0043", "Graph cannot be null.");
            }

            _nodes.Clear();
            _edges.Clear();

            var positions = _layout.Layout(graph.Nodes, graph.Edges);

            // Resolve glyph per service type via design tokens (pure data).
            foreach (var node in graph.Nodes)
            {
                if (!positions.TryGetValue(node.NodeId, out var position))
                {
                    // Fallback: place at a safe default position.
                    position = new System.Numerics.Vector3(0f, -0.10f, CoordinateSystem.NearReachM);
                }

                var status = ResolveStatusMaterial(node.Status);
                var spatialNode = new SpatialNode(
                    node.NodeId,
                    node.DisplayName,
                    node.ServiceType,
                    position,
                    status,
                    glyphName: GlyphForServiceType(node.ServiceType),
                    label: node.DisplayName);
                _nodes[node.NodeId] = spatialNode;
            }

            // Build edges, skipping edges that reference missing nodes.
            foreach (var edge in graph.Edges)
            {
                if (!_nodes.ContainsKey(edge.SourceNodeId) ||
                    !_nodes.ContainsKey(edge.TargetNodeId))
                {
                    // Orphan edge — skip, do not throw.
                    continue;
                }

                var direction = edge.Direction == Faultline.Domain.DependencyDirection.Downstream
                    ? DataFlowDirection.Downstream
                    : DataFlowDirection.Upstream;

                var spatialEdge = new SpatialEdge(
                    edge.EdgeId.ToString(),
                    edge.SourceNodeId,
                    edge.TargetNodeId,
                    direction,
                    edge.Type,
                    edge.LatencyCost);
                _edges[spatialEdge.EdgeId] = spatialEdge;
            }

            ApplyFilters(StatusFilter, ServiceTypeFilter);

            Changed?.Invoke(this, new TopologyChangedEventArgs(
                _nodes.Count, _edges.Count, DateTimeOffset.UtcNow.Ticks));

            return Faultline.Domain.Result<bool>.Success(true);
        }

        /// <summary>
        /// Filter the visible nodes by status and/or service type.
        /// Pass null for either filter to disable it.
        /// </summary>
        public void FilterBy(NodeStatusMaterial? status = null, string serviceType = null)
        {
            StatusFilter = status;
            ServiceTypeFilter = string.IsNullOrWhiteSpace(serviceType) ? null : serviceType;
            ApplyFilters(status, ServiceTypeFilter);

            Changed?.Invoke(this, new TopologyChangedEventArgs(
                VisibleNodeCount, _edges.Count, DateTimeOffset.UtcNow.Ticks));
        }

        /// <summary>Number of nodes currently visible after filtering.</summary>
        public int VisibleNodeCount =>
            _nodes.Values.Count(n => IsVisible(n));

        private bool IsVisible(SpatialNode node)
        {
            if (StatusFilter.HasValue && node.StatusMaterial != StatusFilter.Value)
                return false;
            if (!string.IsNullOrEmpty(ServiceTypeFilter) &&
                !string.Equals(node.ServiceType, ServiceTypeFilter, StringComparison.OrdinalIgnoreCase))
                return false;
            return true;
        }

        private void ApplyFilters(NodeStatusMaterial? status, string serviceType)
        {
            // Filtering is logical only: nodes remain in _nodes but IsVisible
            // gates rendering. This keeps selection/highlight state intact.
            _ = status;
            _ = serviceType;
        }

        /// <summary>Highlight a node and its connected edges (selection feedback).</summary>
        public Faultline.Domain.Result<bool> HighlightNode(string nodeId, bool highlighted)
        {
            if (!_nodes.ContainsKey(nodeId))
            {
                return Faultline.Domain.Result<bool>.Failure("FL-GR-0043", $"Node not found in topology: {nodeId}.");
            }

            _nodes[nodeId].SetSelected(highlighted);

            foreach (var edge in _edges.Values.Where(e =>
                e.SourceNodeId == nodeId || e.TargetNodeId == nodeId))
            {
                edge.SetHighlighted(highlighted);
            }

            return Faultline.Domain.Result<bool>.Success(true);
        }

        /// <summary>Animate data flow on a specific edge.</summary>
        public Faultline.Domain.Result<bool> AnimateEdgeFlow(string edgeId, bool animating)
        {
            if (!_edges.ContainsKey(edgeId))
            {
                return Faultline.Domain.Result<bool>.Failure("FL-GR-0043", $"Edge not found in topology: {edgeId}.");
            }

            _edges[edgeId].SetFlowAnimating(animating);
            return Faultline.Domain.Result<bool>.Success(true);
        }

        // --- Private helpers ---

        private static NodeStatusMaterial ResolveStatusMaterial(Faultline.Domain.OperationalStatus status)
        {
            return status switch
            {
                Faultline.Domain.OperationalStatus.Healthy => NodeStatusMaterial.Healthy,
                Faultline.Domain.OperationalStatus.Degraded => NodeStatusMaterial.Degraded,
                Faultline.Domain.OperationalStatus.Failing => NodeStatusMaterial.Failed,
                Faultline.Domain.OperationalStatus.Down => NodeStatusMaterial.Critical,
                Faultline.Domain.OperationalStatus.Recovering => NodeStatusMaterial.Recovering,
                _ => NodeStatusMaterial.Unknown
            };
        }

        private static string GlyphForServiceType(string serviceType)
        {
            return serviceType switch
            {
                "Database" or "database" => "FL_DATABASE",
                "Gateway" or "gateway" => "FL_GATEWAY",
                "Auth" or "auth" or "Authentication" => "FL_AUTH",
                "Queue" or "queue" => "FL_QUEUE",
                "Worker" or "worker" => "FL_WORKER",
                "Network" or "network" => "FL_NETWORK",
                "Agent" or "agent" => "FL_AGENT",
                "Oracle" or "oracle" => "FL_ORACLE",
                "Sentry" or "sentry" => "FL_SENTRY",
                "Forge" or "forge" => "FL_FORGE",
                _ => "FL_NODE"
            };
        }
    }

    /// <summary>Arguments for topology change events.</summary>
    public sealed class TopologyChangedEventArgs : EventArgs
    {
        public int NodeCount { get; }
        public int EdgeCount { get; }
        public long TimestampTicks { get; }

        public TopologyChangedEventArgs(int nodeCount, int edgeCount, long timestampTicks)
        {
            NodeCount = nodeCount;
            EdgeCount = edgeCount;
            TimestampTicks = timestampTicks;
        }
    }
}
