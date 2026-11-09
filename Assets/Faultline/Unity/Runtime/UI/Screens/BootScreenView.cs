namespace Faultline.Runtime.UI.Screens
{
    using UnityEngine;

    using Faultline.Presentation;
    using Faultline.Runtime.UI;

    /// <summary>
    /// PRD §6.1 screen 1 — Boot. Reports real initialisation progress: XR
    /// provider selection and domain readiness are read from the application
    /// root, never simulated, and a failure surfaces here as an error state.
    /// </summary>
    public sealed class BootScreenView : ScreenViewBase, BootScreen
    {
        private FlButton _continueButton;

        public override string ScreenId => ScreenIds.Boot;
        public override string Title => "FAULTLINE XR — Boot";
        public override string InteractionModel => "Pinch Continue once boot reports ready.";
        public override string AccessibleDescription =>
            "Boot screen. Shows XR provider, domain readiness and any initialisation error.";

        public float BootProgress { get; private set; }
        public bool XrInitialized { get; private set; }
        public bool DomainReady { get; private set; }
        public string BootErrorCode { get; private set; }

        public event System.EventHandler<BootProgressEventArgs> ProgressChanged;

        protected override FlPanel BuildPanel() =>
            CreatePanelInZone("FAULTLINE XR", SpatialZone.Centre, 700f, 420f);

        protected override void BuildContent(FlPanel panel)
        {
            panel.SetBody("Initialising spatial session…");

            _continueButton = panel.AddButton("boot-continue", "Continue",
                "Continue to the welcome screen", OnContinue, interactable: false);

            App.Changed += (_, _) => Refresh();
            Refresh();
        }

        protected override void OnEntered() => Refresh();

        private void OnContinue()
        {
            if (!DomainReady) return;
            App.Navigator.NavigateToScreen(ScreenIds.Welcome);
        }

        private void Refresh()
        {
            XrInitialized = App.ProviderKind != XrProviderKind.None;
            DomainReady = App.IsBooted;
            BootErrorCode = DomainReady ? null : App.BootError;

            var progress = (XrInitialized ? 0.4f : 0f) + (DomainReady ? 0.6f : 0f);
            BootProgress = Mathf.Clamp01(progress);
            ProgressChanged?.Invoke(this, new BootProgressEventArgs(
                BootProgress, DomainReady ? "Ready" : "Waiting for XR provider"));

            if (Panel == null) return;

            if (BootErrorCode != null)
            {
                SetError(ExtractCode(BootErrorCode),
                    "The XR session could not start. Continue anyway runs the labelled simulation fallback.",
                    canRetry: true);
                Panel.SetStatusLine(BootErrorCode);
            }
            else
            {
                Panel.SetBody(
                    $"XR provider: {App.ProviderKind}\n" +
                    $"Screens registered: {App.RegisteredScreenCount} of {ScreenRegistry.Count}\n" +
                    $"Domain engine: {(DomainReady ? "ready" : "starting")}\n\n" +
                    "FAULTLINE XR runs the CASCADE incident entirely offline. Telemetry is " +
                    "generated from a fixed seed and is simulated.");
                Panel.SetStatusLine($"Boot {BootProgress:P0}");
            }

            _continueButton.Interactable = DomainReady;
        }

        private static string ExtractCode(string message)
        {
            var separator = message.IndexOf(':');
            return separator > 0 ? message.Substring(0, separator) : "FL-RT-001";
        }
    }
}