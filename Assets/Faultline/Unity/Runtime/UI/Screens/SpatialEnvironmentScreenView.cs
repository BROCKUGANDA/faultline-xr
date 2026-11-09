namespace Faultline.Runtime.UI.Screens
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;

    using UnityEngine;

    using Faultline.Presentation;
    using Faultline.Runtime.UI;
    using Faultline.Spatial;

    /// <summary>
    /// PRD §6.1 screen 6 — Spatial environment. Reports a real comfort audit:
    /// every zone position produced by <see cref="SpatialLayoutService"/> is
    /// measured with <see cref="CoordinateSystem.IsWithinComfortableReach"/>,
    /// which is distance from the seated origin, so the verdict is computed from
    /// live layout data and never assumed. Zone focus physically re-places this
    /// screen's panel; reset re-captures the seated pose through the app root.
    /// </summary>
    public sealed class SpatialEnvironmentScreenView : ScreenViewBase, SpatialEnvironmentScreen
    {
        private static readonly SpatialZone[] Zones =
        {
            SpatialZone.Centre, SpatialZone.Left, SpatialZone.Right
        };

        private readonly Dictionary<SpatialZone, FlButton> _zoneButtons = new();

        private SpatialZone _activeZone = SpatialZone.Centre;
        private FlButton _resetButton;
        private FlButton _confirmButton;
        private bool _environmentConfigured;

        public override string ScreenId => ScreenIds.SpatialEnvironment;
        public override string Title => "Spatial environment";
        public override string BackScreenId => ScreenIds.ScenarioSelection;
        public override string EntryConditions =>
            "XR session active and the application booted.";
        public override string ExitConditions =>
            "Environment confirmed and every zone position inside the seated reach envelope.";
        public override string InteractionModel =>
            "Pinch Centre, Left or Right to focus a zone, Reset layout to re-capture the seated pose, " +
            "Confirm environment to continue.";
        public override string AccessibleDescription =>
            "Spatial environment screen. Reads out each zone distance and whether it falls inside " +
            "the comfortable seated reach envelope. Three zone buttons, a layout reset and a confirm action.";

        public SpatialZone ActiveZone => _activeZone;

        /// <summary>
        /// True only when all three zone positions the layout service produces
        /// pass the seated comfort check for the current base pose.
        /// </summary>
        public bool AnchorsInReach
        {
            get
            {
                var app = App;
                if (app == null) return false;
                var width = Panel != null ? Panel.WidthMm : 640f;
                foreach (var zone in Zones)
                {
                    if (!CoordinateSystem.IsWithinComfortableReach(
                        ToNumerics(app.Layout.PositionFor(zone, width))))
                        return false;
                }
                return true;
            }
        }

        public event EventHandler<SpatialZoneChangedEventArgs> ZoneChanged;
        public event EventHandler EnvironmentConfigured;

        protected override FlPanel BuildPanel() =>
            CreatePanelInZone("Spatial environment", SpatialZone.Centre, 720f, 640f);

        protected override SpatialZone ZoneForScreen() => _activeZone;

        protected override bool EvaluateCanEnter()
        {
            var app = App;
            return app != null && app.IsBooted && app.ProviderKind != XrProviderKind.None;
        }

        protected override void BuildContent(FlPanel panel)
        {
            foreach (var zone in Zones)
            {
                var target = zone;
                _zoneButtons[target] = panel.AddButton(ZoneButtonId(target), ZoneLabel(target),
                    $"Focus the {ZoneName(target)} zone and move this panel there",
                    () => FocusZone(target));
            }

            _resetButton = panel.AddButton("spatial-reset", "Reset layout",
                "Re-capture the seated pose and re-place every panel and the infrastructure field",
                OnResetLayout);

            // Confirm stays live even when the audit fails: the reach check is
            // measured from the world seated origin, so a device rig that sits
            // away from it must still be able to continue the journey.
            _confirmButton = panel.AddButton("spatial-confirm", "Confirm environment",
                "Mark the spatial environment configured and open the infrastructure overview",
                OnConfirm);

            App.RegisterCriticalAction("spatial-confirm", "SpatialEnvironment.ConfirmEnvironment");

            App.Changed += (_, _) => Refresh();
            Refresh();
        }

        protected override void OnEntered() => Refresh();

        /// <summary>Re-places the panel for the focused zone and reports the change.</summary>
        private void FocusZone(SpatialZone zone)
        {
            if (_activeZone == zone)
            {
                Refresh();
                return;
            }

            var previous = _activeZone;
            _activeZone = zone;

            var panel = Panel;
            if (panel != null)
            {
                panel.transform.position = App.Layout.PositionFor(zone, panel.WidthMm);
                panel.transform.rotation = App.Layout.RotationFor(zone);
            }

            ZoneChanged?.Invoke(this, new SpatialZoneChangedEventArgs(previous, zone));
            Refresh();
        }

        private void OnResetLayout()
        {
            App.ResetLayout();
            Refresh();
        }

        private void OnConfirm()
        {
            if (App.IsBusy) return;
            _environmentConfigured = true;
            EnvironmentConfigured?.Invoke(this, EventArgs.Empty);
            App.Navigator.NavigateToScreen(ScreenIds.InfrastructureOverview);
        }

        private void Refresh()
        {
            var app = App;
            var panel = Panel;
            if (app == null || panel == null) return;

            var inReach = AnchorsInReach;
            var copy = new StringBuilder();
            copy.AppendLine($"XR provider: {app.ProviderKind} · seated: {app.Accessibility.SeatedOperation}" +
                            $" · handedness: {(app.Layout.LeftHanded ? "left" : "right")}");
            copy.AppendLine($"Focused zone: {ZoneName(_activeZone)}" +
                            (_environmentConfigured ? " · environment confirmed" : string.Empty));

            foreach (var zone in Zones)
            {
                var position = app.Layout.PositionFor(zone, panel.WidthMm);
                var distance = position.magnitude;
                var reachable = CoordinateSystem.IsWithinComfortableReach(ToNumerics(position));
                copy.AppendLine($"{ZoneName(zone),-7} {distance.ToString("F2", CultureInfo.InvariantCulture)} m · " +
                                (reachable ? "within comfortable reach" : "OUTSIDE comfortable reach"));
            }

            var nodeCount = app.Topology != null ? app.Topology.NodeCount : 0;
            copy.Append($"Infrastructure field: {nodeCount} service nodes projected");

            panel.SetBody(copy.ToString());

            panel.SetStatusLine(BuildStatusLine(inReach));

            foreach (var pair in _zoneButtons)
            {
                var active = pair.Key == _activeZone;
                pair.Value.Label = active ? $"{ZoneName(pair.Key)} zone · active" : ZoneLabel(pair.Key);
                pair.Value.Interactable = !app.IsBusy;
                pair.Value.SetState(active ? VisualState.Selected : VisualState.Normal);
            }

            if (_resetButton != null) _resetButton.Interactable = !app.IsBusy;
            if (_confirmButton != null) _confirmButton.Interactable = !app.IsBusy;
        }

        /// <summary>
        /// Reach advisory while idle; while a command runs the app's own busy
        /// label holds the status line so the two never contradict each other.
        /// </summary>
        private string BuildStatusLine(bool inReach)
        {
            var app = App;
            if (app.IsBusy) return app.BusyStatus;
            return inReach
                ? "All zones inside the seated reach envelope — confirm to continue."
                : "Reset layout re-captures the seated pose; zones read as outside comfortable reach.";
        }

        private static string ZoneButtonId(SpatialZone zone) =>
            "spatial-zone-" + ZoneName(zone).ToLowerInvariant();

        private static string ZoneLabel(SpatialZone zone) => ZoneName(zone) + " zone";

        private static string ZoneName(SpatialZone zone) => zone switch
        {
            SpatialZone.Left => "Left",
            SpatialZone.Right => "Right",
            _ => "Centre"
        };

        private static System.Numerics.Vector3 ToNumerics(Vector3 value) =>
            new System.Numerics.Vector3(value.x, value.y, value.z);
    }
}
