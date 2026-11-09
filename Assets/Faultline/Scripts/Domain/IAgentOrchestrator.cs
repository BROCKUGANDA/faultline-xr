namespace Faultline.Domain
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Agent orchestration contract (PRD §3.3). Dispatches deterministic local
    /// agent tasks and collects findings. Implemented in Agents.
    /// </summary>
    public interface IAgentOrchestrator
    {
        Result<string> DispatchAgent(string agentType, string incidentId, IDictionary<string, object> context);
        Result<bool> CompleteTask(string taskId, IList<string> findingIds);
        IReadOnlyDictionary<string, object> GetTaskState(string taskId);
    }

    /// <summary>
    /// A single deterministic specialist investigation. Implementations are
    /// pure functions over incident evidence (graph + telemetry + logs/traces)
    /// — no wall-clock, no external calls. Role implementations may live in
    /// <c>Faultline.Agents</c>; LLM-backed roles implement the same shapes
    /// behind the Integrations seam and never execute domain mutations.
    /// </summary>
    public interface IAgentRole
    {
        AgentRole Role { get; }

        /// <summary>Investigate and produce one finding with real evidence ids.</summary>
        Result<AgentFinding> Investigate(Task<Result<AgentFinding>> previous);
    }

    /// <summary>Full investigation report produced by an orchestrator.</summary>
    public sealed class InvestigationReport
    {
        public string IncidentId { get; }
        public IReadOnlyList<AgentFinding> Findings { get; }
        public string Recommendation { get; }
        public double AverageConfidence { get; }

        public InvestigationReport(string incidentId,
            IReadOnlyList<AgentFinding> findings,
            string recommendation,
            double averageConfidence)
        {
            IncidentId = incidentId;
            Findings = findings;
            Recommendation = recommendation;
            AverageConfidence = averageConfidence;
        }

        public Task<Result<InvestigationReport>> ToTask() =>
            Task.FromResult(Result<InvestigationReport>.Success(this));

        public Task<Result<InvestigationReport>> ToTask(string errorCode, string errorMessage) =>
            Task.FromResult(Result<InvestigationReport>.Failure(errorCode, errorMessage));
    }
}
