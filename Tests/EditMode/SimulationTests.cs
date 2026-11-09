namespace Faultline.Tests.EditMode
{
    using System.Linq;

    using Faultline.Domain;
    using Faultline.Infrastructure;
    using Faultline.Incidents;
    using NUnit.Framework;

    /// <summary>
    /// EditMode tests for the deterministic simulation engine:
    /// reproducible scenario, dependency propagation, telemetry generation,
    /// checkpoint/rollback.
    ///
    /// Pure C# — no UnityEngine.
    /// </summary>
    [TestFixture]
    public sealed class SimulationTests
    {
        [Test]
        public void SameSeed_ReproducesSameScenario()
        {
            var (g1, root1, id1) = CascadeScenario.Create(42);
            var (g2, root2, id2) = CascadeScenario.Create(42);

            Assert.That(id1, Is.EqualTo(id2));
            Assert.That(root1, Is.EqualTo(root2));
            Assert.That(root1, Is.EqualTo("database"));

            var n1 = g1.Nodes.First(n => n.NodeId == "database");
            var n2 = g2.Nodes.First(n => n.NodeId == "database");
            Assert.That(n1.HealthScore, Is.EqualTo(n2.HealthScore));
            Assert.That(n1.Status, Is.EqualTo(n2.Status));
            Assert.That(g1.Edges.Count, Is.EqualTo(g2.Edges.Count));
        }

        [Test]
        public void Engine_Tick_PropagatesDependencyHealth()
        {
            var engine = new SimulationEngine(seed: 42, clock: new FixedClock());
            var before = engine.AggregateHealth;
            engine.Tick();
            var after = engine.AggregateHealth;

            // Root cause (database) drifts down; dependents cascade.
            Assert.That(after, Is.LessThanOrEqualTo(before));
            Assert.That(engine.Tick, Is.EqualTo(1));
        }

        [Test]
        public void Engine_Tick_DeterministicSameSeed()
        {
            var e1 = new SimulationEngine(seed: 99, clock: new FixedClock());
            var e2 = new SimulationEngine(seed: 99, clock: new FixedClock());

            e1.Tick();
            e2.Tick();

            Assert.That(e1.AggregateHealth, Is.EqualTo(e2.AggregateHealth));
            Assert.That(e1.Nodes["database"].HealthScore,
                Is.EqualTo(e2.Nodes["database"].HealthScore));
            Assert.That(e1.Telemetry.Count, Is.EqualTo(e2.Telemetry.Count));
            for (int i = 0; i < e1.Telemetry.Count; i++)
            {
                Assert.That(e1.Telemetry[i].EventId, Is.EqualTo(e2.Telemetry[i].EventId));
                Assert.That(e1.Telemetry[i].MetricValue,
                    Is.EqualTo(e2.Telemetry[i].MetricValue));
            }
        }

        [Test]
        public void Engine_Telemetry_CorrelatedWithHealth()
        {
            var engine = new SimulationEngine(seed: 42, clock: new FixedClock());
            engine.Tick();

            Assert.That(engine.Telemetry.Count, Is.GreaterThan(0));
            // Each node should have latency + error rate + throughput samples.
            var dbSamples = engine.Telemetry.Where(t => t.ServiceId == "database").ToList();
            Assert.That(dbSamples.Count, Is.GreaterThanOrEqualTo(3));
        }

        [Test]
        public void CheckpointAndRestore_Rollback()
        {
            var engine = new SimulationEngine(seed: 42, clock: new FixedClock());
            engine.Tick();
            var healthBefore = engine.Nodes["database"].HealthScore;

            engine.Checkpoint();
            engine.Tick();

            var restore = engine.RestoreCheckpoint();
            Assert.That(restore.IsSuccess, Is.True);
            Assert.That(engine.Tick, Is.EqualTo(1));
            Assert.That(engine.Nodes["database"].HealthScore, Is.EqualTo(healthBefore));
        }

        [Test]
        public void ApplyRecovery_ImprovesHealth()
        {
            var engine = new SimulationEngine(seed: 42, clock: new FixedClock());
            var before = engine.Nodes["database"].HealthScore;

            var result = engine.ApplyRecovery(new[] { "database" }, 0.4);
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(engine.Nodes["database"].HealthScore, Is.GreaterThan(before));
        }

        [Test]
        public void GenerateIncidentTelemetry_DownstreamSymptoms()
        {
            var generator = new TelemetryGenerator(seed: 42, clock: new FixedClock());
            var events = generator.GenerateIncidentTelemetry("database",
                OperationalStatus.Failing, 0.25,
                new[] { "app-service", "message-broker" });

            Assert.That(events.Count, Is.GreaterThan(0));
            var primary = events.Where(e => e.ServiceId == "database").ToList();
            Assert.That(primary.Count, Is.GreaterThanOrEqualTo(1));

            var downstream = events.Where(e => e.ServiceId == "app-service").ToList();
            Assert.That(downstream.Count, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void MissingNode_DisconnectedHandling()
        {
            var graph = new InfrastructureGraph();
            graph.AddNode(new ServiceNode("A", "A", "Service", OperationalStatus.Healthy));

            var result = graph.AnalyzeImpact("nonexistent");
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("FL-GR-0043"));
        }
    }
}
