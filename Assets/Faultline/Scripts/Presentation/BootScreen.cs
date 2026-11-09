namespace Faultline.Presentation
{
    using System;

    /// <summary>
    /// Boot screen contract (PRD §6.1, screen 1).
    /// Entry conditions: application starting, no prior state.
    /// Exit conditions: XR session initialized and domain ready.
    /// Interaction model: automatic progression; no user gesture required.
    /// Back navigation: none (root screen).
    /// Accessibility: announces system readiness; no mandatory voice.
    /// </summary>
    public interface BootScreen : IScreen
    {
        /// <summary>Boot progress in [0, 1].</summary>
        float BootProgress { get; }

        /// <summary>Whether the XR session has been initialized.</summary>
        bool XrInitialized { get; }

        /// <summary>Whether the domain engine is ready.</summary>
        bool DomainReady { get; }

        /// <summary>Last boot error code, if any (FL-XR-*, FL-RT-*, etc.).</summary>
        string BootErrorCode { get; }

        event EventHandler<BootProgressEventArgs> ProgressChanged;
    }

    public sealed class BootProgressEventArgs : EventArgs
    {
        public float Progress { get; }
        public string StatusMessage { get; }
        public BootProgressEventArgs(float progress, string statusMessage)
        {
            Progress = progress;
            StatusMessage = statusMessage;
        }
    }
}
