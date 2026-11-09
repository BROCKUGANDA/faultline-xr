namespace Faultline.Presentation
{
    using System;

    /// <summary>
    /// Onboarding screen contract (PRD §6.1, screen 3).
    /// Entry conditions: welcome screen → onboarding selected.
    /// Exit conditions: all onboarding steps completed or skipped.
    /// Interaction model: step-through with Next/Back hand buttons.
    /// Back navigation: WelcomeScreen.
    /// Accessibility: each step announced; skip option available.
    /// </summary>
    public interface OnboardingScreen : IScreen
    {
        /// <summary>Current onboarding step index (0-based).</summary>
        int CurrentStep { get; }

        /// <summary>Total number of onboarding steps.</summary>
        int TotalSteps { get; }

        /// <summary>Whether onboarding may be skipped.</summary>
        bool Skippable { get; }

        event EventHandler<OnboardingStepChangedEventArgs> StepChanged;
        event EventHandler OnboardingCompleted;
        event EventHandler OnboardingSkipped;
    }

    public sealed class OnboardingStepChangedEventArgs : EventArgs
    {
        public int PreviousStep { get; }
        public int NewStep { get; }
        public OnboardingStepChangedEventArgs(int previousStep, int newStep)
        {
            PreviousStep = previousStep;
            NewStep = newStep;
        }
    }
}
