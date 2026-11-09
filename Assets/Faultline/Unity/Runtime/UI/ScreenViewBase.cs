namespace Faultline.Runtime.UI
{
    using System;

    using UnityEngine;

    using Faultline.Domain;
    using Faultline.Presentation;
    using Faultline.Spatial;

    /// <summary>
    /// Common behaviour for every Unity screen view: lifecycle state, the
    /// world-space panel that renders it, entry / exit condition reporting, and
    /// the loading / empty / content / error presentation states required by
    /// the PRD. Concrete screens implement the specific PRD §6.1 contract and
    /// declare their content through the protected surface.
    /// </summary>
    public abstract class ScreenViewBase : MonoBehaviour, IScreen
    {
        private ScreenState _state = ScreenState.Loading;
        private FlPanel _panel;
        private FaultlineUnityApp _app;
        private string _errorGuidance;
        private bool _canRetry;
        private bool _hasBuilt;

        public abstract string ScreenId { get; }
        public abstract string Title { get; }
        public virtual string BackScreenId => null;
        public virtual string EntryConditions => "None.";
        public virtual string ExitConditions => "None.";
        public virtual string InteractionModel => "Pinch a button to activate it.";
        public virtual bool FocusWraps => true;
        public virtual string AccessibleDescription => Title + ". " + InteractionModel;

        public ScreenState State => _state;
        public bool IsVisible => gameObject.activeSelf;
        public FlPanel Panel => _panel;

        /// <summary>Shared services, injected by the application root.</summary>
        protected FaultlineUnityApp App => _app;

        /// <summary>Node or plan the screen was opened against, when relevant.</summary>
        public string FocusTargetId { get; set; }

        public event EventHandler<ScreenStateChangedEventArgs> StateChanged;

        /// <summary>Wire services and build the panel geometry once.</summary>
        public void Bind(FaultlineUnityApp app)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            if (_hasBuilt) return;
            _panel = BuildPanel();
            if (_panel != null) BuildContent(_panel);
            _hasBuilt = true;
            ValidateOrLog();
        }

        /// <summary>Create the panel for this screen. Return null for non-panel screens.</summary>
        protected abstract FlPanel BuildPanel();

        /// <summary>Add headings, body copy and buttons. Every button must do real work.</summary>
        protected abstract void BuildContent(FlPanel panel);

        /// <summary>Called each time the screen becomes active; refresh dynamic copy.</summary>
        protected virtual void OnEntered() { }

        /// <summary>Called when leaving; release hover state and timers.</summary>
        protected virtual void OnExited() { }

        public bool CanEnter => EvaluateCanEnter();

        public bool CanExit => true;

        protected virtual bool EvaluateCanEnter() => true;

        public Result<bool> Enter()
        {
            if (!CanEnter)
            {
                return Result<bool>.Failure("FL-UI-0020",
                    $"{ScreenId} cannot be entered: {EntryConditions}");
            }

            gameObject.SetActive(true);
            if (_panel != null) _panel.Open();
            SetState(ScreenState.Content);
            OnEntered();
            return Result<bool>.Success(true);
        }

        public Result<bool> Exit()
        {
            OnExited();
            if (_panel != null) _panel.Close();
            gameObject.SetActive(false);
            return Result<bool>.Success(true);
        }

        public void SetLoading()
        {
            if (_panel != null)
            {
                _panel.SetBody(App != null ? App.Messages.Loading : "Loading…");
                _panel.SetStatusLine(string.Empty);
            }
            SetState(ScreenState.Loading);
        }

        public void SetEmpty(string guidance)
        {
            if (_panel != null)
            {
                _panel.SetBody(string.IsNullOrEmpty(guidance) ? App.Messages.EmptyGuidance : guidance);
                _panel.SetStatusLine(string.Empty);
            }
            SetState(ScreenState.Empty);
        }

        public void SetContent()
        {
            SetState(ScreenState.Content);
        }

        public void SetError(string errorCode, string userGuidance, bool canRetry)
        {
            _errorGuidance = userGuidance;
            _canRetry = canRetry;
            if (_panel != null)
            {
                _panel.SetBody(userGuidance);
                _panel.SetStatusLine($"{App.Messages.ErrorPrefix} {errorCode}" +
                    (canRetry ? " · " + App.Messages.RetryAvailable : string.Empty));
                _panel.SetState(VisualState.Critical);
            }
            SetState(ScreenState.Error);
        }

        /// <summary>Whether the screen is currently showing an error state.</summary>
        public bool HasError => _state == ScreenState.Error;

        protected bool ErrorCanRetry => _canRetry;

        protected string ErrorGuidance => _errorGuidance;

        protected void SetState(ScreenState next)
        {
            if (_state == next) return;
            var previous = _state;
            _state = next;
            StateChanged?.Invoke(this, new ScreenStateChangedEventArgs(
                ScreenId, previous, next, DateTimeOffset.UtcNow.Ticks));
        }

        /// <summary>
        /// Place a panel in a spatial zone relative to the user anchor. Zone
        /// placement is shared so left-handed layouts mirror without touching
        /// any screen logic.
        /// </summary>
        protected FlPanel CreatePanelInZone(string heading, SpatialZone zone,
            float widthMm = 640f, float heightMm = 460f)
        {
            var position = App.Layout.PositionFor(zone, widthMm);
            var rotation = App.Layout.RotationFor(zone);
            var panel = FaultlineUi.CreatePanel($"{ScreenId}:Panel", heading, position, rotation,
                widthMm, heightMm);
            panel.transform.SetParent(transform, true);
            return panel;
        }

        /// <summary>Re-place the panel for the current handedness / comfort reset.</summary>
        public void Relayout()
        {
            if (_panel == null) return;
            var zone = ZoneForScreen();
            _panel.transform.position = App.Layout.PositionFor(App.Layout.Mirror(zone), _panel.WidthMm);
            _panel.transform.rotation = App.Layout.RotationFor(App.Layout.Mirror(zone));
        }

        /// <summary>Default zone; workflow screens override to sit left or right.</summary>
        protected virtual SpatialZone ZoneForScreen() => SpatialZone.Centre;

        public void ApplyAccessibility(Faultline.Presentation.AccessibilitySettings settings)
        {
            if (_panel == null || settings == null) return;
            _panel.SetTextScale(settings.TextSizeScale);
            _panel.SetHighContrast(settings.HighContrast);
            OnAccessibilityApplied(settings);
        }

        protected virtual void OnAccessibilityApplied(Faultline.Presentation.AccessibilitySettings settings) { }

        private void ValidateOrLog()
        {
            if (_panel == null) return;
            if (_panel.ValidateConfiguration(out var errorMessage)) return;
            Debug.LogError($"Screen '{ScreenId}' has an invalid configuration: {errorMessage}");
        }
    }
}