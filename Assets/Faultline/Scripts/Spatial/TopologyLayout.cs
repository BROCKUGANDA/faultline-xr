namespace Faultline.Spatial
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Faultline.Domain;

    /// <summary>
    /// Computes node positions in the consistent right-handed coordinate
    /// system (see <see cref="CoordinateSystem"/>).
    ///
    /// Responsibilities:
    ///   - Position nodes so labels do not overlap (minimum spacing enforced).
    ///   - Handle missing / disconnected graph elements gracefully (orphan
    ///     nodes placed at a safe default, never crash).
    ///   - Produce a deterministic layout given the same graph input.
    ///
    /// Pure C# — no UnityEngine. The result is consumed by
    /// <see cref="SpatialTopology"/> to build <see cref="SpatialNode"/> /
    /// <see cref="SpatialEdge"/> instances.
    /// </summary>
    public sealed class TopologyLayout
    {
        private readonly float _minNodeSpacingM = CoordinateSystem.MinNodeSpacingM;
        private readonly float _maxRadiusM = CoordinateSystem.FarReachM;

        /// <summary>Minimum spacing between node positions (metres).</summary>
        public float MinNodeSpacingM => _minNodeSpacingM;

        public TopologyLayout()
        {
        }

        /// <summary>
        /// Layout a set of nodes and edges into world positions.
        ///
        /// Nodes are placed in a circular field centered on the seated anchor,
        /// ordered deterministically by node id. Disconnected nodes (no edges)
        /// are placed on the outer ring; orphan edges (referencing missing
        /// nodes) are skipped with no exception.
        /// </summary>
        /// <param name="nodes">Domain nodes to position.</param>
        /// <param name="edges">Domain edges (used only to detect connectedness).</param>
        /// <returns>Mapping of node id -> world position.</returns>
        public IReadOnlyDictionary<string, System.Numerics.Vector3> Layout(
            IEnumerable<ServiceNode> nodes,
            IEnumerable<DependencyEdge> edges)
        {
            var nodeList = (nodes ?? Enumerable.Empty<ServiceNode>()).ToList();
            var edgeList = (edges ?? Enumerable.Empty<DependencyEdge>()).ToList();

            var result = new Dictionary<string, System.Numerics.Vector3>(StringComparer.Ordinal);

            if (nodeList.Count == 0)
            {
                return result;
            }

            // Deterministic ordering by node id for stable layouts.
            var ordered = nodeList.OrderBy(n => n.NodeId, StringComparer.Ordinal).ToList();

            // Determine connectedness: a node is connected if it appears in any edge.
            var connectedIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var edge in edgeList)
            {
                if (!string.IsNullOrEmpty(edge.SourceNodeId))
                    connectedIds.Add(edge.SourceNodeId);
                if (!string.IsNullOrEmpty(edge.TargetNodeId))
                    connectedIds.Add(edge.TargetNodeId);
            }

            var connected = ordered.Where(n => connectedIds.Contains(n.NodeId)).ToList();
            var disconnected = ordered.Where(n => !connectedIds.Contains(n.NodeId)).ToList();

            var positions = new Dictionary<string, System.Numerics.Vector3>(StringComparer.Ordinal);

            // Place connected nodes on an inner ring; disconnected on an outer ring.
            PlaceRing(connected, _maxRadiusM * 0.55f, positions);
            PlaceRing(disconnected, _maxRadiusM * 0.85f, positions);

            // Resolve any overlaps by pushing nodes apart (simple iterative repulsion).
            ResolveOverlaps(positions);

            foreach (var kvp in positions)
                result[kvp.Key] = kvp.Value;

            return result;
        }

        private void PlaceRing(
            List<ServiceNode> nodes,
            float radiusM,
            Dictionary<string, System.Numerics.Vector3> positions)
        {
            if (nodes.Count == 0) return;

            var step = MathF.Tau / nodes.Count;
            for (int i = 0; i < nodes.Count; i++)
            {
                var angle = i * step;
                var x = MathF.Cos(angle) * radiusM;
                var z = MathF.Sin(angle) * radiusM;
                // Y is up; keep nodes at a comfortable height (slightly below eye level).
                var y = -0.10f;
                positions[nodes[i].NodeId] = new System.Numerics.Vector3(x, y, z);
            }
        }

        private void ResolveOverlaps(
            Dictionary<string, System.Numerics.Vector3> positions)
        {
            // Simple repulsion: iterate a few times pushing overlapping nodes apart.
            // Keeps layout deterministic and bounded within the reach envelope.
            var items = positions.ToList();
            for (int iteration = 0; iteration < 10; iteration++)
            {
                bool anyMoved = false;
                for (int i = 0; i < items.Count; i++)
                {
                    for (int j = i + 1; j < items.Count; j++)
                    {
                        var a = items[i];
                        var b = items[j];
                        var diff = a.Value - b.Value;
                        var distSq = diff.LengthSquared();
                        var minDistSq = _minNodeSpacingM * _minNodeSpacingM;
                        if (distSq < minDistSq && distSq > 0.0001f)
                        {
                            var dist = MathF.Sqrt(distSq);
                            var overlap = (_minNodeSpacingM - dist) * 0.5f;
                            var direction = diff / dist;
                            var offset = direction * overlap;

                            items[i] = (a.Key, a.Value + offset);
                            items[j] = (b.Key, b.Value - offset);
                            anyMoved = true;
                        }
                    }
                }
                if (!anyMoved) break;

                // Clamp all back into the reach envelope.
                for (int i = 0; i < items.Count; i++)
                {
                    var (clamped, _) = CoordinateSystem.ClampToReachEnvelope(items[i].Value);
                    items[i] = (items[i].Key, clamped);
                }
            }

            // Write back.
            foreach (var kvp in items)
                positions[kvp.Key] = kvp.Value;
        }
    }
}
