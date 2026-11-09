namespace Faultline.Tests.EditMode
{
    using System;
    using System.IO;
    using System.Text.Json;

    using Faultline.Domain;
    using Faultline.Persistence;
    using Faultline.Remediation;
    using NUnit.Framework;

    /// <summary>
    /// EditMode tests for serialization / persistence round-trips.
    /// Domain entities use DTOs for persistence — direct round-trip of
    /// immutable entities is not required; this suite exercises the real
    /// <see cref="PersistenceService"/> save/load fidelity path.
    ///
    /// Pure C# — no UnityEngine.
    /// </summary>
    [TestFixture]
    public sealed class SerializationTests
    {
        [Test]
        public void ServiceNode_Serializes()
        {
            var node = new ServiceNode("api-gateway", "api-gateway", "Gateway",
                OperationalStatus.Healthy, 1.0, TimeSpan.FromMilliseconds(12),
                0.01, 1200, new[] { "auth-service" }, new[] { "evt-1" }, new[] { "alert-1" });

            var json = JsonSerializer.Serialize(node, new JsonSerializerOptions { WriteIndented = true });
            Assert.That(json, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void Incident_Serializes()
        {
            var incident = Incident.Create(Guid.NewGuid(), "scenario-1", "database", "Critical",
                "rule-db", DateTimeOffset.UtcNow);
            var json = JsonSerializer.Serialize(incident);
            Assert.That(json, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void TelemetryEvent_Serializes()
        {
            var ev = new TelemetryEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, "database",
                TelemetrySeverity.Error, "Latency spike.", "latency_ms", 420.0);
            var json = JsonSerializer.Serialize(ev);
            Assert.That(json, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void RecoveryResult_Serializes()
        {
            var result = new RecoveryResult(Guid.NewGuid(), true, 0.3,
                verificationPassed: true);
            var json = JsonSerializer.Serialize(result);
            Assert.That(json, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void RemediationProposal_Serializes()
        {
            var action = new RemediationAction(Guid.NewGuid(), RemediationActionType.RestartService,
                "Restart database.", new[] { "database" }, "Restore health.");
            var proposal = RemediationProposal.Create("prop-1", "inc-1", action, null,
                DateTimeOffset.UtcNow);
            var json = JsonSerializer.Serialize(proposal);
            Assert.That(json, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void Persistence_RoundTrip_PreservesSession()
        {
            var path = Path.Combine(Path.GetTempPath(),
                $"faultline-test-{Guid.NewGuid():N}.json");
            try
            {
                var service = new PersistenceService(path);
                var state = SessionState.Empty();
                state.SessionId = "session-fixed";
                state.ScenarioId = "cascade-42";
                state.IncidentId = "inc-1";
                state.IncidentState = "Investigating";
                state.IncidentNodeId = "database";
                state.ProgressPct = 55.0;
                state.ProgressStatus = "InProgress";

                var save = service.Save(state);
                Assert.That(save.IsSuccess, Is.True, save.ErrorMessage);
                Assert.That(service.HasSaveData(), Is.True);

                var load = service.Load();
                Assert.That(load.IsSuccess, Is.True, load.ErrorMessage);
                Assert.That(load.Value.SessionId, Is.EqualTo("session-fixed"));
                Assert.That(load.Value.ScenarioId, Is.EqualTo("cascade-42"));
                Assert.That(load.Value.IncidentId, Is.EqualTo("inc-1"));
                Assert.That(load.Value.IncidentState, Is.EqualTo("Investigating"));
                Assert.That(load.Value.IncidentNodeId, Is.EqualTo("database"));
                Assert.That(load.Value.ProgressPct, Is.EqualTo(55.0));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
                var tmp = path + ".tmp";
                if (File.Exists(tmp)) File.Delete(tmp);
            }
        }

        [Test]
        public void Persistence_Load_MissingFile_ReturnsFailure()
        {
            var path = Path.Combine(Path.GetTempPath(),
                $"faultline-test-{Guid.NewGuid():N}.json");
            var service = new PersistenceService(path);

            var load = service.Load();
            Assert.That(load.IsSuccess, Is.False);
            Assert.That(load.ErrorCode, Is.EqualTo("FL-PS-0101"));
            Assert.That(service.HasSaveData(), Is.False);
        }

        [Test]
        public void Persistence_Load_CorruptedFile_DegradesGracefully()
        {
            var path = Path.Combine(Path.GetTempPath(),
                $"faultline-test-{Guid.NewGuid():N}.json");
            try
            {
                File.WriteAllText(path, "{ not valid json ]");
                var service = new PersistenceService(path);

                var load = service.Load();
                Assert.That(load.IsSuccess, Is.False);
                Assert.That(load.ErrorCode, Is.EqualTo("FL-PS-0101"));
                Assert.That(service.HasSaveData(), Is.False);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }
}
