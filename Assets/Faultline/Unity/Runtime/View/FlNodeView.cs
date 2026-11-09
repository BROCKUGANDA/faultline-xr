namespace Faultline.Runtime.View
{
    using System;

    using UnityEngine;

    using Faultline.Presentation;
    using Faultline.Runtime.UI;
    using Faultline.Spatial;

    /// <summary>
    /// Unity rendering of a <see cref="SpatialNode"/>: a core sphere, a halo,
    /// an optional selection ring, and a billboarded label. Colour expresses
    /// health status and is always paired with a text status word so meaning is
    /// never carried by hue alone. Geometry is created once and reused; only
    /// material colours and transform scale animate.
    /// </summary>
    public sealed class FlNodeView : MonoBehaviour, IFlNodeView, IRayTarget
    {
        private const float CoreRadiusM = 0.026f;
        private const float HaloRadiusM = 0.052f;
        private const float HoverScaleMultiplier = 1.16f;
        private const float SelectedScaleMultiplier = 1.28f;
        private const float PulseAmplitude = 0.09f;

        private readonly Color _idleColour = FaultlinePalette.SignalCyan;

        private Transform _core;
        private Transform _halo;
        private Transform _ring;
        private Text _label;
        private MeshRenderer _coreRenderer;
        private MeshRenderer _haloRenderer;
        private SphereCollider _collider;
        private Material _coreMaterial;

        private string _nodeId = string.Empty;
        private string _glyphName = "FL_NODE";
        private string _labelText = string.Empty;
        private string _statusText = string.Empty;
        private VisualState _state = VisualState.Normal;
        private Color _targetColour = FaultlinePalette.SignalCyan;
        private float _pulsePhase;
        private bool _pulsing;

        public VisualState CurrentState => _state;
        public string NodeId => _nodeId;
        public string GlyphName { get => _glyphName; set { _glyphName = value; ApplyLabel(); } }
        public string Label { get => _labelText; set { _labelText = value; ApplyLabel(); } }

        public event Action<IFlNodeView> Selected;
        public event Action<IFlNodeView> Deselected;
        public event Action<IFlNodeView, VisualState> StateChanged;
        public event Action<IFlNodeView, System.Numerics.Vector3> Dragged;

        internal Transform VisualRoot { get; private set; }

        /// <summary>Create the node geometry. Called once by <see cref="TopologyView"/>.</summary>
        public void Build(string nodeId, SpatialNode spatial)
        {
            _nodeId = nodeId;
            _labelText = spatial.Label;
            _glyphName = spatial.GlyphName;

            VisualRoot = transform;

            var coreObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            coreObject.name = "Core";
            Destroy(coreObject.GetComponent<Collider>());
            coreObject.transform.SetParent(transform, false);
            coreObject.transform.localScale = Vector3.one * CoreRadiusM * 2f;
            _core = coreObject.transform;
            _coreRenderer = coreObject.GetComponent<MeshRenderer>();
            _coreMaterial = FaultlineUi.LitMaterial(_idleColour, 0.9f);
            if (_coreMaterial != null) _coreRenderer.material = _coreMaterial;

            var haloObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            haloObject.name = "Halo";
            Destroy(haloObject.GetComponent<Collider>());
            haloObject.transform.SetParent(transform, false);
            haloObject.transform.localScale = Vector3.one * HaloRadiusM * 2f;
            _halo = haloObject.transform;
            _haloRenderer = haloObject.GetComponent<MeshRenderer>();
            var haloMaterial = FaultlineUi.LitMaterial(_idleColour, 0.25f);
            if (haloMaterial != null)
            {
                haloMaterial.SetFloat("_Surface", 1f);
                haloMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                haloMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                haloMaterial.SetFloat("_ZWrite", 0f);
                haloMaterial.renderQueue = 3000;
                _haloRenderer.material = haloMaterial;
            }

            var ringObject = GameObject.CreatePrimitive(PrimitiveType.Torus);
            ringObject.name = "SelectionRing";
            Destroy(ringObject.GetComponent<Collider>());
            ringObject.transform.SetParent(transform, false);
            ringObject.transform.localScale = Vector3.one * 0.075f;
            ringObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            _ring = ringObject.transform;
            var ringRenderer = ringObject.GetComponent<MeshRenderer>();
            var ringMaterial = FaultlineUi.LitMaterial(FaultlinePalette.Ice, 1.1f);
            if (ringMaterial != null) ringRenderer.material = ringMaterial;
            ringObject.SetActive(false);

            var labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.layer = 5;
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 0.075f, 0f);
            var rect = labelObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(260f, 70f);
            rect.localScale = Vector3.one * 0.001f;

            _label = labelObject.AddComponent<Text>();
            _label.font = FaultlineUi.UiFont;
            _label.fontSize = FaultlineUi.MetaSize * 4;
            _label.alignment = TextAnchor.MiddleCenter;
            _label.color = FaultlinePalette.Ice;
            _label.raycastTarget = false;
            _label.horizontalOverflow = HorizontalWrapMode.Overflow;
            _label.verticalOverflow = VerticalWrapMode.Overflow;

            _collider = gameObject.AddComponent<SphereCollider>();
            _collider.radius = Mathf.Max(Faultline.Spatial.CoordinateSystem.NodeInteractionRadiusM * 0.4f, HaloRadiusM * 1.4f);

            ApplyLabel();
        }

        /// <summary>
        /// Project the current domain health onto this view. The view never
        /// writes back to the domain: it is a projection of validated state.
        /// </summary>
        public void SetHealth(double health, bool recovering, bool reducedMotion)
        {
            _targetColour = FaultlinePalette.ForHealth(health, recovering);
            _statusText = FaultlinePalette.StatusLabel(health, recovering);
            _pulsing = !reducedMotion && (health < 0.6);
            ApplyLabel();
        }

        public void SetSelectedVisual(bool selected)
        {
            if (_ring != null) _ring.gameObject.SetActive(selected);
            SetState(selected ? VisualState.Selected : VisualState.Normal);
        }

        private void Update()
        {
            if (_coreRenderer == null) return;

            var shared = _coreRenderer.material;
            if (shared.color != _targetColour)
                shared.color = Color.Lerp(shared.color, _targetColour, Time.deltaTime * 6f);

            if (_haloRenderer != null)
                _haloRenderer.material.color = new Color(_targetColour.r, _targetColour.g, _targetColour.b, 0.28f);

            if (_pulsing)
            {
                _pulsePhase += Time.deltaTime * 2.4f;
                var scale = 1f + Mathf.Sin(_pulsePhase) * PulseAmplitude;
                if (_core != null) _core.localScale = Vector3.one * CoreRadiusM * 2f * scale;
            }
        }

        public void SetState(VisualState state)
        {
            if (_state == state) return;
            _state = state;
            ApplyScale();
            StateChanged?.Invoke(this, state);
        }

        public void OnGrabBegin() { }

        public void OnDrag(System.Numerics.Vector3 worldDelta)
        {
            // Nodes are laid out deterministically by TopologyLayout, so a drag
            // is published for the caller to decide whether the topology itself
            // rotates. Positions are domain-owned and never moved here.
            Dragged?.Invoke(this, worldDelta);
        }

        public void OnGrabEnd() { }

        public void OnPointerEnter()
        {
            if (_state == VisualState.Selected || _state == VisualState.Disabled) return;
            SetState(VisualState.Hover);
        }

        public void OnPointerExit()
        {
            if (_state == VisualState.Selected || _state == VisualState.Disabled) return;
            SetState(VisualState.Normal);
        }

        public void OnSelect()
        {
            SetState(VisualState.Selected);
            Selected?.Invoke(this);
        }

        public void OnDeselect()
        {
            if (_state == VisualState.Selected) SetState(VisualState.Normal);
            Deselected?.Invoke(this);
        }

        public bool RayTargetInteractable => true;

        public bool ValidateConfiguration(out string errorMessage)
        {
            if (string.IsNullOrEmpty(_nodeId))
            {
                errorMessage = "FlNodeView requires a node id.";
                return false;
            }
            if (_label == null)
            {
                errorMessage = $"FlNodeView '{_nodeId}' has no label.";
                return false;
            }
            errorMessage = null;
            return true;
        }

        private void ApplyScale()
        {
            if (transform == null) return;
            var multiplier = _state switch
            {
                VisualState.Selected => SelectedScaleMultiplier,
                VisualState.Hover => HoverScaleMultiplier,
                _ => 1f
            };
            transform.localScale = Vector3.one * multiplier;
        }

        private void ApplyLabel()
        {
            if (_label == null) return;
            _label.text = string.IsNullOrEmpty(_statusText)
                ? _labelText
                : $"{_labelText}\n{_statusText}";
        }
    }
}