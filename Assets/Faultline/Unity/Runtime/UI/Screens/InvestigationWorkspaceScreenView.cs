namespace Faultline.Runtime.UI.Screens
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using UnityEngine;

    using Faultline.App;
    using Faultline.Domain;
    using Faultline.Presentation;
    using Faultline.Runtime.UI;

    /// <summary>
    /// PRD §6.1 screen 11 — the investigation hub. It mirrors the controller's real
    /// phase, the simulated aggregate health and the recorded findings, and routes to
    /// the analyst panel, the evidence explorer and the remediation proposal. Every
    /// route is gated on the target screen's own entry conditions.
    /// </summary>
    public sealed class InvestigationWorkspaceScreenView : ScreenViewBase, InvestigationWorkspaceScreen
    {
        private string _openedIncidentId;
        private int _citedEvidenceSeen = -1;
        private bool _showingError;

        public override string ScreenId => ScreenIds.InvestigationWorkspace;
        public override string Title => "Investigation workspace";
        public override string BackScreenId => ScreenIds.IncidentDetail;
        public override string EntryConditions =>
            "The CASCADE scenario must be running with an incident raised.";
        public override string ExitConditions =>
            "Findings reviewed and a remediation proposed, or the workspace is left.";
        public override string InteractionModel =>
            "Pinch Agents, Evidence, Propose remediation or the recorded proposal; " +
            "the panel reads phase, health and findings back from the controller.";
        public override string AccessibleDescription =>
            "Investigation workspace hub. Announces the incident id, phase, simulated aggregate " +
            "health, recorded findings and whether a remediation proposal exists.";

        public string IncidentId => App?.Controller?.IncidentId;

        public bool ProposalExists => App?.Controller?.ProposalId != null;

        public event EventHandler<IncidentIdEventArgs> InvestigationOpened;
        public event EventHandler ProposeRemediationRequested;
        public event EventHandler EvidenceAdded;

        protected override SpatialZone ZoneForScreen() => SpatialZone.Centre;

        protected override FlPanel BuildPanel() =>
            CreatePanelInZone("INVESTIGATION WORKSPACE", SpatialZone.Centre, 720f, 560f);

        protected override void BuildContent(FlPanel panel)
        {
            _ = panel.AddButton("workspace-agents", "Analyst agents",
                "Open the analyst agent panel to dispatch the four roles",
                () => App.Navigator.NavigateToScreen(ScreenIds.AgentControlPanel));

            _ = panel.AddButton("workspace-evidence", "Evidence explorer",
                "Open the evidence cited by the recorded findings",
                () => App.Navigator.NavigateToScreen(ScreenIds.EvidenceExplorer));

            _ = panel.AddButton("workspace-propose", "Propose remediation",
                "Ask the remediation service to record a RestartService proposal",
                OnPropose);

            _ = panel.AddButton("workspace-proposal", "Remediation proposal",
                "Open the recorded remediation proposal",
                () => App.Navigator.NavigateToScreen(ScreenIds.RemediationProposal));

            _ = panel.AddButton("workspace-back", "Back", "Return to the previous screen",
                () => App.Navigator.GoBack());

            App.Changed += (_, _) => Refresh();
        }

        protected override void OnEntered()
        {
            _showingError = false;
            Refresh();
        }

        private void OnPropose()
        {
            if (App.IsBusy) return;
            if (App.Controller.Phase != InvestigationPhase.Findings) return;

            ProposeRemediationRequested?.Invoke(this, EventArgs.Empty);

            var result = App.ProposeRemediation();
            if (!result.IsSuccess)
            {
                _showingError = true;
                SetError(result.ErrorCode,
                    "No proposal was recorded: " + result.ErrorMessage +
                    " Finish the investigation first, then propose again.",
                    canRetry: true);
                return;
            }

            _showingError = false;
            App.Navigator.NavigateToScreen(ScreenIds.RemediationProposal);
            Refresh();
        }

        private void Refresh()
        {
            if (!isActiveAndEnabled || Panel == null) return;

            var controller = App?.Controller;

            if (controller == null || controller.IncidentId == null)
            {
                SetEmpty("No incident is under investigation. Start the CASCADE scenario and open " +
                         "the incident to fill this workspace.");
                return;
            }

            if (_showingError)
            {
                Panel.SetStatusLine(App.StatusLine);
                return;
            }

            RaiseOpened(controller.IncidentId);

            var lines = new List<string>
            {
                $"Phase: {controller.Phase}",
                $"Incident {FaultlineMessages.ShortId(controller.IncidentId)} · root cause " +
                $"{controller.RootCauseNodeId} · scenario {ScenarioId(controller)}",
                $"Aggregate health (simulated): {App.CurrentAggregateHealth:P0}",
                FindingsLine(controller),
                ProposalLine(controller),
                string.Empty,
                NextStep(controller.Phase)
            };

            Panel.SetBody(string.Join(Environment.NewLine, lines));
            Panel.SetStatusLine(App.IsBusy ? App.BusyStatus : App.StatusLine);
            SetContent();

            if (Panel.TryGetButton("workspace-evidence", out var evidence))
                evidence.Interactable = CanOpen(ScreenIds.EvidenceExplorer);
            if (Panel.TryGetButton("workspace-proposal", out var proposal))
                proposal.Interactable = ProposalExists;
            if (Panel.TryGetButton("workspace-propose", out var propose))
                propose.Interactable = controller.Phase == InvestigationPhase.Findings && !App.IsBusy;

            TrackCitedEvidence();
        }

        private void RaiseOpened(string incidentId)
        {
            if (string.Equals(_openedIncidentId, incidentId, StringComparison.Ordinal)) return;
            _openedIncidentId = incidentId;
            InvestigationOpened?.Invoke(this, new IncidentIdEventArgs(incidentId));
        }

        private static string ScenarioId(InvestigationController controller) =>
            controller.Engine?.ScenarioId ?? "not started";

        private string FindingsLine(InvestigationController controller)
        {
            var report = controller.Report;
            return report == null
                ? "Findings: none recorded yet — dispatch the analyst agents."
                : App.Messages.FindingsBody(report.Findings.Count, report.AverageConfidence) +
                  $" Recommendation: {report.Recommendation}";
        }

        private static string ProposalLine(InvestigationController controller) =>
            controller.ProposalId == null
                ? "Proposal: none recorded for this incident."
                : $"Proposal {FaultlineMessages.ShortId(controller.ProposalId)} recorded and awaiting " +
                  "an explicit human decision.";

        private static string NextStep(InvestigationPhase phase) => phase switch
        {
            InvestigationPhase.Detected =>
                "Open the incident, then dispatch the analyst agents.",
            InvestigationPhase.Investigating =>
                "Dispatch the analyst agents; each finding cites the events it was built from.",
            InvestigationPhase.Findings =>
                "Review the cited evidence, then propose a remediation.",
            InvestigationPhase.Proposed =>
                "Submit the proposal for approval — nothing executes before that decision.",
            InvestigationPhase.Approved =>
                "Approved. Execute the remediation in the recovery sequence.",
            InvestigationPhase.Executed =>
                "Executed. Verify recovery against the simulated metrics.",
            InvestigationPhase.Verified =>
                "Verified. Resolve the incident to open the after-action report.",
            InvestigationPhase.Resolved =>
                "Incident resolved. Replay from the results screen for a fresh deterministic run.",
            InvestigationPhase.Rejected =>
                "Proposal rejected; no change was applied to the simulation. Replay for a new run.",
            _ => "No further step is recorded for this phase."
        };

        /// <summary>
        /// Evidence is only "added" when the domain actually records findings that
        /// cite new events, so the event follows the orchestrator rather than a click.
        /// </summary>
        private void TrackCitedEvidence()
        {
            var controller = App?.Controller;

            IEnumerable<AgentFinding> findings = controller?.Report?.Findings;
            if (findings == null)
            {
                var recorded = controller?.Orchestrator?.Findings;
                findings = recorded == null
                    ? Enumerable.Empty<AgentFinding>()
                    : recorded.Values;
            }

            var ids = new HashSet<Guid>();
            foreach (var finding in findings)
                foreach (var evidenceId in finding.EvidenceIds)
                    ids.Add(evidenceId);

            if (_citedEvidenceSeen >= 0 && ids.Count > _citedEvidenceSeen)
                EvidenceAdded?.Invoke(this, EventArgs.Empty);

            _citedEvidenceSeen = ids.Count;
        }

        private bool CanOpen(string screenId) =>
            App.Navigator.TryGetScreen(screenId, out var screen) && screen.CanEnter;
    }
}
