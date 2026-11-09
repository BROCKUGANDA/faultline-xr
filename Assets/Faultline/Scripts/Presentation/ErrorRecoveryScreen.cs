namespace Faultline.Presentation
{
    using System;

    /// <summary>
    /// Error recovery screen contract (PRD §6.1, screen 23).
    /// Entry conditions: an FL- error requires user action (retry/back/abort).
    /// Exit conditions: error resolved, retried, or user aborts.
    /// Interaction model: error code + user guidance; hand targets for
    /// Retry / Back / Abort. Tracking-loss errors show simple recovery.
    /// Back navigation: context-dependent (screen that raised the error).
    /// Accessibility: error code and guidance announced; non-colour-only
    /// status (icon + text); simple tracking-loss recovery (no hidden
    /// gesture).
    /// </summary>
    public interface ErrorRecoveryScreen : IScreen
    {
        /// <summary>FL- error code, e.g. FL-XR-0010.</summary>
        string ErrorCode { get; }

        /// <summary>User-facing guidance for recovery.</summary>
        string UserGuidance { get; }

        /// <summary>Whether retry is possible.</summary>
        bool CanRetry { get; }

        event EventHandler<ErrorCodeEventArgs> ErrorDisplayed;
        event EventHandler RetryRequested;
        event EventHandler BackRequested;
        event EventHandler AbortRequested;
    }

    public sealed class ErrorCodeEventArgs : EventArgs
    {
        public string ErrorCode { get; }
        public string UserGuidance { get; }
        public ErrorCodeEventArgs(string errorCode, string userGuidance)
        {
            ErrorCode = errorCode;
            UserGuidance = userGuidance;
        }
    }
}
