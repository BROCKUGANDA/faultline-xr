namespace Faultline.Presentation
{
    using System;

    /// <summary>
    /// Pause menu screen contract (PRD §6.1, screen 22).
    /// Entry conditions: user pauses during an active scenario.
    /// Exit conditions: resume, restart, or exit to welcome.
    /// Interaction model: large hand buttons for Resume / Restart / Settings /
    /// Exit.
    /// Back navigation: resumes the previous screen (no fixed target).
    /// Accessibility: pause announced; options announced; no mandatory voice.
    /// </summary>
    public interface PauseMenuScreen : IScreen
    {
        /// <summary>Whether a scenario is currently active and pausable.</summary>
        bool ScenarioActive { get; }

        event EventHandler ResumeRequested;
        event EventHandler RestartRequested;
        event EventHandler SettingsRequested;
        event EventHandler ExitToWelcomeRequested;
    }
}
