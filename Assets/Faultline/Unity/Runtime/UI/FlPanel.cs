namespace Faultline.Runtime.UI
{
    using System;
    using System.Collections.Generic;

    using UnityEngine;
    using UnityEngine.UI;

    using Faultline.Presentation;

    /// <summary>
    /// Unity implementation of <see cref="IFlPanel"/>: a world-space panel with
    /// a heading, a scroll-free body text block, a status line, and a vertically
    /// stacked button group. Layout is authored in millimetres so the same
    /// geometry that was validated in the WebXR build carries over, and the
    /// panel edge carries a collider for pinch-drag repositioning.
    /// </summary>
    public sealed class FlPanel : MonoBehaviour, IFlPanel, IRayTarget
    {
        private const float PaddingMm = 28f;
        private const float HeadingHeightMm = 34f;
        private const float StatusHeightMm = 24f;
        private const float ButtonGapMm = 12f;

        private readonly Dictionary<string, FlButton> _buttons = new(StringComparer.Ordinal);
        private readonly List<FlButton> _buttonOrder = new();
        private readonly List<(Text Text, int BaseSize)> _texts = new();

        private Image _background;
        private Text _headingText;
        private Text _bodyText;
        private Text _statusText;
        private RectTransform _rect;
        private BoxCollider _grabCollider;

        private VisualState _state = VisualState.Normal;
        private string _heading = string.Empty;
        private bool _isOpen = true;
        private bool _isPinned;
        private float _widthMm = 640f;
        private float _heightMm = 460f;

        public VisualState CurrentState => _state;

        public string Heading
        {
            get => _heading;
            set { _heading = value; if (_headingText != null) _headingText.text = value; }
        }

        public bool IsOpen => _isOpen;
        public bool IsPinned => _isPinned;
        public float WidthMm => _widthMm;
        public float HeightMm => _heightMm;

        public event Action<IFlPanel> Opened;
        public event Action<IFlPanel> Closed;
        public event Action<IFlPanel, VisualState> StateChanged;

        /// <summary>Raised when a hand pinch-drags the panel surface itself.</summary>
        public event Action<Vector3> DragRequested;

        public void Configure(string heading, float widthMm, float heightMm)
        {
            _widthMm = widthMm;
            _heightMm = heightMm;

            gameObject.layer = 5;
            _rect = GetComponent<RectTransform>();
            if (_rect == null) _rect = gameObject.AddComponent<RectTransform>();
            _rect.sizeDelta = new Vector2(widthMm, heightMm);

            _background = gameObject.AddComponent<Image>();
            _background.color = new Color(FaultlinePalette.Obsidian.r, FaultlinePalette.Obsidian.g,
                FaultlinePalette.Obsidian.b, 0.94f);
            _background.raycastTarget = false;

            var border = new GameObject("Border", typeof(RectTransform));
            border.layer = 5;
            border.transform.SetParent(transform, false);
            var borderImage = border.AddComponent<Image>();
            borderImage.color = new Color(FaultlinePalette.Slate.r, FaultlinePalette.Slate.g,
                FaultlinePalette.Slate.b, 0.55f);
            borderImage.raycastTarget = false;
            var borderRect = border.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = new Vector2(2f, 2f);
            borderRect.offsetMax = new Vector2(-2f, -2f);
            var inner = border.AddComponent<Outline>();
            inner.effectColor = FaultlinePalette.SignalCyan;
            inner.effectDistance = new Vector2(1.5f, 1.5f);

            _headingText = CreateText("Heading", FaultlineUi.TitleSize, FaultlinePalette.Ice);
            PlaceTop(_headingText.rectTransform, PaddingMm, HeadingHeightMm);
            _headingText.alignment = TextAnchor.UpperLeft;
            Heading = heading;

            _bodyText = CreateText("Body", FaultlineUi.BodySize, FaultlinePalette.Ice);
            var bodyRect = _bodyText.rectTransform;
            bodyRect.anchorMin = new Vector2(0f, 0f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.offsetMin = new Vector2(PaddingMm, PaddingMm * 2f + HeadingHeightMm);
            bodyRect.offsetMax = new Vector2(-PaddingMm, -(HeadingHeightMm + PaddingMm * 2f));
            _bodyText.alignment = TextAnchor.UpperLeft;

            _statusText = CreateText("Status", FaultlineUi.MetaSize, FaultlinePalette.Mist);
            _statusText.rectTransform.anchorMin = new Vector2(0f, 0f);
            _statusText.rectTransform.anchorMax = new Vector2(1f, 0f);
            _statusText.rectTransform.pivot = new Vector2(0.5f, 0f);
            _statusText.rectTransform.offsetMin = new Vector2(PaddingMm, 6f);
            _statusText.rectTransform.offsetMax = new Vector2(-PaddingMm, 6f + StatusHeightMm);
            _statusText.alignment = TextAnchor.LowerLeft;

            _grabCollider = gameObject.AddComponent<BoxCollider>();
            _grabCollider.size = new Vector3(widthMm, heightMm, 18f);
            _grabCollider.center = Vector3.zero;
        }

        /// <summary>Add a labelled action button. Buttons stack from the bottom up.</summary>
        public FlButton AddButton(string buttonId, string label, string accessibleDescription,
            Action handler, bool interactable = true)
        {
            if (_buttons.ContainsKey(buttonId))
                throw new InvalidOperationException($"Duplicate button id '{buttonId}' on panel '{_heading}'.");

            var buttonObject = new GameObject("Button:" + buttonId, typeof(RectTransform));
            buttonObject.transform.SetParent(transform, false);

            var button = buttonObject.AddComponent<FlButton>();
            button.Build(_widthMm - PaddingMm * 2f);
            button.Configure(label, accessibleDescription, FaultlinePalette.Graphite);
            button.Interactable = interactable;
            if (handler != null) button.Activated += _ => handler();

            _buttons[buttonId] = button;
            _buttonOrder.Add(button);
            LayoutButtons();
            return button;
        }

        public bool TryGetButton(string buttonId, out FlButton button) =>
            _buttons.TryGetValue(buttonId, out button);

        public void SetButtonInteractable(string buttonId, bool interactable)
        {
            if (_buttons.TryGetValue(buttonId, out var button)) button.Interactable = interactable;
        }

        public void SetButtonLabel(string buttonId, string label)
        {
            if (_buttons.TryGetValue(buttonId, out var button)) button.Label = label;
        }

        public void RemoveButton(string buttonId)
        {
            if (!_buttons.TryGetValue(buttonId, out var button)) return;
            _buttons.Remove(buttonId);
            _buttonOrder.Remove(button);
            Destroy(button.gameObject);
            LayoutButtons();
        }

        public void SetBody(string body)
        {
            if (_bodyText != null) _bodyText.text = body ?? string.Empty;
        }

        public void SetStatusLine(string status)
        {
            if (_statusText != null) _statusText.text = status ?? string.Empty;
        }

        public void SetState(VisualState state)
        {
            if (_state == state) return;
            _state = state;
            if (_background != null)
            {
                var tint = state == VisualState.Critical
                    ? FaultlinePalette.CriticalRed
                    : (state == VisualState.Selected ? FaultlinePalette.SignalCyan : FaultlinePalette.Slate);
                _background.color = new Color(FaultlinePalette.Obsidian.r, FaultlinePalette.Obsidian.g,
                    FaultlinePalette.Obsidian.b, 0.94f);
                var stateOutline = GetComponentInChildren<Outline>();
            if (stateOutline != null) stateOutline.effectColor = tint;
            }
            StateChanged?.Invoke(this, state);
        }

        public void Open()
        {
            if (_isOpen) return;
            _isOpen = true;
            gameObject.SetActive(true);
            Opened?.Invoke(this);
        }

        public void Close()
        {
            if (!_isOpen) return;
            _isOpen = false;
            Closed?.Invoke(this);
            gameObject.SetActive(false);
        }

        public void SetPinned(bool pinned) => _isPinned = pinned;

        /// <summary>
        /// Scale every text block relative to its design size. Buttons keep a
        /// minimum legible size so small scales never become unreadable.
        /// </summary>
        public void SetTextScale(float scale)
        {
            foreach (var (text, baseSize) in _texts)
            {
                if (text == null) continue;
                text.fontSize = Mathf.Max(8, Mathf.RoundToInt(baseSize * Mathf.Clamp(scale, 0.5f, 2f)));
            }
            foreach (var button in _buttonOrder)
                if (button != null) button.SetTextScale(scale);
        }

        /// <summary>
        /// High-contrast styling: opaque panel, brighter body text, stronger
        /// border. Status meaning still comes from the text label as well.
        /// </summary>
        public void SetHighContrast(bool enabled)
        {
            if (_background != null)
            {
                _background.color = enabled
                    ? new Color(0.02f, 0.03f, 0.05f, 1f)
                    : new Color(FaultlinePalette.Obsidian.r, FaultlinePalette.Obsidian.g,
                        FaultlinePalette.Obsidian.b, 0.94f);
            }
            if (_bodyText != null) _bodyText.color = enabled ? Color.white : FaultlinePalette.Ice;
            if (_statusText != null)
                _statusText.color = enabled ? new Color(0.85f, 0.95f, 1f) : FaultlinePalette.Mist;
            if (_headingText != null) _headingText.color = enabled ? Color.white : FaultlinePalette.Ice;
            var outline = GetComponentInChildren<Outline>();
            if (outline != null)
            {
                outline.effectColor = enabled ? FaultlinePalette.Ice : FaultlinePalette.SignalCyan;
                outline.effectDistance = new Vector2(enabled ? 2.5f : 1.5f, enabled ? 2.5f : 1.5f);
            }
        }

        public void OnPointerEnter() => SetState(_state == VisualState.Critical ? VisualState.Critical : VisualState.Hover);
        public void OnPointerExit() => SetState(_state == VisualState.Critical ? VisualState.Critical : VisualState.Normal);
        public void OnSelect() { }
        public void OnDeselect() { }

        /// <summary>Move the whole panel; positions stay inside the seated reach envelope.</summary>
        public void MoveTo(Vector3 worldPosition)
        {
            transform.position = FaultlineUi.ClampToReach(worldPosition);
            DragRequested?.Invoke(transform.position);
        }

        public bool RayTargetInteractable => true;

        public bool ValidateConfiguration(out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(_heading))
            {
                errorMessage = "FlPanel requires a heading.";
                return false;
            }
            foreach (var button in _buttonOrder)
            {
                if (!button.ValidateConfiguration(out var buttonError))
                {
                    errorMessage = $"Panel '{_heading}': {buttonError}";
                    return false;
                }
            }
            errorMessage = null;
            return true;
        }

        private void LayoutButtons()
        {
            var y = PaddingMm + StatusHeightMm + ButtonGapMm;
            // Reverse order so the primary action ends up lowest on the panel.
            for (var i = _buttonOrder.Count - 1; i >= 0; i--)
            {
                var button = _buttonOrder[i];
                var rect = button.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, y);
                y += button.HeightMm + ButtonGapMm;
            }
        }

        private Text CreateText(string objectName, int sizeMm, Color colour)
        {
            var go = new GameObject(objectName, typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(transform, false);
            var text = go.AddComponent<Text>();
            text.font = FaultlineUi.UiFont;
            text.fontSize = sizeMm;
            _texts.Add((text, sizeMm));
            text.lineSpacing = 1.18f;
            text.color = colour;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private void PlaceTop(RectTransform rect, float paddingMm, float heightMm)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(paddingMm, -paddingMm - heightMm);
            rect.offsetMax = new Vector2(-paddingMm, -paddingMm);
        }
    }
}