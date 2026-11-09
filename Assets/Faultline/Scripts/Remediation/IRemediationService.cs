namespace Faultline.Domain
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Remediation propose / approve / execute / verify contract (PRD §3.3).
    /// Implemented in Remediation.
    /// </summary>
    public interface IRemediationService
    {
        Result<string> ProposeRemediation(string incidentId, string actionType, IDictionary<string, object> parameters);
        Result<bool> ApproveRemediation(string planId, string approvedBy);
        Result<bool> RejectRemediation(string planId, string rejectedBy, string reason);
        Task<Result<bool>> ExecuteRemediation(string planId, CancellationToken cancellationToken = default);
        Result<bool> VerifyRemediation(string planId, string incidentId);
        IDictionary<string, object> GetRemediationState(string planId);
    }
}
