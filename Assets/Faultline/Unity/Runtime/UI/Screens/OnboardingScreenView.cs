namespace Faultline.Runtime.UI.Screens
{
    using System;

    using UnityEngine;

    using Faultline.Presentation;
    using Faultline.Runtime.UI;

    /// <summary>
    /// PRD §6.1 screen 3 — onboarding. Four steps of progressive disclosure over
    /// the journey the application actually runs: the spatial dependency field,
    /// the four analyst agents, the approval decision, and verified recovery.
    /// Finishing or skipping routes to the hand tutorial.
    /// </summary>
    public sealed class OnboardingScreenView : ScreenViewBase, OnboardingScreen
    {
        private static readonly (string Heading, string Body)[] Steps =
        {
            ("01 · THE FIELD",
             "Seven services hang in front of you as a dependency graph. One of them is " +
             "already failing when CASCADE starts, and the edges show what it drags down. " +
             "Pinch a service to read its status and its dependencies."),
            ("02 · THE ANALYSTS",
             "You direct four agents: ORACLE reads the logs, TRACE follows the calls, " +
             "SENTRY maps the blast radius, FORGE drafts the fix. Every finding cites the " +
             "evidence it was built from — nothing is guessed on your behalf."),
            ("03 · THE DECISION",
             "The agents hand you one remediation and stop. You approve or reject it. " +
             "Execution is idempotent, the change is reversible from a checkpoint, and a " +
             "second activation cannot apply the same fix twice."),
            ("04 · THE PROOF",
             "Recovery is verified against the simulated metrics, not animated. The " +
             "after-action report lists the ordered events, the health delta and what stayed " +
             "uncertain. Same seed, same incident, every run."),
        };

        private int _step;
        private FlButton _nextButton;
        private FlButton _backButton;

        public override string ScreenId => ScreenIds.Onboarding;
        public override string Title => "Onboarding";
        public override string BackScreenId => ScreenIds.Welcome;
        public override string EntryConditions => "The welcome screen asked for onboarding.";
        public override string ExitConditions => "All four steps read, or onboarding skipped.";
        public override string InteractionModel =>
            "Pinch Next and Back to step through the brief, or Skip to end onboarding now.";
        public override string AccessibleDescription =>
            $"Onboarding. Step {CurrentStep + 1} of {TotalSteps}: {Steps[_step].Heading}. " +
            "Next, Back and Skip are announced as labelled buttons.";

        public int CurrentStep => _step;
        public int TotalSteps => Steps.Length;
        public bool Skippable => true;

        public event EventHandler<OnboardingStepChangedEventArgs> StepChanged;
        public event EventHandler OnboardingCompleted;
        public event EventHandler OnboardingSkipped;

        protected override FlPanel BuildPanel() =>
            CreatePanelInZone("OPERATOR BRIEF", SpatialZone.Centre, 740f, 480f);

        protected override void BuildContent(FlPanel panel)
        {
            _nextButton = panel.AddButton("onboarding-next", "Next step",
                "Continue to the next onboarding step, or to the hand tutorial after the last one",
                OnNext);
            _backButton = panel.AddButton("onboarding-back", "Previous step",
                "Return to the previous onboarding step, or to the welcome screen at the first one",
                OnBack);
            panel.AddButton("onboarding-skip", "Skip onboarding",
                "End onboarding now and open the hand gesture tutorial", OnSkip);

            App.Changed += (_, _) => Refresh();
            Refresh();
        }

        protected override void OnEntered() => Refresh();

        private void OnNext()
        {
            if (_step < Steps.Length - 1) StepTo(_step + 1);
            else Complete();
        }

        private void OnBack()
        {
            if (_step > 0)
            {
                StepTo(_step - 1);
                return;
            }

            var welcome = App.Navigator.NavigateToScreen(ScreenIds.Welcome);
            if (!welcome.IsSuccess && Panel != null)
                Panel.SetStatusLine($"{welcome.ErrorCode} · the welcome screen would not open.");
        }

        private void OnSkip()
        {
            OnboardingSkipped?.Invoke(this, EventArgs.Empty);
            RouteToTutorial();
        }

        private void Complete()
        {
            OnboardingCompleted?.Invoke(this, EventArgs.Empty);
            RouteToTutorial();
        }

        private void StepTo(int next)
        {
            var previous = _step;
            _step = Mathf.Clamp(next, 0, Steps.Length - 1);
            StepChanged?.Invoke(this, new OnboardingStepChangedEventArgs(previous, _step));
            Refresh();
        }

        private void RouteToTutorial()
        {
            var tutorial = App.Navigator.NavigateToScreen(ScreenIds.HandTutorial);
            if (tutorial.IsSuccess) return;

            SetError(tutorial.ErrorCode,
                "The hand tutorial would not open, so onboarding stayed on screen. Starting " +
                "CASCADE from the welcome screen does not need the tutorial.", canRetry: true);
        }

        private void Refresh()
        {
            if (Panel == null || HasError) return;

            var (heading, body) = Steps[_step];
            Panel.SetBody($"{heading}\n\n{body}\n\n{ProviderNote()}");
            Panel.SetStatusLine($"Step {_step + 1} of {TotalSteps} · " +
                (_step == Steps.Length - 1
                    ? "Next opens the hand gesture tutorial"
                    : "Next continues the brief"));

            _nextButton.Label = _step == Steps.Length - 1 ? "Start hand tutorial" : "Next step";
            _backButton.Label = _step == 0 ? "Back to Welcome" : "Previous step";
        }

        private string ProviderNote() =>
            App.ProviderKind == XrProviderKind.Device
                ? "Hands only, seated. No controllers, no keyboard, nothing leaves the headset."
                : $"Provider {App.ProviderKind}: no hand-tracking device was found, so the gesture " +
                  "tutorial runs on labelled simulation input and reports right-hand gestures only.";
    }
}
