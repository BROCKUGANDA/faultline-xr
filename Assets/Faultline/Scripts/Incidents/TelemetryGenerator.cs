namespace Faultline.Incidents
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    using Faultline.Domain;
    using Faultline.Infrastructure;

    /// <summary>
    /// Generates correlated telemetry for a simulation. A database incident
    /// produces realistic downstream symptoms: elevated latency on the app
    /// service and gateway, rising error rates, and falling throughput.
    ///
    /// Deterministic per seed: each event id is derived from (seed, sequence)
    /// and each timestamp from the injected clock, so replays are identical.
    /// </summary>
    public sealed class TelemetryGenerator
    {
        private readonly Random _random;
        private readonly int _seed;
        private readonly IClock _clock;
        private int _seq;

        public TelemetryGenerator(int seed, IClock clock = null)
        {
            _random = new Random(seed);
            _seed = seed;
            _clock = clock ?? SystemClock.Instance;
        }

        /// <summary>
        /// Emit a correlated telemetry batch for a node that has just
        /// transitioned into an incident state. Downstream dependents receive
        /// degraded samples.
        /// </summary>
        public IReadOnlyList<TelemetryEvent> GenerateIncidentTelemetry(string serviceId,
            OperationalStatus status, double healthScore, IEnumerable<string> downstreamServiceIds)
        {
            var now = _clock.Now;
            var events = new List<TelemetryEvent>();

            // Primary (faulted) service symptoms.
            var primaryLatency = LatencyBase(serviceId) * (2.0 + 3.0 * (1.0 - healthScore));
            var primaryError = Math.Clamp(0.2 + 0.5 * (1.0 - healthScore), 0.0, 1.0);
            var primaryThroughput = Math.Max(0, ThroughputBase(serviceId) * healthScore);

            events.Add(new TelemetryEvent(NextId(), now, serviceId,
                TelemetrySeverity.Error, "Incident latency spike.", "latency_ms", primaryLatency));
            events.Add(new TelemetryEvent(NextId(), now, serviceId,
                TelemetrySeverity.Error, "Incident error rate elevated.", "error_rate", primaryError));
            events.Add(new TelemetryEvent(NextId(), now, serviceId,
                TelemetrySeverity.Warning, "Incident throughput degraded.", "throughput_rps", primaryThroughput));

            // Downstream dependents: milder, correlated symptoms.
            foreach (var downstream in downstreamServiceIds ?? Enumerable.Empty<string>())
            {
                var downstreamHealth = Math.Clamp(healthScore + 0.2 * _random.NextDouble(), 0.0, 1.0);
                var dl = LatencyBase(downstream) * (1.5 + (1.0 - downstreamHealth));
                var de = Math.Clamp(0.1 + 0.3 * (1.0 - downstreamHealth), 0.0, 1.0);
                var dt = Math.Max(0, ThroughputBase(downstream) * downstreamHealth);

                events.Add(new TelemetryEvent(NextId(), now, downstream,
                    TelemetrySeverity.Warning, "Downstream latency elevated.", "latency_ms", dl));
                events.Add(new TelemetryEvent(NextId(), now, downstream,
                    TelemetrySeverity.Warning, "Downstream error rate elevated.", "error_rate", de));
                events.Add(new TelemetryEvent(NextId(), now, downstream,
                    TelemetrySeverity.Information, "Downstream throughput degraded.", "throughput_rps", dt));
            }

            return new ReadOnlyCollection<TelemetryEvent>(events);
        }

        public IReadOnlyList<TelemetryEvent> GenerateSteadyState(IEnumerable<string> serviceIds)
        {
            var now = _clock.Now;
            var events = new List<TelemetryEvent>();

            foreach (var serviceId in serviceIds)
            {
                events.Add(new TelemetryEvent(NextId(), now, serviceId,
                    TelemetrySeverity.Information, "Steady-state latency.", "latency_ms",
                    LatencyBase(serviceId) * (1.0 + 0.1 * _random.NextDouble())));
                events.Add(new TelemetryEvent(NextId(), now, serviceId,
                    TelemetrySeverity.Information, "Steady-state error rate.", "error_rate",
                    0.01 + 0.01 * _random.NextDouble()));
                events.Add(new TelemetryEvent(NextId(), now, serviceId,
                    TelemetrySeverity.Information, "Steady-state throughput.", "throughput_rps",
                    ThroughputBase(serviceId) * (0.9 + 0.2 * _random.NextDouble())));
            }

            return new ReadOnlyCollection<TelemetryEvent>(events);
        }

        private Guid NextId() => new DeterministicId(_seed, _seq++);

        /// <summary>Stable Guid derived from (seed, sequence) — FNV-1a, no process randomness.</summary>
        private readonly struct DeterministicId
        {
            private readonly int _seed;
            private readonly int _seq;

            public DeterministicId(int seed, int seq)
            {
                _seed = seed;
                _seq = seq;
            }

            public static implicit operator Guid(DeterministicId id)
            {
                unchecked
                {
                    var bytes = new byte[16];
                    for (int lane = 0; lane < 4; lane++)
                    {
                        uint hash = 2166136261u;
                        foreach (var b in System.Text.Encoding.UTF8.GetBytes($"{id._seed}:{id._seq}:{lane}"))
                        {
                            hash ^= b;
                            hash *= 16777619u;
                        }

                        System.BitConverter.GetBytes(hash).CopyTo(bytes, lane * 4);
                    }

                    return new Guid(bytes);
                }
            }
        }

        private static double LatencyBase(string serviceId) => serviceId switch
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

        private static double ThroughputBase(string serviceId) => serviceId switch
        {
            "api-gateway" => 1200,
            "auth-service" => 900,
            "app-service" => 450,
            "database" => 120,
            "message-broker" => 2000,
            "worker-cluster" => 300,
            "monitoring" => 500,
            _ => 100
        };
    }
}
