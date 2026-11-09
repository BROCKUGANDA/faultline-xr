namespace Faultline.Runtime.Xr
{
    using UnityEngine;
    using UnityEngine.XR.Hands;

    using Faultline.XR;

    /// <summary>
    /// Reads XR Hands joint poses and converts them into the engine-agnostic
    /// <see cref="HandData"/> samples that the Interaction layer consumes.
    ///
    /// Pinch is derived from the thumb-tip / index-tip separation with
    /// hysteresis, mirroring the proven WebXR interaction model: a pinch turns
    /// on below <see cref="PinchOnDistanceM"/> and turns off above
    /// <see cref="PinchOffDistanceM"/> so tracking noise cannot double-fire.
    /// All distances are metres in the OpenXR tracking space.
    /// </summary>
    public static class XrHandPoseReader
    {
        public const float PinchOnDistanceM = 0.022f;
        public const float PinchOffDistanceM = 0.030f;

        /// <summary>Distance beyond which a hand sample counts as untracked noise.</summary>
        private const float MaxConfidentSpanM = 0.055f;

        /// <summary>
        /// Build one hand sample. Returns <see cref="HandTrackingStatus.NotAvailable"/>
        /// when the subsystem is missing and <see cref="HandTrackingStatus.Lost"/>
        /// when the runtime reports the hand as not tracked.
        /// </summary>
        public static HandData ReadHand(XRHandSubsystem subsystem, HandSide side, bool previousPinchActive)
        {
            var now = System.DateTimeOffset.UtcNow.Ticks;

            if (subsystem == null)
            {
                return Untracked(side, HandTrackingStatus.NotAvailable, now);
            }

            var xrHand = side == HandSide.Left ? subsystem.leftHand : subsystem.rightHand;

            if (!xrHand.isTracked)
            {
                return Untracked(side, HandTrackingStatus.Lost, now);
            }

            var wrist = xrHand.GetJoint(XRHandJointID.Wrist);
            var palm = xrHand.GetJoint(XRHandJointID.Palm);
            var thumbTip = xrHand.GetJoint(XRHandJointID.ThumbTip);
            var indexTip = xrHand.GetJoint(XRHandJointID.IndexTip);

            if (!palm.TryGetPose(out var palmPose) ||
                !thumbTip.TryGetPose(out var thumbPose) ||
                !indexTip.TryGetPose(out var indexPose))
            {
                return Untracked(side, HandTrackingStatus.Lost, now);
            }

            var separation = Vector3.Distance(thumbPose.position, indexPose.position);
            var pinchActive = previousPinchActive
                ? separation <= PinchOffDistanceM
                : separation <= PinchOnDistanceM;

            // Confidence rises as the fingertips approach; clamped to [0, 1].
            var confidence = Mathf.Clamp01(1f - separation / MaxConfidentSpanM);

            var position = ToNumerics(palmPose.position);
            var rotation = ToNumerics(palmPose.rotation);

            return new HandData(
                side,
                HandTrackingStatus.Tracking,
                position,
                rotation,
                confidence,
                pinchActive,
                grabActive: false,
                timestampTicks: now);
        }

        /// <summary>Ray origin and direction for a tracked hand (index-tip through the fingertips).</summary>
        public static bool TryGetPointerRay(XRHandSubsystem subsystem, HandSide side,
            out Vector3 origin, out Vector3 direction)
        {
            origin = Vector3.zero;
            direction = Vector3.forward;

            if (subsystem == null) return false;

            var xrHand = side == HandSide.Left ? subsystem.leftHand : subsystem.rightHand;
            if (!xrHand.isTracked) return false;

            var wrist = xrHand.GetJoint(XRHandJointID.Wrist);
            var indexTip = xrHand.GetJoint(XRHandJointID.IndexTip);
            var middleTip = xrHand.GetJoint(XRHandJointID.MiddleTip);

            if (!wrist.TryGetPose(out var wristPose) || !indexTip.TryGetPose(out var indexPose))
                return false;

            origin = indexPose.position;

            // Point along the index finger; fall back to wrist→tip when the
            // middle finger is not yet reported by the runtime.
            if (middleTip.TryGetPose(out var middlePose))
            {
                var fingertipMid = Vector3.Lerp(indexPose.position, middlePose.position, 0.5f);
                direction = (fingertipMid - wristPose.position).normalized;
            }
            else
            {
                direction = (indexPose.position - wristPose.position).normalized;
            }

            return direction.sqrMagnitude > 0.0001f;
        }

        /// <summary>World-space hand distance used for two-hand scale gestures.</summary>
        public static bool TryGetHandSeparation(XRHandSubsystem subsystem, out float separationM)
        {
            separationM = 0f;
            if (subsystem == null) return false;
            if (!subsystem.leftHand.isTracked || !subsystem.rightHand.isTracked) return false;

            if (!subsystem.leftHand.GetJoint(XRHandJointID.Palm).TryGetPose(out var leftPalm) ||
                !subsystem.rightHand.GetJoint(XRHandJointID.Palm).TryGetPose(out var rightPalm))
                return false;

            separationM = Vector3.Distance(leftPalm.position, rightPalm.position);
            return true;
        }

        /// <summary>Whether the runtime reports hand tracking as usable at all.</summary>
        public static bool IsHandTrackingSupported(XRHandSubsystem subsystem) =>
            subsystem != null && subsystem.running;

        private static HandData Untracked(HandSide side, HandTrackingStatus status, long now) =>
            new HandData(side, status,
                System.Numerics.Vector3.Zero,
                System.Numerics.Quaternion.Identity,
                0f, false, false, now);

        private static System.Numerics.Vector3 ToNumerics(Vector3 value) =>
            new System.Numerics.Vector3(value.x, value.y, value.z);

        private static System.Numerics.Quaternion ToNumerics(Quaternion value) =>
            new System.Numerics.Quaternion(value.x, value.y, value.z, value.w);
    }
}