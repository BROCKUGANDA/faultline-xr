namespace Faultline.Incidents
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Faultline.Domain;

    /// <summary>
    /// Concrete incident detection and lifecycle service (PRD §3.3).
    /// Owns incident state and validates every transition via the Incident
    /// state machine. Returns typed FL-IC errors on invalid transitions.
    ///
    /// <see cref="DetectIncident"/> returns the new incident id. Duplicate
    /// detection of the same (nodeId, ruleId) while an identical incident
    /// is still active returns FL-IC-0052 instead of creating a duplicate.
    /// Inject a <see cref="FixedClock"/> in tests for deterministic output.
    /// </summary>
    public sealed class IncidentService : IIncidentService
    {
        private readonly Dictionary<string, Incident> _incidents = new();
        private readonly IClock _clock;

        public IncidentService(IClock clock = null)
        {
            _clock = clock ?? SystemClock.Instance;
        }

        public IReadOnlyList<string> ActiveIncidentIds =>
            _incidents.Values
                .Where(i => i.State != IncidentLifecycleState.Resolved
                    && i.State != IncidentLifecycleState.Rejected
                    && i.State != IncidentLifecycleState.Cancelled
                    && i.State != IncidentLifecycleState.Failed
                    && i.State != IncidentLifecycleState.Incomplete)
                .Select(i => i.IncidentId.ToString())
                .ToList();

        public Result<string> DetectIncident(string nodeId, string ruleId, string severity) =>
            DetectIncident(nodeId, ruleId, severity, "cascade-42");

        internal Result<string> DetectIncident(string nodeId, string ruleId, string severity,
            string scenarioId)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                return Result<string>.Failure("FL-SM-0090", "Node id is required.");
            }

            var existing = _incidents.Values.FirstOrDefault(i =>
                i.NodeId == nodeId && i.RuleId == ruleId &&
                i.State != IncidentLifecycleState.Resolved &&
                i.State != IncidentLifecycleState.Rejected &&
                i.State != IncidentLifecycleState.Cancelled &&
                i.State != IncidentLifecycleState.Failed &&
                i.State != IncidentLifecycleState.Incomplete);

            if (existing != null)
            {
                return Result<string>.Failure(IncidentLifecycleErrors.AlreadyTerminal,
                    $"Active incident already exists for node {nodeId} rule {ruleId}: {existing.IncidentId}.");
            }

            var incidentId = Guid.NewGuid();
            var incident = Incident.Create(incidentId, scenarioId, nodeId, severity, ruleId,
                _clock.Now);

            _incidents[incidentId.ToString()] = incident;
            return Result<string>.Success(incidentId.ToString());
        }

        public Result<bool> TransitionIncident(string incidentId, string toState)
        {
            if (!_incidents.TryGetValue(incidentId, out var incident))
            {
                return Result<bool>.Failure(IncidentLifecycleErrors.NotFound,
                    $"Incident not found: {incidentId}.");
            }

            if (!Enum.TryParse<IncidentLifecycleState>(toState, ignoreCase: true, out var targetState))
            {
                return Result<bool>.Failure(IncidentLifecycleErrors.InvalidTransition,
                    $"Unknown state: {toState}.");
            }

            var validation = incident.ValidateTransition(targetState);
            if (!validation.IsValid)
            {
                return Result<bool>.Failure(validation.ErrorCode, validation.ErrorMessage);
            }

            var next = incident.TransitionTo(targetState, out var _);
            _incidents[incidentId] = next;
            return Result<bool>.Success(true);
        }

        /// <summary>Entity accessor so callers never parse dictionaries.</summary>
        public Result<Incident> GetIncidentEntity(string incidentId) =>
            _incidents.TryGetValue(incidentId, out var incident)
                ? Result<Incident>.Success(incident)
                : Result<Incident>.Failure(IncidentLifecycleErrors.NotFound,
                    $"Incident not found: {incidentId}.");

        public Result<IDictionary<string, object>> GetIncident(string incidentId)
        {
            if (!_incidents.TryGetValue(incidentId, out var incident))
            {
                return Result<IDictionary<string, object>>.Failure(
                    IncidentLifecycleErrors.NotFound, $"Incident not found: {incidentId}.");
            }

            return Result<IDictionary<string, object>>.Success(
                new Dictionary<string, object>
                {
                    ["incidentId"] = incident.IncidentId,
                    ["state"] = incident.State.ToString(),
                    ["nodeId"] = incident.NodeId,
                    ["severity"] = incident.Severity,
                    ["ruleId"] = incident.RuleId,
                    ["detectedAt"] = incident.DetectedAt,
                    ["eventIds"] = incident.EventIds,
                    ["evidenceIds"] = incident.EvidenceIds
                });
        }
    }
}
