namespace Faultline.Presentation
{
    using System;

    /// <summary>
    /// Scenario selection screen contract (PRD §6.1, screen 5).
    /// Entry conditions: welcome → scenarios selected.
    /// Exit conditions: a scenario selected and confirmed.
    /// Interaction model: hand-selectable scenario cards; large targets.
    /// Back navigation: WelcomeScreen.
    /// Accessibility: scenario names and descriptions announced; no colour-only
    /// status (glyph + text).
    /// </summary>
    public interface ScenarioSelectionScreen : IScreen
    {
        /// <summary>Ids of available scenarios.</summary>
        System.Collections.Generic.IReadOnlyList<string> ScenarioIds { get; }

        /// <summary>Currently selected scenario id, or null.</summary>
        string SelectedScenarioId { get; }

        /// <summary>Whether the selected scenario can be started.</summary>
        bool CanStart { get; }

        event EventHandler<ScenarioSelectedEventArgs> ScenarioSelected;
        event EventHandler StartConfirmed;
    }

    public sealed class ScenarioSelectedEventArgs : EventArgs
    {
        public string ScenarioId { get; }
        public ScenarioSelectedEventArgs(string scenarioId)
        {
            ScenarioId = scenarioId;
        }
    }
}
