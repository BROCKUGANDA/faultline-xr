namespace Faultline.Presentation
{
    using System;

    /// <summary>
    /// Results screen contract (PRD §6.1, screen 17).
    /// Entry conditions: verification complete.
    /// Exit conditions: user returns to overview or starts new scenario.
    /// Interaction model: outcome summary, metrics, hand targets for
    /// NewScenario / ReturnToOverview.
    /// Back navigation: VerificationScreen.
    /// Accessibility: outcome via colour + glyph + text; metrics announced.
    /// </summary>
    public interface ResultsScreen : IScreen
    {
        /// <summary>Outcome summary text.</summary>
        string OutcomeSummary { get; }

        /// <summary>Whether the scenario ended successfully.</summary>
        bool Success { get; }

        event EventHandler ResultsViewed;
        event EventHandler NewScenarioRequested;
        event EventHandler ReturnToOverviewRequested;
    }
}
