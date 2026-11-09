namespace Faultline.Runtime.UI.Screens
{
    using System;
    using System.Linq;

    using UnityEngine;

    using Faultline.App;
    using Faultline.Presentation;
    using Faultline.Runtime.UI;

    /// <summary>
    /// PRD §6.1 screen 17 — results. The summary is composed entirely from the
    /// real outcome: finding count and mean confidence from the investigation
    /// report, the approved action from the remediation record, the health delta
    /// and rollback evidence from the simulation, and unresolved uncertainty
    /// from finding limitations. A rejected or failed journey is shown as
    /// rejected — never as a green card.
    /// </summary>
    public sealed class ResultsScreenView : ScreenViewBase, ResultsScreen
    {
        private string _outcomeSummary = string.Empty;
        private bool _success;

        private FlButton _replayButton;
        private FlButton _overviewButton;

        public override string ScreenId => ScreenIds.Results;
        public override string Title => "Results";
        public override string BackScreenId => ScreenIds.Verification;
        public override string EntryConditions => "Verification complete, or the proposal was rejected.";
        public override string ExitConditions => "The user returns to the overview or replays the scenario.";
        public override string InteractionModel =>
            "Pinch Run scenario again to replay, or Resolve & overview to close the incident.";
        public override string AccessibleDescription =>
            "Results screen. The outcome is stated as text with an OK or REJECTED glyph and " +
            "lists real finding, action, health-delta, rollback and uncertainty numbers.";

        public string OutcomeSummary => _outcomeSummary;
        public bool Success => _success;

        public event EventHandler ResultsViewed;
        public event EventHandler NewScenarioRequested;
        public event EventHandler ReturnToOverviewRequested;

        protected override FlPanel BuildPanel() =>
            CreatePanelInZone("Outcome", SpatialZone.Centre, 720f, 540f);

        protected override void BuildContent(FlPanel panel)
        {
            _replayButton = panel.AddButton("results-replay", "Run scenario again",
                "Reset the deterministic simulation and replay the CASCADE incident from the start",
                OnReplay);
            _overviewButton = panel.AddButton("results-overview", "Resolve & return to overview",
                "Close the incident in the domain and return to the infrastructure overview",
                OnReturnToOverview);

            App.Changed += (_, _) => Refresh();
            Refresh();
        }

        protected override bool EvaluateCanEnter()
        {
            var phase = App?.Controller?.Phase;
            return phase == InvestigationPhase.Verified ||
                   phase == InvestigationPhase.Resolved ||
                   phase == InvestigationPhase.Rejected ||
                   phase == InvestigationPhase.Executed;
        }

        protected override void OnEntered()
        {
            ResultsViewed?.Invoke(this, EventArgs.Empty);
            Refresh();
        }

        private void OnReplay()
        {
            if (App.IsBusy) return;
            NewScenarioRequested?.Invoke(this, EventArgs.Empty);

            var replay = App.ReplayScenario();
            if (!replay.IsSuccess)
            {
                SetError(replay.ErrorCode,
                    "The scenario could not be reset. The previous run state is unchanged.",
                    canRetry: true);
                return;
            }

            App.Navigator.NavigateToScreen(ScreenIds.ScenarioSelection);
        }

        private void OnReturnToOverview()
        {
            if (App.IsBusy) return;
            ReturnToOverviewRequested?.Invoke(this, EventArgs.Empty);

            if (App.Controller.Phase == InvestigationPhase.Verified)
            {
                var resolve = App.ResolveIncident();
                if (!resolve.IsSuccess)
                {
                    SetError(resolve.ErrorCode,
                        "The incident could not be closed in the domain. Review the checks " +
                        "on the verification screen and retry.",
                        canRetry: true);
                    return;
                }
            }

            App.Navigator.NavigateToScreen(ScreenIds.InfrastructureOverview);
        }

        private void Refresh()
        {
            if (Panel == null) return;

            var controller = App.Controller;
            var phase = controller?.Phase;
            var report = controller?.Report;
            var recovery = controller?.Recovery;

            _success = recovery != null && recovery.Success &&
                       (phase == InvestigationPhase.Verified || phase == InvestigationPhase.Resolved);

            var rejected = phase == InvestigationPhase.Rejected;
            var pending = phase == InvestigationPhase.Executed;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine(_success ? "[OK] OUTCOME: RECOVERED & VERIFIED"
                : rejected ? "[X] OUTCOME: REJECTED — NO CHANGE APPLIED"
                : "[-] OUTCOME: VERIFICATION PENDING");
            sb.AppendLine();

            if (report != null)
            {
                sb.AppendLine($"Findings: {report.Findings.Count} grounded · " +
                              $"mean confidence {report.AverageConfidence:P0}");
                var uncertain = report.Findings.Count(f => f.Limitations.Count > 0);
                var lowConfidence = report.Findings.Count(f => f.Confidence < 0.6);
                sb.AppendLine($"Unresolved uncertainty: {uncertain} finding(s) carry stated " +
                              $"limitations, {lowConfidence} below 60% confidence.");
            }

            if (TryDescribeAction(controller, out var actionLine))
                sb.AppendLine(actionLine);
            else
                sb.AppendLine("Approved action: none recorded.");

            if (rejected)
            {
                sb.AppendLine("The proposal was rejected by the operator. No remediation was " +
                              "executed, aggregate health is unchanged, and the incident stays " +
                              "open in the graph.");
            }
            else if (recovery != null)
            {
                sb.AppendLine($"Health delta: {recovery.HealthDelta:P0} · " +
                              $"aggregate now {App.CurrentAggregateHealth:P0}");
                sb.AppendLine($"Rollback: {(RollbackOccurred(controller)
                    ? "a checkpoint was restored during a failed attempt"
                    : "not needed — the executed attempt applied cleanly")}");
                if (pending)
                    sb.AppendLine("Verification has not passed yet, so this is not reported as success.");
            }

            _outcomeSummary = sb.ToString();
            if (!HasError)
            {
                Panel.SetBody(_outcomeSummary);
                Panel.SetState(_success ? VisualState.Selected :
                    (rejected ? VisualState.Critical : VisualState.Normal));
                Panel.SetStatusLine(App.IsBusy ? App.BusyStatus :
                    $"Incident {FaultlineMessages.ShortId(controller?.IncidentId ?? string.Empty)} · " +
                    $"phase {phase?.ToString() ?? "n/a"}");
            }

            _replayButton.Interactable = !App.IsBusy;
            _overviewButton.Interactable = !App.IsBusy && !pending;
        }

        private static bool TryDescribeAction(Faultline.App.InvestigationController controller,
            out string line)
        {
            line = null;
            if (controller?.Remediation == null || controller.ProposalId == null) return false;
            if (!controller.Remediation.Proposals.TryGetValue(controller.ProposalId, out var p))
                return false;
            line = $"Approved action: {p.Action.Type} on " +
                   $"{string.Join(", ", p.Action.AffectedServiceIds)} · " +
                   $"approved by {p.ApprovedBy ?? "operator"} · status {p.Status}";
            return true;
        }

        // The simulation engine writes a log line whenever a checkpoint is
        // restored — the only durable, real evidence that a rollback happened.
        private static bool RollbackOccurred(Faultline.App.InvestigationController controller) =>
            controller?.Engine?.Logs != null &&
            controller.Engine.Logs.Any(l => l.Message != null &&
                l.Message.Contains("Restored checkpoint"));
    }
}
