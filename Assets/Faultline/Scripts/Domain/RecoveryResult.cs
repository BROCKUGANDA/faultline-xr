namespace Faultline.Domain
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    /// <summary>
    /// Immutable domain entity: outcome of executing remediation against an
    /// incident; captures before/after node health and verification result.
    /// </summary>
    public sealed class RecoveryResult : IEquatable<RecoveryResult>
    {
        public Guid ResultId { get; }
        public bool Success { get; }
        public double HealthDelta { get; }       // change in aggregate health, -1..1
        public IReadOnlyList<Guid> EventIds { get; }
        public bool VerificationPassed { get; }
        public DateTimeOffset CompletedAt { get; }

        public RecoveryResult(Guid resultId, bool success, double healthDelta,
            IEnumerable<Guid> eventIds = null, bool verificationPassed = false,
            DateTimeOffset? completedAt = null)
        {
            ResultId = resultId;
            Success = success;
            HealthDelta = Math.Clamp(healthDelta, -1.0, 1.0);
            EventIds = new ReadOnlyCollection<Guid>((eventIds ?? Enumerable.Empty<Guid>()).ToList());
            VerificationPassed = verificationPassed;
            CompletedAt = completedAt ?? DateTimeOffset.UtcNow;
        }

        public bool Equals(RecoveryResult other) =>
            other is not null && ResultId.Equals(other.ResultId);

        public override bool Equals(object obj) => Equals(obj as RecoveryResult);

        public override int GetHashCode() => ResultId.GetHashCode();

        public static bool operator ==(RecoveryResult left, RecoveryResult right) =>
            EqualityComparer<RecoveryResult>.Default.Equals(left, right);

        public static bool operator !=(RecoveryResult left, RecoveryResult right) =>
            !(left == right);

        public override string ToString() =>
            $"RecoveryResult(success={Success}, healthDelta={HealthDelta:P0}, verified={VerificationPassed})";
    }
}
