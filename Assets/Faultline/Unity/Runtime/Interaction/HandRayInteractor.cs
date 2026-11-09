namespace Faultline.Runtime.Interaction
{
    using System;
    using System.Collections.Generic;

    using UnityEngine;

    using Faultline.Runtime.UI;
    using Faultline.Runtime.View;
    using Faultline.Runtime.Xr;
    using Faultline.XR;

    /// <summary>
    /// Turns hand rays into interface focus and pinch into activation. Each
    /// hand owns a ray visual and a hover target; a pinch selects and releasing
    /// the pinch activates, so a noisy tracking frame can never fire a
    /// consequential control on its own.
    /// </summary>
    public sealed class HandRayInteractor : MonoBehaviour
    {
        private const float RayLengthM = 0.55f;
        private const float RayWidthM = 0.0016f;
        private const float ReticleScaleM = 0.012f;

        private MetaXrInputProvider _input;
        private TopologyView _topology;
        private readonly Dictionary<HandSide, RayVisual> _visuals = new();
        private readonly HashSet<IRayTarget> _hovered = new();
        private FlButton _hoveredButton;
        private FlPanel _hoveredPanel;
        private FlPanel _draggingPanel;
        private Vector3 _dragAnchor;
        private string _hoveredNodeId;

        /// <summary>Raised when the pointer moves onto a new node id (or null).</summary>
        public event Action<string> HoverNodeChanged;

        public void Bind(MetaXrInputProvider input, TopologyView topology)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _topology = topology ?? throw new ArgumentNullException(nameof(topology));

            _input.PinchStarted += OnPinchStarted;
            _input.PinchEnded += OnPinchEnded;
            _input.GrabStarted += OnGrabStarted;
            _input.GrabEnded += OnGrabEnded;
            _input.TwoHandScale += OnTwoHandScale;

            foreach (HandSide side in Enum.GetValues(typeof(HandSide)))
                _visuals[side] = CreateRayVisual(side.ToString());
        }

        private void OnDisable()
        {
            if (_input == null) return;
            _input.PinchStarted -= OnPinchStarted;
            _input.PinchEnded -= OnPinchEnded;
            _input.GrabStarted -= OnGrabStarted;
            _input.GrabEnded -= OnGrabEnded;
            _input.TwoHandScale -= OnTwoHandScale;
        }

        private void Update()
        {
            if (_input == null || _topology == null) return;

            var tracked = false;
            foreach (var pair in _visuals)
            {
                var hasRay = _input.TryGetPointerRay(pair.Key, out var origin, out var direction);
                pair.Value.SetVisible(hasRay);
                if (!hasRay) continue;

                tracked = true;
                pair.Value.SetRay(origin, direction * RayLengthM);

                var hitTarget = RaycastTarget(origin, direction, out var hitPoint);
                pair.Value.SetReticle(hitTarget.HasValue, hitPoint);

                if (pair.Key == HandSide.Right || !_hoveredAnything())
                    UpdateHover(hitTarget, origin, direction);
            }

            if (!tracked) ClearHover();
        }

        private bool _hoveredAnything() => _hoveredButton != null || _hoveredPanel != null || _hoveredNodeId != null;

        private void UpdateHover(RayTargetHit? hit, Vector3 origin, Vector3 direction)
        {
            var button = hit?.Target as FlButton;
            var panel = hit?.Target as FlPanel;

            if (button != _hoveredButton)
            {
                _hoveredButton?.OnPointerExit();
                _hoveredButton = button;
                _hoveredButton?.OnPointerEnter();
            }

            if (panel != _hoveredPanel && panel != null)
            {
                _hoveredPanel?.OnPointerExit();
                _hoveredPanel = panel;
                _hoveredPanel.OnPointerEnter();
            }

            // The topology view owns node hover so highlight state and the
            // reported hover id can never disagree.
            _topology.UpdateHover(origin, direction);
            var nodeId = hit == null ? _topology.HoveredNodeId : null;
            if (nodeId != _hoveredNodeId)
            {
                _hoveredNodeId = nodeId;
                HoverNodeChanged?.Invoke(nodeId);
            }
        }

        private RayTargetHit? RaycastTarget(Vector3 origin, Vector3 direction)
        {
            var ray = new Ray(origin, direction.normalized);
            if (!Physics.Raycast(ray, out var hit, RayLengthM * 2.2f, ~0, QueryTriggerInteraction.Ignore))
                return null;

            var button = hit.collider.GetComponentInParent<FlButton>();
            if (button != null)
                return button.RayTargetInteractable ? new RayTargetHit(button, hit.point) : null;

            if (hit.collider.GetComponentInParent<FlNodeView>() != null) return null;

            var panel = hit.collider.GetComponentInParent<FlPanel>();
            return panel != null ? new RayTargetHit(panel, hit.point) : null;
        }

        private void OnPinchStarted(object sender, PinchEventArgs args)
        {
            if (_hoveredButton != null && _hoveredButton.RayTargetInteractable)
            {
                _hoveredButton.OnSelect();
                return;
            }

            if (_topology != null && _hoveredNodeId != null)
            {
                _topology.ActivateHovered();
                return;
            }

            if (_hoveredPanel != null)
            {
                _draggingPanel = _hoveredPanel;
                _dragAnchor = ToUnity(args.Position);
            }
        }

        private void OnPinchEnded(object sender, PinchEventArgs args)
        {
            _hoveredButton?.OnDeselect();
            _draggingPanel = null;
        }

        private void OnGrabStarted(object sender, GrabEventArgs args)
        {
            if (_hoveredPanel != null && _draggingPanel == null)
            {
                _draggingPanel = _hoveredPanel;
                _dragAnchor = ToUnity(args.Position);
            }
        }

        private void OnGrabEnded(object sender, GrabEventArgs args) => _draggingPanel = null;

        private void OnTwoHandScale(object sender, TwoHandScaleEventArgs args)
        {
            if (args.DeltaScale <= 0f) return;
            _topology?.ScaleBy(Mathf.Clamp(args.DeltaScale, 0.9f, 1.1f));
        }

        /// <summary>
        /// Drag the panel the user grabbed. Called by the application root with
        /// the live hand position each frame while a grab is active.
        /// </summary>
        public void ApplyDrag(Vector3 handPosition)
        {
            if (_draggingPanel == null) return;
            var delta = handPosition - _dragAnchor;
            _draggingPanel.MoveTo(_draggingPanel.transform.position + delta);
            _dragAnchor = handPosition;
        }

        public bool IsDraggingPanel => _draggingPanel != null;

        private void ClearHover()
        {
            _hoveredButton?.OnPointerExit();
            _hoveredButton = null;
            _hoveredPanel?.OnPointerExit();
            _hoveredPanel = null;
            if (_hoveredNodeId != null)
            {
                _hoveredNodeId = null;
                HoverNodeChanged?.Invoke(null);
            }
        }

        private static Vector3 ToUnity(System.Numerics.Vector3 value) =>
            new Vector3(value.X, value.Y, value.Z);

        private static RayVisual CreateRayVisual(string name)
        {
            var root = new GameObject("HandRay:" + name);
            var line = root.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.widthMultiplier = RayWidthM;
            var material = FaultlineUi.LitMaterial(FaultlinePalette.SignalCyan, 1.5f);
            if (material != null) line.material = material;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;

            var reticleObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            reticleObject.name = name + ":Reticle";
            UnityEngine.Object.Destroy(reticleObject.GetComponent<Collider>());
            reticleObject.transform.localScale = Vector3.one * ReticleScaleM;
            var reticleMaterial = FaultlineUi.LitMaterial(FaultlinePalette.Ice, 1.8f);
            if (reticleMaterial != null) reticleObject.GetComponent<MeshRenderer>().material = reticleMaterial;

            return new RayVisual(line, reticleObject.transform);
        }

        private readonly struct RayTargetHit
        {
            public RayTargetHit(IRayTarget target, Vector3 point)
            {
                Target = target;
                Point = point;
            }

            public IRayTarget Target { get; }
            public Vector3 Point { get; }
        }

        private sealed class RayVisual
        {
            private readonly LineRenderer _line;
            private readonly Transform _reticle;

            public RayVisual(LineRenderer line, Transform reticle)
            {
                _line = line;
                _reticle = reticle;
            }

            public void SetVisible(bool visible)
            {
                _line.enabled = visible;
                _reticle.gameObject.SetActive(visible);
            }

            public void SetRay(Vector3 origin, Vector3 end)
            {
                _line.SetPosition(0, origin);
                _line.SetPosition(1, end);
            }

            public void SetReticle(bool onTarget, Vector3 point)
            {
                _reticle.position = point;
                _reticle.localScale = Vector3.one * (onTarget ? ReticleScaleM * 1.6f : ReticleScaleM);
            }
        }
    }
}