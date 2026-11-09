namespace Faultline.Runtime.UI
{
    using System;

    using UnityEngine;
    using UnityEngine.UI;

    using Faultline.Presentation;

    /// <summary>
    /// A world-space hand-target that a pointer ray can hover and activate.
    /// Implemented by buttons, nodes and panel grab handles so the interactor
    /// does not need to know what kind of thing it is pointing at.
    /// </summary>
    public interface IRayTarget
    {
        bool RayTargetInteractable { get; }
        void OnPointerEnter();
        void OnPointerExit();
        void OnSelect();
        void OnDeselect();
    }

    /// <summary>
    /// Unity implementation of the <see cref="IFlButton"/> contract: a rounded
    /// panel button with hover / focus / pressed / disabled / critical states,
    /// a text label, an accessible description, and a physical collider sized
    /// for reliable pinch targeting (minimum 45 mm, per the interaction
    /// guidelines).
    /// </summary>
    public sealed class FlButton : MonoBehaviour, IFlButton, IRayTarget
    {
        private const float MinTargetWidthMm = 220f;
        private const float TargetHeightMm = 58f;
        private const float HoverScale = 1.06f;
        private const float PressScale = 0.95f;

        private Image _background;
        private Text _labelText;
        private RectTransform _rect;
        private BoxCollider _collider;

        private VisualState _state = VisualState.Normal;
        private bool _interactable = true;
        private string _label = "Button";
        private string _accessibleDescription = string.Empty;
        private bool _pressed;
        private Color _baseColor = FaultlinePalette.Graphite;

        public VisualState CurrentState => _state;
        public string Label { get => _label; set { _label = value; ApplyLabel(); } }
        public string AccessibleDescription { get => _accessibleDescription; set => _accessibleDescription = value; }

        public bool Interactable
        {
            get => _interactable;
            set
            {
                _interactable = value;
                SetState(value ? VisualState.Normal : VisualState.Disabled);
            }
        }

        /// <summary>Height in canvas millimetres, used by panel layout.</summary>
        public float HeightMm => TargetHeightMm;

        public event Action<IFlButton> Activated;
        public event Action<IFlButton, VisualState> StateChanged;

        public void Configure(string label, string accessibleDescription, Color baseColor)
        {
            _label = label;
            _accessibleDescription = string.IsNullOrEmpty(accessibleDescription) ? label : accessibleDescription;
            _baseColor = baseColor;
            ApplyLabel();
            ApplyColour();
        }

        public void SetState(VisualState state)
        {
            if (!_interactable && state != VisualState.Disabled && state != VisualState.Critical)
                state = VisualState.Disabled;

            if (_state == state) return;
            _state = state;
            ApplyColour();
            ApplyScale();
            StateChanged?.Invoke(this, state);
        }

        public void OnPointerEnter()
        {
            if (_state == VisualState.Disabled || _state == VisualState.Selected) return;
            SetState(VisualState.Hover);
        }

        public void OnPointerExit()
        {
            if (_state == VisualState.Disabled || _state == VisualState.Selected) return;
            SetState(VisualState.Normal);
        }

        public void OnSelect()
        {
            if (_state == VisualState.Disabled) return;
            _pressed = true;
            SetState(VisualState.Selected);
            ApplyScale();
        }

        public void OnDeselect()
        {
            if (_state == VisualState.Disabled) return;
            var wasPressed = _pressed;
            _pressed = false;
            SetState(VisualState.Normal);
            ApplyScale();
            if (wasPressed) OnActivate();
        }

        public void OnActivate()
        {
            if (!_interactable) return;
            Activated?.Invoke(this);
        }

        public bool RayTargetInteractable => _interactable;

        public bool ValidateConfiguration(out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(_label))
            {
                errorMessage = "FlButton requires a non-empty label.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(_accessibleDescription))
            {
                errorMessage = $"FlButton '{_label}' requires an accessible description.";
                return false;
            }
            if (_rect != null && _rect.rect.width < MinTargetWidthMm)
            {
                errorMessage = $"FlButton '{_label}' is narrower than the {MinTargetWidthMm} mm pinch target minimum.";
                return false;
            }
            errorMessage = null;
            return true;
        }

        /// <summary>
        /// Build the visual and physical structure on this component's own
        /// GameObject; the panel owns layout and creates the GameObject first.
        /// </summary>
        internal void Build(float widthMm)
        {
            gameObject.layer = 5;
            _rect = GetComponent<RectTransform>();
            if (_rect == null) _rect = gameObject.AddComponent<RectTransform>();
            _rect.sizeDelta = new Vector2(Mathf.Max(MinTargetWidthMm, widthMm), TargetHeightMm);

            _background = gameObject.AddComponent<Image>();
            _background.color = _baseColor;
            _background.raycastTarget = false;

            var textObject = new GameObject("Label", typeof(RectTransform));
            textObject.layer = 5;
            textObject.transform.SetParent(transform, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16f, 0f);
            textRect.offsetMax = new Vector2(-16f, 0f);

            _labelText = textObject.AddComponent<Text>();
            _labelText.font = FaultlineUi.UiFont;
            _labelText.fontSize = FaultlineUi.BodySize;
            _labelText.alignment = TextAnchor.MiddleLeft;
            _labelText.color = FaultlinePalette.Ice;
            _labelText.raycastTarget = false;
            _labelText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _labelText.verticalOverflow = VerticalWrapMode.Overflow;

            _collider = gameObject.AddComponent<BoxCollider>();
            _collider.size = new Vector3(_rect.sizeDelta.x, TargetHeightMm, 12f);
            _collider.center = Vector3.zero;

            ApplyLabel();
            ApplyColour();
        }

        private int _baseFontSize;

        internal void SetTextScale(float scale)
        {
            if (_labelText == null) return;
            if (_baseFontSize == 0) _baseFontSize = _labelText.fontSize;
            _labelText.fontSize = Mathf.Max(9, Mathf.RoundToInt(_baseFontSize * Mathf.Clamp(scale, 0.5f, 2f)));
        }

        internal void SetWidth(float widthMm)
        {
            if (_rect == null) return;
            var size = _rect.sizeDelta;
            size.x = Mathf.Max(MinTargetWidthMm, widthMm);
            _rect.sizeDelta = size;
            if (_collider != null) _collider.size = new Vector3(size.x, TargetHeightMm, 12f);
        }

        private void ApplyLabel()
        {
            if (_labelText != null) _labelText.text = _label;
            name = "FlButton:" + _label;
        }

        private void ApplyColour()
        {
            if (_background == null) return;
            switch (_state)
            {
                case VisualState.Disabled:
                    _background.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, 0.35f);
                    if (_labelText != null) _labelText.color = FaultlinePalette.Mist;
                    break;
                case VisualState.Hover:
                    _background.color = FaultlinePalette.Slate;
                    if (_labelText != null) _labelText.color = FaultlinePalette.Ice;
                    break;
                case VisualState.Selected:
                    _background.color = FaultlinePalette.SignalCyan;
                    if (_labelText != null) _labelText.color = FaultlinePalette.Void;
                    break;
                case VisualState.Critical:
                    _background.color = FaultlinePalette.CriticalRed;
                    if (_labelText != null) _labelText.color = FaultlinePalette.Ice;
                    break;
                default:
                    _background.color = _baseColor;
                    if (_labelText != null) _labelText.color = FaultlinePalette.Ice;
                    break;
            }
        }

        private void ApplyScale()
        {
            if (transform == null) return;
            var scale = _pressed ? PressScale : (_state == VisualState.Hover ? HoverScale : 1f);
            transform.localScale = Vector3.one * scale;
        }
    }
}