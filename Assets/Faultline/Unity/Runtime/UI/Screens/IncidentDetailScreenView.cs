namespace Faultline.Runtime.UI.Screens
{
    using System;
    using System.Text;

    using UnityEngine;

    using Faultline.App;
    using Faultline.Domain;
    using Faultline.Presentation;
    using Faultline.Runtime.UI;

    /// <summary>
    /// PRD §6.1 screen 9 — Incident detail. Lifecycle state, severity and the
    /// causal impact summary are read back from the incident entity through
    /// <c>IncidentService.GetIncidentEntity</c>; nothing here is hard-coded.
    /// Investigate runs the real async investigation through the app root, so
    /// the buttons reflect the domain phase machine.
    /// </summary>
    public sealed class IncidentDetailScreenView : ScreenViewBase, IncidentDetailScreen
    {
        private string _raisedIncidentId;
        private FlButton _investigateButton;
        private FlButton _remediateButton;
        private FlButton _timelineButton;

        public override string ScreenId => ScreenIds.IncidentDetail;
        public override string Title => "Incident detail";
        public override string BackScreenId => ScreenIds.ServiceInspection;
        public override string EntryConditions =>
            "An incident raised for a node; the scenario must have begun.";
        public override string ExitConditions =>
            "Incident lifecycle advanced, or the screen closed.";
        public override string InteractionModel =>
            "Read the lifecycle and impact, then pinch Investigate, Remediate or Timeline.";
        public override string AccessibleDescription =>
            "Incident detail screen. Announces the incident identifier, its lifecycle state, severity " +
            "word, affected service and blast radius. Investigate, remediate and timeline actions.";

        public string IncidentId => App?.Controller?.IncidentId;
        public string LifecycleState { get; private set; }
        public string Severity { get; private set; }

        public event EventHandler<IncidentIdEventArgs> IncidentSelected;
        public event EventHandler InvestigateRequested;
        public event EventHandler RemediateRequested;

        protected override FlPanel BuildPanel() =>
            CreatePanelInZone("Incident detail", SpatialZone.Right, 700f, 700f);

        protected override SpatialZone ZoneForScreen() => SpatialZone.Right;

        protected override bool EvaluateCanEnter()
        {
            var app = App;
            return app != null && !string.IsNullOrEmpty(app.Controller?.IncidentId);
        }

        protected override void BuildContent(FlPanel panel)
        {
            _investigateButton = panel.AddButton("incident-investigate", "Investigate",
                "Run the analyst investigation on this incident", OnInvestigate);
            _remediateButton = panel.AddButton("incident-remediate", "Remediate",
                "Open the remediation proposal for this incident", OnRemediate);
            _timelineButton = panel.AddButton("incident-timeline", "Timeline",
                "Open the ordered causal timeline for this incident", OnTimeline);

            App.Changed += (_, _) => Refresh();
            Refresh();
        }

        protected override void OnEntered()
        {
            Refresh();
            var incidentId = IncidentId;
            if (!string.IsNullOrEmpty(incidentId))
                IncidentSelected?.Invoke(this, new IncidentIdEventArgs(incidentId));
        }

        private void OnInvestigate()
        {
            var app = App;
            if (app.IsBusy || app.Controller?.Phase != InvestigationPhase.Investigating) return;

            InvestigateRequested?.Invoke(this, EventArgs.Empty);
            app.RunAsync(() => app.InvestigateAsync(), "Investigating the CASCADE incident…",
                result =>
                {
                    if (!result.IsSuccess)
                        SetError(result.ErrorCode,
                            "The investigation could not complete. " + result.ErrorMessage,
                            canRetry: true);
                    Refresh();
                });
        }

        private void OnRemediate()
        {
            var app = App;
            if (app.IsBusy || !CanRemediate(app.Controller)) return;
            RemediateRequested?.Invoke(this, EventArgs.Empty);
            app.Navigator.NavigateToScreen(ScreenIds.RemediationProposal);
        }

        private void OnTimeline()
        {
            var app = App;
            if (app.IsBusy || string.IsNullOrEmpty(IncidentId)) return;
            app.Navigator.NavigateToScreen(ScreenIds.IncidentTimeline);
        }

        private void Refresh()
        {
            var app = App;
            var panel = Panel;
            if (app == null || panel == null) return;

            var incidentId = app.Controller?.IncidentId;
            if (string.IsNullOrEmpty(incidentId))
            {
                LifecycleState = string.Empty;
                Severity = string.Empty;
                SetEmpty("No incident is active. Begin the CASCADE scenario to raise one.");
                SetActions(false, false, false);
                return;
            }

            var controller = app.Controller;
            var read = controller.App?.Incidents?.GetIncidentEntity(incidentId);
            if (read == null || !read.IsSuccess)
            {
                LifecycleState = "unavailable";
                Severity = "unavailable";
                SetError(read != null ? read.ErrorCode : "FL-IC-0051",
                    "The incident record could not be read from the incident service.",
                    canRetry: true);
                SetActions(false, false, false);
                return;
            }

            var incident = read.Value;
            LifecycleState = incident.State.ToString();
            Severity = string.IsNullOrEmpty(incident.Severity) ? "unspecified" : incident.Severity;
            FocusTargetId = incidentId;

            if (!string.Equals(incidentId, _raisedIncidentId, StringComparison.Ordinal))
            {
                _raisedIncidentId = incidentId;
                IncidentSelected?.Invoke(this, new IncidentIdEventArgs(incidentId));
            }

            var copy = new StringBuilder();
            copy.AppendLine($"Incident {FaultlineMessages.ShortId(incidentId)} · node {incident.NodeId}");
            copy.AppendLine($"Lifecycle: {LifecycleState} · severity: {Severity}");
            copy.AppendLine($"Rule {incident.RuleId} · detected {incident.DetectedAt:O}");
            copy.AppendLine($"Correlated events: {incident.EventIds.Count} · evidence: {incident.EvidenceIds.Count}");
            copy.AppendLine($"Journey phase: {controller.Phase} · root cause {controller.RootCauseNodeId ?? "unset"}");
            copy.AppendLine($"Impact: {DescribeImpact(incident.NodeId)}");
            if (!string.IsNullOrEmpty(controller.LastError))
                copy.Append($"Last domain error: {controller.LastError}");

            panel.SetBody(copy.ToString());
            panel.SetStatusLine(app.IsBusy
                ? app.BusyStatus
                : $"Aggregate health {app.CurrentAggregateHealth:P0} · lifecycle {LifecycleState}");
            SetContent();

            var busy = app.IsBusy;
            var investigatable = controller.Phase == InvestigationPhase.Investigating;
            var remediable = CanRemediate(controller);
            SetActions(!busy && investigatable, !busy && remediable, !busy);

            if (_investigateButton != null)
                _investigateButton.Label = investigatable
                    ? "Investigate"
                    : $"Investigate · phase {controller.Phase}";
            if (_remediateButton != null)
                _remediateButton.Label = remediable ? "Remediate" : "Remediate · findings first";
        }

        private void SetActions(bool investigate, bool remediate, bool timeline)
        {
            if (_investigateButton != null) _investigateButton.Interactable = investigate;
            if (_remediateButton != null) _remediateButton.Interactable = remediate;
            if (_timelineButton != null) _timelineButton.Interactable = timeline;
        }

        /// <summary>Remediation only makes sense once the investigation reported findings.</summary>
        private static bool CanRemediate(InvestigationController controller)
        {
            if (controller == null) return false;
            switch (controller.Phase)
            {
                case InvestigationPhase.Findings:
                case InvestigationPhase.Proposed:
                case InvestigationPhase.Approved:
                case InvestigationPhase.Executed:
                case InvestigationPhase.Verified:
                case InvestigationPhase.Resolved:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Blast radius from the graph impact analysis when the engine hands
        /// back the concrete graph; the read-only query view only exposes direct
        /// edges, so the summary says exactly what was measured.
        /// </summary>
        private string DescribeImpact(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId)) return "no affected node";
            var view = App?.Controller?.Engine?.GraphView;
            if (view is Faultline.Infrastructure.InfrastructureGraph graph)
            {
                var impact = graph.AnalyzeImpact(nodeId);
                if (impact.IsSuccess)
                    return $"{impact.Value.BlastRadius.Count} services in the blast radius" +
                           (impact.Value.HasCycle ? " · dependency cycle present" : string.Empty);
                return $"impact analysis failed ({impact.ErrorCode})";
            }

            if (view == null) return "graph unavailable";
            return $"{view.GetDependents(nodeId).Count} direct dependents · " +
                   $"{view.GetDependencies(nodeId).Count} direct dependencies";
        }
    }
}
