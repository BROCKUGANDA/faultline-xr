namespace Faultline.Runtime.UI.Screens
{
    using System;
    using System.IO;
    using System.Text;

    using UnityEngine;

    using Faultline.Observability;
    using Faultline.Presentation;
    using Faultline.Runtime.UI;

    /// <summary>
    /// PRD §6.1 screen 21 — diagnostics. Session and hand-tracking health come
    /// straight from the live <c>IXrSession</c>; the summary is the app root's
    /// real <c>DiagnosticsSummary()</c>. The provider kind is shown first so a
    /// simulated fallback can never be mistaken for device validation. Export
    /// writes sanitized text only — no secrets, no stack traces.
    /// </summary>
    public sealed class DiagnosticsScreenView : ScreenViewBase, DiagnosticsScreen
    {
        private readonly FaultlineLogger _logger = new();

        private string _summary = string.Empty;

        private FlButton _refreshButton;
        private FlButton _exportButton;
        private FlButton _backButton;

        public override string ScreenId => ScreenIds.Diagnostics;
        public override string Title => "Diagnostics";
        public override string BackScreenId => ScreenIds.Settings;
        public override string EntryConditions => "Opened from settings or error recovery.";
        public override string ExitConditions => "Diagnostics closed.";
        public override string InteractionModel =>
            "Pinch Refresh to re-read live state, Export to write the diagnostics file, Back to return.";
        public override string AccessibleDescription =>
            "Diagnostics screen. XR session and hand tracking reported as OK or FAILED text " +
            "with a bracket glyph, plus the labelled provider kind and the running state summary.";

        public bool XrSessionHealthy => App?.Session != null && App.Session.IsAvailable;
        public bool HandTrackingAvailable => App?.Session != null && App.Session.IsHandTrackingAvailable;
        public string DiagnosticSummary => _summary;

        public event EventHandler DiagnosticsOpened;
        public event EventHandler RefreshRequested;
        public event EventHandler ExportLogsRequested;

        protected override FlPanel BuildPanel() =>
            CreatePanelInZone("Diagnostics", SpatialZone.Centre, 720f, 540f);

        protected override void BuildContent(FlPanel panel)
        {
            _refreshButton = panel.AddButton("diagnostics-refresh", "Refresh",
                "Re-read session health and the running diagnostics summary", OnRefresh);
            _exportButton = panel.AddButton("diagnostics-export", "Export logs",
                "Write the sanitized diagnostics summary to the app's persistent data folder", OnExport);
            _backButton = panel.AddButton("diagnostics-back", "Back to settings",
                "Return to the settings screen", () => App.Navigator.GoBack());

            App.Changed += (_, _) => Refresh();
            Refresh();
        }

        protected override void OnEntered()
        {
            _logger.Info("Diagnostics screen opened.", correlationId: ScreenIds.Diagnostics);
            DiagnosticsOpened?.Invoke(this, EventArgs.Empty);
            Refresh();
        }

        private void OnRefresh()
        {
            RefreshRequested?.Invoke(this, EventArgs.Empty);
            _logger.Info("Diagnostics refresh requested.", correlationId: ScreenIds.Diagnostics);
            SetContent(); // refresh doubles as the retry path for an export failure
            Refresh();
        }

        private void OnExport()
        {
            if (App.IsBusy) return;
            ExportLogsRequested?.Invoke(this, EventArgs.Empty);
            SetContent();
            Refresh();

            var summary = BuildExportText();
            try
            {
                System.IO.File.WriteAllText(
                    System.IO.Path.Combine(UnityEngine.Application.persistentDataPath,
                        "faultline-diagnostics.txt"),
                    summary);
                _logger.Info("Diagnostics exported to faultline-diagnostics.txt.",
                    correlationId: ScreenIds.Diagnostics);
                Panel?.SetStatusLine(
                    "Exported sanitized diagnostics to faultline-diagnostics.txt.");
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // Only the exception type reaches user copy — never the message
                // or a stack trace, which could carry paths or device detail.
                _logger.Error("Diagnostics export failed.", correlationId: ScreenIds.Diagnostics);
                SetError("FL-PS-0100",
                    "The diagnostics file could not be written to storage. Retry, or check " +
                    "the device's available storage.",
                    canRetry: true);
            }
        }

        private string BuildExportText()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"FAULTLINE XR diagnostics — captured {DateTime.UtcNow:O}");
            sb.AppendLine(ProviderLine());
            sb.AppendLine($"xr_session={(XrSessionHealthy ? "available" : "unavailable")}");
            sb.AppendLine($"hand_tracking={(HandTrackingAvailable ? "available" : "unavailable")}");
            sb.AppendLine(App.DiagnosticsSummary());
            foreach (var entry in _logger.Entries)
                sb.AppendLine(_logger.RenderUiSafe(entry));
            return sb.ToString();
        }

        private string ProviderLine() => App.ProviderKind switch
        {
            XrProviderKind.Device => "provider=Device — running against the live Meta XR session",
            XrProviderKind.Simulated =>
                "provider=Simulated — LABELLED SIMULATION FALLBACK; device validation still required",
            _ => "provider=None — no XR session has been initialised",
        };

        private void Refresh()
        {
            if (Panel == null) return;

            var summary = App.DiagnosticsSummary();
            _summary =
                $"{ProviderLine()}\n" +
                $"XR session: {(XrSessionHealthy ? "[OK] OK" : "[X] FAILED")}\n" +
                $"Hand tracking: {(HandTrackingAvailable ? "[OK] available" : "[X] not available")}\n\n" +
                summary;

            if (!HasError)
            {
                Panel.SetBody(_summary);
                Panel.SetState(XrSessionHealthy && App.ProviderKind != XrProviderKind.None
                    ? VisualState.Normal
                    : VisualState.Critical);
                Panel.SetStatusLine(App.IsBusy ? App.BusyStatus :
                    "Provider kind is always shown so a simulation fallback cannot be read as device validation.");
            }

            _refreshButton.Interactable = !App.IsBusy;
            _exportButton.Interactable = !App.IsBusy;
            _backButton.Interactable = !App.IsBusy;
        }
    }
}
