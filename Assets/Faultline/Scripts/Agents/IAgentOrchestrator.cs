namespace Faultline.Domain
{
    using System.Collections.Generic;

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
}