namespace Faultline.App
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Faultline.Agents;
    using Faultline.Domain;
    using Faultline.Incidents;
    using Faultline.Infrastructure;
    using Faultline.Persistence;
    using Faultline.Remediation;

    /// <summary>
    /// Presentation-facing phase of the investigation journey. Mirrors the
    /// proven WebXR flow: detect → investigate → findings → propose → approve →
    /// execute → verify → resolve. The domain incident lifecycle is driven in
    /// lockstep through validated transitions; this enum is the UI projection.
    /// </summary>
    public enum InvestigationPhase
    {
        Detected,
        Investigating,
        Findings,
        Proposed,
        Approved,
        Executed,
        Verified,
        Resolved,
        Rejected
    }

    /// <summary>
    /// Application controller for the single CASCADE incident journey. Composes
    /// the deterministic domain services and exposes validated commands to the
    /// Unity presentation layer. All domain writes pass through the owning
    /// services; the controller only sequences transitions and never mutates
    /// entity state directly. Deterministic for a given seed.
    /// </summary>
    public sealed class InvestigationController
    {
        private readonly IClock _clock;
        private readonly int _seed;

        public FaultlineApp App { get; private set; }
        public AgentOrchestrator Orchestrator { get; private set; }
        public RemediationService Remediation { get; private set; }
        public PersistenceService Persistence { get; }

        public InvestigationPhase Phase { get; private set; } = InvestigationPhase.Detected;
        public string IncidentId { get; private set; }
        public string ProposalId { get; private set; }
        public InvestigationReport Report { get; private set; }
        public RecoveryResult Recovery { get; private set; }
        public string LastError { get; private set; }

        public SimulationEngine Engine => App?.Engine;
        public string RootCauseNodeId => App?.Scenarios.RootCauseNodeId;
        public bool IsScenarioRunning => App != null && App.Scenarios.IsRunning;

        /// <summary>Raised after any command changes phase or domain state.</summary>
        public event EventHandler Changed;

        public InvestigationController(IClock clock = null, int seed = 42, string saveFilePath = null)
        {
            _clock = clock ?? SystemClock.Instance;
            _seed = seed;
            Persistence = new PersistenceService(saveFilePath ?? "faultline-session.json");
        }

        /// <summary>Start (or restart) the deterministic CASCADE scenario and raise its incident.</summary>
        public Result<bool> BeginScenario()
        {
            App = new FaultlineApp(_clock, _seed);
            var start = App.StartScenario($"cascade-{_seed}");
            if (!start.IsSuccess) return Fail(start);

            Orchestrator = new AgentOrchestrator(_seed, _clock);
            Remediation = new RemediationService(App.Engine, App.Incidents, Orchestrator, _clock);

            var root = App.Scenarios.RootCauseNodeId;
            var detect = App.Incidents.DetectIncident(root, "SLO-LATENCY-P99", "critical");
            if (!detect.IsSuccess) return Fail(detect);
            IncidentId = detect.Value;

            ProposalId = null;
            Report = null;
            Recovery = null;
            LastError = null;
            Phase = InvestigationPhase.Detected;
            RaiseChanged();
            return Result<bool>.Success(true);
        }

        /// <summary>Open the incident for investigation (Detected → Triaged → Investigating).</summary>
        public Result<bool> OpenIncident()
        {
            if (IncidentId == null) return Fail(Result<bool>.Failure("FL-SM-0090", "No active incident."));
            var triage = Transition("Triaged");
            if (!triage.IsSuccess) return triage;
            var investigate = Transition("Investigating");
            if (!investigate.IsSuccess) return investigate;
            Phase = InvestigationPhase.Investigating;
            RaiseChanged();
            return Result<bool>.Success(true);
        }

        /// <summary>
        /// Run the deterministic specialist investigation (ORACLE, TRACE, SENTRY,
        /// FORGE) grounded in the scenario's causal impact analysis and telemetry.
        /// </summary>
        public async Task<Result<bool>> Investigate(CancellationToken cancellationToken = default)
        {
            if (Phase != InvestigationPhase.Investigating)
                return Fail(Result<bool>.Failure("FL-SM-0091", "Incident is not open for investigation."));

            var impact = App.Engine.Graph.AnalyzeImpact(RootCauseNodeId);
            if (!impact.IsSuccess) return Fail(impact);

            var report = await Orchestrator.InvestigateAsync(
                IncidentId,
                RootCauseNodeId,
                impact.Value.PropagationPath,
                impact.Value.BlastRadius,
                App.Engine.Telemetry,
                cancellationToken);

            if (!report.IsSuccess) return Fail(report);
            Report = report.Value;
            Phase = InvestigationPhase.Findings;
            RaiseChanged();
            return Result<bool>.Success(true);
        }

        /// <summary>Propose the validated RestartService remediation (Investigating → AwaitingApproval).</summary>
        public Result<bool> ProposeRemediation()
        {
            if (Phase != InvestigationPhase.Findings)
                return Fail(Result<bool>.Failure("FL-SM-0091", "Review findings before proposing remediation."));

            var transition = Transition("AwaitingApproval");
            if (!transition.IsSuccess) return transition;

            var proposal = Remediation.Propose(IncidentId, "RestartService", null);
            if (!proposal.IsSuccess) return Fail(proposal);
            ProposalId = proposal.Value.ProposalId;
            Phase = InvestigationPhase.Proposed;
            RaiseChanged();
            return Result<bool>.Success(true);
        }

        /// <summary>Explicitly approve the proposal (AwaitingApproval → Remediating).</summary>
        public Result<bool> ApproveRemediation()
        {
            if (Phase != InvestigationPhase.Proposed || ProposalId == null)
                return Fail(Result<bool>.Failure("FL-RM-0080", "No proposal awaiting approval."));

            var approve = Remediation.Approve(ProposalId, "operator");
            if (!approve.IsSuccess) return Fail(approve);
            var transition = Transition("Remediating");
            if (!transition.IsSuccess) return transition;
            Phase = InvestigationPhase.Approved;
            RaiseChanged();
            return Result<bool>.Success(true);
        }

        /// <summary>Reject the proposal and close the incident as rejected.</summary>
        public Result<bool> RejectRemediation(string reason)
        {
            if (ProposalId == null)
                return Fail(Result<bool>.Failure("FL-RM-0080", "No proposal to reject."));

            var reject = Remediation.Reject(ProposalId, "operator", reason ?? "Operator rejected proposal.");
            if (!reject.IsSuccess) return Fail(reject);
            var transition = Transition("Rejected");
            if (!transition.IsSuccess) return transition;
            Phase = InvestigationPhase.Rejected;
            RaiseChanged();
            return Result<bool>.Success(true);
        }

        /// <summary>Execute the approved remediation against the simulation (Remediating → Verifying).</summary>
        public async Task<Result<bool>> ExecuteRemediation(CancellationToken cancellationToken = default)
        {
            if (Phase != InvestigationPhase.Approved || ProposalId == null)
                return Fail(Result<bool>.Failure("FL-RM-0080", "Remediation is not approved for execution."));

            var execution = await Remediation.ExecuteAsync(ProposalId, cancellationToken);
            if (!execution.IsSuccess) return Fail(execution);
            Recovery = execution.Value;
            var transition = Transition("Verifying");
            if (!transition.IsSuccess) return transition;
            Phase = InvestigationPhase.Executed;
            RaiseChanged();
            return Result<bool>.Success(true);
        }

        /// <summary>Verify recovery from actual simulated metrics (Verifying → Resolved).</summary>
        public Result<bool> VerifyRecovery()
        {
            if (Phase != InvestigationPhase.Executed || ProposalId == null)
                return Fail(Result<bool>.Failure("FL-RM-0080", "Remediation has not been executed."));

            var verify = Remediation.Verify(ProposalId, IncidentId);
            if (!verify.IsSuccess) return Fail(verify);
            var transition = Transition("Resolved");
            if (!transition.IsSuccess) return transition;
            Phase = InvestigationPhase.Verified;
            RaiseChanged();
            return Result<bool>.Success(true);
        }

        /// <summary>Close the journey and open the after-action report.</summary>
        public Result<bool> ResolveIncident()
        {
            if (Phase != InvestigationPhase.Verified)
                return Fail(Result<bool>.Failure("FL-SM-0091", "Recovery must be verified before resolving."));
            Phase = InvestigationPhase.Resolved;
            RaiseChanged();
            return Result<bool>.Success(true);
        }

        /// <summary>Deterministic replay: reset all domain state and begin again.</summary>
        public Result<bool> Replay() => BeginScenario();

        private Result<bool> Transition(string toState)
        {
            var result = App.Incidents.TransitionIncident(IncidentId, toState);
            if (!result.IsSuccess) LastError = $"{result.ErrorCode}: {result.ErrorMessage}";
            return result;
        }

        private Result<bool> Fail<T>(Result<T> result)
        {
            LastError = $"{result.ErrorCode}: {result.ErrorMessage}";
            RaiseChanged();
            return Result<bool>.Failure(result.ErrorCode, result.ErrorMessage);
        }

        private void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);
    }
}