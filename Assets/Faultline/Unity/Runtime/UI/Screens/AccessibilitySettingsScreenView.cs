namespace Faultline.Runtime.UI.Screens
{
    using System;

    using UnityEngine;

    using Faultline.Presentation;
    using Faultline.Runtime.UI;

    /// <summary>
    /// PRD §6.1 screen 20 — accessibility settings. Every toggle calls the real
    /// app-root setter (which drives panel scaling, contrast, the topology
    /// view's reduced-motion path and the seated layout) and then raises the
    /// matching Changed event with the domain EventArgs type. State getters read
    /// the live <c>AccessibilitySettings</c>, never a view-local copy.
    /// </summary>
    public sealed class AccessibilitySettingsScreenView : ScreenViewBase, AccessibilitySettingsScreen
    {
        // Design-system text ramp: 100% → 115% → 130% → 100%.
        private const float ScaleDefault = 1.00f;
        private const float ScaleLarge = 1.15f;
        private const float ScaleLargest = 1.30f;

        private FlButton _seatedButton;
        private FlButton _motionButton;
        private FlButton _contrastButton;
        private FlButton _textSizeButton;
        private FlButton _backButton;

        public override string ScreenId => ScreenIds.AccessibilitySettings;
        public override string Title => "Accessibility Settings";
        public override string BackScreenId => ScreenIds.Settings;
        public override string EntryConditions => "Settings screen opened with accessibility selected.";
        public override string ExitConditions => "User returns to settings; choices persist immediately.";
        public override string InteractionModel =>
            "Pinch a toggle to flip it, or the text-size button to cycle 100, 115, 130 percent.";
        public override string AccessibleDescription =>
            "Accessibility settings. Each control shows its current value as text and applies " +
            "immediately to every panel, the topology view and the layout.";

        public bool SeatedOperation => App?.Accessibility?.SeatedOperation ?? true;
        public bool ReducedMotion => App?.Accessibility?.ReducedMotion ?? false;
        public float TextSizeScale => App?.Accessibility?.TextSizeScale ?? ScaleDefault;
        public bool HighContrast => App?.Accessibility?.HighContrast ?? false;

        public event EventHandler<SeatedOperationChangedEventArgs> SeatedOperationChanged;
        public event EventHandler<ReducedMotionChangedEventArgs> ReducedMotionChanged;
        public event EventHandler<TextSizeChangedEventArgs> TextSizeChanged;
        public event EventHandler<HighContrastChangedEventArgs> HighContrastChanged;

        protected override FlPanel BuildPanel() =>
            CreatePanelInZone("Accessibility", SpatialZone.Centre, 680f, 540f);

        protected override void BuildContent(FlPanel panel)
        {
            _seatedButton = panel.AddButton("a11y-seated", "Seated operation",
                "Toggle seated operation mode; all targets stay inside the seated reach envelope",
                OnToggleSeated);
            _motionButton = panel.AddButton("a11y-motion", "Reduced motion",
                "Toggle reduced motion; the topology view and panel animation paths honour it",
                OnToggleMotion);
            _textSizeButton = panel.AddButton("a11y-text", "Text size",
                "Cycle the text size scale through 100, 115 and 130 percent", OnCycleTextSize);
            _contrastButton = panel.AddButton("a11y-contrast", "High contrast",
                "Toggle high contrast styling on every panel", OnToggleContrast);
            _backButton = panel.AddButton("a11y-back", "Back to settings",
                "Save and return to the settings screen", OnBack);

            // The app root re-raises domain-state changes on App.Changed; this
            // view subscribes once and reads its values back from the settings.
            App.Changed += (_, _) => Refresh();
            Refresh();
        }

        protected override void OnEntered() => Refresh();

        protected override void OnAccessibilityApplied(AccessibilitySettings settings) => Refresh();

        private void OnToggleSeated()
        {
            if (App.IsBusy) return;
            App.SetSeatedOperation(!SeatedOperation);
            SeatedOperationChanged?.Invoke(this,
                new SeatedOperationChangedEventArgs(App.Accessibility.SeatedOperation));
            App.SaveSettings();
            Refresh();
        }

        private void OnToggleMotion()
        {
            if (App.IsBusy) return;
            App.SetReducedMotion(!ReducedMotion);
            ReducedMotionChanged?.Invoke(this,
                new ReducedMotionChangedEventArgs(App.Accessibility.ReducedMotion));
            App.SaveSettings();
            Refresh();
        }

        private void OnCycleTextSize()
        {
            if (App.IsBusy) return;
            var current = TextSizeScale;
            var next = current < (ScaleDefault + ScaleLarge) / 2f ? ScaleLarge
                : current < (ScaleLarge + ScaleLargest) / 2f ? ScaleLargest
                : ScaleDefault;
            App.SetTextSizeScale(next);
            TextSizeChanged?.Invoke(this,
                new TextSizeChangedEventArgs(App.Accessibility.TextSizeScale));
            App.SaveSettings();
            Refresh();
        }

        private void OnToggleContrast()
        {
            if (App.IsBusy) return;
            App.SetHighContrast(!HighContrast);
            HighContrastChanged?.Invoke(this,
                new HighContrastChangedEventArgs(App.Accessibility.HighContrast));
            App.SaveSettings();
            Refresh();
        }

        private void OnBack()
        {
            App.SaveSettings();
            App.Navigator.GoBack();
        }

        private void Refresh()
        {
            if (Panel == null) return;

            _seatedButton.Label = $"Seated operation: {(SeatedOperation ? "On" : "Off")}";
            _motionButton.Label = $"Reduced motion: {(ReducedMotion ? "On" : "Off")}";
            _contrastButton.Label = $"High contrast: {(HighContrast ? "On" : "Off")}";
            _textSizeButton.Label = $"Text size: {TextSizeScale:P0} (pinch to cycle)";

            Panel.SetBody(
                $"Seated operation: {(SeatedOperation ? "[ON]" : "[OFF]")} — keeps every target " +
                "inside the seated reach envelope.\n" +
                $"Reduced motion: {(ReducedMotion ? "[ON]" : "[OFF]")} — applied to the topology " +
                "view and panel transitions.\n" +
                $"Text size: [{TextSizeScale:P0}] — ramps 100% → 115% → 130%.\n" +
                $"High contrast: {(HighContrast ? "[ON]" : "[OFF]")} — opaque panels, brighter text.\n\n" +
                "Changes apply immediately and are saved.");
            Panel.SetStatusLine(App.IsBusy ? App.BusyStatus :
                $"Provider {App.ProviderKind} · values read live from the accessibility model");
        }
    }
}
