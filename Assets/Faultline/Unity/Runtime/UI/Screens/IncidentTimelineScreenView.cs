namespace Faultline.Runtime.UI.Screens
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    using UnityEngine;

    using Faultline.Domain;
    using Faultline.Presentation;
    using Faultline.Runtime.UI;

    /// <summary>
    /// PRD §6.1 screen 18 — Incident timeline. Renders the ordered causal
    /// events that actually exist: the identifiers recorded on the incident
    /// entity plus the telemetry / log / trace records whose service id sits in
    /// the incident's dependency scope. Filters re-build the row set, so a trace
    /// filter on a run without spans honestly shows nothing rather than a
    /// placeholder, and <see cref="EventCount"/> is the number of rows rendered.
    /// </summary>
    public sealed class IncidentTimelineScreenView : ScreenViewBase, IncidentTimelineScreen
    {
        /// <summary>Row buttons are hand targets, so the list is paged to what fits the panel.</summary>
        private const int MaxRows = 5;
        private const string RowButtonPrefix = "timeline-event-";

        private const string FilterAll = "all";
        private const string FilterMetrics = "metrics";
        private const string FilterLogs = "logs";
        private const string FilterTraces = "traces";

        private readonly List<string> _buttonIds = new();
        private readonly Dictionary<string, FlButton> _rowButtons = new(StringComparer.Ordinal);

        private string _filter = FilterAll;

        /// <summary>Null until the first refresh, which forces the button stack to be built.</summary>
        private string _rowSignature;
        private string _selectedEventId;
        private int _totalCandidateCount;
        private FlButton _allButton;
        private FlButton _metricsButton;
        private FlButton _logsButton;
        private FlButton _tracesButton;

        public override string ScreenId => ScreenIds.IncidentTimeline;
        public override string Title => "Incident timeline";
        public override string BackScreenId => ScreenIds.IncidentDetail;
        public override string EntryConditions =>
            "An active incident; correlated events appear as the simulation ticks.";
        public override string ExitConditions => "Timeline closed.";
        public override string InteractionModel =>
            "Pinch an event row to inspect it, pinch a filter to change the event kind.";
        public override string AccessibleDescription =>
            "Incident timeline screen. Announces each causal event in time order with its kind, " +
            "timestamp and message, then the active filter and how many rows are shown.";

        public string IncidentId => App?.Controller?.IncidentId;
        public int EventCount { get; private set; }

        public event EventHandler<IncidentIdEventArgs> TimelineOpened;
        public event EventHandler<TimelineEventSelectedEventArgs> EventSelected;
        public event EventHandler TimelineFilterChanged;

        protected override FlPanel BuildPanel() =>
            CreatePanelInZone("Incident timeline", SpatialZone.Left, 720f, 900f);

        protected override SpatialZone ZoneForScreen() => SpatialZone.Left;

        protected override bool EvaluateCanEnter() => !string.IsNullOrEmpty(IncidentId);

        protected override void BuildContent(FlPanel panel)
        {
            // Build the stack first so the filter actions exist even before the
            // simulation has correlated any event to the incident.
            var initial = BuildRows();
            RebuildButtons(panel, initial);
            _rowSignature = Signature(initial);

            App.Changed += (_, _) => Refresh();
            Refresh();
        }

        protected override void OnEntered()
        {
            Refresh();
            var incidentId = IncidentId;
            if (!string.IsNullOrEmpty(incidentId))
                TimelineOpened?.Invoke(this, new IncidentIdEventArgs(incidentId));
        }

        private void ApplyFilter(string filter)
        {
            if (string.Equals(_filter, filter, StringComparison.Ordinal)) return;
            _filter = filter;
            TimelineFilterChanged?.Invoke(this, EventArgs.Empty);
            Refresh();
        }

        private void OnEventRow(string eventId)
        {
            _selectedEventId = eventId;
            FocusTargetId = eventId;
            EventSelected?.Invoke(this, new TimelineEventSelectedEventArgs(eventId));
            Refresh();
        }

        private void Refresh()
        {
            var app = App;
            var panel = Panel;
            if (app == null || panel == null) return;

            if (string.IsNullOrEmpty(app.Controller?.IncidentId))
            {
                EventCount = 0;
                SetEmpty("No incident is active, so there is no timeline to read.");
                return;
            }

            var rows = BuildRows();
            var signature = Signature(rows);
            if (!string.Equals(signature, _rowSignature, StringComparison.Ordinal))
            {
                RebuildButtons(panel, rows);
                _rowSignature = signature;
            }

            EventCount = rows.Count;

            foreach (var row in rows)
            {
                if (_rowButtons.TryGetValue(row.Key, out var button))
                {
                    button.Label = RowLabel(row);
                    button.AccessibleDescription = $"{row.Kind} event at {row.Timestamp:HH:mm:ss}. {row.Summary}";
                    button.SetState(string.Equals(row.Key, _selectedEventId, StringComparison.Ordinal)
                        ? VisualState.Selected : VisualState.Normal);
                }
            }

            var copy = new StringBuilder();
            copy.AppendLine($"Incident {FaultlineMessages.ShortId(app.Controller.IncidentId)} · filter: {_filter}");
            copy.AppendLine(rows.Count == 0
                ? $"No {_filter} events correlated yet — the simulation has not emitted any."
                : $"Showing the earliest {rows.Count} of {_totalCandidateCount} correlated events.");
            copy.Append(string.IsNullOrEmpty(_selectedEventId)
                ? "Pinch an event row to inspect it."
                : $"Selected event {_selectedEventId}");
            panel.SetBody(copy.ToString());

            panel.SetStatusLine(app.IsBusy
                ? app.BusyStatus
                : $"{EventCount} events shown · {_totalCandidateCount} correlated · tick {app.Controller.Engine?.Tick ?? 0}");

            SetFilterState(app.IsBusy);
            SetContent();
        }

        /// <summary>
        /// Filtering changes the rendered rows, so the whole stack is rebuilt in
        /// canonical order — the panel stacks buttons bottom-up by insertion
        /// order and the filter actions must stay below the list.
        /// </summary>
        private void RebuildButtons(FlPanel panel, IReadOnlyList<TimelineRow> rows)
        {
            foreach (var id in _buttonIds) panel.RemoveButton(id);
            _buttonIds.Clear();
            _rowButtons.Clear();

            foreach (var row in rows)
            {
                var captured = row;
                var button = panel.AddButton(RowButtonId(captured.Key), RowLabel(captured),
                    $"{captured.Kind} event at {captured.Timestamp:HH:mm:ss}. {captured.Summary}",
                    () => OnEventRow(captured.Key));
                _rowButtons[captured.Key] = button;
                _buttonIds.Add(RowButtonId(captured.Key));
            }

            _allButton = AddFilter(panel, "timeline-filter-all", "All events", FilterAll);
            _metricsButton = AddFilter(panel, "timeline-filter-metrics", "Metrics only", FilterMetrics);
            _logsButton = AddFilter(panel, "timeline-filter-logs", "Logs only", FilterLogs);
            _tracesButton = AddFilter(panel, "timeline-filter-traces", "Traces only", FilterTraces);
        }

        private FlButton AddFilter(FlPanel panel, string buttonId, string label, string filter)
        {
            var button = panel.AddButton(buttonId, label,
                $"Show only {filter} events on the timeline", () => ApplyFilter(filter));
            _buttonIds.Add(buttonId);
            return button;
        }

        private void SetFilterState(bool busy)
        {
            ApplyFilterState(_allButton, FilterAll, busy);
            ApplyFilterState(_metricsButton, FilterMetrics, busy);
            ApplyFilterState(_logsButton, FilterLogs, busy);
            ApplyFilterState(_tracesButton, FilterTraces, busy);
        }

        private void ApplyFilterState(FlButton button, string filter, bool busy)
        {
            if (button == null) return;
            var active = string.Equals(_filter, filter, StringComparison.Ordinal);
            button.SetState(active ? VisualState.Selected : VisualState.Normal);
            button.Interactable = !busy;
        }

        private IReadOnlyList<TimelineRow> BuildRows()
        {
            var rows = new List<TimelineRow>();
            var engine = App?.Controller?.Engine;
            if (engine == null)
            {
                _totalCandidateCount = 0;
                return rows;
            }

            var incident = ReadIncident();
            var scope = BuildScope(engine, incident);
            var linked = new HashSet<Guid>();
            if (incident != null)
                foreach (var eventId in incident.EventIds) linked.Add(eventId);

            foreach (var telemetry in engine.Telemetry)
            {
                if (!Matches(FilterMetrics)) continue;
                if (!InScope(scope, telemetry.ServiceId) && !linked.Contains(telemetry.EventId)) continue;
                rows.Add(new TimelineRow(telemetry.EventId.ToString("D"), telemetry.Timestamp, "METRIC",
                    $"{telemetry.ServiceId} · {telemetry.MetricName} {Format(telemetry.MetricValue)} · " +
                    telemetry.Severity));
            }

            foreach (var log in engine.Logs)
            {
                if (!Matches(FilterLogs)) continue;
                if (!InScope(scope, log.ServiceId) && !linked.Contains(log.EventId)) continue;
                rows.Add(new TimelineRow(log.EventId.ToString("D"), log.Timestamp, "LOG",
                    $"{log.ServiceId} · {log.Level} · {log.Message}"));
            }

            foreach (var trace in engine.Traces)
            {
                if (!Matches(FilterTraces)) continue;
                if (!InScope(scope, trace.ServiceId) && !linked.Contains(trace.EventId)) continue;
                rows.Add(new TimelineRow(trace.EventId.ToString("D"), trace.Timestamp, "TRACE",
                    $"{trace.ServiceId} · {trace.Operation} · {trace.TraceId}/{trace.SpanId} · " +
                    $"{trace.Duration.TotalMilliseconds:F0} ms"));
            }

            rows.Sort((a, b) =>
            {
                var byTime = a.Timestamp.CompareTo(b.Timestamp);
                return byTime != 0 ? byTime : string.CompareOrdinal(a.Key, b.Key);
            });

            _totalCandidateCount = rows.Count;
            if (rows.Count > MaxRows) rows.RemoveRange(MaxRows, rows.Count - MaxRows);
            return rows;
        }

        private Incident ReadIncident()
        {
            var controller = App?.Controller;
            var incidentId = controller?.IncidentId;
            if (string.IsNullOrEmpty(incidentId)) return null;
            var read = controller.App?.Incidents?.GetIncidentEntity(incidentId);
            return read != null && read.IsSuccess ? read.Value : null;
        }

        /// <summary>
        /// Causal scope: the affected node, the services directly attached to it
        /// in the dependency graph, and the root cause the scenario started from.
        /// </summary>
        private static HashSet<string> BuildScope(Faultline.Incidents.SimulationEngine engine, Incident incident)
        {
            var scope = new HashSet<string>(StringComparer.Ordinal);
            var focus = incident?.NodeId ?? engine.RootCauseNodeId;
            if (!string.IsNullOrEmpty(focus)) scope.Add(focus);
            if (engine.RootCauseNodeId != null) scope.Add(engine.RootCauseNodeId);
            if (incident == null || string.IsNullOrEmpty(focus)) return scope;

            var graph = engine.GraphView;
            if (graph == null) return scope;
            foreach (var dependent in graph.GetDependents(focus)) scope.Add(dependent);
            foreach (var dependency in graph.GetDependencies(focus)) scope.Add(dependency);
            return scope;
        }

        private bool Matches(string kind) =>
            string.Equals(_filter, FilterAll, StringComparison.Ordinal) ||
            string.Equals(_filter, kind, StringComparison.Ordinal);

        private static bool InScope(HashSet<string> scope, string serviceId) =>
            !string.IsNullOrEmpty(serviceId) && scope.Contains(serviceId);

        private static string Format(double? value) =>
            value.HasValue ? value.Value.ToString("F2") : "no value";

        private static string RowButtonId(string eventId) => RowButtonPrefix + eventId;

        private static string RowLabel(TimelineRow row) =>
            $"{row.Timestamp:HH:mm:ss} · {row.Kind} · {Shorten(row.Summary)}";

        private static string Shorten(string summary) =>
            summary.Length <= 46 ? summary : summary.Substring(0, 43) + "…";

        private static string Signature(IReadOnlyList<TimelineRow> rows)
        {
            var builder = new StringBuilder();
            foreach (var row in rows) builder.Append(row.Key).Append('|');
            return builder.ToString();
        }

        /// <summary>One rendered causal event, whatever domain collection it came from.</summary>
        private sealed class TimelineRow
        {
            public TimelineRow(string key, DateTimeOffset timestamp, string kind, string summary)
            {
                Key = key;
                Timestamp = timestamp;
                Kind = kind;
                Summary = summary;
            }

            public string Key { get; }
            public DateTimeOffset Timestamp { get; }
            public string Kind { get; }
            public string Summary { get; }
        }
    }
}
