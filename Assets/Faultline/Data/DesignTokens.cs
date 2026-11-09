namespace Faultline.DesignSystem
{
    using System.Collections.Generic;
    using Faultline.Domain;

    // ========================================================================
    // Colour tokens — PRD §7 (FAULTLINE / OBSIDIAN SIGNAL)
    // All colours are stored as platform-independent ColorRgba (Domain, pure C#).
    // The Presentation layer maps these to UnityEngine.Color at runtime.
    // ========================================================================

    /// <summary>
    /// Named colour tokens for the FAULTLINE / OBSIDIAN SIGNAL design system.
    /// </summary>
    [Serializable]
    public sealed class ColorTokenPalette
    {
        public ColorRgba Void { get; } = ColorRgba.FromHex("#070B12");
        public ColorRgba Obsidian { get; } = ColorRgba.FromHex("#0D1420");
        public ColorRgba Graphite { get; } = ColorRgba.FromHex("#152131");
        public ColorRgba Slate { get; } = ColorRgba.FromHex("#26364A");
        public ColorRgba Ice { get; } = ColorRgba.FromHex("#EAF3FF");
        public ColorRgba Mist { get; } = ColorRgba.FromHex("#91A4BA");
        public ColorRgba SignalCyan { get; } = ColorRgba.FromHex("#41D9F5");
        public ColorRgba ElectricBlue { get; } = ColorRgba.FromHex("#537EFF");
        public ColorRgba Violet { get; } = ColorRgba.FromHex("#9C83FF");
        public ColorRgba Amber { get; } = ColorRgba.FromHex("#FFBE55");
        public ColorRgba CriticalRed { get; } = ColorRgba.FromHex("#FF526A");
        public ColorRgba RecoveryGreen { get; } = ColorRgba.FromHex("#54D99A");

        /// <summary>
        /// Resolve a named token to its ColorRgba. Returns transparent black
        /// (and logs nothing — Domain is pure C#) when the name is unknown.
        /// </summary>
        public ColorRgba this[string tokenName]
        {
            get
            {
                return tokenName switch
                {
                    "Void" => Void,
                    "Obsidian" => Obsidian,
                    "Graphite" => Graphite,
                    "Slate" => Slate,
                    "Ice" => Ice,
                    "Mist" => Mist,
                    "SignalCyan" or "Signal Cyan" => SignalCyan,
                    "ElectricBlue" or "Electric Blue" => ElectricBlue,
                    "Violet" => Violet,
                    "Amber" => Amber,
                    "CriticalRed" or "Critical Red" => CriticalRed,
                    "RecoveryGreen" or "Recovery Green" => RecoveryGreen,
                    _ => new ColorRgba(0f, 0f, 0f, 0f)
                };
            }
        }
    }

    // ========================================================================
    // Gradient definitions — start/end colours + angle (optional mid stop)
    // ========================================================================

    /// <summary>
    /// A gradient defined by a start colour, end colour, optional mid stop, and
    /// angle in degrees. Pure data — no UnityEngine gradient or shader is
    /// referenced here; Presentation builds the actual gradient asset.
    /// </summary>
    [Serializable]
    public sealed class GradientDefinition
    {
        public string Name { get; }
        public ColorRgba Start { get; }
        public ColorRgba End { get; }
        public ColorRgba Mid { get; }
        public bool HasMid { get; }
        public float AngleDegrees { get; }

        public GradientDefinition(
            string name,
            ColorRgba start,
            ColorRgba end,
            float angleDegrees,
            ColorRgba? mid = null)
        {
            Name = name;
            Start = start;
            End = end;
            AngleDegrees = angleDegrees;
            if (mid.HasValue)
            {
                Mid = mid.Value;
                HasMid = true;
            }
            else
            {
                Mid = new ColorRgba(0f, 0f, 0f, 0f);
                HasMid = false;
            }
        }
    }

    /// <summary>
    /// Registered gradient definitions used sparingly: panel headers, glow
    /// hints, and progress fills (PRD §7 §2).
    /// </summary>
    [Serializable]
    public sealed class GradientRegistry
    {
        public readonly List<GradientDefinition> Gradients = new();

        public GradientRegistry()
        {
            ColorTokenPalette p = new();

            // Panel depth: Void → Obsidian → Graphite (elevated panel tops)
            Gradients.Add(new GradientDefinition(
                "PanelDepth", p.Void, p.Graphite, 90f, p.Obsidian));

            // Primary signal: Signal Cyan → Electric Blue (primary action glow)
            Gradients.Add(new GradientDefinition(
                "PrimarySignal", p.SignalCyan, p.ElectricBlue, 135f));

            // Intelligence: Violet → Electric Blue (agent/automation hint)
            Gradients.Add(new GradientDefinition(
                "Intelligence", p.Violet, p.ElectricBlue, 135f));

            // Critical incident: Critical Red → Void (incident alert pulse)
            Gradients.Add(new GradientDefinition(
                "CriticalIncident", p.CriticalRed, p.Void, 90f));

            // Recovery: Recovery Green → Signal Cyan (resolved / healthy)
            Gradients.Add(new GradientDefinition(
                "Recovery", p.RecoveryGreen, p.SignalCyan, 135f));
        }

        public GradientDefinition Find(string name) =>
            Gradients.Find(g => g.Name == name) ??
            throw new KeyNotFoundException($"Gradient not registered: '{name}'");
    }

    // ========================================================================
    // Typography scale
    // ========================================================================

    /// <summary>
    /// One typography token: size range, font family, weight, and intended use.
    /// Sizes are in points; the XR layer scales by distance. Weights follow the
    /// OpenType numeric scale (400 Regular, 500 Medium, 600 SemiBold, 700 Bold).
    /// </summary>
    [Serializable]
    public sealed class TypographyToken
    {
        public string Name { get; }
        public float MinSizePt { get; }
        public float MaxSizePt { get; }
        public string FontFamily { get; }
        public int Weight { get; }
        public string Use { get; }

        public TypographyToken(
            string name,
            float minSizePt,
            float maxSizePt,
            string fontFamily,
            int weight,
            string use)
        {
            Name = name;
            MinSizePt = minSizePt;
            MaxSizePt = maxSizePt;
            FontFamily = fontFamily;
            Weight = weight;
            Use = use;
        }
    }

    /// <summary>
    /// Registered typography scale (PRD §7 §3).
    /// All text: sans-serif, high legibility at distance in VR; avoid thin
    /// weights below 400.
    /// </summary>
    [Serializable]
    public sealed class TypographyScale
    {
        public readonly List<TypographyToken> Tokens = new();

        public TypographyScale()
        {
            Tokens.Add(new TypographyToken("Hero", 36f, 44f, "Space Grotesk", 700,
                "Scene titles, large HUD headers"));
            Tokens.Add(new TypographyToken("ScreenTitle", 28f, 32f, "Space Grotesk", 600,
                "Panel headings, screen titles"));
            Tokens.Add(new TypographyToken("Section", 22f, 24f, "Inter", 600,
                "Section titles within panels"));
            Tokens.Add(new TypographyToken("Panel", 18f, 20f, "Inter", 500,
                "Panel body labels, list items"));
            Tokens.Add(new TypographyToken("Body", 16f, 18f, "Inter", 400,
                "Primary body text"));
            Tokens.Add(new TypographyToken("Secondary", 14f, 16f, "Inter", 400,
                "Secondary body text, captions"));
            Tokens.Add(new TypographyToken("TechnicalLabels", 12f, 14f, "JetBrains Mono", 400,
                "Technical labels, timestamps, data values"));
        }

        public TypographyToken Find(string name) =>
            Tokens.Find(t => t.Name == name) ??
            throw new KeyNotFoundException($"Typography token not registered: '{name}'");
    }

    // ========================================================================
    // Icon glyph tokens — FL_* glyphs with Normal/Hover/Selected/Disabled/Critical states
    // ========================================================================

    /// <summary>
    /// One icon glyph with a glyph name per visual state. The actual glyph
    /// character (SDF font character / sprite atlas key) is resolved by
    /// Presentation; this is pure data.
    /// </summary>
    [Serializable]
    public sealed class GlyphToken
    {
        public string Name { get; }      // e.g. FL_NODE
        public string Description { get; }
        public string Normal { get; }
        public string Hover { get; }
        public string Selected { get; }
        public string Disabled { get; }
        public string Critical { get; }

        public GlyphToken(
            string name,
            string description,
            string normal,
            string hover,
            string selected,
            string disabled,
            string critical)
        {
            Name = name;
            Description = description;
            Normal = normal;
            Hover = hover;
            Selected = selected;
            Disabled = disabled;
            Critical = critical;
        }
    }

    /// <summary>
    /// Registry of FL_* icon glyphs, each mapped to a description and the
    /// glyph name used for each visual state (PRD §7 §4).
    /// </summary>
    [Serializable]
    public sealed class GlyphRegistry
    {
        public readonly List<GlyphToken> Glyphs = new();

        public GlyphRegistry()
        {
            Add("FL_NODE", "Infrastructure service node",
                "fl_node", "fl_node_hover", "fl_node_selected", "fl_node_disabled", "fl_node_critical");
            Add("FL_DATABASE", "Database / data store node",
                "fl_database", "fl_database_hover", "fl_database_selected", "fl_database_disabled", "fl_database_critical");
            Add("FL_GATEWAY", "Gateway / entry point",
                "fl_gateway", "fl_gateway_hover", "fl_gateway_selected", "fl_gateway_disabled", "fl_gateway_critical");
            Add("FL_AUTH", "Authentication / identity",
                "fl_auth", "fl_auth_hover", "fl_auth_selected", "fl_auth_disabled", "fl_auth_critical");
            Add("FL_QUEUE", "Message queue",
                "fl_queue", "fl_queue_hover", "fl_queue_selected", "fl_queue_disabled", "fl_queue_critical");
            Add("FL_WORKER", "Worker / background process",
                "fl_worker", "fl_worker_hover", "fl_worker_selected", "fl_worker_disabled", "fl_worker_critical");
            Add("FL_NETWORK", "Network link / dependency edge",
                "fl_network", "fl_network_hover", "fl_network_selected", "fl_network_disabled", "fl_network_critical");
            Add("FL_TRACE", "Distributed trace span",
                "fl_trace", "fl_trace_hover", "fl_trace_selected", "fl_trace_disabled", "fl_trace_critical");
            Add("FL_METRIC", "Metric sample / gauge",
                "fl_metric", "fl_metric_hover", "fl_metric_selected", "fl_metric_disabled", "fl_metric_critical");
            Add("FL_LOG", "Log event",
                "fl_log", "fl_log_hover", "fl_log_selected", "fl_log_disabled", "fl_log_critical");
            Add("FL_AGENT", "Autonomous agent",
                "fl_agent", "fl_agent_hover", "fl_agent_selected", "fl_agent_disabled", "fl_agent_critical");
            Add("FL_ORACLE", "Oracle / external data feed",
                "fl_oracle", "fl_oracle_hover", "fl_oracle_selected", "fl_oracle_disabled", "fl_oracle_critical");
            Add("FL_SENTRY", "Sentry / watcher alert",
                "fl_sentry", "fl_sentry_hover", "fl_sentry_selected", "fl_sentry_disabled", "fl_sentry_critical");
            Add("FL_FORGE", "Forge / build or deploy action",
                "fl_forge", "fl_forge_hover", "fl_forge_selected", "fl_forge_disabled", "fl_forge_critical");
            Add("FL_APPROVE", "Approve action",
                "fl_approve", "fl_approve_hover", "fl_approve_selected", "fl_approve_disabled", "fl_approve_critical");
            Add("FL_REJECT", "Reject action",
                "fl_reject", "fl_reject_hover", "fl_reject_selected", "fl_reject_disabled", "fl_reject_critical");
            Add("FL_WARNING", "Warning / degraded status",
                "fl_warning", "fl_warning_hover", "fl_warning_selected", "fl_warning_disabled", "fl_warning_critical");
            Add("FL_FAILURE", "Failure / critical status",
                "fl_failure", "fl_failure_hover", "fl_failure_selected", "fl_failure_disabled", "fl_failure_critical");
            Add("FL_RECOVERY", "Recovery / remediated status",
                "fl_recovery", "fl_recovery_hover", "fl_recovery_selected", "fl_recovery_disabled", "fl_recovery_critical");
            Add("FL_VERIFY", "Verify / check action",
                "fl_verify", "fl_verify_hover", "fl_verify_selected", "fl_verify_disabled", "fl_verify_critical");
            Add("FL_TIMELINE", "Timeline / event history",
                "fl_timeline", "fl_timeline_hover", "fl_timeline_selected", "fl_timeline_disabled", "fl_timeline_critical");
            Add("FL_SETTINGS", "Settings / configuration",
                "fl_settings", "fl_settings_hover", "fl_settings_selected", "fl_settings_disabled", "fl_settings_critical");
            Add("FL_ACCESSIBILITY", "Accessibility / comfort controls",
                "fl_accessibility", "fl_accessibility_hover", "fl_accessibility_selected", "fl_accessibility_disabled", "fl_accessibility_critical");
            Add("FL_PAUSE", "Pause / suspend action",
                "fl_pause", "fl_pause_hover", "fl_pause_selected", "fl_pause_disabled", "fl_pause_critical");
        }

        private void Add(
            string name, string description,
            string normal, string hover, string selected, string disabled, string critical)
        {
            Glyphs.Add(new GlyphToken(name, description, normal, hover, selected, disabled, critical));
        }

        public GlyphToken Find(string name) =>
            Glyphs.Find(g => g.Name == name) ??
            throw new KeyNotFoundException($"Glyph not registered: '{name}'");
    }

    // ========================================================================
    // Central token registry facade
    // ========================================================================

    /// <summary>
    /// Central design-system token registry (PRD §7). Exposes colours,
    /// gradients, typography, and glyphs. Pure C# — no UnityEngine. The
    /// Presentation layer consumes this registry and maps tokens onto Unity
    /// assets at runtime.
    /// </summary>
    [Serializable]
    public sealed class DesignTokens
    {
        public static DesignTokens Instance { get; } = new();

        public ColorTokenPalette Colours { get; } = new();
        public GradientRegistry Gradients { get; } = new();
        public TypographyScale Typography { get; } = new();
        public GlyphRegistry Glyphs { get; } = new();

        private DesignTokens()
        {
        }
    }
}
