namespace Faultline.Spatial
{
    using System;
    using Faultline.Domain;

    /// <summary>
    /// Direction of data flow along a spatial edge.
    /// </summary>
    public enum DataFlowDirection
    {
        /// <summary>Source depends on target (data flows source -> target).</summary>
        Downstream,
        /// <summary>Target depends on source (data flows target -> source).</summary>
        Upstream
    }

    /// <summary>
    /// Spatial representation of a directed dependency edge between two
    /// spatial nodes. Carries dependency direction, type, latency cost, and
    /// animated data-flow state. Pure data — Presentation renders the line /
    /// arrow and pulse animation at the boundary.
    /// </summary>
    public sealed class SpatialEdge
    {
        public string EdgeId { get; }
        public string SourceNodeId { get; }
        public string TargetNodeId { get; }
        public DataFlowDirection Direction { get; }
        public DependencyType Type { get; }
        public TimeSpan LatencyCost { get; }

        /// <summary>Whether the edge is currently highlighted (selection/impact).</summary>
        public bool IsHighlighted { get; private set; }

        /// <summary>Animated data-flow progress in [0, 1]. 1 = one full pulse cycle.</summary>
        public float DataFlowProgress { get; private set; }

        /// <summary>Whether animated data flow is currently active.</summary>
        public bool IsFlowAnimating { get; private set; }

        public event EventHandler<SpatialEdgeChangedEventArgs> Changed;

        public SpatialEdge(
            string edgeId,
            string sourceNodeId,
            string targetNodeId,
            DataFlowDirection direction,
            DependencyType type,
            TimeSpan latencyCost)
        {
            if (string.IsNullOrWhiteSpace(edgeId))
                throw new ArgumentException("Edge id is required.", nameof(edgeId));
            if (string.IsNullOrWhiteSpace(sourceNodeId))
                throw new ArgumentException("Source node id is required.", nameof(sourceNodeId));
            if (string.IsNullOrWhiteSpace(targetNodeId))
                throw new ArgumentException("Target node id is required.", nameof(targetNodeId));

            EdgeId = edgeId;
            SourceNodeId = sourceNodeId;
            TargetNodeId = targetNodeId;
            Direction = direction;
            Type = type;
            LatencyCost = latencyCost;
            IsHighlighted = false;
            DataFlowProgress = 0f;
            IsFlowAnimating = false;
        }

        /// <summary>Set highlight state (e.g. when a connected node is selected).</summary>
        public void SetHighlighted(bool highlighted)
        {
            if (IsHighlighted == highlighted) return;
            IsHighlighted = highlighted;
            Changed?.Invoke(this, new SpatialEdgeChangedEventArgs(EdgeId));
        }

        /// <summary>Start or stop the animated data-flow pulse.</summary>
        public void SetFlowAnimating(bool animating)
        {
            if (IsFlowAnimating == animating) return;
            IsFlowAnimating = animating;
            if (!animating) DataFlowProgress = 0f;
            Changed?.Invoke(this, new SpatialEdgeChangedEventArgs(EdgeId));
        }

        /// <summary>Advance the data-flow animation progress (called each tick while animating).</summary>
        public void AdvanceFlow(float deltaProgress)
        {
            if (!IsFlowAnimating) return;
            DataFlowProgress += deltaProgress;
            if (DataFlowProgress >= 1f) DataFlowProgress = 0f;
            Changed?.Invoke(this, new SpatialEdgeChangedEventArgs(EdgeId));
        }

        public override string ToString() =>
            $"SpatialEdge({EdgeId}, {SourceNodeId} -> {TargetNodeId}, {Direction}, {Type}, flow={DataFlowProgress:P0})";
    }

    /// <summary>Arguments for spatial edge changes.</summary>
    public sealed class SpatialEdgeChangedEventArgs : EventArgs
    {
        public string EdgeId { get; }

        public SpatialEdgeChangedEventArgs(string edgeId)
        {
            EdgeId = edgeId;
        }
    }
}
