namespace Faultline.Presentation
{
    using System;

    /// <summary>
    /// Approval confirmation screen contract (PRD §6.1, screen 14).
    /// Entry conditions: remediation proposal submitted for approval.
    /// Exit conditions: approved, rejected, or dismissed.
    /// Interaction model: large visible Approve / Reject hand buttons
    /// (controller-free); no hidden gesture for approval.
    /// Back navigation: RemediationProposalScreen.
    /// Accessibility: approve/reject via colour + glyph + text; confirm
    /// dialog before irreversible approval.
    /// </summary>
    public interface ApprovalConfirmationScreen : IScreen
    {
        /// <summary>Plan id awaiting approval.</summary>
        string PlanId { get; }

        /// <summary>Whether approval has already been granted (duplicate guard).</summary>
        bool AlreadyApproved { get; }

        event EventHandler<PlanIdEventArgs> ApprovalOpened;
        event EventHandler ApproveRequested;
        event EventHandler RejectRequested;
    }

    public sealed class PlanIdEventArgs : EventArgs
    {
        public string PlanId { get; }
        public PlanIdEventArgs(string planId)
        {
            PlanId = planId;
        }
    }
}
