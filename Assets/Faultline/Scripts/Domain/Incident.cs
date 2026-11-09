namespace Faultline.Domain
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    /// <summary>
    /// Lifecycle states of an incident (PRD §5.1).
    /// </summary>
    public enum IncidentLifecycleState
    {
        Detected,
        Triaged,
        Investigating,
        AwaitingApproval,
        Remediating,
        Verifying,
        Resolved,

        Rejected,
        Cancelled,
        Failed,
        Incomplete
    }

    /// <summary>
    /// Typed incident lifecycle error codes (FL-IC-005x).
    /// </summary>
    public static class IncidentLifecycleErrors
    {
        public const string InvalidTransition = "FL-IC-0050";
        public const string NotFound = "FL-IC-0051";
        public const string AlreadyTerminal = "FL-IC-0052";
    }

    /// <summary>
    /// Result of a validated incident transition.
    /// </summary>
    public sealed class IncidentTransitionResult
    {
        public IncidentLifecycleState FromState { get; }
        public IncidentLifecycleState ToState { get; }
        public bool IsValid { get; }
        public string ErrorCode { get; }
        public string ErrorMessage { get; }

        private IncidentTransitionResult(IncidentLifecycleState fromState, IncidentLifecycleState toState,
            bool isValid, string errorCode, string errorMessage)
        {
            FromState = fromState;
            ToState = toState;
            IsValid = isValid;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
        }

        public static IncidentTransitionResult Valid(IncidentLifecycleState fromState,
            IncidentLifecycleState toState) =>
            new IncidentTransitionResult(fromState, toState, true, null, null);

        public static IncidentTransitionResult Invalid(IncidentLifecycleState fromState,
            IncidentLifecycleState toState, string errorCode, string errorMessage) =>
            new IncidentTransitionResult(fromState, toState, false, errorCode, errorMessage);
    }

    /// <summary>
    /// Immutable domain entity: an incident raised when a node or edge breaches
    /// an SLO. Carries a validated lifecycle state machine:
    /// DETECTED → TRIAGED → INVESTIGATING → AWAITING_APPROVAL → REMEDIATING →
    /// VERIFYING → RESOLVED. Terminal states: REJECTED, CANCELLED, FAILED,
    /// INCOMPLETE.
    /// </summary>
    public sealed class Incident : IEquatable<Incident>
    {
        private static readonly HashSet<IncidentLifecycleState> TerminalStates = new()
        {
            IncidentLifecycleState.Resolved,
            IncidentLifecycleState.Rejected,
            IncidentLifecycleState.Cancelled,
            IncidentLifecycleState.Failed,
            IncidentLifecycleState.Incomplete
        };

        private static readonly Dictionary<IncidentLifecycleState, HashSet<IncidentLifecycleState>>
            AllowedTransitions = new()
        {
            [IncidentLifecycleState.Detected] = new HashSet<IncidentLifecycleState>
            {
                IncidentLifecycleState.Triaged,
                IncidentLifecycleState.Rejected,
                IncidentLifecycleState.Cancelled,
                IncidentLifecycleState.Incomplete
            },
            [IncidentLifecycleState.Triaged] = new HashSet<IncidentLifecycleState>
            {
                IncidentLifecycleState.Investigating,
                IncidentLifecycleState.Rejected,
                IncidentLifecycleState.Cancelled,
                IncidentLifecycleState.Incomplete
            },
            [IncidentLifecycleState.Investigating] = new HashSet<IncidentLifecycleState>
            {
                IncidentLifecycleState.AwaitingApproval,
                IncidentLifecycleState.Rejected,
                IncidentLifecycleState.Cancelled,
                IncidentLifecycleState.Incomplete
            },
            [IncidentLifecycleState.AwaitingApproval] = new HashSet<IncidentLifecycleState>
            {
                IncidentLifecycleState.Remediating,
                IncidentLifecycleState.Rejected,
                IncidentLifecycleState.Cancelled,
                IncidentLifecycleState.Incomplete
            },
            [IncidentLifecycleState.Remediating] = new HashSet<IncidentLifecycleState>
            {
                IncidentLifecycleState.Verifying,
                IncidentLifecycleState.Failed,
                IncidentLifecycleState.Rejected,
                IncidentLifecycleState.Cancelled,
                IncidentLifecycleState.Incomplete
            },
            [IncidentLifecycleState.Verifying] = new HashSet<IncidentLifecycleState>
            {
                IncidentLifecycleState.Resolved,
                IncidentLifecycleState.Failed,
                IncidentLifecycleState.Rejected,
                IncidentLifecycleState.Cancelled,
                IncidentLifecycleState.Incomplete
            }
        };

        public Guid IncidentId { get; }
        public string ScenarioId { get; }
        public IncidentLifecycleState State { get; }
        public string NodeId { get; }
        public string Severity { get; }
        public string RuleId { get; }
        public DateTimeOffset DetectedAt { get; }
        public DateTimeOffset? TransitionedAt { get; }
        public IReadOnlyList<Guid> EventIds { get; }
        public IReadOnlyList<Guid> EvidenceIds { get; }
        public string ResolutionNote { get; }

        private Incident(Guid incidentId, string scenarioId, IncidentLifecycleState state,
            string nodeId, string severity, string ruleId, DateTimeOffset detectedAt,
            DateTimeOffset? transitionedAt, IEnumerable<Guid> eventIds,
            IEnumerable<Guid> evidenceIds, string resolutionNote)
        {
            IncidentId = incidentId;
            ScenarioId = !string.IsNullOrWhiteSpace(scenarioId)
                ? scenarioId
                : throw new ArgumentException("Scenario id is required.", nameof(scenarioId));
            State = state;
            NodeId = nodeId;
            Severity = severity;
            RuleId = ruleId;
            DetectedAt = detectedAt;
            TransitionedAt = transitionedAt;
            EventIds = new ReadOnlyCollection<Guid>((eventIds ?? Enumerable.Empty<Guid>()).ToList());
            EvidenceIds = new ReadOnlyCollection<Guid>((evidenceIds ?? Enumerable.Empty<Guid>()).ToList());
            ResolutionNote = resolutionNote;
        }

        /// <summary>
        /// Create a new incident in the DETECTED state.
        /// </summary>
        public static Incident Create(Guid incidentId, string scenarioId, string nodeId,
            string severity, string ruleId, DateTimeOffset detectedAt,
            IEnumerable<Guid> eventIds = null, IEnumerable<Guid> evidenceIds = null) =>
            new Incident(incidentId, scenarioId, IncidentLifecycleState.Detected,
                nodeId, severity, ruleId, detectedAt, detectedAt,
                eventIds ?? Enumerable.Empty<Guid>(), evidenceIds ?? Enumerable.Empty<Guid>(), null);

        /// <summary>
        /// Validate a transition without applying it.
        /// </summary>
        public IncidentTransitionResult ValidateTransition(IncidentLifecycleState toState)
        {
            if (TerminalStates.Contains(State))
            {
                return IncidentTransitionResult.Invalid(State, toState,
                    IncidentLifecycleErrors.AlreadyTerminal,
                    $"Incident {IncidentId} is already in terminal state {State}.");
            }

            if (!AllowedTransitions.TryGetValue(State, out var allowed) || !allowed.Contains(toState))
            {
                return IncidentTransitionResult.Invalid(State, toState,
                    IncidentLifecycleErrors.InvalidTransition,
                    $"Incident {IncidentId} transition not allowed from {State} to {toState}.");
            }

            return IncidentTransitionResult.Valid(State, toState);
        }

        /// <summary>
        /// Return a new incident with the given state applied, if the transition
        /// is valid. Returns <c>this</c> and populates the result when invalid.
        /// </summary>
        public Incident TransitionTo(IncidentLifecycleState toState, out IncidentTransitionResult result)
        {
            result = ValidateTransition(toState);
            if (!result.IsValid)
            {
                return this;
            }

            return new Incident(IncidentId, ScenarioId, toState, NodeId, Severity, RuleId,
                DetectedAt, DateTimeOffset.UtcNow, EventIds, EvidenceIds, ResolutionNote);
        }

        public Incident WithEvents(IEnumerable<Guid> eventIds) =>
            new Incident(IncidentId, ScenarioId, State, NodeId, Severity, RuleId,
                DetectedAt, TransitionedAt, eventIds, EvidenceIds, ResolutionNote);

        public Incident WithEvidence(IEnumerable<Guid> evidenceIds) =>
            new Incident(IncidentId, ScenarioId, State, NodeId, Severity, RuleId,
                DetectedAt, TransitionedAt, EventIds, evidenceIds, ResolutionNote);

        public Incident WithResolutionNote(string resolutionNote) =>
            new Incident(IncidentId, ScenarioId, State, NodeId, Severity, RuleId,
                DetectedAt, TransitionedAt, EventIds, EvidenceIds, resolutionNote);

        public bool Equals(Incident other) =>
            other is not null && IncidentId.Equals(other.IncidentId);

        public override bool Equals(object obj) => Equals(obj as Incident);

        public override int GetHashCode() => IncidentId.GetHashCode();

        public static bool operator ==(Incident left, Incident right) =>
            EqualityComparer<Incident>.Default.Equals(left, right);

        public static bool operator !=(Incident left, Incident right) =>
            !(left == right);

        public override string ToString() =>
            $"Incident({IncidentId}, {State}, node={NodeId}, severity={Severity})";
    }
}
