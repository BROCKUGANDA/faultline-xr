namespace Faultline.Runtime.UI.Screens
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using UnityEngine;

    using Faultline.Agents;
    using Faultline.App;
    using Faultline.Domain;
    using Faultline.Presentation;
    using Faultline.Runtime.UI;

    /// <summary>
    /// PRD §6.1 screen 10 — analyst agent control panel. The role list is the real
    /// <see cref="AgentRole"/> set (Trace, Sentry, Forge, Oracle) and every status,
    /// finding count and confidence shown here is read back from the orchestrator's
    /// task and finding records, so a finding the domain did not produce is never
    /// claimed on screen.
    /// </summary>
    public sealed class AgentControlPanelScreenView : ScreenViewBase, AgentControlPanelScreen
    {
        /// <summary>Order the deterministic pipeline evaluates the roles in.</summary>
        private static readonly AgentRole[] Pipeline =
        {
            AgentRole.Trace,
            AgentRole.Sentry,
            AgentRole.Forge,
            AgentRole.Oracle
        };

        private static readonly IReadOnlyList<string> RoleNames =
            Pipeline.Select(role => role.ToString()).ToList();

        private readonly Dictionary<string, FlButton> _dispatchButtons = new(StringComparer.Ordinal);
        private readonly HashSet<string> _announcedTasks = new(StringComparer.Ordinal);

        private AgentOrchestrator _announcedOrchestrator;
        private bool _showingError;

        public override string ScreenId => ScreenIds.AgentControlPanel;
        public override string Title => "Analyst agents";
        public override string BackScreenId => ScreenIds.IncidentDetail;
        public override string EntryConditions =>
            "An incident must be open for investigation (phase Investigating).";
        public override string ExitConditions =>
            "Every dispatched task reports the status the orchestrator holds, or the panel is closed.";
        public override string InteractionModel =>
            "Pinch an analyst role to request its dispatch; the investigation runs through the " +
            "application's async bridge and never blocks the frame loop.";
        public override string AccessibleDescription =>
            "Analyst agent panel. Four roles — Trace, Sentry, Forge and Oracle — each showing the " +
            "task status, finding count and confidence the domain actually recorded.";

        public IReadOnlyList<string> AgentRoles => RoleNames;

        /// <summary>The controller investigates the root cause node; nothing else.</summary>
        public string TargetId => App?.Controller?.RootCauseNodeId;

        public event EventHandler<AgentDispatchRequestedEventArgs> AgentDispatchRequested;
        public event EventHandler AgentTaskCompleted;

        protected override SpatialZone ZoneForScreen() => SpatialZone.Right;

        protected override FlPanel BuildPanel() =>
            CreatePanelInZone("ANALYST AGENTS", SpatialZone.Right, 660f, 560f);

        protected override void BuildContent(FlPanel panel)
        {
            foreach (var role in Pipeline)
            {
                var roleName = role.ToString();
                _dispatchButtons[roleName] = panel.AddButton(
                    "dispatch-" + roleName.ToLowerInvariant(),
                    "Dispatch " + roleName,
                    $"Ask the orchestrator to dispatch the {roleName} analyst against the root cause service",
                    () => OnDispatch(roleName));
            }

            panel.AddButton("agents-open-workspace", "Investigation workspace",
                "Open the investigation hub with phase, health and next step",
                () => App.Navigator.NavigateToScreen(ScreenIds.InvestigationWorkspace));

            panel.AddButton("agents-back", "Back", "Return to the previous screen",
                () => App.Navigator.GoBack());

            App.Changed += (_, _) => Refresh();
        }

        protected override void OnEntered()
        {
            _showingError = false;
            Refresh();
        }

        private void OnDispatch(string role)
        {
            if (App.IsBusy || !CanDispatch()) return;

            AgentDispatchRequested?.Invoke(this,
                new AgentDispatchRequestedEventArgs(role, TargetId));

            _showingError = false;

            // InvestigateAsync evaluates the whole deterministic pipeline and is
            // idempotent per (incident, role), so re-pinning a role re-runs the
            // investigation instead of inventing a per-agent result.
            App.RunAsync(() => App.InvestigateAsync(),
                $"Running the deterministic investigation for {role}…",
                OnInvestigationFinished);

            Refresh();
        }

        private void OnInvestigationFinished(Result<bool> result)
        {
            if (!result.IsSuccess)
            {
                _showingError = true;
                SetError(result.ErrorCode,
                    "The analyst run was rejected by the domain: " + result.ErrorMessage +
                    " Nothing was recorded as a finding. Pinch the role again to retry.",
                    canRetry: true);
                return;
            }

            Refresh();
        }

        private bool CanDispatch() =>
            App?.Controller?.Orchestrator != null &&
            App.Controller.Phase == InvestigationPhase.Investigating;

        private void Refresh()
        {
            if (!isActiveAndEnabled || Panel == null) return;

            var controller = App?.Controller;
            var orchestrator = controller?.Orchestrator;

            foreach (var button in _dispatchButtons.Values)
                button.Interactable = !App.IsBusy && CanDispatch();

            if (App.IsBusy)
            {
                SetLoading();
                Panel.SetStatusLine(App.BusyStatus);
                return;
            }

            if (controller == null || controller.IncidentId == null)
            {
                SetEmpty("No incident is open, so no analyst can be dispatched. " +
                         "Start the CASCADE scenario from the scenario screen first.");
                return;
            }

            if (_showingError)
            {
                Panel.SetStatusLine(App.StatusLine);
                return;
            }

            var byRole = LatestTasksByRole(orchestrator, controller.IncidentId);
            if (byRole.Count == 0)
            {
                SetEmpty($"No analyst task has been dispatched against {controller.RootCauseNodeId}. " +
                         "Pinch a role to run the deterministic investigation.");
                Panel.SetStatusLine(App.StatusLine);
                return;
            }

            var lines = new List<string>
            {
                $"Target: {controller.RootCauseNodeId} · incident " +
                FaultlineMessages.ShortId(controller.IncidentId),
                string.Format("{0,-8}{1}", "ROLE", "STATUS (orchestrator task record)"),
            };

            foreach (var role in Pipeline)
            {
                if (!byRole.TryGetValue(role, out var task))
                {
                    lines.Add(string.Format("{0,-8}", role) + "not dispatched");
                    continue;
                }

                var findings = FindingsOf(orchestrator, task);
                var outcome = findings.Count == 0
                    ? "no finding recorded"
                    : $"{findings.Count} finding · confidence " +
                      string.Join(", ", findings.Select(finding => $"{finding.Confidence:P0}"));

                lines.Add(string.Format("{0,-8}", role) + $"{task.Status} · {outcome}");
            }

            lines.Add(string.Empty);
            lines.Add(controller.Phase == InvestigationPhase.Investigating
                ? App.Messages.InvestigatingBody(controller.RootCauseNodeId)
                : $"Phase: {controller.Phase}. Findings are held by the investigation workspace.");

            Panel.SetBody(string.Join(Environment.NewLine, lines));
            Panel.SetStatusLine(App.StatusLine);
            SetContent();

            AnnounceFinishedTasks(orchestrator, byRole);
        }

        /// <summary>
        /// One task per role for the active incident. The orchestrator keys tasks by
        /// id and is idempotent per (incident, role), so replaying a scenario builds
        /// a fresh orchestrator rather than overwriting history here.
        /// </summary>
        private static Dictionary<AgentRole, AgentTask> LatestTasksByRole(
            AgentOrchestrator orchestrator, string incidentId)
        {
            var byRole = new Dictionary<AgentRole, AgentTask>();
            if (orchestrator == null) return byRole;

            foreach (var task in orchestrator.Tasks.Values)
            {
                if (!BelongsToIncident(task, incidentId)) continue;
                if (byRole.TryGetValue(task.Role, out var current) && current.DispatchedAt > task.DispatchedAt)
                    continue;
                byRole[task.Role] = task;
            }

            return byRole;
        }

        private static bool BelongsToIncident(AgentTask task, string incidentId)
        {
            if (string.IsNullOrEmpty(incidentId) || task.Context == null) return true;
            if (!task.Context.TryGetValue("incidentId", out var value) || value == null) return true;
            return string.Equals(value.ToString(), incidentId, StringComparison.Ordinal);
        }

        private static List<AgentFinding> FindingsOf(AgentOrchestrator orchestrator, AgentTask task)
        {
            var findings = new List<AgentFinding>();
            if (orchestrator == null || task.FindingIds == null) return findings;

            foreach (var findingId in task.FindingIds)
                if (orchestrator.Findings.TryGetValue(findingId.ToString(), out var finding) && finding != null)
                    findings.Add(finding);

            return findings;
        }

        private void AnnounceFinishedTasks(AgentOrchestrator orchestrator,
            Dictionary<AgentRole, AgentTask> byRole)
        {
            if (!ReferenceEquals(orchestrator, _announcedOrchestrator))
            {
                _announcedOrchestrator = orchestrator;
                _announcedTasks.Clear();
            }

            if (orchestrator == null) return;

            foreach (var task in byRole.Values)
            {
                if (!IsFinished(task.Status)) continue;
                if (!_announcedTasks.Add(task.TaskId.ToString())) continue;
                AgentTaskCompleted?.Invoke(this, EventArgs.Empty);
            }
        }

        private static bool IsFinished(AgentTaskStatus status) =>
            status == AgentTaskStatus.Completed ||
            status == AgentTaskStatus.Failed ||
            status == AgentTaskStatus.Cancelled ||
            status == AgentTaskStatus.TimedOut;
    }
}
