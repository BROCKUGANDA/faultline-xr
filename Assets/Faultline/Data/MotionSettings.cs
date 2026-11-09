namespace Faultline.DesignSystem
{
    using System;

    /// <summary>
    /// Motion principles data for FAULTLINE XR (PRD §7 §5).
    ///
    /// Pure C# data — no UnityEngine animation or timeline types. Presentation
    /// applies these values to Unity Animator / Tween / Transform motion at
    /// runtime. All durations are in milliseconds.
    ///
    /// Design constraints:
    ///   - Short interaction transitions; slower explanatory sequences.
    ///   - No constant background movement.
    ///   - No flashing / strobing.
    ///   - No rapid camera movement.
    ///   - Respect reduced-motion / comfort mode.
    /// </summary>
    [Serializable]
    public sealed class MotionSettings
    {
        // --- Interaction transitions (short, crisp) ---
        public float ButtonPressMs { get; set; } = 120f;
        public float ButtonReleaseMs { get; set; } = 80f;
        public float HoverFadeInMs { get; set; } = 100f;
        public float HoverFadeOutMs { get; set; } = 80f;
        public float PanelOpenMs { get; set; } = 200f;
        public float PanelCloseMs { get; set; } = 160f;
        public float ToastAppearMs { get; set; } = 150f;
        public float ToastDismissMs { get; set; } = 120f;
        public float ModalAppearMs { get; set; } = 220f;
        public float ModalDismissMs { get; set; } = 180f;

        // --- Explanatory sequences (slower, deliberate) ---
        public float NodeStatusPulseMs { get; set; } = 1500f;
        public float ProgressFillMs { get; set; } = 600f;
        public float EvidenceCardRevealMs { get; set; } = 400f;
        public float AgentCardRevealMs { get; set; } = 450f;

        // --- Spatial / comfort ---
        public float WorldLockFollowLagMs { get; set; } = 300f; // deadzone lag to avoid nausea
        public float GazeDwellTooltipMs { get; set; } = 800f;
        public float CameraDollyMs { get; set; } = 700f; // slow, never rapid

        // --- Glow hints (slow breathe on first appearance only) ---
        public float GlowBreatheMs { get; set; } = 1500f;

        // --- Accessibility / reduced motion ---
        public bool ReducedMotion { get; set; } = false;
        public bool ComfortMode { get; set; } = false;
        public float ReducedMotionScale { get; set; } = 0.25f; // apply to all durations when reduced

        /// <summary>
        /// Returns the effective duration for a given nominal duration,
        /// honouring reduced-motion/comfort mode.
        /// </summary>
        public float EffectiveMs(float nominalMs)
        {
            if (ReducedMotion || ComfortMode)
            {
                return nominalMs * ReducedMotionScale;
            }
            return nominalMs;
        }

        /// <summary>
        /// Validate that motion settings obey the design-system motion
        /// principles (no flashing: pulse period must be ≥ 1000 ms;
        /// no rapid camera movement: camera dolly must be ≥ 500 ms).
        /// </summary>
        public void Validate()
        {
            if (NodeStatusPulseMs < 1000f)
            {
                throw new InvalidOperationException(
                    $"MotionSettings.NodeStatusPulseMs ({NodeStatusPulseMs}ms) must be >= 1000ms " +
                    "(no flashing / strobing per motion principles).");
            }
            if (GlowBreatheMs < 1000f)
            {
                throw new InvalidOperationException(
                    $"MotionSettings.GlowBreatheMs ({GlowBreatheMs}ms) must be >= 1000ms " +
                    "(glow hints breathe slowly, never strobe).");
            }
            if (CameraDollyMs < 500f)
            {
                throw new InvalidOperationException(
                    $"MotionSettings.CameraDollyMs ({CameraDollyMs}ms) must be >= 500ms " +
                    "(no rapid camera movement per motion principles).");
            }
            if (ReducedMotionScale < 0f || ReducedMotionScale > 1f)
            {
                throw new InvalidOperationException(
                    $"MotionSettings.ReducedMotionScale ({ReducedMotionScale}) must be in [0, 1].");
            }
        }
    }
}
