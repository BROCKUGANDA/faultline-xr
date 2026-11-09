namespace Faultline.Presentation
{
    using System;

    /// <summary>
    /// Incident detail screen contract (PRD §6.1, screen 9).
    /// Entry conditions: an incident raised for a node.
    /// Exit conditions: incident lifecycle advanced or screen closed.
    /// Interaction model: lifecycle state display, severity, impact; hand
    /// targets for Investigate / Remediate.
    /// Back navigation: ServiceInspectionScreen or InfrastructureOverviewScreen.
    /// Accessibility: severity via colour + glyph + text (never colour-only).
    /// </summary>
    public interface IncidentDetailScreen : IScreen
    {
        /// <summary>Incident id.</summary>
        string IncidentId { get; }

        /// <summary>Current lifecycle state name.</summary>
        string LifecycleState { get; }

        /// <summary>Severity level (user-facing).</summary>
        string Severity { get; }

        event EventHandler<IncidentIdEventArgs> IncidentSelected;
        event EventHandler InvestigateRequested;
        event EventHandler RemediateRequested;
    }

    public sealed class IncidentIdEventArgs : EventArgs
    {
        public string IncidentId { get; }
        public IncidentIdEventArgs(string incidentId)
        {
            IncidentId = incidentId;
        }
    }
}
