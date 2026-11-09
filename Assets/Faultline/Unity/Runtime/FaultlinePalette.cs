namespace Faultline.Runtime
{
    using UnityEngine;

    using Faultline.DesignSystem;
    using Faultline.Domain;
    using Faultline.Spatial;

    /// <summary>
    /// Maps the engine-agnostic design tokens (Obsidian Signal palette,
    /// <see cref="ColorRgba"/>) onto UnityEngine colour values and resolves the
    /// semantic status colours used by nodes, edges and panels. Status meaning
    /// is never carried by hue alone: every status also maps to a glyph and a
    /// text label supplied by the callers.
    /// </summary>
    public static class FaultlinePalette
    {
        public static ColorRgba PaletteColour(string tokenName) => Tokens.Colour(tokenName);

        public static Color ToUnity(ColorRgba colour) =>
            new Color(colour.R, colour.G, colour.B, colour.A);

        public static Color Void => ToUnity(Tokens.Colour("Void"));
        public static Color Obsidian => ToUnity(Tokens.Colour("Obsidian"));
        public static Color Graphite => ToUnity(Tokens.Colour("Graphite"));
        public static Color Slate => ToUnity(Tokens.Colour("Slate"));
        public static Color Ice => ToUnity(Tokens.Colour("Ice"));
        public static Color Mist => ToUnity(Tokens.Colour("Mist"));
        public static Color SignalCyan => ToUnity(Tokens.Colour("SignalCyan"));
        public static Color ElectricBlue => ToUnity(Tokens.Colour("ElectricBlue"));
        public static Color Violet => ToUnity(Tokens.Colour("Violet"));
        public static Color Amber => ToUnity(Tokens.Colour("Amber"));
        public static Color CriticalRed => ToUnity(Tokens.Colour("CriticalRed"));
        public static Color RecoveryGreen => ToUnity(Tokens.Colour("RecoveryGreen"));

        /// <summary>
        /// Status colour for a node, edge or metric. Mirrors the thresholds the
        /// WebXR implementation was validated with: healthy ≥ 0.9, degraded
        /// ≥ 0.6, failing ≥ 0.3, down below that; recovering is always green.
        /// </summary>
        public static Color ForHealth(double health, bool recovering = false)
        {
            if (recovering) return RecoveryGreen;
            if (health >= 0.9) return SignalCyan;
            if (health >= 0.6) return Amber;
            if (health >= 0.3) return CriticalRed;
            return new Color(0.70f, 0.14f, 0.23f); // down: deep red, distinct from failing
        }

        public static Color ForStatusMaterial(NodeStatusMaterial material)
        {
            switch (material)
            {
                case NodeStatusMaterial.Healthy: return SignalCyan;
                case NodeStatusMaterial.Degraded: return Amber;
                case NodeStatusMaterial.Failed: return CriticalRed;
                case NodeStatusMaterial.Critical: return new Color(0.70f, 0.14f, 0.23f);
                case NodeStatusMaterial.Recovering: return RecoveryGreen;
                default: return Mist; // unknown is grey, never green
            }
        }

        public static Color ForVisualState(VisualState state)
        {
            switch (state)
            {
                case VisualState.Hover: return ElectricBlue;
                case VisualState.Selected: return SignalCyan;
                case VisualState.Disabled: return new Color(Mist.r, Mist.g, Mist.b, 0.45f);
                case VisualState.Critical: return CriticalRed;
                default: return Slate;
            }
        }

        /// <summary>
        /// Non-colour status word shown beside every status colour swatch.
        /// </summary>
        public static string StatusLabel(double health, bool recovering = false)
        {
            if (recovering) return "RECOVERING";
            if (health >= 0.9) return "HEALTHY";
            if (health >= 0.6) return "DEGRADED";
            if (health >= 0.3) return "FAILING";
            return "DOWN";
        }

        private static class TokensHolder
        {
            public static readonly DesignSystemSettings Value = new DesignSystemSettings();
        }

        private static DesignSystemSettings Tokens => TokensHolder.Value;
    }
}