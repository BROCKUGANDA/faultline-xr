namespace Faultline.Incidents
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    using Faultline.Domain;
    using Faultline.Infrastructure;

    /// <summary>
    /// Snapshot of the simulation state at a given tick, used for checkpoints
    /// and rollback.
    /// </summary>
    public sealed class SimulationSnapshot
    {
        public int Tick { get; }
        public IReadOnlyDictionary<string, ServiceNode> Nodes { get; }
        public IReadOnlyList<TelemetryEvent> Telemetry { get; }
        public IReadOnlyList<LogEvent> Logs { get; }
        public IReadOnlyList<TraceEvent> Traces { get; }

        public SimulationSnapshot(int tick,
            IDictionary<string, ServiceNode> nodes,
            IEnumerable<TelemetryEvent> telemetry,
            IEnumerable<LogEvent> logs,
            IEnumerable<TraceEvent> traces)
        {
            Tick = tick;
            Nodes = new ReadOnlyDictionary<string, ServiceNode>(nodes
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
            Telemetry = new ReadOnlyCollection<TelemetryEvent>(telemetry.ToList());
            Logs = new ReadOnlyCollection<LogEvent>(logs.ToList());
            Traces = new ReadOnlyCollection<TraceEvent>(traces.ToList());
        }
    }

    /// <summary>
    /// Deterministic simulation engine. Ticks the scenario forward,
    /// propagates dependency health, generates correlated telemetry, tracks
    /// the root cause, executes remediation, and supports checkpoint /
    /// rollback for failed executions.
    ///
    /// Pure C# — no UnityEngine. Seeded random => reproducible runs.
    /// </summary>
    public sealed class SimulationEngine
    {
        private readonly Random _random;
        private readonly int _seed;
        private readonly IClock _clock;
        private readonly InfrastructureGraph _graph;
        private readonly string _rootCauseNodeId;
        private readonly string _scenarioId;

        private int _tick;
        private Dictionary<string, ServiceNode> _nodes;
        private List<TelemetryEvent> _telemetry = new();
        private List<LogEvent> _logs = new();
        private List<TraceEvent> _traces = new();
        private int _seq;

        private SimulationSnapshot _lastCheckpoint;

        public int TickCount => _tick;
        public string ScenarioId => _scenarioId;
        public string RootCauseNodeId => _rootCauseNodeId;
        public IReadOnlyDictionary<string, ServiceNode> Nodes => _nodes;
        public IReadOnlyList<TelemetryEvent> Telemetry => _telemetry;
        public IReadOnlyList<LogEvent> Logs => _logs;
        public IReadOnlyList<TraceEvent> Traces => _traces;

        public SimulationEngine(int seed, string scenarioId = null, IClock clock = null)
        {
            _random = new Random(seed);
            _seed = seed;
            _clock = clock ?? SystemClock.Instance;
            _scenarioId = scenarioId ?? $"sim-{seed}";

            var (graph, rootCause, id) = CascadeScenario.Create(seed);
            _graph = graph;
            _rootCauseNodeId = rootCause;
            _scenarioId = id;

            _nodes = graph.Nodes.ToDictionary(n => n.NodeId, n => n);
            _tick = 0;

            EmitLog("simulation", TelemetrySeverity.Information,
                $"Simulation {_scenarioId} initialized with root cause {_rootCauseNodeId}.");
        }

        /// <summary>
        /// Advance the simulation by one tick: propagate dependency health,
        /// generate correlated telemetry, apply fault drift.
        /// </summary>
        public void Tick()
        {
            _tick++;

            // Propagate dependency health: a failing/degraded dependency drags
            // down its dependents.
            var updated = new Dictionary<string, ServiceNode>(_nodes);
            foreach (var kvp in _nodes)
            {
                var nodeId = kvp.Key;
                var node = kvp.Value;

                var dependencies = _graph.GetDependencies(nodeId);
                double worstDependencyHealth = 1.0;
                foreach (var depId in dependencies)
                {
                    if (_nodes.TryGetValue(depId, out var dep))
                    {
                        worstDependencyHealth = Math.Min(worstDependencyHealth, dep.HealthScore);
                    }
                }

                double newHealth = node.HealthScore;
                if (nodeId == _rootCauseNodeId)
                {
                    // Root cause drifts downward over time unless remediated.
                    newHealth = Math.Max(0.0, newHealth - 0.02 * _random.NextDouble());
                }
                else if (worstDependencyHealth < 0.5)
                {
                    // Cascade: degraded dependency drags this node down.
                    var drag = 0.15 * (1.0 - worstDependencyHealth);
                    newHealth = Math.Max(0.0, newHealth - drag);
                }
                else if (newHealth < 1.0 && worstDependencyHealth >= 1.0)
                {
                    // Mild recovery when dependencies are healthy.
                    newHealth = Math.Min(1.0, newHealth + 0.05);
                }

                var newStatus = HealthToStatus(newHealth);
                if (newStatus != node.Status || Math.Abs(newHealth - node.HealthScore) > 0.001)
                {
                    updated[nodeId] = node.WithHealth(newHealth).WithStatus(newStatus);
                }
            }

            _nodes = updated;

            // Generate telemetry correlated with current state.
            GenerateTelemetry();

            EmitLog("simulation", TelemetrySeverity.Information,
                $"Tick {_tick}: aggregate health {AggregateHealth:P0}.");
        }

        /// <summary>
        /// Create a simulation checkpoint for rollback on failed remediation.
        /// </summary>
        public void Checkpoint()
        {
            _lastCheckpoint = new SimulationSnapshot(_tick, _nodes, _telemetry, _logs, _traces);
            EmitLog("simulation", TelemetrySeverity.Information,
                $"Checkpoint created at tick {_tick}.");
        }
        /// <summary>
        /// Restore the most recent checkpoint. Returns failure if no
        /// checkpoint exists.
        /// </summary>
        public Faultline.Domain.Result<bool> RestoreCheckpoint()
        {
            if (_lastCheckpoint == null)
            {
                return Faultline.Domain.Result<bool>.Failure("FL-RM-0084",
                    "No simulation checkpoint available to restore.");
            }

            _tick = _lastCheckpoint.Tick;
            _nodes = new Dictionary<string, ServiceNode>(_lastCheckpoint.Nodes);
            _telemetry = new List<TelemetryEvent>(_lastCheckpoint.Telemetry);
            _logs = new List<LogEvent>(_lastCheckpoint.Logs);
            _traces = new List<TraceEvent>(_lastCheckpoint.Traces);

            EmitLog("simulation", TelemetrySeverity.Warning,
                $"Restored checkpoint at tick {_tick}.");
            return Faultline.Domain.Result<bool>.Success(true);
        }

        /// <summary>
        /// Apply a remediation outcome to the graph: improve the health of the
        /// affected services by the given delta.
        /// </summary>
        public Faultline.Domain.Result<bool> ApplyRecovery(IEnumerable<string> affectedServiceIds,
            double healthDelta, IEnumerable<Guid> eventIds = null)
        {
            foreach (var serviceId in affectedServiceIds)
            {
                if (!_nodes.TryGetValue(serviceId, out var node)) continue;
                var newHealth = Math.Clamp(node.HealthScore + healthDelta, 0.0, 1.0);
                var newStatus = HealthToStatus(newHealth);
                _nodes[serviceId] = node.WithHealth(newHealth).WithStatus(newStatus);
            }

            if (eventIds != null)
            {
                foreach (var evId in eventIds)
                {
                    EmitTelemetry(serviceId: "simulation", severity: TelemetrySeverity.Information,
                        message: $"Remediation applied for event {evId}.",
                        metricName: "recovery_applied", metricValue: healthDelta);
                }
            }

            return Faultline.Domain.Result<bool>.Success(true);
        }

        public double AggregateHealth =>
            _nodes.Count == 0 ? 1.0 : _nodes.Values.Average(n => n.HealthScore);

        public Faultline.Domain.IInfrastructureGraph GraphView =>
            new InfrastructureGraphView(_graph);

        /// <summary>
        /// The concrete graph behind <see cref="GraphView"/>. Reachability and
        /// blast-radius analysis (<see cref="InfrastructureGraph.AnalyzeImpact"/>)
        /// live on the graph itself, so investigation callers need this in
        /// addition to the read-only interface.
        /// </summary>
        public InfrastructureGraph Graph => _graph;

        private void GenerateTelemetry()
        {
            foreach (var kvp in _nodes)
            {
                var nodeId = kvp.Key;
                var node = kvp.Value;

                // Latency telemetry correlated with health.
                var latencyMs = LatencyFor(nodeId) * (1.0 + (1.0 - node.HealthScore) * 3.0)
                    * (1.0 + 0.1 * _random.NextDouble());
                EmitTelemetry(nodeId, TelemetrySeverity.Information,
                    "Latency sample.", "latency_ms", latencyMs);

                // Error rate correlated with health (worse health => higher errors).
                var errorRate = Math.Clamp(node.ErrorRate + (1.0 - node.HealthScore) * 0.3, 0.0, 1.0);
                EmitTelemetry(nodeId, TelemetrySeverity.Warning,
                    "Error rate sample.", "error_rate", errorRate);

                // Throughput correlated with health.
                var throughput = Math.Max(0, node.Throughput * node.HealthScore
                    * (1.0 + 0.05 * _random.NextDouble()));
                EmitTelemetry(nodeId, TelemetrySeverity.Information,
                    "Throughput sample.", "throughput_rps", throughput);
            }
        }

        private void EmitTelemetry(string serviceId, TelemetrySeverity severity, string message,
            string metricName, double metricValue)
        {
            var ev = new TelemetryEvent(NextId(), _clock.Now.AddSeconds(_tick * 10L), serviceId, severity,
                message, metricName, metricValue);
            _telemetry.Add(ev);
        }

        private void EmitLog(string serviceId, TelemetrySeverity level, string message)
        {
            _logs.Add(new LogEvent(NextId(), _clock.Now.AddSeconds(_tick * 10L), serviceId, level, message));
        }

        private Guid NextId()
        {
            unchecked
            {
                var bytes = new byte[16];
                for (int lane = 0; lane < 4; lane++)
                {
                    uint hash = 2166136261u;
                    foreach (var b in System.Text.Encoding.UTF8.GetBytes($"{_seed}:{_tick}:{_seq++}:{lane}"))
                    {
                        hash ^= b;
                        hash *= 16777619u;
                    }

                    System.BitConverter.GetBytes(hash).CopyTo(bytes, lane * 4);
                }

                return new Guid(bytes);
            }
        }

        private static double LatencyFor(string nodeId) => nodeId switch
        {
            "api-gateway" => 12,
            "auth-service" => 18,
            "app-service" => 95,
            "database" => 240,
            "message-broker" => 8,
            "worker-cluster" => 110,
            "monitoring" => 5,
            _ => 20
        };

        private static Domain.OperationalStatus HealthToStatus(double health) =>
            health switch
            {
                >= 0.9 => Domain.OperationalStatus.Healthy,
                >= 0.6 => Domain.OperationalStatus.Degraded,
                >= 0.3 => Domain.OperationalStatus.Failing,
                _ => Domain.OperationalStatus.Down
            };
    }
}
