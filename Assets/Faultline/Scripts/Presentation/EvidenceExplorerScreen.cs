namespace Faultline.Presentation
{
    using System;

    /// <summary>
    /// Evidence explorer screen contract (PRD §6.1, screen 12).
    /// Entry conditions: investigation workspace → evidence selected.
    /// Exit conditions: evidence inspected or explorer closed.
    /// Interaction model: list of evidence items; hand select to inspect.
    /// Back navigation: InvestigationWorkspaceScreen.
    /// Accessibility: evidence timestamps and types announced; non-colour-only
    /// status.
    /// </summary>
    public interface EvidenceExplorerScreen : IScreen
    {
        /// <summary>Incident whose evidence is being explored.</summary>
        string IncidentId { get; }

        /// <summary>Id of the currently selected evidence item, or null.</summary>
        string SelectedEvidenceId { get; }

        event EventHandler<EvidenceSelectedEventArgs> EvidenceSelected;
        event EventHandler InspectEvidenceRequested;
    }

    public sealed class EvidenceSelectedEventArgs : EventArgs
    {
        public string EvidenceId { get; }
        public EvidenceSelectedEventArgs(string evidenceId)
        {
            EvidenceId = evidenceId;
        }
    }
}
