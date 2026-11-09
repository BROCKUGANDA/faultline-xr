namespace Faultline.Runtime.UI.Screens
{
    using System;
    using System.Collections.Generic;

    using UnityEngine;

    using Faultline.App;
    using Faultline.Presentation;
    using Faultline.Runtime.UI;

    /// <summary>
    /// PRD §6.1 screen 5 — scenario selection. This build ships exactly one
    /// deterministic scenario: ScenarioService starts the id the controller begins
    /// and rejects anything else with FL-SC-0121, so the list is the real id
    /// rather than a menu of unbuilt scenarios. Beginning routes through
    /// <see cref="FaultlineUnityApp.BeginScenarioAndOpenIncident"/>.
    /// </summary>
    public sealed class ScenarioSelectionScreenView : ScreenViewBase, ScenarioSelectionScreen
    {
        private const string ShippedScenarioId = "cascade-42";

        private string[] _scenarioIds = { ShippedScenarioId };
        private bool _selected;

        private FlButton _cardButton;
        private FlButton _startButton;

        public override string ScreenId => ScreenIds.ScenarioSelection;
        public override string Title => "Scenario selection";
        public override string BackScreenId => ScreenIds.Welcome;
        public override string EntryConditions => "The boot sequence completed.";
        public override string ExitConditions => "CASCADE selected and the start confirmed.";
        public override string InteractionModel =>
            "Pinch the CASCADE card to select it, then Begin CASCADE; Back returns to welcome.";
        public override string AccessibleDescription =>
            "Scenario selection. Lists the scenarios this build actually ships, names CASCADE and " +
            "its objective, and says whether it can be started yet.";

        public IReadOnlyList<string> ScenarioIds => _scenarioIds;
        public string SelectedScenarioId => _selected ? _scenarioIds[0] : null;
        public bool CanStart => SelectedScenarioId != null && (App?.IsBusy != true);

        public event EventHandler<ScenarioSelectedEventArgs> ScenarioSelected;
        public event EventHandler StartConfirmed;

        protected override FlPanel BuildPanel() =>
            CreatePanelInZone("SELECT SCENARIO", SpatialZone.Centre, 740f, 500f);

        protected override void BuildContent(FlPanel panel)
        {
            _cardButton = panel.AddButton("scenario-cascade", "CASCADE — 7 services",
                "Select the CASCADE incident scenario", OnSelectCard);
            _startButton = panel.AddButton("scenario-start", "Begin CASCADE",
                "Start the deterministic CASCADE incident and open the investigation workspace",
                OnStart, interactable: false);
            panel.AddButton("scenario-back", "Back to Welcome",
                "Return to the welcome screen without starting a scenario", () =>
                {
                    var welcome = App.Navigator.NavigateToScreen(ScreenIds.Welcome);
                    if (!welcome.IsSuccess)
                        Panel.SetStatusLine($"{welcome.ErrorCode} · the welcome screen would not open.");
                });

            App.Changed += (_, _) => Refresh();
            Refresh();
        }

        protected override void OnEntered() => Refresh();

        private void OnSelectCard()
        {
            if (_selected) return;
            _selected = true;
            ScenarioSelected?.Invoke(this, new ScenarioSelectedEventArgs(SelectedScenarioId));
            Refresh();
        }

        private void OnStart()
        {
            if (!CanStart) return;
            StartConfirmed?.Invoke(this, EventArgs.Empty);

            var started = App.BeginScenarioAndOpenIncident();
            if (!started.IsSuccess)
            {
                SetError(started.ErrorCode,
                    $"CASCADE did not start ({started.ErrorMessage}). No incident was raised and " +
                    "the previous run state is unchanged.", canRetry: true);
                return;
            }

            var workspace = App.Navigator.NavigateToScreen(ScreenIds.InvestigationWorkspace);
            if (!workspace.IsSuccess)
                SetError(workspace.ErrorCode,
                    "The scenario started but the investigation workspace would not open: " +
                    workspace.ErrorMessage, canRetry: true);
        }

        private void Refresh()
        {
            if (Panel == null || HasError) return;

            var liveId = App?.Controller?.Engine?.ScenarioId;
            if (!string.IsNullOrEmpty(liveId) &&
                !string.Equals(liveId, _scenarioIds[0], StringComparison.Ordinal))
                _scenarioIds = new[] { liveId };

            var running = App?.Controller?.IsScenarioRunning == true;
            _cardButton.Label = _selected ? "CASCADE — selected" : "CASCADE — 7 services";
            _startButton.Label = running ? "Restart CASCADE" : "Begin CASCADE";
            _startButton.Interactable = CanStart;

            Panel.SetBody(string.Join(Environment.NewLine, new[]
            {
                $"{_scenarioIds[0]} — CASCADE",
                App.Messages.ScenarioBody,
                "Objective: detect → dispatch agents → propose → approve → verify.",
                "The root cause stays hidden until the agents investigate it.",
                running ? RunningLine(App.Controller) : "One scenario ships in this build; " +
                    "any other id is rejected with FL-SC-0121.",
            }));

            Panel.SetStatusLine(StatusLine(running));
        }

        private string StatusLine(bool running)
        {
            if (App.IsBusy) return App.BusyStatus;
            if (running) return "A run is already active — restarting replaces it.";
            return _selected
                ? "Selected · Begin CASCADE raises the incident"
                : "Select the CASCADE card first.";
        }

        private static string RunningLine(InvestigationController controller) =>
            $"A run is active: phase {controller.Phase}, incident " +
            $"{FaultlineMessages.ShortId(controller.IncidentId)}, root cause still " +
            "under investigation.";
    }
}
