namespace Faultline.Domain
{
    /// <summary>
    /// Platform-independent RGBA colour value.
    ///
    /// Pure C# — no UnityEngine dependency. Lives in Faultline.Domain so the
    /// design-system colour layer remains engine-agnostic and headless-testable.
    /// All channels are normalised 0..1 floats.
    ///
    /// Implemented per PRD §7 (FAULTLINE / OBSIDIAN SIGNAL): the design-system
    /// token registry stores colours as ColorRgba values; the Presentation
    /// layer is responsible for mapping them to UnityEngine.Color.
    /// </summary>
    public readonly struct ColorRgba : IEquatable<ColorRgba>
    {
        public float R { get; }
        public float G { get; }
        public float B { get; }
        public float A { get; }

        public ColorRgba(float r, float g, float b, float a = 1f)
        {
            R = Clamp01(r);
            G = Clamp01(g);
            B = Clamp01(b);
            A = Clamp01(a);
        }

        private static float Clamp01(float v) =>
            v < 0f ? 0f : (v > 1f ? 1f : v);

        /// <summary>
        /// Parse an HTML hex colour string into a ColorRgba.
        /// Supports "#RRGGBB", "RRGGBB", "#AARRGGBB", and "AARRGGBB".
        /// When no alpha channel is supplied, <paramref name="defaultAlpha"/> is used.
        /// </summary>
        /// <param name="hex">Hex colour string, e.g. "#070B12" or "#FF526A".</param>
        /// <param name="defaultAlpha">Alpha used when the string carries no alpha channel.</param>
        /// <param name="result">Parsed colour, or transparent black on failure.</param>
        /// <returns>true if parsing succeeded.</returns>
        public static bool TryParseHex(string hex, float defaultAlpha, out ColorRgba result)
        {
            result = new ColorRgba(0f, 0f, 0f, 0f);

            if (string.IsNullOrEmpty(hex))
            {
                return false;
            }

            string h = hex.Trim();
            if (h.StartsWith("#"))
            {
                h = h.Substring(1);
            }

            int length = h.Length;
            if (length != 6 && length != 8)
            {
                return false;
            }

            uint rgba;
            try
            {
                rgba = Convert.ToUInt32(h, 16);
            }
            catch
            {
                return false;
            }

            if (length == 6)
            {
                // #RRGGBB -> apply default alpha
                float r = ((rgba >> 16) & 0xFF) / 255f;
                float g = ((rgba >> 8) & 0xFF) / 255f;
                float b = (rgba & 0xFF) / 255f;
                result = new ColorRgba(r, g, b, defaultAlpha);
            }
            else
            {
                // #AARRGGBB
                float a = ((rgba >> 24) & 0xFF) / 255f;
                float r = ((rgba >> 16) & 0xFF) / 255f;
                float g = ((rgba >> 8) & 0xFF) / 255f;
                float b = (rgba & 0xFF) / 255f;
                result = new ColorRgba(r, g, b, a);
            }

            return true;
        }

        /// <summary>
        /// Convenience overload: parse "#RRGGBB" (no alpha channel) with alpha = 1.
        /// </summary>
        public static bool TryParseHex(string hex, out ColorRgba result) =>
            TryParseHex(hex, 1f, out result);

        /// <summary>
        /// Parse an HTML hex colour string. Throws on malformed input.
        /// </summary>
        public static ColorRgba FromHex(string hex, float defaultAlpha = 1f)
        {
            if (!TryParseHex(hex, defaultAlpha, out ColorRgba result))
            {
                throw new FormatException($"Invalid colour hex string: '{hex}'");
            }
            return result;
        }

        /// <summary>
        /// Produce the hex representation, e.g. "#070B12". Set <paramref name="includeAlpha"/>
        /// to emit "#AARRGGBB" when alpha differs from 1.
        /// </summary>
        public string ToHex(bool includeAlpha = false)
        {
            byte r = (byte)(R * 255f);
            byte g = (byte)(G * 255f);
            byte b = (byte)(B * 255f);
            byte a = (byte)(A * 255f);

            if (includeAlpha && A < 1f)
            {
                return $"#{a:X2}{r:X2}{g:X2}{b:X2}";
            }
            return $"#{r:X2}{g:X2}{b:X2}";
        }

        public bool Equals(ColorRgba other) =>
            R.Equals(other.R) && G.Equals(other.G) && B.Equals(other.B) && A.Equals(other.A);

        public override bool Equals(object obj) =>
            obj is ColorRgba other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(R, G, B, A);

        public static bool operator ==(ColorRgba left, ColorRgba right) => left.Equals(right);
        public static bool operator !=(ColorRgba left, ColorRgba right) => !left.Equals(right);

        public override string ToString() =>
            $"ColorRgba(R={R:F3}, G={G:F3}, B={B:F3}, A={A:F3}) [{ToHex(true)}]";
    }
}
