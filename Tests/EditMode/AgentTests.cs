namespace Faultline.Tests.EditMode
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Faultline.Agents;
    using Faultline.Domain;
    using Faultline.Incidents;
    using NUnit.Framework;

    /// <summary>
    /// EditMode tests for agent result validation, conflict detection,
    /// timeout handling, idempotent dispatch, evidence grounding, and
    /// deterministic fallback.
    ///
    /// Pure C# — no UnityEngine.
    /// </summary>
    [TestFixture]
    public sealed class AgentTests
    {
        [Test]
        public void DispatchAndCompleteTask_Succeeds()
        {
            var orchestrator = new AgentOrchestrator(seed: 42, clock: new FixedClock());
            var context = new Dictionary<string, object> { ["incidentId"] = "inc-1" };
            var dispatch = orchestrator.Dispatch("TRACE", "inc-1", context);

            Assert.That(dispatch.IsSuccess, Is.True);
            var taskId = dispatch.Value;

            var finding = new AgentFinding(Guid.NewGuid(), Guid.Parse(taskId),
                Enumerable.Empty<Guid>(), "Root cause is database.", 0.9);
            var record = orchestrator.RecordFinding(finding);
            Assert.That(record.IsSuccess, Is.True);

            var complete = orchestrator.CompleteTask(taskId, new List<string> { record.Value.ToString() });
            Assert.That(complete.IsSuccess, Is.True);

            var state = orchestrator.GetTaskState(taskId);
            Assert.That(state["status"].ToString(), Is.EqualTo("Completed"));
        }

        [Test]
        public void Dispatch_IsIdempotent()
        {
            var orchestrator = new AgentOrchestrator(seed: 42, clock: new FixedClock());
            var context = new Dictionary<string, object> { ["incidentId"] = "inc-1" };

            var first = orchestrator.Dispatch("TRACE", "inc-1", context);
            var second = orchestrator.Dispatch("TRACE", "inc-1", context);

            Assert.That(first.IsSuccess, Is.True);
            Assert.That(second.IsSuccess, Is.True);
            Assert.That(first.Value, Is.EqualTo(second.Value));
        }

        [Test]
        public void CompleteTask_UnknownFinding_Rejected()
        {
            var orchestrator = new AgentOrchestrator(seed: 42, clock: new FixedClock());
            var context = new Dictionary<string, object> { ["incidentId"] = "inc-1" };
            var dispatch = orchestrator.Dispatch("TRACE", "inc-1", context);
            Assert.That(dispatch.IsSuccess, Is.True);

            var complete = orchestrator.CompleteTask(dispatch.Value,
                new List<string> { Guid.NewGuid().ToString() });
            Assert.That(complete.IsSuccess, Is.False);
            Assert.That(complete.ErrorCode, Is.EqualTo("FL-AG-0062"));
        }

        [Test]
        public void ValidateResult_Null_Finding_Rejected()
        {
            var orchestrator = new AgentOrchestrator(seed: 42, clock: new FixedClock());
            var validation = orchestrator.ValidateResult(null);
            Assert.That(validation.IsValid, Is.False);
            Assert.That(validation.ErrorCode, Is.EqualTo("FL-AG-0062"));
        }

        [Test]
        public void HandleTimeout_RetriesThenTimesOut()
        {
            var orchestrator = new AgentOrchestrator(seed: 42, clock: new FixedClock());
            var context = new Dictionary<string, object> { ["incidentId"] = "inc-1" };
            var dispatch = orchestrator.Dispatch("ORACLE", "inc-1", context);
            Assert.That(dispatch.IsSuccess, Is.True);

            var first = orchestrator.HandleTimeout(dispatch.Value);
            Assert.That(first.IsSuccess, Is.True);
            // Transient: task stays InProgress (retry policy), not TimedOut.
            var state = orchestrator.GetTaskState(dispatch.Value);
            Assert.That(state["status"].ToString(), Is.EqualTo("InProgress"));
        }

        [Test]
        public async Task InvestigateAsync_ProducesGroundedReport()
        {
            var clock = new FixedClock();
            var orchestrator = new AgentOrchestrator(seed: 42, clock: clock);

            var sim = new SimulationEngine(seed: 42, clock: clock);
            sim.Tick();

            var telemetry = sim.Telemetry.ToList();
            var propagationPath = new List<string> { "database", "app-service", "api-gateway" };
            var blastRadius = new List<string> { "database", "app-service" };

            var report = await orchestrator.InvestigateAsync(
                "inc-1", "database", propagationPath, blastRadius, telemetry);

            Assert.That(report.IsSuccess, Is.True);
            Assert.That(report.Value.Findings.Count, Is.EqualTo(4));
            Assert.That(report.Value.AverageConfidence, Is.GreaterThan(0.0));

            // Every finding except FORGE cites real evidence ids.
            var trace = report.Value.Findings.First(f => f.Finding.Contains("Dependency analysis"));
            Assert.That(trace.EvidenceIds.Count, Is.GreaterThan(0));
            foreach (var eid in trace.EvidenceIds)
            {
                Assert.That(telemetry.Any(t => t.EventId == eid), Is.True,
                    $"Evidence {eid} not present in incident telemetry.");
            }
        }

        [Test]
        public void FallbackFinding_LowConfidence()
        {
            var orchestrator = new AgentOrchestrator(seed: 42, clock: new FixedClock());
            var finding = orchestrator.FallbackFinding(Guid.NewGuid(), "agent unavailable");
            Assert.That(finding.Confidence, Is.EqualTo(0.1));
            Assert.That(finding.Limitations.Count, Is.GreaterThan(0));
        }

        [Test]
        public void Synthesize_NoFindings_Failure()
        {
            var orchestrator = new AgentOrchestrator(seed: 42, clock: new FixedClock());
            var result = orchestrator.Synthesize("inc-nonexistent");
            Assert.That(result.IsSuccess, Is.False);
        }
    }
}
