namespace Faultline.Presentation
{
    using System;

    /// <summary>
    /// Remediation proposal screen contract (PRD §6.1, screen 13).
    /// Entry conditions: investigation complete; remediation proposed.
    /// Exit conditions: proposal submitted for approval or rejected.
    /// Interaction model: proposal details, actions, risk; hand targets for
    /// SubmitForApproval / CancelProposal.
    /// Back navigation: InvestigationWorkspaceScreen.
    /// Accessibility: risk and impact via colour + glyph + text.
    /// </summary>
    public interface RemediationProposalScreen : IScreen
    {
        /// <summary>Proposal id.</summary>
        string ProposalId { get; }

        /// <summary>Whether the proposal can be submitted for approval.</summary>
        bool CanSubmit { get; }

        event EventHandler<ProposalIdEventArgs> ProposalOpened;
        event EventHandler SubmitForApprovalRequested;
        event EventHandler CancelProposalRequested;
    }

    public sealed class ProposalIdEventArgs : EventArgs
    {
        public string ProposalId { get; }
        public ProposalIdEventArgs(string proposalId)
        {
            ProposalId = proposalId;
        }
    }
}
