namespace Faultline.Runtime.Xr
{
    using System;

    using UnityEngine;
    using UnityEngine.XR.Hands;

    using Faultline.Domain;
    using Faultline.XR;

    /// <summary>
    /// Device-backed <see cref="IXrInputProvider"/>. Converts XR Hands joint
    /// poses into pinch / grab / open-palm / two-hand-scale / point events so
    /// the Interaction layer receives identical commands from real hands and
    /// from the simulated provider used in tests.
    ///
    /// Gesture edges are debounced by state transitions, not timers alone, so a
    /// noisy tracking frame cannot emit a duplicate selection.
    /// </summary>
    public sealed class MetaXrInputProvider : IXrInputProvider
    {
        private const float GrabHoldSeconds = 0.12f;
        private const float OpenPalmThumbSpanM = 0.060f;
        private const float OpenPalmFingerSpanM = 0.070f;
        private const float TwoHandMinSeparationDeltaM = 0.02f;

        private readonly MetaXrSession _session;

        private HandTrackingState _lastState;
        private bool _leftPinchDown;
        private bool _rightPinchDown;
        private bool _leftGrabDown;
        private bool _rightGrabDown;
        private bool _leftPalmAnnounced;
        private bool _rightPalmAnnounced;
        private long _leftPinchStartTicks;
        private long _rightPinchStartTicks;
        private float _previousSeparationM;
        private Vector3 _leftRayOrigin;
        private Vector3 _leftRayDirection;
        private Vector3 _rightRayOrigin;
        private Vector3 _rightRayDirection;
        private bool _hasLeftRay;
        private bool _hasRightRay;

