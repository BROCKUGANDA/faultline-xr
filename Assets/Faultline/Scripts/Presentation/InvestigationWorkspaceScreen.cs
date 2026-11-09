namespace Faultline.Presentation
{
    using System;

    /// <summary>
    /// Investigation workspace screen contract (PRD §6.1, screen 11).
    /// Entry conditions: incident in Investigating state.
    /// Exit conditions: investigation complete (proposal created or abandoned).
    /// Interaction model: evidence timeline, notes, hypothesis; hand targets
    /// for AddEvidence, InspectEvidence, ProposeRemediation.
    /// Back navigation: IncidentDetailScreen.
    /// Accessibility: timeline events announced; text entries readable at
    /// distance; reduced-motion respected.
    /// </summary>
    public interface InvestigationWorkspaceScreen : IScreen
    {
        /// <summary>Incident under investigation.</summary>
        string IncidentId { get; }

        /// <summary>Whether a remediation proposal exists for this incident.</summary>
        bool ProposalExists { get; }

        event EventHandler<IncidentIdEventArgs> InvestigationOpened;
        event EventHandler ProposeRemediationRequested;
        event EventHandler EvidenceAdded;
    }
}
