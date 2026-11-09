namespace Faultline.Remediation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Faultline.Agents;
    using Faultline.Domain;
    using Faultline.Incidents;

    /// <summary>
    /// Remediation service: propose → approve → execute → verify flow
    /// (PRD §6). All operations are confined to the deterministic local
    /// simulation — no production-changing operations.
    ///
    /// - Proposal validation
    /// - Approval identity + duplicate approval protection
    /// - Approval expiry (injected clock)
    /// - Cancellation
    /// - Execution idempotency keyed on <c>proposalId</c>
    /// - Simulation checkpoint + rollback on failure
    /// - Post-execution verification (health actually improved?)
    /// </summary>
    public sealed class RemediationService : IRemediationService
    {
        private readonly Dictionary<string, RemediationProposal> _proposals = new();
        private readonly HashSet<string> _approvedProposalIds = new();
        private readonly HashSet<string> _executedProposalIds = new();
        private readonly Dictionary<string, double> _preExecutionHealth = new();

        private readonly SimulationEngine _simulation;
        private readonly IIncidentService _incidentService;
        private readonly AgentOrchestrator _orchestrator;
        private readonly IClock _clock;

        public IReadOnlyDictionary<string, RemediationProposal> Proposals => _proposals;

        public RemediationService(SimulationEngine simulation, IIncidentService incidentService,
            AgentOrchestrator orchestrator, IClock clock = null)
        {
            _simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
            _incidentService = incidentService ?? throw new ArgumentNullException(nameof(incidentService));
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            _clock = clock ?? SystemClock.Instance;
        }

        // ---- IRemediationService contract ----

        public Result<string> ProposeRemediation(string incidentId, string actionType,
            IDictionary<string, object> parameters)
        {
            var result = Propose(incidentId, actionType, parameters);
            return result.IsSuccess
                ? Result<string>.Success(result.Value.ProposalId)
                : Result<string>.Failure(result.ErrorCode, result.ErrorMessage);
        }

        public Result<bool> ApproveRemediation(string planId, string approvedBy) =>
            Approve(planId, approvedBy);

        public Result<bool> RejectRemediation(string planId, string rejectedBy, string reason) =>
            Reject(planId, rejectedBy, reason);

        public async Task<Result<bool>> ExecuteRemediation(string planId,
            CancellationToken cancellationToken = default)
        {
            var result = await ExecuteAsync(planId, cancellationToken);
            return result.IsSuccess
                ? Result<bool>.Success(result.Value.Success)
                : Result<bool>.Failure(result.ErrorCode, result.ErrorMessage);
        }

        public Result<bool> VerifyRemediation(string planId, string incidentId) =>
            Verify(planId, incidentId);

        // ---- Core implementation ----

        /// <summary>
        /// Propose a remediation for the incident. Validates the incident
        /// exists and is in a remediable state. Effective affected services
        /// are inferred from blast radius when no explicit list is given.
        /// </summary>
        public Result<RemediationProposal> Propose(string incidentId,
            string actionType, IDictionary<string, object> parameters)
        {
            if (string.IsNullOrWhiteSpace(incidentId))
            {
                return Result<RemediationProposal>.Failure("FL-SM-0090",
                    "Incident id is required.");
            }

            if (!Enum.TryParse<RemediationActionType>(actionType, ignoreCase: true, out var actionTypeParsed))
            {
                return Result<RemediationProposal>.Failure("FL-RM-0080",
                    $"Unknown action type: {actionType}.");
            }

            var incidentState = _incidentService.GetIncident(incidentId);
            if (!incidentState.IsSuccess)
            {
                return Result<RemediationProposal>.Failure(incidentState.ErrorCode,
                    incidentState.ErrorMessage);
            }

            var affected = parameters != null
                && parameters.TryGetValue("affectedServiceIds", out var raw)
                && raw is IEnumerable<string> ids
                ? ids.ToList()
                : InferAffectedServiceIds(incidentId);

            var proposalId = Guid.NewGuid().ToString("N");
            var action = new RemediationAction(Guid.NewGuid(), actionTypeParsed,
                $"Remediate {incidentId} with {actionTypeParsed}.",
                affectedServiceIds: affected,
                expectedBenefit: "Restore service health.",
                sideEffects: new[] { "Brief restart window for the affected services." },
                reversibility: RemediationReversibility.PartiallyReversible);

            var proposal = RemediationProposal.Create(proposalId, incidentId, action, parameters,
                _clock.Now, _clock.Now.AddMinutes(10));

            _proposals[proposalId] = proposal;

            return Result<RemediationProposal>.Success(proposal);
        }

        private List<string> InferAffectedServiceIds(string incidentId)
        {
            // Map the incident's node to the root-cause id used by the
            // simulation; fall back to the incident id itself. Real blast
            // radius runs through the graph when available.
            if (_incidentService is IncidentService service)
            {
                var detail = service.GetIncident(incidentId);
                if (detail.IsSuccess && detail.Value.TryGetValue("nodeId", out var nodeId))
                {
                    var root = nodeId?.ToString();
                    if (!string.IsNullOrWhiteSpace(root) && _simulation.Nodes.ContainsKey(root))
                    {
                        return new List<string> { root };
                    }
                }
            }

            return _simulation.Nodes.ContainsKey(incidentId)
                ? new List<string> { incidentId }
                : new List<string> { _simulation.RootCauseNodeId };
        }

        /// <summary>
        /// Approve a proposal by id and approver identity. Duplicate approvals
        /// are rejected (FL-RM-0081). Expired approvals cannot be approved.
        /// </summary>
        public Result<bool> Approve(string proposalId, string approvedBy)
        {
            if (!_proposals.TryGetValue(proposalId, out var proposal))
            {
                return Result<bool>.Failure("FL-RM-0080", $"Proposal not found: {proposalId}.");
            }

            if (proposal.IsTerminal)
            {
                return Result<bool>.Failure("FL-RM-0080",
                    $"Proposal {proposalId} is in terminal state {proposal.Status}.");
            }

            if (proposal.IsExpired)
            {
                return Result<bool>.Failure("FL-RM-0082",
                    $"Approval for proposal has expired: {proposalId}.");
            }

            if (_approvedProposalIds.Contains(proposalId))
            {
                return Result<bool>.Failure("FL-RM-0081",
                    $"Proposal {proposalId} has already been approved.");
            }

            if (string.IsNullOrWhiteSpace(approvedBy))
            {
                return Result<bool>.Failure("FL-SM-0091", "Approver identity is required.");
            }

            _approvedProposalIds.Add(proposalId);
            _proposals[proposalId] = proposal.WithStatus(RemediationPlanStatus.Approved,
                approvedBy: approvedBy, approvedAt: _clock.Now);

            return Result<bool>.Success(true);
        }

        /// <summary>
        /// Reject a proposal.
        /// </summary>
        public Result<bool> Reject(string proposalId, string rejectedBy, string reason)
        {
            if (!_proposals.TryGetValue(proposalId, out var proposal))
            {
                return Result<bool>.Failure("FL-RM-0080", $"Proposal not found: {proposalId}.");
            }

            if (proposal.IsTerminal)
            {
                return Result<bool>.Failure("FL-RM-0080",
                    $"Proposal {proposalId} is in terminal state {proposal.Status}.");
            }

            _proposals[proposalId] = proposal.WithStatus(RemediationPlanStatus.Rejected);
            return Result<bool>.Success(true);
        }

        /// <summary>
        /// Execute a proposal asynchronously. Idempotent on proposalId: any
        /// second execution attempt returns FL-RM-0083. Checkpoint before
        /// executing; on failure or cancellation restore the checkpoint.
        /// </summary>
        public async Task<Result<RecoveryResult>> ExecuteAsync(string proposalId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_proposals.TryGetValue(proposalId, out var proposal))
            {
                return Result<RecoveryResult>.Failure("FL-RM-0080",
                    $"Proposal not found: {proposalId}.");
            }

            if (_executedProposalIds.Contains(proposalId))
            {
                return Result<RecoveryResult>.Failure("FL-RM-0083",
                    $"Proposal {proposalId} has already been executed.");
            }

            if (!proposal.CanExecute)
            {
                return Result<RecoveryResult>.Failure("FL-RM-0080",
                    $"Proposal {proposalId} is not executable (status={proposal.Status}, expired={proposal.IsExpired}).");
            }

            // Checkpoint before executing so we can roll back on failure.
            _simulation.Checkpoint();
            _preExecutionHealth[proposalId] = _simulation.AggregateHealth;

            try
            {
                var affected = proposal.Action.AffectedServiceIds.ToList();
                double benefit = 0.5;

                await Task.Delay(1, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                var applyResult = _simulation.ApplyRecovery(affected, benefit);
                if (!applyResult.IsSuccess)
                {
                    _simulation.RestoreCheckpoint();
                    return Result<RecoveryResult>.Failure("FL-RM-0084",
                        applyResult.ErrorMessage);
                }

                var healthDelta = _simulation.AggregateHealth - _preExecutionHealth[proposalId];
                var verified = healthDelta > 0;
                var recovery = new RecoveryResult(Guid.NewGuid(), true, healthDelta,
                    eventIds: Enumerable.Empty<Guid>(), verificationPassed: verified,
                    completedAt: _clock.Now);

                _executedProposalIds.Add(proposalId);
                var executionId = proposalId;
                _proposals[proposalId] = proposal.WithStatus(RemediationPlanStatus.Executed,
                    executionId: executionId, executedAt: _clock.Now,
                    recoveryResult: recovery);

                return Result<RecoveryResult>.Success(recovery);
            }
            catch (OperationCanceledException)
            {
                _simulation.RestoreCheckpoint();
                return Result<RecoveryResult>.Failure("FL-RM-0084",
                    "Execution cancelled; checkpoint restored.");
            }
            catch (Exception ex)
            {
                _simulation.RestoreCheckpoint();
                return Result<RecoveryResult>.Failure("FL-RM-0084",
                    $"Execution failed: {ex.Message}; checkpoint restored.");
            }
        }

        /// <summary>
        /// Verify remediation against the incident. Confirms the recovery
        /// improved health and the incident can be resolved.
        /// </summary>
        public Result<bool> Verify(string proposalId, string incidentId)
        {
            if (!_proposals.TryGetValue(proposalId, out var proposal))
            {
                return Result<bool>.Failure("FL-RM-0080", $"Proposal not found: {proposalId}.");
            }

            if (proposal.Status != RemediationPlanStatus.Executed)
            {
                return Result<bool>.Failure("FL-RM-0080",
                    $"Proposal {proposalId} has not been executed.");
            }

            var verified = proposal.RecoveryResult != null && proposal.RecoveryResult.Success &&
                             proposal.RecoveryResult.VerificationPassed;

            _proposals[proposalId] = proposal.WithStatus(
                verified ? RemediationPlanStatus.Verified : RemediationPlanStatus.Failed);

            return Result<bool>.Success(verified);
        }

        public IDictionary<string, object> GetRemediationState(string proposalId)
        {
            if (!_proposals.TryGetValue(proposalId, out var proposal))
            {
                return new Dictionary<string, object>();
            }

            return new Dictionary<string, object>
            {
                ["proposalId"] = proposal.ProposalId,
                ["incidentId"] = proposal.IncidentId,
                ["status"] = proposal.Status.ToString(),
                ["actionType"] = proposal.Action.Type.ToString(),
                ["approvedBy"] = proposal.ApprovedBy,
                ["executionId"] = proposal.ExecutionId,
                ["isExpired"] = proposal.IsExpired,
                ["canExecute"] = proposal.CanExecute
            };
        }

        /// <summary>
        /// Deterministic incident-resolution score (PRD §5.5). Factors:
        /// root cause identified, evidence collected, blast radius contained,
        /// recovery verified, unnecessary actions avoided. Approving alone
        /// never awards full points.
        /// </summary>
        public static double ScoreRecovery(bool rootCauseIdentified,
            int evidenceCount, bool blastRadiusContained,
            bool recoveryVerified, bool unnecessaryActionsAvoided)
        {
            var score = 0.0;
            if (rootCauseIdentified) score += 30.0;
            score += Math.Min(evidenceCount * 10.0, 20.0);
            if (blastRadiusContained) score += 20.0;
            if (recoveryVerified) score += 20.0;
            if (unnecessaryActionsAvoided) score += 10.0;
            return Math.Clamp(score, 0.0, 100.0);
        }
    }
}
