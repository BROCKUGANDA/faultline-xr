namespace Faultline.Domain.Errors
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    /// <summary>
    /// Severity of an FL- domain error.
    /// </summary>
    public enum ErrorSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// Whether an operation failing with this error can be retried/recovered.
    /// </summary>
    public enum Recoverability
    {
        Recoverable,
        NonRecoverable
    }

    /// <summary>
    /// Structured domain error carrying a stable FL- code and diagnostic context.
    /// Every module maps its failures to its domain code; raw exceptions never
    /// leak across module boundaries.
    /// </summary>
    public readonly struct Error
    {
        public string Code { get; }
        public string Message { get; }
        public ErrorSeverity Severity { get; }
        public string CorrelationId { get; }
        public DateTimeOffset Timestamp { get; }
        public Recoverability Recoverability { get; }
        public string UserGuidance { get; }
        public IReadOnlyDictionary<string, object> DiagnosticContext { get; }

        public Error(string code, string message,
            ErrorSeverity severity = ErrorSeverity.Error,
            string correlationId = null,
            Recoverability recoverability = Recoverability.Recoverable,
            string userGuidance = null,
            IDictionary<string, object> diagnosticContext = null)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentNullException(nameof(code));
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentNullException(nameof(message));
            }

            Code = code;
            Message = message;
            Severity = severity;
            CorrelationId = correlationId ?? Guid.NewGuid().ToString("N");
            Timestamp = DateTimeOffset.UtcNow;
            Recoverability = recoverability;
            UserGuidance = userGuidance;
            DiagnosticContext = new ReadOnlyDictionary<string, object>(
                diagnosticContext ?? new Dictionary<string, object>());
        }

        public override string ToString() =>
            $"{Code}: {Message} (severity={Severity}, recoverable={Recoverability == Recoverability.Recoverable})";
    }

    /// <summary>
    /// Factory for the FL-[DOMAIN]-[NUMBER] error catalogue (PRD §8.4, ADR-005).
    /// Domains: RT, XR, IN, UI, GR, IC, AG, EV, RM, SM, PS, IT, SC.
    /// </summary>
    public static class ErrorCatalogue
    {
        private static Error Make(string code, string message, ErrorSeverity severity = ErrorSeverity.Error,
            string correlationId = null, Recoverability recoverability = Recoverability.Recoverable,
            string userGuidance = null, IDictionary<string, object> diagnosticContext = null) =>
            new Error(code, message, severity, correlationId, recoverability, userGuidance, diagnosticContext);

        // ------------------------------------------------------------------
        // RT — Runtime
        // ------------------------------------------------------------------
        public static Error RuntimeUnhandledException(string correlationId, string detail,
            IDictionary<string, object> context = null) =>
            Make("FL-RT-0001", $"Unhandled exception in domain command handler: {detail}",
                ErrorSeverity.Critical, correlationId, Recoverability.NonRecoverable,
                "An internal engine fault occurred. Please report with the correlation id.", context);

        // ------------------------------------------------------------------
        // XR — XR / OpenXR / Meta XR SDK
        // ------------------------------------------------------------------
        public static Error XrSessionFailed(string correlationId, IDictionary<string, object> context = null) =>
            Make("FL-XR-0010", "OpenXR session failed to start on device.",
                ErrorSeverity.Critical, correlationId, Recoverability.Recoverable,
                "Re-launch the app and ensure the headset is powered on.", context);

        public static Error XrHandTrackingUnavailable(string correlationId,
            IDictionary<string, object> context = null) =>
            Make("FL-XR-0011", "Hand tracking is not available or not supported on this device.",
                ErrorSeverity.Error, correlationId, Recoverability.NonRecoverable,
                "Use controller or gaze input fallback where available.", context);

        // ------------------------------------------------------------------
        // IN — Interaction
        // ------------------------------------------------------------------
        public static Error InteractionPinchRejected(string correlationId, float confidence,
            IDictionary<string, object> context = null) =>
            Make("FL-IN-0020", $"Pinch gesture rejected: confidence {confidence:P0} below threshold.",
                ErrorSeverity.Warning, correlationId, Recoverability.Recoverable,
                "Retry the pinch gesture with higher confidence.", context);

        public static Error InteractionTrackingLost(string correlationId,
            IDictionary<string, object> context = null) =>
            Make("FL-IN-0021", "Tracking lost during an active grab; object was dropped.",
                ErrorSeverity.Warning, correlationId, Recoverability.Recoverable,
                "Re-establish tracking before resuming interaction.", context);

        // ------------------------------------------------------------------
        // UI — Presentation / HUD / panels
        // ------------------------------------------------------------------
        public static Error UiPanelNotRegistered(string correlationId, string panelId,
            IDictionary<string, object> context = null) =>
            Make("FL-UI-0030", $"Panel requested with no registered view: {panelId}.",
                ErrorSeverity.Error, correlationId, Recoverability.Recoverable,
                "The requested panel is unavailable; try again later.", context);

        // ------------------------------------------------------------------
        // GR — Graph / Infrastructure
        // ------------------------------------------------------------------
        public static Error GraphCycleDetected(string correlationId, IEnumerable<string> cycle,
            IDictionary<string, object> context = null) =>
            Make("FL-GR-0040", $"Cycle detected in dependency graph: {string.Join(" -> ", cycle)}.",
                ErrorSeverity.Error, correlationId, Recoverability.NonRecoverable,
                "Remove or re-wire the circular dependency before continuing.", context);

        public static Error GraphUnknownNode(string correlationId, string nodeId,
            IDictionary<string, object> context = null) =>
            Make("FL-GR-0041", $"Dependency edge references unknown node: {nodeId}.",
                ErrorSeverity.Error, correlationId, Recoverability.NonRecoverable,
                "The referenced service node does not exist in the graph.", context);

        public static Error GraphDuplicateEdge(string correlationId, string source, string target,
            IDictionary<string, object> context = null) =>
            Make("FL-GR-0042", $"Duplicate dependency edge from {source} to {target}.",
                ErrorSeverity.Warning, correlationId, Recoverability.Recoverable,
                "The edge already exists; no action needed.", context);

        public static Error GraphNodeNotFound(string correlationId, string nodeId,
            IDictionary<string, object> context = null) =>
            Make("FL-GR-0043", $"Service node not found: {nodeId}.",
                ErrorSeverity.Error, correlationId, Recoverability.Recoverable,
                "Verify the node id is correct.", context);

        // ------------------------------------------------------------------
        // IC — Incidents
        // ------------------------------------------------------------------
        public static Error IncidentInvalidTransition(string correlationId, string incidentId,
            string fromState, string toState,
            IDictionary<string, object> context = null) =>
            Make("FL-IC-0050", $"Incident {incidentId} transition not allowed from {fromState} to {toState}.",
                ErrorSeverity.Error, correlationId, Recoverability.Recoverable,
                "Use a valid lifecycle transition for the incident's current state.", context);

        public static Error IncidentNotFound(string correlationId, string incidentId,
            IDictionary<string, object> context = null) =>
            Make("FL-IC-0051", $"Incident not found: {incidentId}.",
                ErrorSeverity.Error, correlationId, Recoverability.Recoverable,
                "Verify the incident id is correct.", context);

        public static Error IncidentAlreadyTerminal(string correlationId, string incidentId,
            string state,
            IDictionary<string, object> context = null) =>
            Make("FL-IC-0052", $"Incident {incidentId} is already in terminal state {state}.",
                ErrorSeverity.Warning, correlationId, Recoverability.NonRecoverable,
                "No further transitions are permitted on a terminal incident.", context);

        // ------------------------------------------------------------------
        // AG — Agents
        // ------------------------------------------------------------------
        public static Error AgentDispatchFailed(string correlationId, string agentType,
            IDictionary<string, object> context = null) =>
            Make("FL-AG-0060", $"Agent task dispatch failed; no agent available for role: {agentType}.",
                ErrorSeverity.Error, correlationId, Recoverability.Recoverable,
                "The agent orchestrator could not assign the task. Retry the dispatch.", context);

        public static Error AgentTaskTimeout(string correlationId, string taskId,
            IDictionary<string, object> context = null) =>
            Make("FL-AG-0061", $"Agent task {taskId} timed out before producing a result.",
                ErrorSeverity.Warning, correlationId, Recoverability.Recoverable,
                "The task will be retried or cancelled per policy.", context);

        public static Error AgentResultMalformed(string correlationId, string taskId,
            IDictionary<string, object> context = null) =>
            Make("FL-AG-0062", $"Agent task {taskId} produced a malformed or unsupported result.",
                ErrorSeverity.Error, correlationId, Recoverability.Recoverable,
                "The result was rejected and will not be used for synthesis.", context);

        // ------------------------------------------------------------------
        // EV — Events / Observability
        // ------------------------------------------------------------------
        public static Error EventDuplicateIdempotencyKey(string correlationId, string key,
            IDictionary<string, object> context = null) =>
            Make("FL-EV-0070", $"Duplicate event idempotency key rejected: {key}.",
                ErrorSeverity.Warning, correlationId, Recoverability.NonRecoverable,
                "The event was already processed; redelivery is ignored.", context);

        // ------------------------------------------------------------------
        // RM — Remediation
        // ------------------------------------------------------------------
        public static Error RemediationNotApproved(string correlationId, string planId,
            IDictionary<string, object> context = null) =>
            Make("FL-RM-0080", $"Remediation action not approved; execution blocked: {planId}.",
                ErrorSeverity.Error, correlationId, Recoverability.NonRecoverable,
                "Approval is required before execution.", context);

        public static Error RemediationDuplicateApproval(string correlationId, string planId,
            IDictionary<string, object> context = null) =>
            Make("FL-RM-0081", $"Duplicate approval protection triggered for plan: {planId}.",
                ErrorSeverity.Warning, correlationId, Recoverability.NonRecoverable,
                "The plan has already been approved; repeated approval gestures are ignored.", context);

        public static Error RemediationApprovalExpired(string correlationId, string planId,
            IDictionary<string, object> context = null) =>
            Make("FL-RM-0082", $"Approval for plan has expired: {planId}.",
                ErrorSeverity.Warning, correlationId, Recoverability.Recoverable,
                "Re-submit the proposal for a fresh approval.", context);

        public static Error RemediationAlreadyExecuted(string correlationId, string planId,
            IDictionary<string, object> context = null) =>
            Make("FL-RM-0083", $"Remediation plan has already been executed: {planId}.",
                ErrorSeverity.Warning, correlationId, Recoverability.NonRecoverable,
                "Execution idempotency prevents re-execution.", context);

        public static Error RemediationExecutionFailed(string correlationId, string planId,
            IDictionary<string, object> context = null) =>
            Make("FL-RM-0084", $"Remediation execution failed for plan: {planId}.",
                ErrorSeverity.Error, correlationId, Recoverability.Recoverable,
                "The previous simulation checkpoint will be restored.", context);

        // ------------------------------------------------------------------
        // SM — State management / Domain model
        // ------------------------------------------------------------------
        public static Error StateEntityNotFound(string correlationId, string entityType, string id,
            IDictionary<string, object> context = null) =>
            Make("FL-SM-0090", $"Invalid command: {entityType} not found: {id}.",
                ErrorSeverity.Error, correlationId, Recoverability.Recoverable,
                "Verify the entity id is correct.", context);

        public static Error StateInvalidCommand(string correlationId, string command, string reason,
            IDictionary<string, object> context = null) =>
            Make("FL-SM-0091", $"Invalid command '{command}': {reason}.",
                ErrorSeverity.Error, correlationId, Recoverability.Recoverable,
                "Correct the command parameters and retry.", context);

        // ------------------------------------------------------------------
        // PS — Persistence
        // ------------------------------------------------------------------
        public static Error PersistenceSaveFailed(string correlationId, string reason,
            IDictionary<string, object> context = null) =>
            Make("FL-PS-0100", $"Save failed: {reason}.",
                ErrorSeverity.Error, correlationId, Recoverability.Recoverable,
                "The scenario could not be persisted. Retry the save.", context);

        public static Error PersistenceLoadFailed(string correlationId, string reason,
            IDictionary<string, object> context = null) =>
            Make("FL-PS-0101", $"Load failed: {reason}.",
                ErrorSeverity.Error, correlationId, Recoverability.Recoverable,
                "The saved scenario could not be restored.", context);

        // ------------------------------------------------------------------
        // IT — Integrations
        // ------------------------------------------------------------------
        public static Error IntegrationAdapterUnreachable(string correlationId, string adapter,
            IDictionary<string, object> context = null) =>
            Make("FL-IT-0110", $"External adapter unreachable; fallback used: {adapter}.",
                ErrorSeverity.Warning, correlationId, Recoverability.Recoverable,
                "The operation proceeded with a local fallback.", context);

        // ------------------------------------------------------------------
        // SC — Scenario
        // ------------------------------------------------------------------
        public static Error ScenarioInvalid(string correlationId, string scenarioId, string reason,
            IDictionary<string, object> context = null) =>
            Make("FL-SC-0120", $"Scenario definition invalid; cannot start: {scenarioId} — {reason}.",
                ErrorSeverity.Error, correlationId, Recoverability.NonRecoverable,
                "Correct the scenario definition before starting.", context);

        public static Error ScenarioNotFound(string correlationId, string scenarioId,
            IDictionary<string, object> context = null) =>
            Make("FL-SC-0121", $"Scenario not found: {scenarioId}.",
                ErrorSeverity.Error, correlationId, Recoverability.Recoverable,
                "Verify the scenario id is correct.", context);
    }
}
