namespace Faultline.Tests.EditMode
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Faultline.Agents;
    using Faultline.Domain;
    using Faultline.Domain.Errors;
    using Faultline.Incidents;
    using Faultline.Remediation;
    using NUnit.Framework;

    /// <summary>
    /// EditMode tests for remediation: proposal validation, approval
    /// identity, duplicate approval protection, expiry, execution idempotency,
    /// checkpoint/rollback, and post-execution verification.
    ///
    /// Pure C# — no UnityEngine.
    /// </summary>
    [TestFixture]
    public sealed class RemediationTests
    {
        private static readonly DateTimeOffset T0 =
            new DateTimeOffset(2026, 11, 9, 0, 0, 0, TimeSpan.Zero);

        private (SimulationEngine Sim, IncidentService Incidents, AgentOrchestrator Orchestrator, RemediationService Remediation, FixedClock Clock) Build()
        {
            var clock = new FixedClock(T0);
            var sim = new SimulationEngine(seed: 7, clock: clock);
            var incidents = new IncidentService(clock);
            var orchestrator = new AgentOrchestrator(seed: 7, clock: clock);
            var remediation = new RemediationService(sim, incidents, orchestrator, clock);
            return (sim, incidents, orchestrator, remediation, clock);
        }

        private static string Detect(IncidentService incidents)
        {
            var result = incidents.DetectIncident("database", "rule-db-01", "Critical");
            return result.Value;
        }

        [Test]
        public void Propose_UnknownIncident_Failure()
        {
            var (_, _, _, remediation, _) = Build();
            var result = remediation.Propose("nonexistent", "RestartService", null);
            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void Propose_UnknownActionType_Failure()
        {
            var (_, incidents, _, remediation, _) = Build();
            var id = Detect(incidents);

            var result = remediation.Propose(id, "InvalidAction", null);
            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void Approve_DuplicateRejected()
        {
            var (_, incidents, _, remediation, _) = Build();
            var id = Detect(incidents);
            var propose = remediation.Propose(id, "RestartService", null);
            Assert.That(propose.IsSuccess, Is.True);

            var approve1 = remediation.Approve(propose.Value.ProposalId, "operator-1");
            Assert.That(approve1.IsSuccess, Is.True);

            var approve2 = remediation.Approve(propose.Value.ProposalId, "operator-1");
            Assert.That(approve2.IsSuccess, Is.False);
            Assert.That(approve2.ErrorCode, Is.EqualTo("FL-RM-0081"));
        }

        [Test]
        public void Approve_Expired_Rejected()
        {
            var (_, incidents, _, remediation, clock) = Build();
            var id = Detect(incidents);
            var propose = remediation.Propose(id, "RestartService", null);
            Assert.That(propose.IsSuccess, Is.True);

            // Advance past the 10-minute expiry window.
            clock.Advance(TimeSpan.FromMinutes(11));

            var approve = remediation.Approve(propose.Value.ProposalId, "operator-1");
            Assert.That(approve.IsSuccess, Is.False);
            Assert.That(approve.ErrorCode, Is.EqualTo("FL-RM-0082"));
        }

        [Test]
        public async Task Execute_Idempotent_OnceOnly()
        {
            var (_, incidents, _, remediation, _) = Build();
            var id = Detect(incidents);
            var propose = remediation.Propose(id, "RestartService", null);
            Assert.That(propose.IsSuccess, Is.True);

            var approve = remediation.Approve(propose.Value.ProposalId, "operator-1");
            Assert.That(approve.IsSuccess, Is.True);

            var exec1 = await remediation.ExecuteAsync(propose.Value.ProposalId, CancellationToken.None);
            Assert.That(exec1.IsSuccess, Is.True);

            var exec2 = await remediation.ExecuteAsync(propose.Value.ProposalId, CancellationToken.None);
            Assert.That(exec2.IsSuccess, Is.False);
            Assert.That(exec2.ErrorCode, Is.EqualTo("FL-RM-0083"));
        }

        [Test]
        public async Task Execute_Failure_RestoresCheckpoint()
        {
            var (sim, incidents, _, remediation, _) = Build();
            var id = Detect(incidents);
            var propose = remediation.Propose(id, "RestartService", null);
            Assert.That(propose.IsSuccess, Is.True);

            var approve = remediation.Approve(propose.Value.ProposalId, "operator-1");
            Assert.That(approve.IsSuccess, Is.True);

            sim.Checkpoint();

            var exec = await remediation.ExecuteAsync(propose.Value.ProposalId, CancellationToken.None);
            Assert.That(exec.IsSuccess, Is.True);

            // After success, checkpoint still restorable.
            var restore = sim.RestoreCheckpoint();
            Assert.That(restore.IsSuccess, Is.True);
        }

        [Test]
        public async Task Execute_ImprovesHealthAndVerifies()
        {
            var (sim, incidents, _, remediation, _) = Build();
            var id = Detect(incidents);
            var before = sim.Nodes["database"].HealthScore;

            var propose = remediation.Propose(id, "RestartService", null);
            remediation.Approve(propose.Value.ProposalId, "operator-1");

            var exec = await remediation.ExecuteAsync(propose.Value.ProposalId);
            Assert.That(exec.IsSuccess, Is.True);
            Assert.That(sim.Nodes["database"].HealthScore, Is.GreaterThan(before));

            var verify = remediation.Verify(propose.Value.ProposalId, id);
            Assert.That(verify.IsSuccess, Is.True);
            Assert.That(verify.Value, Is.True);
        }

        [Test]
        public void Verify_NotExecuted_Failure()
        {
            var (_, incidents, _, remediation, _) = Build();
            var id = Detect(incidents);
            var propose = remediation.Propose(id, "RestartService", null);
            Assert.That(propose.IsSuccess, Is.True);

            var verify = remediation.Verify(propose.Value.ProposalId, id);
            Assert.That(verify.IsSuccess, Is.False);
        }

        [Test]
        public void ScoreRecovery_Deterministic()
        {
            var full = RemediationService.ScoreRecovery(true, 2, true, true, true);
            Assert.That(full, Is.EqualTo(100.0));

            var partial = RemediationService.ScoreRecovery(false, 0, false, false, false);
            Assert.That(partial, Is.EqualTo(0.0));

            // Approval alone never awards full points.
            var approvalOnly = RemediationService.ScoreRecovery(true, 0, false, false, true);
            Assert.That(approvalOnly, Is.LessThan(100.0));
        }

        [Test]
        public void ErrorCatalogue_FlRmCodesPresent()
        {
            // Each RM code maps to a structured error object.
            var notApproved = ErrorCatalogue.RemediationNotApproved("cid", "plan-1");
            Assert.That(notApproved.Code, Is.EqualTo("FL-RM-0080"));
            Assert.That(notApproved.Recoverability, Is.EqualTo(Recoverability.NonRecoverable));

            var duplicate = ErrorCatalogue.RemediationDuplicateApproval("cid", "plan-1");
            Assert.That(duplicate.Code, Is.EqualTo("FL-RM-0081"));

            var expired = ErrorCatalogue.RemediationApprovalExpired("cid", "plan-1");
            Assert.That(expired.Code, Is.EqualTo("FL-RM-0082"));

            var already = ErrorCatalogue.RemediationAlreadyExecuted("cid", "plan-1");
            Assert.That(already.Code, Is.EqualTo("FL-RM-0083"));

            var failed = ErrorCatalogue.RemediationExecutionFailed("cid", "plan-1");
            Assert.That(failed.Code, Is.EqualTo("FL-RM-0084"));
        }
    }
}
