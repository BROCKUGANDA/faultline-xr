namespace Faultline.Remediation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    using Faultline.Domain;

    /// <summary>
    /// Status of a remediation plan.
    /// </summary>
    public enum RemediationPlanStatus
    {
        Proposed,
        Approved,
        Rejected,
        Executed,
        Verified,
        Failed,
        RolledBack
    }

    /// <summary>
    /// A remediation proposal for an incident, produced by the Forge agent and
    /// validated before approval.
    /// </summary>
    public sealed class RemediationProposal
    {
        public string ProposalId { get; }
        public string IncidentId { get; }
        public RemediationAction Action { get; }
        public IReadOnlyDictionary<string, object> Parameters { get; }
        public RemediationPlanStatus Status { get; private set; }
        public DateTimeOffset ProposedAt { get; }
        public DateTimeOffset? ApprovedAt { get; private set; }
        public string ApprovedBy { get; private set; }
        public DateTimeOffset? ExpiresAt { get; }
        public string ExecutionId { get; private set; }
        public DateTimeOffset? ExecutedAt { get; private set; }
        public RecoveryResult RecoveryResult { get; private set; }

        private RemediationProposal(string proposalId, string incidentId, RemediationAction action,
            IDictionary<string, object> parameters, DateTimeOffset proposedAt,
            DateTimeOffset? expiresAt, RemediationPlanStatus status,
            DateTimeOffset? approvedAt, string approvedBy, string executionId,
            DateTimeOffset? executedAt, RecoveryResult recoveryResult)
        {
            ProposalId = proposalId;
            IncidentId = incidentId;
            Action = action;
            Parameters = new ReadOnlyDictionary<string, object>(parameters ?? new Dictionary<string, object>());
            Status = status;
            ProposedAt = proposedAt;
            ExpiresAt = expiresAt;
            ApprovedAt = approvedAt;
            ApprovedBy = approvedBy;
            ExecutionId = executionId;
            ExecutedAt = executedAt;
            RecoveryResult = recoveryResult;
        }

        public static RemediationProposal Create(string proposalId, string incidentId,
            RemediationAction action, IDictionary<string, object> parameters,
            DateTimeOffset proposedAt, DateTimeOffset? expiresAt = null) =>
            new RemediationProposal(proposalId, incidentId, action, parameters, proposedAt,
                expiresAt, RemediationPlanStatus.Proposed, null, null, null, null, null);

        public RemediationProposal WithStatus(RemediationPlanStatus status, string approvedBy = null,
            DateTimeOffset? approvedAt = null, string executionId = null,
            DateTimeOffset? executedAt = null, RecoveryResult recoveryResult = null) =>
            new RemediationProposal(ProposalId, IncidentId, Action,
                Parameters.ToDictionary(kvp => kvp.Key, kvp => kvp.Value), ProposedAt, ExpiresAt,
                status, approvedAt, approvedBy, executionId, executedAt, recoveryResult);

        public bool IsExpired => ExpiresAt.HasValue && DateTimeOffset.UtcNow > ExpiresAt.Value;

        public bool CanExecute => Status == RemediationPlanStatus.Approved && !IsExpired &&
            string.IsNullOrEmpty(ExecutionId);

        public bool IsTerminal => Status == RemediationPlanStatus.Executed ||
            Status == RemediationPlanStatus.Verified ||
            Status == RemediationPlanStatus.Failed ||
            Status == RemediationPlanStatus.RolledBack ||
            Status == RemediationPlanStatus.Rejected;
    }
}
