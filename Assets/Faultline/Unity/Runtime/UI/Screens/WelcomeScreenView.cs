namespace Faultline.Runtime.UI.Screens
{
    using System;

    using UnityEngine;

    using Faultline.Presentation;
    using Faultline.Runtime.UI;

    /// <summary>
    /// PRD §6.1 screen 2 — Welcome. One screen of value explanation with two
    /// real routes (learn the gestures, or start the incident) plus settings.
    /// </summary>
    public sealed class WelcomeScreenView : ScreenViewBase, WelcomeScreen
    {
        public override string ScreenId => ScreenIds.Welcome;
        public override string Title => "Welcome";
        public override string BackScreenId => ScreenIds.Boot;
        public override string InteractionModel =>
            "Pinch Learn gestures, Start CASCADE, or Settings.";

        public bool OnboardingCompleted { get; private set; }

        public event EventHandler OnboardingRequested;
        public event EventHandler ScenarioSelectionRequested;

        protected override FlPanel BuildPanel() =>
            CreatePanelInZone("FAULTLINE XR", SpatialZone.Centre, 760f, 460f);

        protected override void BuildContent(FlPanel panel)
        {
            panel.AddButton("welcome-tutorial", "Learn gestures",
                "Open the hand gesture tutorial", () =>
                {
                    OnboardingRequested?.Invoke(this, EventArgs.Empty);
                    App.Navigator.NavigateToScreen(ScreenIds.HandTutorial);
                });

            panel.AddButton("welcome-start", "Start CASCADE",
                "Open scenario selection and begin the incident", () =>
                {
                    ScenarioSelectionRequested?.Invoke(this, EventArgs.Empty);
                    App.Navigator.NavigateToScreen(ScreenIds.ScenarioSelection);
                });

            panel.AddButton("welcome-settings", "Settings",
                "Open accessibility and comfort settings",
                () => App.Navigator.NavigateToScreen(ScreenIds.Settings));

            App.RegisterCriticalAction("welcome-settings", "Welcome.Settings");

            panel.SetBody(App.Messages.WelcomeBody);
            panel.SetStatusLine("Seated · hands only · no controllers required");
        }

        protected override void OnEntered()
        {
            Panel?.SetBody(App.Messages.WelcomeBody);
        }

        /// <summary>Marked complete when the tutorial finishes or is skipped.</summary>
        public void MarkOnboardingCompleted()
        {
            OnboardingCompleted = true;
            Panel?.SetStatusLine("Seated · hands only · onboarding complete");
        }
    }
}