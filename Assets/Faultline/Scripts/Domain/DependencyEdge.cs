namespace Faultline.Domain
{
    using System;

    /// <summary>
    /// Direction of a dependency relationship in the infrastructure graph.
    /// </summary>
    public enum DependencyDirection
    {
        /// <summary>This node depends on the target (downstream of target).</summary>
        Downstream,
        /// <summary>The target depends on this node (upstream of target).</summary>
        Upstream
    }

    /// <summary>
    /// Type of dependency relationship.
    /// </summary>
    public enum DependencyType
    {
        ApiCall,
        Database,
        MessageQueue,
        SharedStorage,
        Network,
        Other
    }

    /// <summary>
    /// Immutable domain entity: a directed dependency from one service node
    /// to another, with an associated latency cost. Duplicate-edge prevention
    /// is handled by the graph, not by this entity.
    /// </summary>
    public sealed class DependencyEdge : IEquatable<DependencyEdge>
    {
        public Guid EdgeId { get; }
        public string SourceNodeId { get; }
        public string TargetNodeId { get; }
        public DependencyDirection Direction { get; }
        public DependencyType Type { get; }
        public TimeSpan LatencyCost { get; }

        public DependencyEdge(Guid edgeId, string sourceNodeId, string targetNodeId,
            DependencyDirection direction, DependencyType type, TimeSpan latencyCost)
        {
            EdgeId = edgeId;
            SourceNodeId = !string.IsNullOrWhiteSpace(sourceNodeId)
                ? sourceNodeId
                : throw new ArgumentException("Source node id is required.", nameof(sourceNodeId));
            TargetNodeId = !string.IsNullOrWhiteSpace(targetNodeId)
                ? targetNodeId
                : throw new ArgumentException("Target node id is required.", nameof(targetNodeId));
            Direction = direction;
            Type = type;
            LatencyCost = latencyCost;
        }

        public bool Equals(DependencyEdge other) =>
            other is not null && EdgeId.Equals(other.EdgeId);

        public override bool Equals(object obj) => Equals(obj as DependencyEdge);

        public override int GetHashCode() => EdgeId.GetHashCode();

        public static bool operator ==(DependencyEdge left, DependencyEdge right) =>
            EqualityComparer<DependencyEdge>.Default.Equals(left, right);

        public static bool operator !=(DependencyEdge left, DependencyEdge right) =>
            !(left == right);

        public override string ToString() =>
            $"DependencyEdge({SourceNodeId} -> {TargetNodeId}, {Direction}, {Type}, {LatencyCost.TotalMilliseconds:F0}ms)";
    }
}
