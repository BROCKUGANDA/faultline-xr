namespace Faultline.App
{
    using System.Collections.Generic;

    using Faultline.Domain;
    using Faultline.Incidents;

    /// <summary>
    /// Application composition root (PRD §9.3 <c>Faultline.App</c>). Wires the
    /// deterministic CASCADE world together: scenario → engine → incident
    /// service. Unity Presentation/XR layers call into this and never mutate
    /// domain state directly — all writes go through validated commands.
    /// </summary>
    public sealed class FaultlineApp
    {
        public ScenarioService Scenarios { get; }
        public IncidentService Incidents { get; }
        public SimulationEngine Engine => Scenarios.Engine;

        public FaultlineApp(IClock clock = null, int seed = 42)
        {
            var effectiveClock = clock ?? SystemClock.Instance;
            Scenarios = new ScenarioService(effectiveClock, seed);
            Incidents = new IncidentService(effectiveClock);
        }

        public Result<bool> StartScenario(string scenarioId) =>
            Scenarios.StartScenario(scenarioId);

        public Result<bool> StopScenario() => Scenarios.StopScenario();

        public IReadOnlyDictionary<string, object> GetState() =>
            Scenarios.GetScenarioState();
    }
}
