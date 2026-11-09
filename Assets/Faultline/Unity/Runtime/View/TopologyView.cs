namespace Faultline.Runtime.View
{
    using System;
    using System.Collections.Generic;

    using UnityEngine;

    using Faultline.App;
    using Faultline.Domain;
    using Faultline.Runtime.UI;
    using Faultline.Runtime.Xr;
    using Faultline.Spatial;

    /// <summary>
    /// Projects the deterministic infrastructure graph into the seated spatial
    /// field. The view subscribes to validated domain state and can never
    /// mutate it: node position comes from <see cref="TopologyLayout"/>, colour
    /// and status word come from the simulation's health, and selection is
    /// published upward as an event the controller turns into a command.
    /// </summary>
    public sealed class TopologyView : MonoBehaviour, IXrRayHitResolver
    {
        private const float HealthRefreshIntervalSeconds = 0.2f;
        private const float MinScale = 0.55f;
        private const float MaxScale = 1.45f;
        private const float MaxYawRadians = 1.1f;
        private const float MaxPitchRadians = 0.55f;

        private readonly Dictionary<string, FlNodeView> _nodeViews = new(StringComparer.Ordinal);
        private readonly Dictionary<string, FlEdgeView> _edgeViews = new(StringComparer.Ordinal);
        private readonly List<FlNodeView> _nodeViewList = new();

        private Transform _group;
        private InvestigationController _controller;
        private SpatialTopology _topology;
        private float _healthTimer;
        private float _yaw;
        private float _pitch;
        private float _scale = 1f;
        private bool _reducedMotion;
        private string _selectedNodeId;
        private FlNodeView _hovered;

        /// <summary>Raised when a hand pinch selects a node (domain command source).</summary>
        public event Action<string> NodeSelectionRequested;

        /// <summary>Raised when the pointer moves off or onto a node.</summary>
        public event Action<string> HoverChanged;

        public IReadOnlyCollection<FlNodeView> NodeViews => _nodeViewList;
        public int NodeCount => _nodeViews.Count;

        /// <summary>Node currently under the pointer ray, or null.</summary>
        public string HoveredNodeId => _hovered?.NodeId;
        public int EdgeCount => _edgeViews.Count;
        public float Scale => _scale;

        public void Initialise(InvestigationController controller, Transform cameraAnchor)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));

            if (_group == null)
            {
                var groupObject = new GameObject("InfrastructureField");
                groupObject.transform.SetParent(transform, false);
                _group = groupObject.transform;
            }

            // Place the field in front of the seated user regardless of whether
            // the runtime reports a floor or device origin.
            var anchor = cameraAnchor != null ? cameraAnchor.position : Vector3.zero;
            var forward = cameraAnchor != null ? cameraAnchor.forward : Vector3.forward;
            _group.position = anchor + forward * 0.52f + Vector3.down * 0.06f;
            _group.localScale = Vector3.one * _scale;
        }

        public void SetReducedMotion(bool reducedMotion) => _reducedMotion = reducedMotion;

        /// <summary>
        /// (Re)build all node and edge views from the spatial projection of the
        /// domain graph. Idempotent: repeated calls destroy and recreate the
        /// projections so replay leaves no stale geometry behind.
        /// </summary>
        public void Rebuild(SpatialTopology topology)
        {
            _topology = topology ?? throw new ArgumentNullException(nameof(topology));

            foreach (var view in _nodeViews.Values) Destroy(view.gameObject);
            foreach (var view in _edgeViews.Values) Destroy(view.gameObject);
            _nodeViews.Clear();
            _edgeViews.Clear();
            _nodeViewList.Clear();
            _hovered = null;
            _selectedNodeId = null;

            foreach (var pair in topology.Nodes)
            {
                var spatial = pair.Value;
                var nodeObject = new GameObject("Node:" + spatial.NodeId);
                nodeObject.transform.SetParent(_group, false);
                nodeObject.transform.position = ToUnity(spatial.Position);

                var view = nodeObject.AddComponent<FlNodeView>();
                view.Build(spatial.NodeId, spatial);
                _nodeViews[spatial.NodeId] = view;
                _nodeViewList.Add(view);
            }

            foreach (var pair in topology.Edges)
            {
                var spatial = pair.Value;
                if (!_nodeViews.ContainsKey(spatial.SourceNodeId) ||
                    !_nodeViews.ContainsKey(spatial.TargetNodeId))
                    continue;

                var edgeObject = new GameObject("Edge:" + spatial.EdgeId);
                edgeObject.transform.SetParent(_group, false);
                var view = edgeObject.AddComponent<FlEdgeView>();
                view.Build(spatial.EdgeId, spatial.SourceNodeId, spatial.TargetNodeId);
                _edgeViews[spatial.EdgeId] = view;
            }

            RefreshHealth(force: true);
        }

        /// <summary>
        /// Remove every projection. Called when the scenario stops so a return
        /// to the welcome flow cannot leave a graph with no domain behind it.
        /// </summary>
        public void ClearField()
        {
            foreach (var view in _nodeViews.Values) Destroy(view.gameObject);
            foreach (var view in _edgeViews.Values) Destroy(view.gameObject);
            _nodeViews.Clear();
            _edgeViews.Clear();
            _nodeViewList.Clear();
            _hovered = null;
            _selectedNodeId = null;
            _topology = null;
        }

        private void Update()
        {
            _healthTimer += Time.deltaTime;
            if (_healthTimer >= HealthRefreshIntervalSeconds)
            {
                _healthTimer = 0f;
                RefreshHealth(force: false);
            }
        }

        /// <summary>
        /// Pull current health from the simulation and push it into the
        /// projections. Called on a throttle so telemetry visualisation never
        /// competes with the XR frame budget.
        /// </summary>
        public void RefreshHealth(bool force)
        {
            if (_controller?.Engine == null || _topology == null) return;


            var nodes = _controller.Engine.Nodes;

            foreach (var pair in _nodeViews)
            {
                if (!nodes.TryGetValue(pair.Key, out var node)) continue;
                var recovering = node.Status == OperationalStatus.Recovering;
                pair.Value.SetHealth(node.HealthScore, recovering, _reducedMotion);
            }

            foreach (var pair in _edgeViews)
            {
                var spatialEdge = _topology.Edges[pair.Key];
                if (!nodes.TryGetValue(spatialEdge.SourceNodeId, out var source) ||
                    !nodes.TryGetValue(spatialEdge.TargetNodeId, out var target))
                    continue;

                if (_nodeViews.TryGetValue(spatialEdge.SourceNodeId, out var sourceView) &&
                    _nodeViews.TryGetValue(spatialEdge.TargetNodeId, out var targetView))
                {
                    var health = Math.Min(source.HealthScore, target.HealthScore);
                    pair.Value.SetEndpoints(sourceView.transform.position, targetView.transform.position,
                        health, _reducedMotion);
                    pair.Value.ApplyHealth(health);
                    pair.Value.SetFlowAnimating(!_reducedMotion && health < 0.9);
                }
            }
        }

        public void SelectNode(string nodeId)
        {
            if (_selectedNodeId == nodeId) return;

            if (_selectedNodeId != null && _nodeViews.TryGetValue(_selectedNodeId, out var previous))
                previous.SetSelectedVisual(false);

            _selectedNodeId = nodeId;

            if (nodeId != null && _nodeViews.TryGetValue(nodeId, out var current))
            {
                current.SetSelectedVisual(true);
                HighlightConnectedEdges(nodeId, true);
            }
        }

        /// <summary>Rotate the field with a clamped two-hand / drag gesture.</summary>
        public void Rotate(float deltaYawRadians, float deltaPitchRadians)
        {
            if (_group == null) return;
            _yaw = Mathf.Clamp(_yaw + deltaYawRadians, -MaxYawRadians, MaxYawRadians);
            _pitch = Mathf.Clamp(_pitch + deltaPitchRadians, -MaxPitchRadians, MaxPitchRadians);
            _group.rotation = Quaternion.Euler(Mathf.Rad2Deg * _pitch, Mathf.Rad2Deg * _yaw, 0f);
        }

        /// <summary>Scale the field within bounded limits.</summary>
        public void ScaleBy(float factor)
        {
            if (_group == null || factor <= 0f) return;
            _scale = Mathf.Clamp(_scale * factor, MinScale, MaxScale);
            _group.localScale = Vector3.one * _scale;
        }

        /// <summary>Reset orientation and scale without touching domain progress.</summary>
        public void ResetView()
        {
            _yaw = 0f;
            _pitch = 0f;
            _scale = 1f;
            if (_group != null)
            {
                _group.rotation = Quaternion.identity;
                _group.localScale = Vector3.one;
            }
        }

        public bool TryResolve(Vector3 origin, Vector3 direction, out string nodeId, out float distanceM)
        {
            nodeId = null;
            distanceM = 0f;

            var ray = new Ray(origin, direction.normalized);
            if (!Physics.Raycast(ray, out var hit, 5f, ~0, QueryTriggerInteraction.Ignore)) return false;

            var view = hit.collider.GetComponentInParent<FlNodeView>();
            if (view == null) return false;

            nodeId = view.NodeId;
            distanceM = hit.distance;
            return true;
        }

        /// <summary>
        /// Pointer motion handler: publishes hover changes so the interaction
        /// layer can set the interpreter's hovered target before a pinch.
        /// </summary>
        public void UpdateHover(Vector3 origin, Vector3 direction)
        {
            var hitNode = TryResolve(origin, direction, out var nodeId, out _) ? nodeId : null;
            if (hitNode == _hovered?.NodeId) return;

            if (_hovered != null) _hovered.OnPointerExit();

            if (hitNode != null && _nodeViews.TryGetValue(hitNode, out var view))
            {
                _hovered = view;
                view.OnPointerEnter();
            }
            else
            {
                _hovered = null;
            }

            HoverChanged?.Invoke(hitNode);
        }

        /// <summary>Activate the node currently under the pointer ray.</summary>
        public void ActivateHovered()
        {
            if (_hovered == null) return;
            _hovered.OnSelect();
            _hovered.OnDeselect();
            NodeSelectionRequested?.Invoke(_hovered.NodeId);
        }

        private void HighlightConnectedEdges(string nodeId, bool highlight)
        {
            foreach (var pair in _edgeViews)
            {
                var edge = pair.Value;
                if (edge.SourceNodeId != nodeId && edge.TargetNodeId != nodeId) continue;
                if (highlight) edge.Highlight();
                else edge.Unhighlight();
            }
        }

        private static Vector3 ToUnity(System.Numerics.Vector3 value) =>
            new Vector3(value.X, value.Y, value.Z);
    }
}