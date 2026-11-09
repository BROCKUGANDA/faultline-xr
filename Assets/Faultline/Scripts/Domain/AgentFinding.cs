namespace Faultline.Domain
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    /// <summary>
    /// Immutable domain entity: an evidence/finding produced during an agent
    /// task; references source event ids.
    /// </summary>
    public sealed class AgentFinding : IEquatable<AgentFinding>
    {
        public Guid FindingId { get; }
        public Guid TaskId { get; }
        public IReadOnlyList<Guid> EvidenceIds { get; }
        public string Finding { get; }
        public double Confidence { get; }      // 0..1
        public IReadOnlyList<string> Limitations { get; }
        public string Recommendation { get; }

        public AgentFinding(Guid findingId, Guid taskId, IEnumerable<Guid> evidenceIds,
            string finding, double confidence, IEnumerable<string> limitations = null,
            string recommendation = null)
        {
            FindingId = findingId;
            TaskId = taskId;
            EvidenceIds = new ReadOnlyCollection<Guid>((evidenceIds ?? Enumerable.Empty<Guid>()).ToList());
            Finding = !string.IsNullOrWhiteSpace(finding)
                ? finding
                : throw new ArgumentException("Finding is required.", nameof(finding));
            Confidence = Math.Clamp(confidence, 0.0, 1.0);
            Limitations = new ReadOnlyCollection<string>(
                (limitations ?? Enumerable.Empty<string>()).ToList());
            Recommendation = recommendation;
        }

        public bool Equals(AgentFinding other) =>
            other is not null && FindingId.Equals(other.FindingId);

        public override bool Equals(object obj) => Equals(obj as AgentFinding);

        public override int GetHashCode() => FindingId.GetHashCode();

        public static bool operator ==(AgentFinding left, AgentFinding right) =>
            EqualityComparer<AgentFinding>.Default.Equals(left, right);

        public static bool operator !=(AgentFinding left, AgentFinding right) =>
            !(left == right);

        public override string ToString() =>
            $"AgentFinding({Confidence:P0}, {Finding})";
    }
}
