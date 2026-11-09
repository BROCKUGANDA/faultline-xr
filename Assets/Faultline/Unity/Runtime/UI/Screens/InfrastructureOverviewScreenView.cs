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
    /// PRD §6.1 screen 7 — Infrastructure overview. One hand-target per service
    /// node, labelled with the node's live health percentage and the non-colour
    /// status word from <see cref="FaultlinePalette.StatusLabel"/>, so status is
    /// never carried by hue alone. The filter genuinely re-builds the row set,
    /// and selection is published through the application root so the spatial
    /// field and this list always agree.
    /// </summary>
    public sealed class InfrastructureOverviewScreenView : ScreenViewBase, InfrastructureOverviewScreen
    {
        /// <summary>Row labels and panel height are sized for the CASCADE field; longer graphs page.</summary>
        private const int MaxRows = 7;
        private const string RowButtonPrefix = "overview-node-";
        private const string FilterAll = "all";
        private const string FilterUnhealthy = "unhealthy";

        /// <summary>Health below which FaultlinePalette stops reporting HEALTHY.</summary>
        private const double HealthyThreshold = 0.9;

        private readonly List<string> _buttonIds = new();
        private readonly Dictionary<string, FlButton> _rowButtons = new(StringComparer.Ordinal);

        private string _filter = FilterAll;

        /// <summary>Null until the first refresh, which forces the button stack to be built.</summary>
        private string _rowSignature;
        private string _selectedNodeId;
        private FlButton _allButton;
        private FlButton _unhealthyButton;
        private FlButton _openIncidentButton;

        public override string ScreenId => ScreenIds.InfrastructureOverview;
        public override string Title => "Infrastructure overview";
        public override string BackScreenId => ScreenIds.SpatialEnvironment;
        public override string EntryConditions =>
            "Spatial environment configured and a running scenario with service nodes.";
        public override string ExitConditions =>
            "A service node selected, or the incident opened from the selected node.";
        public override string InteractionModel =>
            "Pinch a service row to select it in the spatial field, pinch a filter to narrow the list, " +
            "pinch Open incident for the active incident.";
        public override string AccessibleDescription =>
            "Infrastructure overview screen. Lists every simulated service worst-first with its health " +
            "percentage and a spoken status word. Two filters and an open-incident action.";

        public string SelectedNodeId => FindSelectedNodeId() ?? _selectedNodeId;
        public bool FilterActive => !string.Equals(_filter, FilterAll, StringComparison.Ordinal);

        public event EventHandler<NodeSelectedEventArgs> NodeSelected;
        public event EventHandler<FilterChangedEventArgs> FilterChanged;
        public event EventHandler IncidentOpened;

        protected override FlPanel BuildPanel() =>
            CreatePanelInZone("Infrastructure overview", SpatialZone.Centre, 720f, 900f);

        protected override bool EvaluateCanEnter()
        {
            var app = App;
            var engine = app != null ? app.Controller?.Engine : null;
            return engine != null && engine.Nodes.Count > 0;
        }

        protected override void BuildContent(FlPanel panel)
        {
            // Build the stack first so the filter and open-incident actions
            // exist even before the scenario has produced any service rows.
            var initial = VisibleRows();
            RebuildButtons(panel, initial);
            _rowSignature = Signature(initial);

            App.Changed += (_, _) => Refresh();
            Refresh();
        }

        protected override void OnEntered() => Refresh();

        private void OnNodeRow(string nodeId)
        {
            _selectedNodeId = nodeId;
            FocusTargetId = nodeId;
            App.SelectNode(nodeId);
            NodeSelected?.Invoke(this, new NodeSelectedEventArgs(nodeId));
            Refresh();
        }

        private void ApplyFilter(string filter)
        {
            if (string.Equals(_filter, filter, StringComparison.Ordinal)) return;
            _filter = filter;
            FilterChanged?.Invoke(this, new FilterChangedEventArgs(filter));
            Refresh();
        }

        private void OnOpenIncident()
        {
            var incidentId = App.Controller?.IncidentId;
            if (string.IsNullOrEmpty(incidentId) || App.IsBusy) return;
            IncidentOpened?.Invoke(this, EventArgs.Empty);
            App.Navigator.NavigateToScreen(ScreenIds.IncidentDetail);
        }

        private void Refresh()
        {
            var app = App;
            var panel = Panel;
            if (app == null || panel == null) return;

            var engine = app.Controller?.Engine;
            if (engine == null)
            {
                SetEmpty("Begin the CASCADE scenario to populate the infrastructure field.");
                return;
            }

            var rows = VisibleRows();
            var signature = Signature(rows);
            if (!string.Equals(signature, _rowSignature, StringComparison.Ordinal))
            {
                RebuildButtons(panel, rows);
                _rowSignature = signature;
            }

            foreach (var node in rows)
            {
                if (_rowButtons.TryGetValue(node.NodeId, out var button))
                {
                    button.Label = RowLabel(node);
                    button.AccessibleDescription = RowDescription(node);
                }
            }

            var selected = SelectedNodeId;
            FocusTargetId = selected;
            var incidentId = app.Controller?.IncidentId;

            var unhealthy = CountUnhealthy(engine);
            var copy = new StringBuilder();
            copy.AppendLine($"{rows.Count} of {engine.Nodes.Count} services listed worst-first" +
                            (FilterActive ? $" · filter: {_filter}" : " · filter: all"));
            copy.AppendLine(selected == null
                ? "Pinch a service row to select it in the spatial field."
                : $"Selected: {selected} · health {HealthOf(engine, selected):P0}.");
            panel.SetBody(copy.ToString());

            panel.SetStatusLine(app.IsBusy
                ? app.BusyStatus
                : $"Aggregate health {engine.AggregateHealth:P0} · {unhealthy} service(s) not healthy · " +
                  $"incident {(string.IsNullOrEmpty(incidentId) ? "none" : "active")}");

            if (_allButton != null)
            {
                _allButton.SetState(string.Equals(_filter, FilterAll, StringComparison.Ordinal)
                    ? VisualState.Selected : VisualState.Normal);
                _allButton.Interactable = !app.IsBusy;
            }
            if (_unhealthyButton != null)
            {
                _unhealthyButton.SetState(FilterActive ? VisualState.Selected : VisualState.Normal);
                _unhealthyButton.Interactable = !app.IsBusy;
            }
            if (_openIncidentButton != null)
            {
                _openIncidentButton.Interactable = !app.IsBusy && !string.IsNullOrEmpty(incidentId);
                _openIncidentButton.Label = string.IsNullOrEmpty(incidentId)
                    ? "Open incident · none active"
                    : "Open incident";
            }

            SetContent();
        }

        /// <summary>
        /// Rows are the buttons, so filtering removes and re-adds them. The whole
        /// stack is re-created in canonical order because the panel stacks from
        /// the bottom up in insertion order; appending only the rows would push
        /// the list below the filter actions.
        /// </summary>
        private void RebuildButtons(FlPanel panel, IReadOnlyList<ServiceNode> rows)
        {
            foreach (var id in _buttonIds) panel.RemoveButton(id);
            _buttonIds.Clear();
            _rowButtons.Clear();

            foreach (var node in rows)
            {
                var nodeId = node.NodeId;
                var button = panel.AddButton(RowButtonId(nodeId), RowLabel(node), RowDescription(node),
                    () => OnNodeRow(nodeId));
                _rowButtons[nodeId] = button;
                _buttonIds.Add(RowButtonId(nodeId));
            }

            _allButton = panel.AddButton("overview-filter-all", "Show all services",
                "List every simulated service regardless of health", () => ApplyFilter(FilterAll));
            _buttonIds.Add("overview-filter-all");

            _unhealthyButton = panel.AddButton("overview-filter-unhealthy", "Unhealthy only",
                "List only services reporting below the healthy threshold", () => ApplyFilter(FilterUnhealthy));
            _buttonIds.Add("overview-filter-unhealthy");

            _openIncidentButton = panel.AddButton("overview-open-incident", "Open incident",
                "Open the active incident detail screen", OnOpenIncident, interactable: false);
            _buttonIds.Add("overview-open-incident");
        }

        private IReadOnlyList<ServiceNode> VisibleRows()
        {
            var engine = App?.Controller?.Engine;
            var rows = new List<ServiceNode>();
            if (engine == null) return rows;

            foreach (var pair in engine.Nodes)
            {
                if (string.Equals(_filter, FilterAll, StringComparison.Ordinal) ||
                    IsUnhealthy(pair.Value.HealthScore))
                {
                    rows.Add(pair.Value);
                }
            }

            rows.Sort((a, b) =>
            {
                var byHealth = a.HealthScore.CompareTo(b.HealthScore);
                return byHealth != 0 ? byHealth : string.CompareOrdinal(a.NodeId, b.NodeId);
            });

            if (rows.Count > MaxRows) rows.RemoveRange(MaxRows, rows.Count - MaxRows);
            return rows;
        }

        private static int CountUnhealthy(Faultline.Incidents.SimulationEngine engine)
        {
            var count = 0;
            foreach (var pair in engine.Nodes)
                if (IsUnhealthy(pair.Value.HealthScore)) count++;
            return count;
        }

        private static double HealthOf(Faultline.Incidents.SimulationEngine engine, string nodeId) =>
            engine != null && engine.Nodes.TryGetValue(nodeId, out var node) ? node.HealthScore : 0d;

        private static string RowButtonId(string nodeId) => RowButtonPrefix + nodeId;

        private static string RowLabel(ServiceNode node) =>
            $"{node.DisplayName} · {node.HealthScore:P0} · {StatusLabel(node)}";

        private static string RowDescription(ServiceNode node) =>
            $"{node.DisplayName}, service type {node.ServiceType}, health {node.HealthScore:P0}, " +
            $"status {StatusLabel(node)}. Selects this service in the infrastructure field.";

        private static string StatusLabel(ServiceNode node) =>
            FaultlinePalette.StatusLabel(node.HealthScore, node.Status == OperationalStatus.Recovering);

        private static bool IsUnhealthy(double health) => health < HealthyThreshold;

        private static string Signature(IReadOnlyList<ServiceNode> rows)
        {
            var builder = new StringBuilder();
            foreach (var node in rows) builder.Append(node.NodeId).Append('|');
            return builder.ToString();
        }

        /// <summary>
        /// The spatial field is the source of truth for selection: exactly one
        /// node view carries the selected visual state after a selection command.
        /// </summary>
        private string FindSelectedNodeId()
        {
            var topology = App?.Topology;
            if (topology == null) return null;
            foreach (var view in topology.NodeViews)
            {
                if (view != null && view.CurrentState == VisualState.Selected) return view.NodeId;
            }
            return topology.HoveredNodeId;
        }
    }
}
