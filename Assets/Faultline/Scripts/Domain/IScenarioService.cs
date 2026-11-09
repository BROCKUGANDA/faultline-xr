namespace Faultline.Domain
{
    using System.Collections.Generic;

    /// <summary>
    /// Scenario lifecycle contract (PRD §3.3). Owns scenario start/stop and
    /// exposes the current scenario state. Implemented outside Domain.
    /// </summary>
    public interface IScenarioService
    {
        string CurrentScenarioId { get; }
        bool IsRunning { get; }
        Result<bool> StartScenario(string scenarioId);
        Result<bool> StopScenario();
        IReadOnlyDictionary<string, object> GetScenarioState();
    }
}