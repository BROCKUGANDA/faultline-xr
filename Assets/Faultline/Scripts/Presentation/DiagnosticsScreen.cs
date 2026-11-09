namespace Faultline.Presentation
{
    using System;

    /// <summary>
    /// Diagnostics screen contract (PRD §6.1, screen 21).
    /// Entry conditions: settings → diagnostics, or error recovery.
    /// Exit conditions: diagnostics closed.
    /// Interaction model: system health, XR status, logs; hand targets for
    /// Refresh / ExportLogs.
    /// Back navigation: SettingsScreen or ErrorRecoveryScreen.
    /// Accessibility: status via colour + glyph + text; log entries
    /// announced; no colour-only diagnostics.
    /// </summary>
    public interface DiagnosticsScreen : IScreen
    {
        /// <summary>Whether the XR session is healthy.</summary>
        bool XrSessionHealthy { get; }

        /// <summary>Whether hand tracking is available.</summary>
        bool HandTrackingAvailable { get; }

        /// <summary>Last diagnostic message / log summary.</summary>
        string DiagnosticSummary { get; }

        event EventHandler DiagnosticsOpened;
        event EventHandler RefreshRequested;
        event EventHandler ExportLogsRequested;
    }
}
