namespace Faultline.Presentation
{
    using System;

    /// <summary>
    /// Accessibility settings screen contract (PRD §6.1, screen 20).
    /// Entry conditions: SettingsScreen → accessibility selected.
    /// Exit conditions: settings closed.
    /// Interaction model: toggles/sliders for seated operation, reduced
    /// motion, text size, contrast; all hand-interactable.
    /// Back navigation: SettingsScreen.
    /// Accessibility: this screen is itself fully accessible — large targets,
    /// no mandatory voice, non-colour-only status, simple tracking-loss
    /// recovery.
    /// </summary>
    public interface AccessibilitySettingsScreen : IScreen
    {
        /// <summary>Whether seated operation mode is enabled.</summary>
        bool SeatedOperation { get; }

        /// <summary>Whether reduced motion is enabled.</summary>
        bool ReducedMotion { get; }

        /// <summary>Text size scale factor (1.0 = default).</summary>
        float TextSizeScale { get; }

        /// <summary>Whether high contrast mode is enabled.</summary>
        bool HighContrast { get; }

        event EventHandler<SeatedOperationChangedEventArgs> SeatedOperationChanged;
        event EventHandler<ReducedMotionChangedEventArgs> ReducedMotionChanged;
        event EventHandler<TextSizeChangedEventArgs> TextSizeChanged;
        event EventHandler<HighContrastChangedEventArgs> HighContrastChanged;
    }

    public sealed class SeatedOperationChangedEventArgs : EventArgs
    {
        public bool Enabled { get; }
        public SeatedOperationChangedEventArgs(bool enabled)
        {
            Enabled = enabled;
        }
    }

    public sealed class ReducedMotionChangedEventArgs : EventArgs
    {
        public bool Enabled { get; }
        public ReducedMotionChangedEventArgs(bool enabled)
        {
            Enabled = enabled;
        }
    }

    public sealed class TextSizeChangedEventArgs : EventArgs
    {
        public float Scale { get; }
        public TextSizeChangedEventArgs(float scale)
        {
            Scale = scale;
        }
    }

    public sealed class HighContrastChangedEventArgs : EventArgs
    {
        public bool Enabled { get; }
        public HighContrastChangedEventArgs(bool enabled)
        {
            Enabled = enabled;
        }
    }
}
