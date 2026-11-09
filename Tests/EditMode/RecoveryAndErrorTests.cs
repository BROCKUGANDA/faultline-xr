namespace Faultline.Tests.EditMode
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Faultline.Domain;
    using Faultline.Domain.Errors;
    using Faultline.Incidents;
    using NUnit.Framework;

    /// <summary>
    /// EditMode tests for recovery scoring and error mapping.
    ///
    /// Pure C# — no UnityEngine.
    /// </summary>
    [TestFixture]
    public sealed class RecoveryAndErrorTests
    {
        [Test]
        public void RecoveryResult_ClampsHealthDelta()
        {
            var result = new RecoveryResult(Guid.NewGuid(), true, 1.5,
                verificationPassed: true);
            Assert.That(result.HealthDelta, Is.EqualTo(1.0));

            var negative = new RecoveryResult(Guid.NewGuid(), true, -1.5,
                verificationPassed: false);
            Assert.That(negative.HealthDelta, Is.EqualTo(-1.0));
            Assert.That(negative.VerificationPassed, Is.False);
        }

        [Test]
        public void ErrorCatalogue_AllDomainsHaveCodes()
        {
            var codes = new[]
            {
                "FL-RT-0001", "FL-XR-0010", "FL-XR-0011", "FL-IN-0020", "FL-IN-0021",
                "FL-UI-0030", "FL-GR-0040", "FL-GR-0041", "FL-GR-0042", "FL-GR-0043",
                "FL-IC-0050", "FL-IC-0051", "FL-IC-0052", "FL-AG-0060", "FL-AG-0061",
                "FL-AG-0062", "FL-EV-0070", "FL-RM-0080", "FL-RM-0081", "FL-RM-0082",
                "FL-RM-0083", "FL-RM-0084", "FL-SM-0090", "FL-SM-0091", "FL-PS-0100",
                "FL-PS-0101", "FL-IT-0110", "FL-SC-0120", "FL-SC-0121"
            };

            foreach (var code in codes)
            {
                Assert.That(code, Is.Not.Null.And.Not.Empty);
                Assert.That(code.StartsWith("FL-"), Is.True);
            }
        }

        [Test]
        public void ErrorStructuredFields_Populated()
        {
            var err = ErrorCatalogue.GraphCycleDetected("cid-123",
                new[] { "A", "B", "C" },
                new Dictionary<string, object> { ["nodeCount"] = 3 });
            Assert.That(err.Code, Is.EqualTo("FL-GR-0040"));
            Assert.That(err.CorrelationId, Is.EqualTo("cid-123"));
            Assert.That(err.DiagnosticContext["nodeCount"] is int, Is.True);
            Assert.That(err.Timestamp, Is.Not.EqualTo(default(DateTimeOffset)));
        }

        [Test]
        public void Simulation_DeterministicSameSeed()
        {
            var sim1 = new SimulationEngine(seed: 99, clock: new FixedClock());
            var sim2 = new SimulationEngine(seed: 99, clock: new FixedClock());

            sim1.Tick();
            sim2.Tick();

            Assert.That(sim1.AggregateHealth, Is.EqualTo(sim2.AggregateHealth));
            Assert.That(sim1.Telemetry.Count, Is.EqualTo(sim2.Telemetry.Count));
        }

        [Test]
        public void CascadeScenario_InitialStateMatchesPrd()
        {
            var (graph, rootCause, scenarioId) = CascadeScenario.Create(1);
            Assert.That(rootCause, Is.EqualTo("database"));
            Assert.That(graph.Nodes.Count, Is.EqualTo(7));
            Assert.That(graph.Edges.Count, Is.GreaterThan(0));

            var db = graph.Nodes.FirstOrDefault(n => n.NodeId == "database");
            Assert.That(db, Is.Not.Null);
            Assert.That(db.Status, Is.EqualTo(OperationalStatus.Failing));

            var app = graph.Nodes.FirstOrDefault(n => n.NodeId == "app-service");
            Assert.That(app, Is.Not.Null);
            Assert.That(app.Status, Is.EqualTo(OperationalStatus.Degraded));
        }
    }
}
