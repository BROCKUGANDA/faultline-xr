namespace Faultline.DesignSystem
{
    using Faultline.Domain;

    /// <summary>
    /// ScriptableObject-style settings for the active design system.
    ///
    /// Pure C# — no UnityEngine. Marked with Faultline.Domain.Serializable so
    /// Presentation can later map this onto a Unity ScriptableObject once Unity
    /// is installed. Holds the active palette, typography scale, glyph registry,
    /// and motion settings.
    /// </summary>
    [Serializable]
    public sealed class DesignSystemSettings
    {
        public ColorTokenPalette Palette { get; } = new();
        public GradientRegistry Gradients { get; } = new();
        public TypographyScale Typography { get; } = new();
        public GlyphRegistry Glyphs { get; } = new();
        public MotionSettings Motion { get; } = new();

        /// <summary>
        /// Resolve a colour token by name. Throws if the token is unknown so
        /// configuration errors surface at wiring time, not at render time.
        /// </summary>
        public ColorRgba Colour(string tokenName)
        {
            return Palette[tokenName];
        }

        /// <summary>
        /// Resolve a gradient by name. Throws if unknown.
        /// </summary>
        public GradientDefinition Gradient(string name) => Gradients.Find(name);

        /// <summary>
        /// Resolve a typography token by name. Throws if unknown.
        /// </summary>
        public TypographyToken TypographyToken(string name) => Typography.Find(name);

        /// <summary>
        /// Resolve a glyph token by name. Throws if unknown.
        /// </summary>
        public GlyphToken Glyph(string name) => Glyphs.Find(name);
    }
}
