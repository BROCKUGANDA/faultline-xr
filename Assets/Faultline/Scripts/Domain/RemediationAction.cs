namespace Faultline.Domain
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    /// <summary>
    /// Type of remediation action.
    /// </summary>
    public enum RemediationActionType
    {
        RestartService,
        RollbackDeployment,
        ScaleUp,
        DrainNode,
        DrainService,
        Failover,
        ClearCache,
        ResetCircuitBreaker,
        Other
    }

    /// <summary>
    /// Reversibility of a remediation action.
    /// </summary>
    public enum RemediationReversibility
    {
        FullyReversible,
        PartiallyReversible,
        Irreversible
    }

    /// <summary>
    /// Immutable domain entity: one executable remediation step with affected
    /// services, expected benefit, side effects, reversibility and evidence ids.
    /// </summary>
    public sealed class RemediationAction : IEquatable<RemediationAction>
    {
        public Guid ActionId { get; }
        public RemediationActionType Type { get; }
        public string Description { get; }
        public IReadOnlyList<string> AffectedServiceIds { get; }
        public string ExpectedBenefit { get; }
        public IReadOnlyList<string> SideEffects { get; }
        public RemediationReversibility Reversibility { get; }
        public IReadOnlyList<Guid> EvidenceIds { get; }

        public RemediationAction(Guid actionId, RemediationActionType type, string description,
            IEnumerable<string> affectedServiceIds, string expectedBenefit,
            IEnumerable<string> sideEffects = null,
            RemediationReversibility reversibility = RemediationReversibility.PartiallyReversible,
            IEnumerable<Guid> evidenceIds = null)
        {
            ActionId = actionId;
            Type = type;
            Description = !string.IsNullOrWhiteSpace(description)
                ? description
                : throw new ArgumentException("Description is required.", nameof(description));
            AffectedServiceIds = new ReadOnlyCollection<string>(
                (affectedServiceIds ?? Enumerable.Empty<string>()).Distinct().ToList());
            ExpectedBenefit = expectedBenefit;
            SideEffects = new ReadOnlyCollection<string>(
                (sideEffects ?? Enumerable.Empty<string>()).ToList());
            Reversibility = reversibility;
            EvidenceIds = new ReadOnlyCollection<Guid>(
                (evidenceIds ?? Enumerable.Empty<Guid>()).ToList());
        }

        public bool Equals(RemediationAction other) =>
            other is not null && ActionId.Equals(other.ActionId);

        public override bool Equals(object obj) => Equals(obj as RemediationAction);

        public override int GetHashCode() => ActionId.GetHashCode();

        public static bool operator ==(RemediationAction left, RemediationAction right) =>
            EqualityComparer<RemediationAction>.Default.Equals(left, right);

        public static bool operator !=(RemediationAction left, RemediationAction right) =>
            !(left == right);

        public override string ToString() =>
            $"RemediationAction({Type}, {Description}, reversible={Reversibility})";
    }
}
