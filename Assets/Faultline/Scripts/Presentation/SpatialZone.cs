namespace Faultline.Presentation
{
    /// <summary>
    /// Spatial zones for the FAULTLINE XR layout (PRD §6.2).
    ///
    ///   - CENTRE: the infrastructure field (spatial nodes/edges).
    ///   - LEFT:   the investigation rail (evidence, timeline, notes).
    ///   - RIGHT:  the context inspector (node/incident/remediation details).
    ///
    /// Pure C# — no UnityEngine. The Presentation layer maps these zones onto
    /// world-space anchors / canvas positions at runtime.
    /// </summary>
    public enum SpatialZone
    {
        /// <summary>Centre zone: the infrastructure field.</summary>
        Centre,

        /// <summary>Left zone: the investigation rail.</summary>
        Left,

        /// <summary>Right zone: the context inspector.</summary>
        Right
    }

    /// <summary>
    /// Metadata describing a spatial zone's purpose and default placement.
    /// </summary>
    public sealed class SpatialZoneInfo
    {
        public SpatialZone Zone { get; }
        public string Purpose { get; }
        /// <summary>Default world position relative to the seated anchor.</summary>
        public System.Numerics.Vector3 DefaultPosition { get; }
        /// <summary>Default forward direction.</summary>
        public System.Numerics.Vector3 DefaultForward { get; }

        public SpatialZoneInfo(
            SpatialZone zone,
            string purpose,
            System.Numerics.Vector3 defaultPosition,
            System.Numerics.Vector3 defaultForward)
        {
            Zone = zone;
            Purpose = purpose;
            DefaultPosition = defaultPosition;
            DefaultForward = defaultForward;
        }

        public override string ToString() => $"SpatialZoneInfo({Zone}, {Purpose})";
    }

    /// <summary>
    /// Registry of the three standard spatial zones with their default
    /// placements within the seated reach envelope (see
    /// <see cref="Faultline.Spatial.CoordinateSystem"/>).
    /// </summary>
    public static class SpatialZones
    {
        /// <summary>Centre zone info.</summary>
        public static readonly SpatialZoneInfo Centre = new(
            SpatialZone.Centre,
            "Infrastructure field — spatial nodes and edges",
            new System.Numerics.Vector3(0f, -0.10f, 0.50f),
            Faultline.Spatial.CoordinateSystem.Forward);

        /// <summary>Left zone info.</summary>
        public static readonly SpatialZoneInfo Left = new(
            SpatialZone.Left,
            "Investigation rail — evidence, timeline, notes",
            new System.Numerics.Vector3(-0.45f, 0f, 0.55f),
            new System.Numerics.Vector3(0.4f, 0f, -0.9f));

        /// <summary>Right zone info.</summary>
        public static readonly SpatialZoneInfo Right = new(
            SpatialZone.Right,
            "Context inspector — node / incident / remediation details",
            new System.Numerics.Vector3(0.45f, 0f, 0.55f),
            new System.Numerics.Vector3(-0.4f, 0f, -0.9f));
    }
}