        public MetaXrInputProvider(MetaXrSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public bool IsConnected => _session.IsAvailable && _session.HandSubsystem != null;

        public HandTrackingState HandTrackingState => _lastState;

        /// <summary>
        /// Optional resolver supplied by the presentation layer to map a world
        /// ray onto a graph node id. Left null the point events still carry the
        /// ray, which is all the interaction layer requires.
        /// </summary>
        public IXrRayHitResolver RayHitResolver { get; set; }

        public event EventHandler<PinchEventArgs> PinchStarted;
        public event EventHandler<PinchEventArgs> PinchEnded;
        public event EventHandler<GrabEventArgs> GrabStarted;
        public event EventHandler<GrabEventArgs> GrabEnded;
        public event EventHandler<HandEventArgs> OpenPalmDetected;
        public event EventHandler<TwoHandScaleEventArgs> TwoHandScale;
        public event EventHandler<PointEventArgs> PointUpdated;

        public Result<bool> Poll()
        {
            var state = _session.GetHandTrackingState();
            if (!state.IsSuccess)
            {
                return Result<bool>.Failure(state.ErrorCode, state.ErrorMessage);
            }

            var subsystem = _session.HandSubsystem;
            _lastState = state.Value;

            var leftTicks = _lastState.Left.TimestampTicks;
            var rightTicks = _lastState.Right.TimestampTicks;

            UpdatePinchAndGrab(subsystem, HandSide.Left, _lastState.Left, leftTicks,
                ref _leftPinchDown, ref _leftGrabDown, ref _leftPalmAnnounced);
            UpdatePinchAndGrab(subsystem, HandSide.Right, _lastState.Right, rightTicks,
                ref _rightPinchDown, ref _rightGrabDown, ref _rightPalmAnnounced);
            UpdateTwoHandScale(subsystem, leftTicks);
            UpdatePointRays(subsystem, leftTicks, rightTicks);

            return Result<bool>.Success(true);
        }

        /// <summary>Rays currently reported for each hand, in tracking-space metres.</summary>
        public bool TryGetPointerRay(HandSide side, out Vector3 origin, out Vector3 direction)
        {
            if (side == HandSide.Left)
            {
                origin = _leftRayOrigin;
                direction = _leftRayDirection;
                return _hasLeftRay;
            }

            origin = _rightRayOrigin;
            direction = _rightRayDirection;
            return _hasRightRay;
        }

        /// <summary>Clears latch state; call when tracking is lost or the session ends.</summary>
        public void Reset()
        {
            _leftPinchDown = false;
            _rightPinchDown = false;
            _leftGrabDown = false;
            _rightGrabDown = false;
            _leftPalmAnnounced = false;
            _rightPalmAnnounced = false;
            _previousSeparationM = 0f;
            _leftPinchStartTicks = 0;
            _rightPinchStartTicks = 0;
        }

        private void UpdatePinchAndGrab(XRHandSubsystem subsystem, HandSide side, HandData hand,
            long ticks, ref bool pinchDown, ref bool grabDown, ref bool palmAnnounced)
        {
            if (hand.Status != HandTrackingStatus.Tracking)
            {
                if (grabDown)
                {
                    grabDown = false;
                    GrabEnded?.Invoke(this, new GrabEventArgs(side, ToNumerics(hand.Position), ticks));
                }
                if (pinchDown)
                {
                    pinchDown = false;
                    PinchEnded?.Invoke(this,
                        new PinchEventArgs(side, hand.PinchConfidence, ToNumerics(hand.Position), ticks));
                }
                palmAnnounced = false;
                return;
            }

            if (hand.PinchActive && !pinchDown)
            {
                pinchDown = true;
                palmAnnounced = false;
                SetPinchStartTicks(side, ticks);
                PinchStarted?.Invoke(this,
                    new PinchEventArgs(side, hand.PinchConfidence, ToNumerics(hand.Position), ticks));
            }
            else if (!hand.PinchActive && pinchDown)
            {
                pinchDown = false;
                if (grabDown)
                {
                    grabDown = false;
                    GrabEnded?.Invoke(this, new GrabEventArgs(side, ToNumerics(hand.Position), ticks));
                }
                PinchEnded?.Invoke(this,
                    new PinchEventArgs(side, hand.PinchConfidence, ToNumerics(hand.Position), ticks));
            }

            if (pinchDown && !grabDown &&
                TimeSpan.FromTicks(ticks - PinchStartTicks(side)).TotalSeconds >= GrabHoldSeconds)
            {
                grabDown = true;
                GrabStarted?.Invoke(this, new GrabEventArgs(side, ToNumerics(hand.Position), ticks));
            }

            if (!pinchDown && !palmAnnounced && IsOpenPalm(subsystem, side))
            {
                palmAnnounced = true;
                OpenPalmDetected?.Invoke(this, new HandEventArgs(side, ToNumerics(hand.Position), ticks));
            }
        }

        private void UpdateTwoHandScale(XRHandSubsystem subsystem, long ticks)
        {
            if (!XrHandPoseReader.TryGetHandSeparation(subsystem, out var separationM))
            {
                _previousSeparationM = 0f;
                return;
            }

            if (!(_lastState.Left.PinchActive && _lastState.Right.PinchActive))
            {
                _previousSeparationM = separationM;
                return;
            }

            if (_previousSeparationM > 0f &&
                Mathf.Abs(separationM - _previousSeparationM) >= TwoHandMinSeparationDeltaM)
            {
                var delta = separationM / _previousSeparationM;
                TwoHandScale?.Invoke(this, new TwoHandScaleEventArgs(
                    ToNumerics(_lastState.Left.Position),
                    ToNumerics(_lastState.Right.Position),
                    delta, ticks));
            }

            _previousSeparationM = separationM;
        }

        private void UpdatePointRays(XRHandSubsystem subsystem, long leftTicks, long rightTicks)
        {
            _hasLeftRay = XrHandPoseReader.TryGetPointerRay(subsystem, HandSide.Left,
                out _leftRayOrigin, out _leftRayDirection);
            _hasRightRay = XRHandPoseReader.TryGetPointerRay(subsystem, HandSide.Right,
                out _rightRayOrigin, out _rightRayDirection);

            if (_hasRightRay)
            {
                PointUpdated?.Invoke(this, BuildPoint(HandSide.Right, _rightRayOrigin,
                    _rightRayDirection, rightTicks));
            }
            else if (_hasLeftRay)
            {
                PointUpdated?.Invoke(this, BuildPoint(HandSide.Left, _leftRayOrigin,
                    _leftRayDirection, leftTicks));
            }
        }

        private PointEventArgs BuildPoint(HandSide side, Vector3 origin, Vector3 direction, long ticks)
        {
            string hitNodeId = null;
            System.Numerics.Vector3? hit = null;

            if (RayHitResolver != null &&
                RayHitResolver.TryResolve(origin, direction, out var nodeId, out var distance))
            {
                hitNodeId = nodeId;
                hit = ToNumerics(origin + direction * Mathf.Max(0.05f, distance));
            }

            return new PointEventArgs(ToNumerics(origin), ToNumerics(direction), hit, hitNodeId, ticks);
        }

        private void SetPinchStartTicks(HandSide side, long ticks)
        {
            if (side == HandSide.Left) _leftPinchStartTicks = ticks;
            else _rightPinchStartTicks = ticks;
        }

        private long PinchStartTicks(HandSide side) =>
            side == HandSide.Left ? _leftPinchStartTicks : _rightPinchStartTicks;

        private static bool IsOpenPalm(XRHandSubsystem subsystem, HandSide side)
        {
            if (subsystem == null) return false;
            var hand = side == HandSide.Left ? subsystem.leftHand : subsystem.rightHand;
            if (!hand.isTracked) return false;

            if (!hand.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out var thumb) ||
                !hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out var index) ||
                !hand.GetJoint(XRHandJointID.MiddleTip).TryGetPose(out var middle) ||
                !hand.GetJoint(XRHandJointID.PinkyTip).TryGetPose(out var pinky))
                return false;

            var thumbSpread = Vector3.Distance(thumb.position, middle.position);
            var fingerSpread = Vector3.Distance(index.position, pinky.position);
            return thumbSpread > OpenPalmThumbSpanM && fingerSpread > OpenPalmFingerSpanM;
        }

        private static System.Numerics.Vector3 ToNumerics(Vector3 value) =>
            new System.Numerics.Vector3(value.x, value.y, value.z);
    }
}
namespace Faultline.Runtime.Xr
{
    using UnityEngine;

    /// <summary>
    /// Resolves a world-space pointer ray onto the node it is currently
    /// hovering, so the input provider can publish hit metadata without
    /// knowing anything about the scene graph.
    /// </summary>
    public interface IXrRayHitResolver
    {
        bool TryResolve(Vector3 origin, Vector3 direction, out string nodeId, out float distanceM);
    }
}
