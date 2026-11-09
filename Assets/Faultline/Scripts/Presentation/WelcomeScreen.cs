namespace Faultline.Presentation
{
    using System;

    /// <summary>
    /// Welcome screen contract (PRD §6.1, screen 2).
    /// Entry conditions: boot complete.
    /// Exit conditions: user selects onboarding or scenario selection.
    /// Interaction model: large hand-interactable buttons for Onboarding and
    /// Scenarios (controller-free requirement).
    /// Back navigation: none.
    /// Accessibility: large 4.5 cm+ gesture targets, high-contrast labels.
    /// </summary>
    public interface WelcomeScreen : IScreen
    {
        /// <summary>Whether onboarding has been completed previously.</summary>
        bool OnboardingCompleted { get; }

        event EventHandler OnboardingRequested;
        event EventHandler ScenarioSelectionRequested;
    }
}
