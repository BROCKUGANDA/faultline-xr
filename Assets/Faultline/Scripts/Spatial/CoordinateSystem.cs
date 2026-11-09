namespace Faultline.Spatial
{
    using System;

    /// <summary>
    /// Consistent right-handed coordinate system for the FAULTLINE XR
    /// infrastructure field.
    ///
    /// All world positions, directions, and distances in the Spatial module
    /// are expressed in this coordinate system. Pure C# — uses
    /// System.Numerics only; no UnityEngine. When Unity is installed, the
    /// Presentation layer maps these values onto UnityEngine.Vector3 /
    /// Quaternion at the boundary.
    ///
    /// Conventions:
    ///   - +X = right, +Y = up, +Z = forward (right-handed).
    ///   - Units are metres.
    ///   - The seated reach envelope and comfortable interaction distances
    ///     are defined relative to the user's seated head anchor (origin).
    /// </summary>
    public sealed class CoordinateSystem
    {
        /// <summary>Origin: the user's seated head/eye anchor.</summary>
        public static readonly System.Numerics.Vector3 Origin = System.Numerics.Vector3.Zero;

        /// <summary>Forward direction (what the user is facing).</summary>
        public static readonly System.Numerics.Vector3 Forward = -System.Numerics.Vector3.UnitZ;

        /// <summary>Up direction.</summary>
        public static readonly System.Numerics.Vector3 Up = System.Numerics.Vector3.UnitY;

        /// <summary>Right direction.</summary>
        public static readonly System.Numerics.Vector3 Right = System.Numerics.Vector3.UnitX;

        // --- Seated reach envelope (comfort zone) ---
        // Recommended comfortable interaction distances for seated XR:
        //   - Near reach: 0.40 m (comfortable arm's length for fine manipulation)
        //   - Far reach:  0.70 m (maximum comfortable reach without leaning)
        //   - Label minimum distance: 0.30 m (avoid focal conflict / eye strain)
        public const float NearReachM = 0.40f;
        public const float FarReachM = 0.70f;
        public const float LabelMinDistanceM = 0.30f;

        // --- Comfortable interaction distances ---
        public const float NodeInteractionRadiusM = 0.25f;   // grab/select target radius
        public const float PanelInteractionRadiusM = 0.35f;  // panel button target radius
        public const float MinNodeSpacingM = 0.50f;          // prevent overlapping node labels

        /// <summary>
        /// Clamp a world position into the seated reach envelope so that
        /// interactive content never falls outside comfortable reach.
        /// Returns the clamped position and whether it was clamped.
        /// </summary>
        public static (System.Numerics.Vector3 Position, bool WasClamped)
            ClampToReachEnvelope(System.Numerics.Vector3 position)
        {
            var distance = System.Numerics.Vector3.Length(position - Origin);
            var wasClamped = false;
            var clamped = position;

            if (distance > FarReachM)
            {
                var direction = System.Numerics.Vector3.Normalize(position - Origin);
                clamped = Origin + direction * FarReachM;
                wasClamped = true;
            }
            else if (distance < LabelMinDistanceM && distance > 0.01f)
            {
                var direction = System.Numerics.Vector3.Normalize(position - Origin);
                clamped = Origin + direction * LabelMinDistanceM;
                wasClamped = true;
            }

            return (clamped, wasClamped);
        }

        /// <summary>
        /// Convert a world position to a screen-space-friendly direction and
        /// distance from the seated anchor, useful for gaze/point targeting.
        /// </summary>
        public static (System.Numerics.Vector3 Direction, float DistanceM)
            DirectionAndDistanceFromAnchor(System.Numerics.Vector3 position)
        {
            var offset = position - Origin;
            var distance = System.Numerics.Vector3.Length(offset);
            var direction = distance > 0.0001f
                ? System.Numerics.Vector3.Normalize(offset)
                : Forward;
            return (direction, distance);
        }

        /// <summary>
        /// Whether a position is within the comfortable seated reach envelope
        /// (between min label distance and far reach).
        /// </summary>
        public static bool IsWithinComfortableReach(System.Numerics.Vector3 position)
        {
            var distance = System.Numerics.Vector3.Length(position - Origin);
            return distance >= LabelMinDistanceM && distance <= FarReachM;
        }
    }
}
