namespace Faultline.Runtime.UI.Screens
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    using UnityEngine;

    using Faultline.App;
    using Faultline.Domain;
    using Faultline.Presentation;
    using Faultline.Runtime.UI;

    /// <summary>
    /// PRD §6.1 screen 12 — evidence explorer. Only events that analyst findings
    /// actually cite are listed, and each row is resolved against the simulation's
    /// telemetry, log and trace stores; an id that resolves to nothing is reported as
    /// unresolved instead of being shown as a fabricated row.
    /// </summary>
    public sealed class EvidenceExplorerScreenView : ScreenViewBase, EvidenceExplorerScreen
    {
        /// <summary>Row budget: each row is a real pinch target, so the rail stays reachable.</summary>
        private const int MaxRows = 5;

        private readonly Dictionary<string, FlButton> _rowButtons = new(StringComparer.Ordinal);

        private string _selectedEvidenceId;
        private bool _inspectShown;

        public override string ScreenId => ScreenIds.EvidenceExplorer;
        public override string Title => "Evidence explorer";
        public override string BackScreenId => ScreenIds.InvestigationWorkspace;
        public override string EntryConditions =>
            "At least one analyst finding must reference evidence events.";
        public override string ExitConditions =>
            "The selected evidence is inspected, or the explorer is closed.";
        public override string InteractionModel =>
            "Pinch an evidence row to select it, then pinch Inspect to read the full event and " +
            "the findings that cite it.";
        public override string AccessibleDescription =>
            "Evidence explorer. Lists the telemetry metrics, log lines and trace spans the analyst " +
            "findings cite, with source service, value and units.";

        public string IncidentId => App?.Controller?.IncidentId;

        public string SelectedEvidenceId => _selectedEvidenceId;

        public event EventHandler<EvidenceSelectedEventArgs> EvidenceSelected;
        public event EventHandler InspectEvidenceRequested;

        protected override bool EvaluateCanEnter()
        {
            return BuildRows().Count > 0;
        }

        protected override SpatialZone ZoneForScreen() => SpatialZone.Left;

        protected override FlPanel BuildPanel() =>
            CreatePanelInZone("EVIDENCE EXPLORER", SpatialZone.Left, 680f, 620f);

        protected override void BuildContent(FlPanel panel)
        {
            _ = panel.AddButton("evidence-inspect", "Inspect selected evidence",
                "Read the full event record and the findings that cite it",
                OnInspect, interactable: false);

            _ = panel.AddButton("evidence-back", "Back", "Return to the investigation workspace",
                () => App.Navigator.NavigateToScreen(ScreenIds.InvestigationWorkspace));

            App.RegisterCriticalAction("inspect-evidence", "EvidenceExplorer.Inspect");

            App.Changed += (_, _) => Refresh();
        }

        protected override void OnEntered() => Refresh();

        protected override void OnExited()
        {
            _inspectShown = false;
        }

        private void OnSelect(string evidenceId)
        {
            _selectedEvidenceId = evidenceId;
            FocusTargetId = evidenceId;
            _inspectShown = false;
            EvidenceSelected?.Invoke(this, new EvidenceSelectedEventArgs(evidenceId));
            Panel?.SetStatusLine("Evidence " + FaultlineMessages.ShortId(evidenceId) +
                                 " selected · pinch Inspect to read the record");
            Refresh();
        }

        private void OnInspect()
        {
            if (string.IsNullOrEmpty(_selectedEvidenceId)) return;
            InspectEvidenceRequested?.Invoke(this, EventArgs.Empty);
            _inspectShown = true;
            Refresh();
        }

        private void Refresh()
        {
            if (!isActiveAndEnabled || Panel == null) return;

            var rows = BuildRows();
            SyncRowButtons(rows);

            var resolved = rows.Count;
            var hidden = Math.Max(0, resolved - MaxRows) + _unresolvedCount;
            var shown = Math.Min(resolved, MaxRows);

            if (resolved == 0)
            {
                SetEmpty(_unresolvedCount > 0
                    ? $"No cited evidence id could be found in the simulated telemetry, log or trace " +
                      $"stores ({_unresolvedCount} id(s) unresolved). Nothing is shown rather than guessed."
                    : "No analyst finding has cited evidence yet. Dispatch the agents from the analyst " +
                      "panel — evidence appears only when a finding references a real simulated event.");
                return;
            }

            var selected = rows.FirstOrDefault(row =>
                string.Equals(row.EvidenceId, _selectedEvidenceId, StringComparison.Ordinal));

            if (selected == null && _selectedEvidenceId != null)
            {
                _selectedEvidenceId = null;
                FocusTargetId = null;
                _inspectShown = false;
            }

            var lines = new List<string>
            {
                $"Incident {FaultlineMessages.ShortId(IncidentId)} · " +
                $"{TotalCitedCount} event id(s) cited by findings, {resolved} resolved below" +
                (_unresolvedCount > 0 ? $", {_unresolvedCount} unresolved" : string.Empty),
                string.Empty
            };

            foreach (var row in rows.Take(shown))
                lines.Add(row.Summary + (string.Equals(row.EvidenceId, _selectedEvidenceId,
                    StringComparison.Ordinal) ? "  [selected]" : string.Empty));

            if (selected != null)
            {
                lines.Add(string.Empty);
                lines.Add(_inspectShown
                    ? selected.Inspection
                    : "Pinch Inspect to read the full record and the findings that cite this event.");
            }

            if (hidden > 0)
            {
                lines.Add(string.Empty);
                lines.Add($"{hidden} cited event(s) are not shown as rows on this panel.");
            }

            Panel.SetBody(string.Join(Environment.NewLine, lines));
            Panel.SetStatusLine(App.StatusLine);
            SetContent();

            if (Panel.TryGetButton("evidence-inspect", out var inspect))
                inspect.Interactable = selected != null;

            foreach (var buttonId in _rowButtons.Keys)
                if (Panel.TryGetButton(buttonId, out var rowButton))
                    rowButton.Interactable = true;
        }

        private void SyncRowButtons(List<EvidenceRow> rows)
        {
            var wanted = rows.Take(MaxRows).ToList();

            var stale = new List<string>();
            foreach (var buttonId in _rowButtons.Keys)
                if (wanted.All(row => row.ButtonId != buttonId)) stale.Add(buttonId);
            foreach (var buttonId in stale)
            {
                Panel.RemoveButton(buttonId);
                _rowButtons.Remove(buttonId);
            }

            foreach (var row in wanted)
            {
                if (!_rowButtons.TryGetValue(row.ButtonId, out var button))
                {
                    var evidenceId = row.EvidenceId;
                    button = Panel.AddButton(row.ButtonId, row.Label, row.Accessible,
                        () => OnSelect(evidenceId));
                    _rowButtons[row.ButtonId] = button;
                }
                else
                {
                    button.Label = row.Label;
                    button.AccessibleDescription = row.Accessible;
                }
            }
        }

        private int TotalCitedCount { get; set; }

        private int _unresolvedCount;

        /// <summary>
        /// Rows are built from the findings the domain recorded and resolved against
        /// the engine's real event stores. Called outside the panel path too, because
        /// <see cref="EvaluateCanEnter"/> needs the same truth.
        /// </summary>
        private List<EvidenceRow> BuildRows()
        {
            TotalCitedCount = 0;
            _unresolvedCount = 0;

            var rows = new List<EvidenceRow>();
            var controller = App?.Controller;
            var engine = controller?.Engine;
            if (engine == null) return rows;

            var findings = FindingsOf(controller);
            var citedOrder = new List<Guid>();
            var citingRoles = new Dictionary<Guid, List<AgentRole>>();

            foreach (var finding in findings)
            {
                if (!TryRoleOf(controller, finding, out var role)) continue;
                foreach (var evidenceId in finding.EvidenceIds)
                {
                    if (!citingRoles.TryGetValue(evidenceId, out var list))
                    {
                        list = new List<AgentRole>();
                        citingRoles[evidenceId] = list;
                        citedOrder.Add(evidenceId);
                    }

                    if (!list.Contains(role)) list.Add(role);
                }
            }

            TotalCitedCount = citedOrder.Count;

            foreach (var evidenceId in citedOrder)
            {
                var row = Resolve(engine, evidenceId, citingRoles[evidenceId], findings);
                if (row == null)
                {
                    _unresolvedCount++;
                    continue;
                }

                rows.Add(row);
            }

            return rows;
        }

        private static IEnumerable<AgentFinding> FindingsOf(InvestigationController controller)
        {
            IEnumerable<AgentFinding> findings = controller?.Report?.Findings;
            if (findings != null) return findings;

            var recorded = controller?.Orchestrator?.Findings;
            return recorded == null
                ? Enumerable.Empty<AgentFinding>()
                : recorded.Values;
        }

        private static bool TryRoleOf(InvestigationController controller, AgentFinding finding,
            out AgentRole role)
        {
            role = default;
            var orchestrator = controller?.Orchestrator;
            if (orchestrator == null) return false;

            if (orchestrator.Tasks.TryGetValue(finding.TaskId.ToString(), out var task) && task != null)
            {
                role = task.Role;
                return true;
            }

            return false;
        }

        private static EvidenceRow Resolve(Faultline.Incidents.SimulationEngine engine, Guid evidenceId,
            List<AgentRole> citingRoles, IEnumerable<AgentFinding> findings)
        {
            var citedBy = string.Join(", ", citingRoles);

            var telemetry = engine.Telemetry.FirstOrDefault(e => e.EventId == evidenceId);
            if (telemetry != null)
            {
                var value = telemetry.MetricValue.HasValue
                    ? FormatMetric(telemetry.MetricName, telemetry.MetricValue.Value)
                    : telemetry.Message ?? "no value recorded";

                return new EvidenceRow
                {
                    EvidenceId = evidenceId.ToString(),
                    ButtonId = "evidence-" + evidenceId.ToString("N"),
                    Label = $"Metric · {telemetry.ServiceId}",
                    Accessible = $"Telemetry metric from {telemetry.ServiceId}: {value}, " +
                                 $"severity {telemetry.Severity}, taken at " +
                                 $"{telemetry.Timestamp:HH:mm} UTC, cited by {citedBy}",
                    Summary = $"[metric] {telemetry.ServiceId} · {telemetry.MetricName ?? "unspecified"} " +
                              $"= {value} · {telemetry.Severity}",
                    Inspection = $"TELEMETRY METRIC {FaultlineMessages.ShortId(evidenceId.ToString())}\n" +
                                 $"  service: {telemetry.ServiceId}\n" +
                                 $"  metric: {telemetry.MetricName ?? "unspecified"}\n" +
                                 $"  value: {value}\n" +
                                 $"  severity: {telemetry.Severity}\n" +
                                 $"  time (UTC): {telemetry.Timestamp.ToString("HH:mm:ss", CultureInfo.InvariantCulture)}\n" +
                                 $"  message: {telemetry.Message}\n" +
                                 CitingFindings(findings, evidenceId)
                };
            }

            var log = engine.Logs.FirstOrDefault(e => e.EventId == evidenceId);
            if (log != null)
            {
                return new EvidenceRow
                {
                    EvidenceId = evidenceId.ToString(),
                    ButtonId = "evidence-" + evidenceId.ToString("N"),
                    Label = $"Log line · {log.ServiceId}",
                    Accessible = $"Log line from {log.ServiceId} at {log.Timestamp:HH:mm} UTC: " +
                                 $"{log.Message}, level {log.Level}",
                    Summary = $"[log] {log.ServiceId} · {log.Level} · {log.Message}",
                    Inspection = $"LOG LINE {FaultlineMessages.ShortId(evidenceId.ToString())}\n" +
                                 $"  service: {log.ServiceId}\n" +
                                 $"  level: {log.Level}\n" +
                                 $"  time (UTC): {log.Timestamp.ToString("HH:mm:ss", CultureInfo.InvariantCulture)}\n" +
                                 $"  message: {log.Message}\n" +
                                 CitingFindings(findings, evidenceId)
                };
            }

            var trace = engine.Traces.FirstOrDefault(e => e.EventId == evidenceId);
            if (trace != null)
            {
                return new EvidenceRow
                {
                    EvidenceId = evidenceId.ToString(),
                    ButtonId = "evidence-" + evidenceId.ToString("N"),
                    Label = $"Trace · {trace.ServiceId}",
                    Accessible = $"Trace span {trace.Operation} on {trace.ServiceId} at " +
                                 $"{trace.Timestamp:HH:mm} UTC, {trace.Duration.TotalMilliseconds:F0} ms",
                    Summary = $"[trace] {trace.ServiceId} · {trace.Operation} · " +
                              $"{trace.Duration.TotalMilliseconds:F0} ms · {trace.Severity}",
                    Inspection = $"TRACE SPAN {FaultlineMessages.ShortId(evidenceId.ToString())}\n" +
                                 $"  service: {trace.ServiceId}\n" +
                                 $"  operation: {trace.Operation}\n" +
                                 $"  trace/span: {trace.TraceId}/{trace.SpanId}\n" +
                                 $"  duration: {trace.Duration.TotalMilliseconds:F0} ms\n" +
                                 $"  severity: {trace.Severity}\n" +
                                 $"  time (UTC): {trace.Timestamp.ToString("HH:mm:ss", CultureInfo.InvariantCulture)}\n" +
                                 CitingFindings(findings, evidenceId)
                };
            }

            return null;
        }

        private static string CitingFindings(IEnumerable<AgentFinding> findings, Guid evidenceId)
        {
            var lines = findings
                .Where(finding => finding.EvidenceIds.Contains(evidenceId))
                .Select(finding => $"  cited by {finding.Confidence:P0}: {finding.Finding}")
                .ToList();

            return lines.Count == 0
                ? "  cited by: no recorded finding references this event."
                : "  cited by:\n" + string.Join("\n", lines);
        }

        private static string FormatMetric(string metricName, double value) => metricName switch
        {
            "latency_ms" => $"{value:F0} ms",
            "error_rate" => $"{value:P1} of requests",
            "throughput_rps" => $"{value:F1} rps",
            "recovery_applied" => $"{value:F2} recovery event",
            _ => value.ToString("F3", CultureInfo.InvariantCulture) +
                 " " + (metricName ?? "unspecified metric")
        };

        private sealed class EvidenceRow
        {
            public string EvidenceId;
            public string ButtonId;
            public string Label;
            public string Accessible;
            public string Summary;
            public string Inspection;
        }
    }
}
