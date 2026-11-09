namespace Faultline.Runtime.UI.Screens
{
    using System;
    using System.Collections.Generic;

    using UnityEngine;

    using Faultline.Presentation;
    using Faultline.Runtime.UI;

    /// <summary>
    /// PRD §6.1 screen 19 — settings hub. The categories are the three the app
    /// actually services: Accessibility and Diagnostics navigate to their own
    /// screens; Comfort hosts its real controls inline (seated operation,
    /// handedness, layout reset) because no separate comfort screen exists.
    /// Closing returns to whatever screen opened settings and persists the
    /// choices through the app root.
    /// </summary>
    public sealed class SettingsScreenView : ScreenViewBase, SettingsScreen
    {
        private static readonly IReadOnlyList<string> AllCategories =
            new[] { "Accessibility", "Comfort", "Diagnostics" };

        private bool _comfortBuilt;

        private FlButton _seatedButton;
        private FlButton _handednessButton;

        public override string ScreenId => ScreenIds.Settings;
        public override string Title => "Settings";

        // Context-dependent back target: the navigator's own back stack knows
        // where settings was opened from, so no fixed id is declared.
        public override string BackScreenId => null;
        public override string EntryConditions => "None — settings can be opened from any screen.";
        public override string ExitConditions => "Settings closed; choices are persisted on close.";
        public override string InteractionModel =>
            "Pinch a category to open it; Close saves the settings and returns where you came from.";
        public override string AccessibleDescription =>
            "Settings screen. Categories Accessibility, Comfort and Diagnostics, announced as text.";

        public IReadOnlyList<string> Categories => AllCategories;
        public string SelectedCategory { get; private set; }

        public event EventHandler<SettingsCategoryEventArgs> CategorySelected;
        public event EventHandler SettingsClosed;

        protected override FlPanel BuildPanel() =>
            CreatePanelInZone("Settings", SpatialZone.Centre, 680f, 680f);

        protected override void BuildContent(FlPanel panel)
        {
            panel.AddButton("settings-accessibility", "Accessibility",
                "Open seated operation, reduced motion, text size and contrast controls",
                () => Select("Accessibility", ScreenIds.AccessibilitySettings));
            panel.AddButton("settings-comfort", "Comfort",
                "Show comfort controls: seated operation, handedness and layout reset",
                () => SelectComfort());
            panel.AddButton("settings-diagnostics", "Diagnostics",
                "Open XR session health, provider kind and log export",
                () => Select("Diagnostics", ScreenIds.Diagnostics));
            panel.AddButton("settings-close", "Close settings",
                "Save all settings and return to the screen settings was opened from", OnClose);

            App.Changed += (_, _) => Refresh();
            Refresh();
        }

        protected override void OnEntered()
        {
            SelectedCategory = null;
            Refresh();
        }

        private void Select(string categoryId, string screenId)
        {
            SelectedCategory = categoryId;
            CategorySelected?.Invoke(this, new SettingsCategoryEventArgs(categoryId));
            App.Navigator.NavigateToScreen(screenId);
        }

        private void SelectComfort()
        {
            SelectedCategory = "Comfort";
            CategorySelected?.Invoke(this, new SettingsCategoryEventArgs("Comfort"));
            BuildComfortControls(Panel);
            Refresh();
        }

        private void BuildComfortControls(FlPanel panel)
        {
            if (_comfortBuilt || panel == null) return;
            _comfortBuilt = true;

            _seatedButton = panel.AddButton("settings-comfort-seated", "Toggle seated operation",
                "Enable or disable seated operation mode", () =>
                {
                    App.SetSeatedOperation(!App.Accessibility.SeatedOperation);
                    App.SaveSettings();
                    Refresh();
                });
            _handednessButton = panel.AddButton("settings-comfort-handed", "Toggle handedness",
                "Mirror the whole layout for left- or right-handed operation", () =>
                {
                    App.SetLeftHanded(!App.Layout.LeftHanded);
                    App.SaveSettings();
                    Refresh();
                });
            panel.AddButton("settings-comfort-reset", "Reset layout",
                "Re-capture the seated base pose and place every panel back in its zone",
                () =>
                {
                    App.ResetLayout();
                    App.SaveSettings();
                });
        }

        private void OnClose()
        {
            SettingsClosed?.Invoke(this, EventArgs.Empty);
            App.SaveSettings();
            var back = App.Navigator.GoBack();
            if (!back.IsSuccess)
                SetError(back.ErrorCode,
                    "There is no screen to return to, so settings stayed open. Choices were saved.",
                    canRetry: false);
        }

        private void Refresh()
        {
            if (Panel == null) return;

            var lines = new System.Text.StringBuilder();
            lines.AppendLine("Categories:");
            foreach (var category in AllCategories)
                lines.AppendLine($" · {category}" +
                    (SelectedCategory == category ? "  [selected]" : string.Empty));

            if (SelectedCategory == "Comfort")
            {
                lines.AppendLine();
                lines.AppendLine($"Seated operation: {(App.Accessibility.SeatedOperation ? "on" : "off")}");
                lines.AppendLine($"Handedness: {(App.Layout.LeftHanded ? "left" : "right")}");
            }

            // App.Changed fires on every simulation tick; never let it wipe an
            // error presentation (e.g. a failed close or save).
            if (!HasError)
            {
                Panel.SetBody(lines.ToString());
                Panel.SetStatusLine(App.IsBusy ? App.BusyStatus :
                    "Settings are saved when this screen closes.");
            }

            if (_seatedButton != null)
                _seatedButton.Label = $"Seated operation: {(App.Accessibility.SeatedOperation ? "On" : "Off")}";
            if (_handednessButton != null)
                _handednessButton.Label = $"Handedness: {(App.Layout.LeftHanded ? "Left" : "Right")}";
        }
    }
}
