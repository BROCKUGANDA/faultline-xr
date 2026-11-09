namespace Faultline.Runtime.UI.Screens
{
    using System;
    using System.Collections.Generic;

    using UnityEngine;

    using Faultline.Domain;
    using Faultline.Presentation;
    using Faultline.Runtime.UI;

    /// <summary>
    /// PRD §6.1 screen 22 — pause menu. The simulation clock is stopped by the
    /// application root while the state is Paused, so every option here states
    /// what it does to the live run: resume, deterministic replay, settings, or an
    /// exit that abandons the run and returns to welcome.
    /// </summary>
    public sealed class PauseMenuScreenView : ScreenViewBase, PauseMenuScreen
    {
        private FlButton _resumeButton;
        private FlButton _restartButton;

        public override string ScreenId => ScreenIds.PauseMenu;
        public override string Title => "Paused";

        // Back is not a fixed screen: resuming returns to wherever the run was
        // interrupted, so the navigator's own state machine owns that route.
        public override string BackScreenId => null;
        public override string EntryConditions => "The user pauses during an active scenario.";
        public override string ExitConditions =>
            "Resume, restart, or exit to the welcome screen.";
        public override string InteractionModel =>
            "Pinch Resume, Restart scenario, Settings or Exit to welcome.";
        public override string AccessibleDescription =>
            "Pause menu. Says whether a scenario is active, names the incident and phase being " +
            "held, and lists the four ways out as labelled buttons.";

        public bool ScenarioActive => App?.Controller?.IsScenarioRunning == true;

        public event EventHandler ResumeRequested;
        public event EventHandler RestartRequested;
        public event EventHandler SettingsRequested;
        public event EventHandler ExitToWelcomeRequested;

        protected override FlPanel BuildPanel() =>
            CreatePanelInZone("PAUSED", SpatialZone.Centre, 700f, 540f);

        protected override void BuildContent(FlPanel panel)
        {
            _resumeButton = panel.AddButton("pause-resume", "Resume",
                "Resume the paused simulation and return to the investigation workspace", OnResume);
            _restartButton = panel.AddButton("pause-restart", "Restart scenario",
                "Reset the deterministic CASCADE run and replay it from the incident detection point",
                OnRestart);
            panel.AddButton("pause-settings", "Settings",
                "Open accessibility and comfort settings while the run stays paused", () =>
                {
                    SettingsRequested?.Invoke(this, EventArgs.Empty);
                    Report(App.Navigator.NavigateToScreen(ScreenIds.Settings), "Opening settings");
                });
            panel.AddButton("pause-exit", "Exit to Welcome",
                "Abandon the paused run and return to the welcome screen", OnExit);

            App.RegisterCriticalAction("pause-exit-welcome", "PauseMenu.ExitToWelcome");

            App.Changed += (_, _) => Refresh();
            Refresh();
        }

        protected override void OnEntered() => Refresh();

        private void OnResume()
        {
            if (!ScenarioActive) return;
            ResumeRequested?.Invoke(this, EventArgs.Empty);
            Report(App.ResumeScenario(), "Resuming");
        }

        private void OnRestart()
        {
            if (!ScenarioActive || App.IsBusy) return;
            RestartRequested?.Invoke(this, EventArgs.Empty);

            var replay = App.ReplayScenario();
            if (!replay.IsSuccess)
            {
                SetError(replay.ErrorCode,
                    "The scenario could not be reset: " + replay.ErrorMessage +
                    " The paused run is unchanged.", canRetry: true);
                return;
            }

            Report(App.ResumeScenario(), "Returning to the replayed run");
        }

        private void OnExit()
        {
            ExitToWelcomeRequested?.Invoke(this, EventArgs.Empty);
            Report(App.Navigator.TransitionTo(ApplicationState.Ready), "Exiting to welcome");
        }

        private void Refresh()
        {
            if (Panel == null || HasError) return;

            var controller = App?.Controller;
            var lines = new List<string>();

            if (ScenarioActive)
            {
                lines.Add($"Simulation clock stopped · phase {controller.Phase}");
                lines.Add($"Incident {FaultlineMessages.ShortId(controller.IncidentId)} · scenario " +
                          $"{controller.Engine?.ScenarioId ?? "not started"} · provider {App.ProviderKind}");
                lines.Add($"Aggregate health (simulated): {App.CurrentAggregateHealth:P0}");
            }
            else
            {
                lines.Add("No scenario is running.");
                lines.Add("There is nothing held here for Resume or Restart to act on.");
            }

            lines.Add(string.Empty);
            lines.Add("Resume continues from here. Restart replays the same seed from incident " +
                      "detection. Exit leaves the run behind and returns to Welcome.");

            Panel.SetBody(string.Join(Environment.NewLine, lines));
            Panel.SetStatusLine(App.IsBusy
                ? App.BusyStatus
                : ScenarioActive
                    ? "Paused · the simulation is not advancing"
                    : "Nothing to resume · Settings and Exit still work");

            _resumeButton.Interactable = ScenarioActive && !App.IsBusy;
            _restartButton.Interactable = ScenarioActive && !App.IsBusy;
        }

        private void Report(Result<bool> outcome, string operation)
        {
            if (outcome.IsSuccess) return;
            SetError(outcome.ErrorCode,
                operation + " was refused: " + outcome.ErrorMessage +
                " The run is still paused on this screen.", canRetry: true);
        }
    }
}
