namespace Faultline.Runtime.UI
{
    using System.Collections.Generic;
    using System.Globalization;

    using UnityEngine;
    using UnityEngine.UI;

    using Faultline.Presentation;

    /// <summary>
    /// Shared runtime factory for the FAULTLINE visual language: cached
    /// materials, world-space canvases authored in millimetres, and text styled
    /// from the design tokens. Objects are created once and reused so the
    /// headset build does not allocate a material per panel or repeat Shader.Find.
    ///
    /// Canvas convention: one canvas unit == one millimetre, and the canvas
    /// transform is scaled by 0.001, so a panel declared as 640 x 460 units is
    /// 0.64 x 0.46 metres in the seated workspace. Font sizes are therefore
    /// physical heights in millimetres, which is how the design system
    /// specifies legibility at headset viewing distance.
    /// </summary>
    public static class FaultlineUi
    {
        public const int TitleSize = 20;
        public const int SectionSize = 15;
        public const int BodySize = 12;
        public const int MetaSize = 10;

        private const float CanvasUnitsPerMetre = 1000f;
        private const string LitShaderName = "Universal Render Pipeline/Lit";
        private const string StandardShaderName = "Standard";
        private const string UiShaderName = "UI/Default";

        private static readonly Dictionary<string, Material> Materials = new();
        private static Font _uiFont;
        private static Font _monoFont;

        public static Material LitMaterial(Color color, float emission = 0f)
        {
            var key = "lit:" + ColourKey(color) + ":" + emission.ToString("0.00", CultureInfo.InvariantCulture);
            if (Materials.TryGetValue(key, out var cached) && cached != null) return cached;

            var shader = Shader.Find(LitShaderName) ?? Shader.Find(StandardShaderName);
            if (shader == null)
            {
                Debug.LogError("FaultlineUi: no lit shader available. Add '" + LitShaderName + "' to Always Included Shaders.");
                return null;
            }

            var material = new Material(shader) { color = color };
            if (emission > 0f && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * emission);
            }

            Materials[key] = material;
            return material;
        }

        public static Material UiMaterial(Color color)
        {
            var key = "ui:" + ColourKey(color);
            if (Materials.TryGetValue(key, out var cached) && cached != null) return cached;

            var shader = Shader.Find(UiShaderName);
            if (shader == null)
            {
                Debug.LogError("FaultlineUi: '" + UiShaderName + "' is not available. Add it to Always Included Shaders.");
                return null;
            }

            var material = new Material(shader) { color = color };
            Materials[key] = material;
            return material;
        }

        public static Font UiFont => _uiFont != null
            ? _uiFont
            : (_uiFont = LoadFont("Faultline/Fonts/Inter-Regular", new[] { "Roboto", "Arial" }));

        public static Font MonoFont => _monoFont != null
            ? _monoFont
            : (_monoFont = LoadFont("Faultline/Fonts/JetBrainsMono-Regular", new[] { "monospace", "Consolas", "Courier New" }));

        /// <summary>
        /// Create a world-space panel canvas sized in millimetres, placed and
        /// rotated in the seated workspace.
        /// </summary>
        public static FlPanel CreatePanel(string name, string heading, Vector3 position,
            Quaternion rotation, float widthMm = 640f, float heightMm = 460f)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.layer = 5; // UI layer, so hand rays can mask to interface geometry
            root.transform.SetParent(null, false);

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(widthMm, heightMm);
            root.transform.localScale = Vector3.one / CanvasUnitsPerMetre;
            root.transform.position = position;
            root.transform.rotation = rotation;

            var panel = root.AddComponent<FlPanel>();
            panel.Configure(heading, widthMm, heightMm);
            return panel;
        }

        /// <summary>Clamp a desired world position into the seated reach envelope.</summary>
        public static Vector3 ClampToReach(Vector3 position)
        {
            var (clamped, _) = Faultline.Spatial.CoordinateSystem.ClampToReachEnvelope(
                new System.Numerics.Vector3(position.x, position.y, position.z));
            return new Vector3(clamped.X, clamped.Y, clamped.Z);
        }

        public static Quaternion FaceUser(Vector3 panelPosition)
        {
            var look = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            var direction = look - panelPosition;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) direction = Vector3.forward;
            return Quaternion.LookRotation(direction.normalized);
        }

        private static string ColourKey(Color colour) =>
            string.Format(CultureInfo.InvariantCulture, "{0}-{1}-{2}-{3}",
                Mathf.RoundToInt(colour.r * 255f),
                Mathf.RoundToInt(colour.g * 255f),
                Mathf.RoundToInt(colour.b * 255f),
                Mathf.RoundToInt(colour.a * 255f));

        private static Font LoadFont(string resourcePath, string[] osFontCandidates)
        {
            var fromResources = Resources.Load<Font>(resourcePath);
            if (fromResources != null) return fromResources;

            var dynamic = Font.CreateDynamicFontFromOSFont(osFontCandidates, 32);
            if (dynamic != null) return dynamic;

            Debug.LogError("FaultlineUi: no font found (resources path '" + resourcePath + "' and OS fonts both empty).");
            return null;
        }
    }
}