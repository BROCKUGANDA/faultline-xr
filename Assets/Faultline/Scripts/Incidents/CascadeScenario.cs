namespace Faultline.Incidents
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    using Faultline.Domain;
    using Faultline.Infrastructure;

    /// <summary>
    /// Definition of the mandatory CASCADE scenario (PRD §4.3 / §5.4).
    /// Seeded randomness guarantees the same seed reproduces the same scenario.
    /// </summary>
    public sealed class CascadeScenario
    {
        private static readonly string[] ScenarioNodeIds =
        {
            "api-gateway",
            "auth-service",
            "app-service",
            "database",
            "message-broker",
            "worker-cluster",
            "monitoring"
        };

        private static readonly (string Source, string Target, Infrastructure.DependencyDirection Direction,
            Infrastructure.DependencyType Type, double LatencyMs)[] ScenarioEdges =
        {
            ("api-gateway", "auth-service", Infrastructure.DependencyDirection.Downstream,
                Infrastructure.DependencyType.ApiCall, 25),
            ("api-gateway", "app-service", Infrastructure.DependencyDirection.Downstream,
                Infrastructure.DependencyType.ApiCall, 35),
            ("app-service", "database", Infrastructure.DependencyDirection.Downstream,
                Infrastructure.DependencyType.Database, 40),
            ("app-service", "message-broker", Infrastructure.DependencyDirection.Downstream,
                Infrastructure.DependencyType.MessageQueue, 15),
            ("message-broker", "worker-cluster", Infrastructure.DependencyDirection.Downstream,
                Infrastructure.DependencyType.MessageQueue, 20),
            ("database", "message-broker", Infrastructure.DependencyDirection.Upstream,
                Infrastructure.DependencyType.Database, 10)
        };

        /// <summary>
        /// Initial health scores per node for the CASCADE scenario.
        /// API Gateway healthy, Authentication healthy, Application Service
        /// degraded, Database failing, Message Broker healthy, Worker Cluster
        /// degraded, Monitoring healthy.
        /// </summary>
        private static readonly Dictionary<string, double> InitialHealthScores = new()
        {
            ["api-gateway"] = 1.0,
            ["auth-service"] = 1.0,
            ["app-service"] = 0.55,
            ["database"] = 0.25,
            ["message-broker"] = 1.0,
            ["worker-cluster"] = 0.60,
            ["monitoring"] = 1.0
        };

        private static readonly Dictionary<string, Domain.OperationalStatus> InitialStatuses = new()
        {
            ["api-gateway"] = Domain.OperationalStatus.Healthy,
            ["auth-service"] = Domain.OperationalStatus.Healthy,
            ["app-service"] = Domain.OperationalStatus.Degraded,
            ["database"] = Domain.OperationalStatus.Failing,
            ["message-broker"] = Domain.OperationalStatus.Healthy,
            ["worker-cluster"] = Domain.OperationalStatus.Degraded,
            ["monitoring"] = Domain.OperationalStatus.Healthy
        };

        /// <summary>
        /// Build the initial CASCADE scenario graph and root-cause node.
        /// Node identity is the stable human-readable id itself ("database",
        /// "api-gateway", ...), so the graph keys, edge endpoints, simulation
        /// dictionary, and tests all agree. Edge identity is derived from the
        /// ordered endpoint pair plus seed — no process-random values.
        /// </summary>
        public static (InfrastructureGraph Graph, string RootCauseNodeId, string ScenarioId) Create(
            int seed)
        {
            var graph = new InfrastructureGraph();

            foreach (var nodeId in ScenarioNodeIds)
            {
                var health = InitialHealthScores[nodeId];
                var status = InitialStatuses[nodeId];
                var node = new ServiceNode(
                    nodeId,
                    nodeId,
                    ServiceTypeFor(nodeId),
                    status,
                    health,
                    TimeSpan.FromMilliseconds(LatencyFor(nodeId)),
                    ErrorRateFor(nodeId),
                    ThroughputFor(nodeId));

                graph.AddNode(node);
            }

            foreach (var edge in ScenarioEdges)
            {
                var depEdge = new DependencyEdge(
                    DeterministicEdgeId(edge.Source, edge.Target, seed),
                    edge.Source,
                    edge.Target,
                    edge.Direction,
                    edge.Type,
                    TimeSpan.FromMilliseconds(edge.LatencyMs));
                graph.AddEdge(depEdge);
            }

            var scenarioId = $"cascade-{seed}";
            var rootCause = "database"; // Database failing is the root cause per §5.4

            return (graph, rootCause, scenarioId);
        }

        /// <summary>
        /// Reset the scenario to its initial state using the same seed —
        /// reproducible across runs.
        /// </summary>
        public static (InfrastructureGraph Graph, string RootCauseNodeId, string ScenarioId) Reset(
            int seed) => Create(seed);

        private static string ServiceTypeFor(string nodeId) => nodeId switch
        {
            "api-gateway" => "Gateway",
            "auth-service" => "Auth",
            "app-service" => "Application",
            "database" => "Database",
            "message-broker" => "MessageBroker",
            "worker-cluster" => "Worker",
            "monitoring" => "Monitoring",
            _ => "Service"
        };

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

        private static double ErrorRateFor(string nodeId) => nodeId switch
        {
            "api-gateway" => 0.01,
            "auth-service" => 0.01,
            "app-service" => 0.12,
            "database" => 0.35,
            "message-broker" => 0.01,
            "worker-cluster" => 0.08,
            "monitoring" => 0.01,
            _ => 0.02
        };

        private static double ThroughputFor(string nodeId) => nodeId switch
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

        private static Guid DeterministicEdgeId(string source, string target, int seed)
        {
            // FNV-1a over "seed:source->target" — stable across processes.
            unchecked
            {
                var bytes = new byte[16];
                for (int lane = 0; lane < 4; lane++)
                {
                    uint hash = 2166136261u;
                    string s = $"{seed}:{source}->{target}:{lane}";
                    foreach (var b in System.Text.Encoding.UTF8.GetBytes(s))
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
}
