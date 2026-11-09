namespace Faultline.XR
{
    using System;

    /// <summary>
    /// Tracking status of a single hand. Pure C# — no UnityEngine or OpenXR
    /// types referenced here. The OpenXR / Meta XR SDK integration (once Unity
    /// is installed) maps platform hand-tracking state onto this enum.
    /// </summary>
    public enum HandTrackingStatus
    {
        /// <summary>Hand tracking is supported but no data is flowing yet.</summary>
        Available,

        /// <summary>Hand is being tracked; pose and gesture data are valid.</summary>
        Tracking,

        /// <summary>Hand was tracking but tracking has been lost (occlusion, out of FOV, etc.).</summary>
        Lost,

        /// <summary>Hand tracking is not supported on this device/platform.</summary>
        NotAvailable
    }

    /// <summary>
    /// Left / right hand side.
    /// </summary>
    public enum HandSide
    {
        Left,
        Right
    }

    /// <summary>
    /// Snapshot of one hand's tracked state at a moment in time.
    ///
    /// All fields are plain C# value types. Position is expressed in the
    /// application coordinate system (see <see cref="Faultline.Spatial.CoordinateSystem"/>).
    /// </summary>
    public readonly struct HandData
    {
        /// <summary>Which hand this data describes.</summary>
        public HandSide Side { get; }

        /// <summary>Current tracking status for this hand.</summary>
        public HandTrackingStatus Status { get; }

        /// <summary>Hand position (palm centre), in world-space units.</summary>
        public System.Numerics.Vector3 Position { get; }

        /// <summary>Hand orientation (palm facing direction), normalised.</summary>
        public System.Numerics.Quaternion Rotation { get; }

        /// <summary>Pinch confidence in [0, 1]. Higher = more confident a pinch is occurring.</summary>
        public float PinchConfidence { get; }

        /// <summary>Whether a pinch gesture is currently active.</summary>
        public bool PinchActive { get; }

        /// <summary>Whether a grab (sustained pinch) is currently active.</summary>
        public bool GrabActive { get; }

        /// <summary>Timestamp of the sample (UTC ticks).</summary>
        public long TimestampTicks { get; }

        public HandData(
            HandSide side,
            HandTrackingStatus status,
            System.Numerics.Vector3 position,
            System.Numerics.Quaternion rotation,
            float pinchConfidence,
            bool pinchActive,
            bool grabActive,
            long timestampTicks)
        {
            Side = side;
            Status = status;
            Position = position;
            Rotation = rotation;
            PinchConfidence = Math.Clamp(pinchConfidence, 0f, 1f);
            PinchActive = pinchActive;
            GrabActive = grabActive;
            TimestampTicks = timestampTicks;
        }

        /// <summary>Convenience: whether this hand has valid tracking data.</summary>
        public bool IsTracking => Status == HandTrackingStatus.Tracking;

        public override string ToString() =>
            $"HandData({Side}, {Status}, pinch={PinchConfidence:P0}, " +
            $"pinchActive={PinchActive}, grabActive={GrabActive})";
    }

    /// <summary>
    /// Combined hand-tracking state for both hands at a single moment.
    /// </summary>
    public sealed class HandTrackingState
    {
        public HandData Left { get; }
        public HandData Right { get; }
        public long TimestampTicks { get; }

        public HandTrackingState(HandData left, HandData right, long timestampTicks)
        {
            Left = left;
            Right = right;
            TimestampTicks = timestampTicks;
        }

        /// <summary>Whether either hand is currently tracked.</summary>
        public bool AnyTracking => Left.IsTracking || Right.IsTracking;

        /// <summary>Whether both hands are currently tracked (required for two-hand gestures).</summary>
        public bool BothTracking => Left.IsTracking && Right.IsTracking;

        /// <summary>Whether tracking has been lost on either hand that was previously tracked.</summary>
        public bool AnyLost => Left.Status == HandTrackingStatus.Lost || Right.Status == HandTrackingStatus.Lost;
    }
}
