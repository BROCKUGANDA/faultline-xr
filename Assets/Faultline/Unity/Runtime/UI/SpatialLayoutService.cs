namespace Faultline.Runtime.UI
{
    using UnityEngine;

    using Faultline.Presentation;
    using Faultline.Spatial;

    /// <summary>
    /// Places panels inside the seated reach envelope for the current handedness
    /// preference. Screens ask for a zone and receive a world pose; they never
    /// hard-code coordinates, which keeps left-handed layouts and the comfort
    /// reset working without touching any screen logic.
    /// </summary>
    public sealed class SpatialLayoutService
    {
        private const float PanelDistanceM = 0.58f;
        private const float SideYawDegrees = 26f;
        private const float CentreHeightOffsetM = -0.04f;
        private const float SideHeightOffsetM = 0.02f;

        private Vector3 _headPosition = Vector3.zero;
        private Vector3 _headForward = Vector3.forward;
        private bool _leftHanded;

        public bool LeftHanded => _leftHanded;

        /// <summary>
        /// Capture the seated base pose. Called when the XR session starts and
        /// again on a comfort reset, so panels stay put during play.
        /// </summary>
        public void CaptureBasePose(Vector3 headPosition, Vector3 headForward)
        {
            _headPosition = headPosition;
            _headForward = headForward.normalized;
        }

        public void SetLeftHanded(bool leftHanded) => _leftHanded = leftHanded;

        public Vector3 PositionFor(SpatialZone zone, float widthMm)
        {
            var yaw = ZoneYawDegrees(zone);
            var height = zone == SpatialZone.Centre ? CentreHeightOffsetM : SideHeightOffsetM;
            var right = Vector3.Cross(Vector3.up, _headForward).normalized;
            var offset = Quaternion.AngleAxis(yaw, Vector3.up) * _headForward * PanelDistanceM;
            var position = _headPosition + offset + Vector3.up * height;
            // Side panels pull slightly inward so text faces the user centre.
            position -= right * (Mathf.Abs(yaw) / 45f) * 0.04f;
            return FaultlineUi.ClampToReach(position);
        }

        public Quaternion RotationFor(SpatialZone zone)
        {
            var position = PositionFor(zone, 640f);
            var toUser = _headPosition - position;
            toUser.y = 0f;
            if (toUser.sqrMagnitude < 0.0001f) toUser = -_headForward;
            return Quaternion.LookRotation(toUser.normalized);
        }

        /// <summary>Zone mirrored for the current handedness preference.</summary>
        public SpatialZone Mirror(SpatialZone zone)
        {
            if (!_leftHanded) return zone;
            switch (zone)
            {
                case SpatialZone.Left: return SpatialZone.Right;
                case SpatialZone.Right: return SpatialZone.Left;
                default: return zone;
            }
        }

        private float ZoneYawDegrees(SpatialZone zone)
        {
            switch (Mirror(zone))
            {
                case SpatialZone.Left: return SideYawDegrees;
                case SpatialZone.Right: return -SideYawDegrees;
                default: return 0f;
            }
        }
    }
}