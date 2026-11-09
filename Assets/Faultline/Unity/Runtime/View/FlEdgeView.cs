namespace Faultline.Runtime.View
{
    using System;

    using UnityEngine;

    using Faultline.Presentation;
    using Faultline.Runtime.UI;

    /// <summary>
    /// Unity rendering of a <see cref="Faultline.Spatial.SpatialEdge"/>: a
    /// tapered line between two node positions, a travelling flow marker whose
    /// speed expresses latency, and a highlight state used when the connected
    /// node is selected. Edge direction is encoded by the flow marker travel
    /// direction plus an arrowhead, never by colour alone.
    /// </summary>
    public sealed class FlEdgeView : MonoBehaviour, IFlEdgeView
    {
        private const float LineWidthM = 0.0022f;
        private const float FlowMarkerRadiusM = 0.009f;
        private const float HighlightWidthMultiplier = 2.4f;

        private LineRenderer _line;
        private Transform _flowMarker;
        private Transform _arrow;
        private string _edgeId = string.Empty;
        private string _sourceNodeId = string.Empty;
        private string _targetNodeId = string.Empty;
        private VisualState _state = VisualState.Normal;
        private float _progress;
        private float _speed = 0.35f;
        private bool _animating;
        private bool _highlighted;
        private bool _reducedMotion;

        public VisualState CurrentState => _state;
        public string EdgeId => _edgeId;
        public string SourceNodeId => _sourceNodeId;
        public string TargetNodeId => _targetNodeId;

        public event Action<IFlEdgeView> Selected;
        public event Action<IFlEdgeView> Deselected;
        public event Action<IFlEdgeView, VisualState> StateChanged;

        public void Build(string edgeId, string sourceNodeId, string targetNodeId)
        {
            _edgeId = edgeId;
            _sourceNodeId = sourceNodeId;
            _targetNodeId = targetNodeId;

            _line = gameObject.AddComponent<LineRenderer>();
            _line.useWorldSpace = true;
            _line.widthMultiplier = LineWidthM;
            _line.numCapCorners = 2;
            _line.numCornerVertices = 2;
            _line.positionCount = 2;
            var material = FaultlineUi.LitMaterial(FaultlinePalette.Mist, 0.6f);
            if (material != null) _line.material = material;
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _line.receiveShadows = false;

            var markerObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            markerObject.name = "FlowMarker";
            Destroy(markerObject.GetComponent<Collider>());
            markerObject.transform.SetParent(transform, false);
            markerObject.transform.localScale = Vector3.one * FlowMarkerRadiusM * 2f;
            _flowMarker = markerObject.transform;
            var markerMaterial = FaultlineUi.LitMaterial(FaultlinePalette.SignalCyan, 1.2f);
            if (markerMaterial != null) markerObject.GetComponent<MeshRenderer>().material = markerMaterial;

            var arrowObject = GameObject.CreatePrimitive(PrimitiveType.Cone);
            arrowObject.name = "Arrow";
            Destroy(arrowObject.GetComponent<Collider>());
            arrowObject.transform.SetParent(transform, false);
            arrowObject.transform.localScale = Vector3.one * 0.014f;
            _arrow = arrowObject.transform;
            var arrowMaterial = FaultlineUi.LitMaterial(FaultlinePalette.SignalCyan, 0.8f);
            if (arrowMaterial != null) arrowObject.GetComponent<MeshRenderer>().material = arrowMaterial;
        }

        /// <summary>Attach the endpoints after both node views exist.</summary>
        public void SetEndpoints(Vector3 source, Vector3 target, double health, bool reducedMotion)
        {
            _reducedMotion = reducedMotion;
            if (_line != null)
            {
                _line.SetPosition(0, source);
                _line.SetPosition(1, target);
            }
            if (_arrow != null)
            {
                var direction = target - source;
                _arrow.position = target - (direction.normalized * 0.06f);
                if (direction.sqrMagnitude > 0.0001f)
                    _arrow.rotation = Quaternion.LookRotation(direction.normalized);
            }
            ApplyHealth(health);
        }

        public void ApplyHealth(double health)
        {
            var colour = FaultlinePalette.ForHealth(health);
            if (_line != null && _line.material != null)
            {
                _line.material.color = colour;
                _line.widthMultiplier = _highlighted ? LineWidthM * HighlightWidthMultiplier : LineWidthM;
            }
            // Slower flow reads as higher latency; clamped so nothing freezes.
            _speed = Mathf.Clamp(0.15f + (float)health * 0.55f, 0.1f, 0.9f);
        }

        public void SetState(VisualState state)
        {
            if (_state == state) return;
            _state = state;
            StateChanged?.Invoke(this, state);
        }

        public void Highlight()
        {
            _highlighted = true;
            SetState(VisualState.Selected);
        }

        public void Unhighlight()
        {
            _highlighted = false;
            SetState(VisualState.Normal);
        }

        public void OnPointerEnter() { }
        public void OnPointerExit() { }
        public void OnSelect() => Selected?.Invoke(this);
        public void OnDeselect() => Deselected?.Invoke(this);

        public bool ValidateConfiguration(out string errorMessage)
        {
            if (string.IsNullOrEmpty(_edgeId) || string.IsNullOrEmpty(_sourceNodeId) ||
                string.IsNullOrEmpty(_targetNodeId))
            {
                errorMessage = "FlEdgeView requires an edge id and both endpoint node ids.";
                return false;
            }
            errorMessage = null;
            return true;
        }

        private void Update()
        {
            if (!_animating || _reducedMotion || _line == null || _flowMarker == null) return;
            if (_line.positionCount < 2) return;

            _progress += Time.deltaTime * _speed;
            if (_progress > 1f) _progress -= 1f;

            var start = _line.GetPosition(0);
            var end = _line.GetPosition(1);
            _flowMarker.position = Vector3.Lerp(start, end, _progress);
            _flowMarker.gameObject.SetActive(true);
        }

        /// <summary>Turn the travelling marker on or off for this edge.</summary>
        public void SetFlowAnimating(bool animating)
        {
            _animating = animating;
            if (_flowMarker != null) _flowMarker.gameObject.SetActive(animating);
        }
    }
}