namespace Faultline.Presentation
{
    using System;

    /// <summary>
    /// Accessibility configuration for FAULTLINE XR (PRD §6, §7).
    ///
    /// Guarantees:
    ///   - Seated operation: all interactive targets within the seated reach
    ///     envelope (see Faultline.Spatial.CoordinateSystem).
    ///   - Reduced motion: all animation durations scaled per MotionSettings.
    ///   - Adjustable UI: text size scale, high contrast toggle.
    ///   - Readable text: sans-serif, minimum weights, legible at distance.
    ///   - Clear contrast: colour tokens meet contrast requirements.
    ///   - Non-colour-only status: every status conveyed by colour + glyph +
    ///     text, never colour alone.
    ///   - Simple tracking-loss recovery: tracking loss shows a status
    ///     indicator; recovery requires an explicit re-pinch, no hidden
    ///     gesture, no auto-execute after resume.
    ///   - No mandatory voice: all information available visually; voice is
    ///     optional/secondary only.
    ///
    /// Pure C# — no UnityEngine. Consumed by the Presentation layer and the
    /// ControllerFreeValidator.
    /// </summary>
    public sealed class AccessibilitySettings
    {
        private bool _seatedOperation = true;
        private bool _reducedMotion = false;
        private bool _highContrast = false;
        private float _textSizeScale = 1.0f;

        /// <summary>Whether seated operation mode is enabled (default on).</summary>
        public bool SeatedOperation
        {
            get => _seatedOperation;
            set
            {
                if (_seatedOperation == value) return;
                _seatedOperation = value;
                SeatedOperationChanged?.Invoke(this, new SeatedOperationChangedEventArgs(value));
            }
        }

        /// <summary>Whether reduced motion is enabled.</summary>
        public bool ReducedMotion
        {
            get => _reducedMotion;
            set
            {
                if (_reducedMotion == value) return;
                _reducedMotion = value;
                ReducedMotionChanged?.Invoke(this, new ReducedMotionChangedEventArgs(value));
            }
        }

        /// <summary>Whether high contrast mode is enabled.</summary>
        public bool HighContrast
        {
            get => _highContrast;
            set
            {
                if (_highContrast == value) return;
                _highContrast = value;
                HighContrastChanged?.Invoke(this, new HighContrastChangedEventArgs(value));
            }
        }

        /// <summary>
        /// Text size scale factor. 1.0 = default. Validated to [0.5, 2.0].
        /// </summary>
        public float TextSizeScale
        {
            get => _textSizeScale;
            set
            {
                var clamped = Math.Clamp(value, 0.5f, 2.0f);
                if (_textSizeScale.Equals(clamped)) return;
                _textSizeScale = clamped;
                TextSizeChanged?.Invoke(this, new TextSizeChangedEventArgs(clamped));
            }
        }

        public event EventHandler<SeatedOperationChangedEventArgs> SeatedOperationChanged;
        public event EventHandler<ReducedMotionChangedEventArgs> ReducedMotionChanged;
        public event EventHandler<TextSizeChangedEventArgs> TextSizeChanged;
        public event EventHandler<HighContrastChangedEventArgs> HighContrastChanged;

        /// <summary>Validate the current settings are self-consistent.</summary>
        public void Validate()
        {
            if (_textSizeScale < 0.5f || _textSizeScale > 2.0f)
                throw new InvalidOperationException(
                    $"AccessibilitySettings.TextSizeScale ({_textSizeScale}) must be in [0.5, 2.0].");
        }

        public override string ToString() =>
            $"AccessibilitySettings(seated={_seatedOperation}, reducedMotion={_reducedMotion}, " +
            $"highContrast={_highContrast}, textSize={_textSizeScale:P0})";
    }

    public sealed class SeatedOperationChangedEventArgs : EventArgs
    {
        public bool Enabled { get; }
        public SeatedOperationChangedEventArgs(bool enabled) => Enabled = enabled;
    }

    public sealed class ReducedMotionChangedEventArgs : EventArgs
    {
        public bool Enabled { get; }
        public ReducedMotionChangedEventArgs(bool enabled) => Enabled = enabled;
    }

    public sealed class TextSizeChangedEventArgs : EventArgs
    {
        public float Scale { get; }
        public TextSizeChangedEventArgs(float scale) => Scale = scale;
    }

    public sealed class HighContrastChangedEventArgs : EventArgs
    {
        public bool Enabled { get; }
        public HighContrastChangedEventArgs(bool enabled) => Enabled = enabled;
    }
}
