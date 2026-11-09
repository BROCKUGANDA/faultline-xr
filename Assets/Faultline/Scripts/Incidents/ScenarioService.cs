namespace Faultline.Incidents
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Faultline.Domain;

    /// <summary>
    /// Concrete scenario lifecycle service (PRD §3.3). Owns the active
    /// <see cref="SimulationEngine"/> so scenario, graph, agents, and
    /// remediation share one deterministic world. CASCADE is the only
    /// fully-implemented scenario; other ids are rejected with FL-SC-0121.
    /// </summary>
    public sealed class ScenarioService : IScenarioService
    {
        public string CurrentScenarioId { get; private set; }
        public bool IsRunning { get; private set; }
        public SimulationEngine Engine { get; private set; }
        public string RootCauseNodeId { get; private set; }

        private readonly IClock _clock;
        private readonly int _seed;

        public ScenarioService(IClock clock = null, int seed = 42)
        {
            _clock = clock ?? SystemClock.Instance;
            _seed = seed;
        }

        public Result<bool> StartScenario(string scenarioId)
        {
            if (string.IsNullOrWhiteSpace(scenarioId))
            {
                return Result<bool>.Failure("FL-SC-0120", "Scenario id is required.",
                    new Dictionary<string, object> { { "scenarioId", scenarioId } });
            }

            if (!scenarioId.StartsWith("cascade", StringComparison.OrdinalIgnoreCase))
            {
                return Result<bool>.Failure("FL-SC-0121", $"Scenario not found: {scenarioId}.",
                    new Dictionary<string, object> { { "scenarioId", scenarioId } });
            }

            var engine = new SimulationEngine(_seed, scenarioId, _clock);
            Engine = engine;
            CurrentScenarioId = engine.ScenarioId;
            RootCauseNodeId = engine.RootCauseNodeId;
            IsRunning = true;

            return Result<bool>.Success(true);
        }

        public Result<bool> StopScenario()
        {
            if (!IsRunning)
            {
                return Result<bool>.Failure("FL-SC-0120", "No scenario is running.");
            }

            IsRunning = false;
            CurrentScenarioId = null;
            Engine = null;
            RootCauseNodeId = null;

            return Result<bool>.Success(true);
        }

        public IReadOnlyDictionary<string, object> GetScenarioState() =>
            new Dictionary<string, object>
            {
                { "scenarioId", CurrentScenarioId },
                { "isRunning", IsRunning },
                { "rootCauseNodeId", RootCauseNodeId },
                { "tick", Engine != null ? Engine.Tick : 0 }
            };
    }
}
