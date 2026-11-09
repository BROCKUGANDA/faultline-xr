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
    /// PRD §6.1 screen 8 — Service inspection. A contextual inspector for the
    /// node selected in the infrastructure field: every line is read back from
    /// the simulation entity and the graph query view, evidence availability is
    /// counted from real telemetry / log / trace records, and <see cref="FocusTargetId"/>
    /// carries the node id so the rest of the journey inherits the focus.
    /// </summary>
    public sealed class ServiceInspectionScreenView : ScreenViewBase, ServiceInspectionScreen
    {
        private string _nodeId;
        private FlButton _evidenceButton;
        private FlButton _agentButton;

        public override string ScreenId => ScreenIds.ServiceInspection;
        public override string Title => "Service inspection";
        public override string BackScreenId => ScreenIds.InfrastructureOverview;
        public override string EntryConditions =>
            "A service node selected in the infrastructure overview or pinched in the spatial field.";
        public override string ExitConditions =>
            "Inspection closed, or evidence / an agent dispatched for this node.";
        public override string InteractionModel =>
            "Read the inspector, then pinch Inspect evidence to open the evidence explorer " +
            "or Dispatch agent to open the agent panel.";
        public override string AccessibleDescription =>
            "Service inspection screen. Announces the selected service, its type, health percentage, " +
            "status word, latency, error rate, throughput, its dependencies and dependents, " +
            "and whether evidence exists.";

        public string NodeId => _nodeId;
        public bool EvidenceAvailable { get; private set; }

        public event EventHandler<NodeInspectedEventArgs> NodeInspected;
        public event EventHandler InspectEvidenceRequested;
        public event EventHandler DispatchAgentRequested;

        protected override FlPanel BuildPanel() =>
            CreatePanelInZone("Service inspection", SpatialZone.Right, 680f, 640f);

        protected override SpatialZone ZoneForScreen() => SpatialZone.Right;

        protected override bool EvaluateCanEnter() => ResolveNodeId() != null;

        protected override void BuildContent(FlPanel panel)
        {
            _evidenceButton = panel.AddButton("inspection-evidence", "Inspect evidence",
                "Open the evidence explorer for this service", OnInspectEvidence);
            _agentButton = panel.AddButton("inspection-agent", "Dispatch agent",
                "Open the agent control panel to dispatch an analyst for this service", OnDispatchAgent);

            App.RegisterCriticalAction("inspection-evidence", "ServiceInspection.InspectEvidence");

            App.Changed += (_, _) => Refresh();
            Refresh();
        }

        protected override void OnEntered()
        {
            Refresh();
            var nodeId = _nodeId;
            if (!string.IsNullOrEmpty(nodeId))
                NodeInspected?.Invoke(this, new NodeInspectedEventArgs(nodeId));
        }

        private void OnInspectEvidence()
        {
            if (App.IsBusy || string.IsNullOrEmpty(_nodeId) || !EvidenceAvailable) return;
            InspectEvidenceRequested?.Invoke(this, EventArgs.Empty);
            App.Navigator.NavigateToScreen(ScreenIds.EvidenceExplorer);
        }

        private void OnDispatchAgent()
        {
            if (App.IsBusy || string.IsNullOrEmpty(_nodeId)) return;
            DispatchAgentRequested?.Invoke(this, EventArgs.Empty);
            App.Navigator.NavigateToScreen(ScreenIds.AgentControlPanel);
        }

        private void Refresh()
        {
            var app = App;
            var panel = Panel;
            if (app == null || panel == null) return;

            var nodeId = ResolveNodeId();
            var engine = app.Controller?.Engine;

            if (nodeId == null || engine == null)
            {
                EvidenceAvailable = false;
                SetEmpty("No service node is selected. Pinch a node in the infrastructure field " +
                        "or a row on the infrastructure overview.");
                SetButtons(false, false);
                return;
            }

            if (!string.Equals(nodeId, _nodeId, StringComparison.Ordinal))
            {
                _nodeId = nodeId;
                FocusTargetId = nodeId;
                NodeInspected?.Invoke(this, new NodeInspectedEventArgs(nodeId));
            }

            if (!engine.Nodes.TryGetValue(nodeId, out var node))
            {
                var missing = engine.GraphView?.NodeExists(nodeId);
                EvidenceAvailable = false;
                SetError(missing != null && !missing.IsSuccess
                        ? missing.ErrorCode
                        : "FL-GR-0043",
                    $"{nodeId} is no longer part of the running simulation. Go back to the overview " +
                    "and pick another service.",
                    canRetry: true);
                SetButtons(false, false);
                return;
            }

            var graph = engine.GraphView;
            var dependencies = graph != null ? graph.GetDependencies(nodeId) : new List<string>();
            var dependents = graph != null ? graph.GetDependents(nodeId) : new List<string>();

            var metricCount = CountMetrics(engine.Telemetry, nodeId);
            var logCount = CountLogs(engine.Logs, nodeId);
            var traceCount = CountTraces(engine.Traces, nodeId);
            EvidenceAvailable = metricCount + logCount + traceCount > 0;

            var copy = new StringBuilder();
            copy.AppendLine($"Service: {node.DisplayName} · type {node.ServiceType}");
            copy.AppendLine($"Health {node.HealthScore:P0} · {StatusLabel(node)} · status {node.Status}");
            copy.AppendLine($"Latency {node.Latency.TotalMilliseconds:F0} ms · errors {node.ErrorRate:P1}" +
                            $" · throughput {node.Throughput:F0} rps");
            copy.AppendLine($"Depends on: {Describe(dependencies)}");
            copy.AppendLine($"Depended on by: {Describe(dependents)}");
            copy.AppendLine($"Evidence: {metricCount} metric, {logCount} log, {traceCount} trace records");
            if (node.ActiveAlerts.Count > 0)
                copy.AppendLine($"Active alerts: {string.Join(", ", node.ActiveAlerts)}");
            if (node.RecentEvents.Count > 0)
                copy.AppendLine($"Recent events: {string.Join(", ", node.RecentEvents)}");

            panel.SetBody(copy.ToString());
            panel.SetStatusLine(BuildStatusLine(app, node));
            SetContent();

            SetButtons(!app.IsBusy && EvidenceAvailable, !app.IsBusy);
            if (_evidenceButton != null)
                _evidenceButton.Label = EvidenceAvailable ? "Inspect evidence" : "Inspect evidence · no data";
        }

        private void SetButtons(bool evidence, bool agent)
        {
            if (_evidenceButton != null) _evidenceButton.Interactable = evidence;
            if (_agentButton != null) _agentButton.Interactable = agent;
        }

        /// <summary>
        /// Shares the app root's domain status line while it is populated; the
        /// tick loop is the only writer, so a cold session falls back to the
        /// node's own health and status word.
        /// </summary>
        private static string BuildStatusLine(FaultlineUnityApp app, ServiceNode node)
        {
            if (app.IsBusy) return app.BusyStatus;
            var shared = app.StatusLine;
            return string.IsNullOrEmpty(shared)
                ? $"{node.NodeId} · health {node.HealthScore:P0} · {StatusLabel(node)}"
                : shared;
        }

        /// <summary>
        /// Prefers the node the spatial field marks as selected, then the node
        /// currently under the hand ray, then whatever focus the screen was
        /// opened with.
        /// </summary>
        private string ResolveNodeId()
        {
            var app = App;
            if (app == null) return null;

            var topology = app.Topology;
            if (topology != null)
            {
                foreach (var view in topology.NodeViews)
                {
                    if (view != null && view.CurrentState == VisualState.Selected) return view.NodeId;
                }
                if (!string.IsNullOrEmpty(topology.HoveredNodeId)) return topology.HoveredNodeId;
            }

            if (!string.IsNullOrEmpty(FocusTargetId)) return FocusTargetId;
            return _nodeId;
        }

        private string Describe(IReadOnlyList<string> nodeIds)
        {
            if (nodeIds == null || nodeIds.Count == 0) return "none";
            var engine = App?.Controller?.Engine;
            var parts = new List<string>(nodeIds.Count);
            foreach (var id in nodeIds)
            {
                if (engine != null && engine.Nodes.TryGetValue(id, out var neighbour))
                    parts.Add($"{id} ({StatusLabel(neighbour)})");
                else
                    parts.Add(id);
            }
            return string.Join(", ", parts);
        }

        private static string StatusLabel(ServiceNode node) =>
            FaultlinePalette.StatusLabel(node.HealthScore, node.Status == OperationalStatus.Recovering);

        private static int CountMetrics(IReadOnlyList<TelemetryEvent> events, string nodeId)
        {
            var count = 0;
            foreach (var item in events)
                if (string.Equals(item.ServiceId, nodeId, StringComparison.Ordinal)) count++;
            return count;
        }

        private static int CountLogs(IReadOnlyList<LogEvent> events, string nodeId)
        {
            var count = 0;
            foreach (var item in events)
                if (string.Equals(item.ServiceId, nodeId, StringComparison.Ordinal)) count++;
            return count;
        }

        private static int CountTraces(IReadOnlyList<TraceEvent> events, string nodeId)
        {
            var count = 0;
            foreach (var item in events)
                if (string.Equals(item.ServiceId, nodeId, StringComparison.Ordinal)) count++;
            return count;
        }
    }
}
