namespace Faultline.Agents
{
    using System.Collections.Generic;
    using Faultline.Domain;

    /// <summary>
    /// Oracle agent — coordinator. Correlates findings from other agents and
    /// produces the incident diagnosis and remediation recommendation.
    /// </summary>
    public interface IOracleAgent
    {
        AgentTask CreateTask(string incidentId, IDictionary<string, object> context);
        AgentFinding Synthesize(string taskId, IEnumerable<AgentFinding> findings,
            IDictionary<string, object> context);
    }

    /// <summary>
    /// Trace agent — dependency analyst. Walks the infrastructure graph and
    /// identifies the propagation path and root cause.
    /// </summary>
    public interface ITraceAgent
    {
        AgentTask CreateTask(string incidentId, IDictionary<string, object> context);
        AgentFinding Analyze(string taskId, string rootCauseNodeId,
            IDictionary<string, object> context);
    }

    /// <summary>
    /// Sentry agent — telemetry analyst. Evaluates telemetry, logs and traces
    /// for symptom correlation and confidence.
    /// </summary>
    public interface ISentryAgent
    {
        AgentTask CreateTask(string incidentId, IDictionary<string, object> context);
        AgentFinding Analyze(string taskId, IReadOnlyList<TelemetryEvent> telemetry,
            IDictionary<string, object> context);
    }

    /// <summary>
    /// Forge agent — remediation specialist. Proposes remediation actions and
    /// estimates expected benefit and side effects.
    /// </summary>
    public interface IForgeAgent
    {
        AgentTask CreateTask(string incidentId, IDictionary<string, object> context);
        AgentFinding Propose(string taskId, string incidentId,
            IDictionary<string, object> context);
    }
}
