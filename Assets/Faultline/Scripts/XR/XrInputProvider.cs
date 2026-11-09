namespace Faultline.XR
{
    using System;
    using System.Collections.Generic;
    using Faultline.Domain;
    using Faultline.Domain.Errors;

    /// <summary>
    /// Pure-C# input provider implementation skeleton.
    ///
    /// Wraps the <see cref="IXrSession"/> and exposes normalized input events
    /// (pinch, grab, open palm, two-hand scale, point/gaze) that the
    /// Interaction module consumes. No OpenXR or UnityEngine calls are made
    /// here — the platform integration replaces the internal probe once
    /// Unity is installed while keeping this contract.
    /// </summary>
    public sealed class XrInputProvider : IXrInputProvider
    {
        private readonly IXrSession _session;
        private bool _connected;
        private HandTrackingState _lastState;

        public event EventHandler<PinchEventArgs> PinchStarted;
        public event EventHandler<PinchEventArgs> PinchEnded;
        public event EventHandler<GrabEventArgs> GrabStarted;
        public event EventHandler<GrabEventArgs> GrabEnded;
        public event EventHandler<HandEventArgs> OpenPalmDetected;
        public event EventHandler<TwoHandScaleEventArgs> TwoHandScale;
        public event EventHandler<PointEventArgs> PointUpdated;

        /// <summary>Whether the provider is currently connected and active.</summary>
        public bool IsConnected => _connected;

        /// <summary>Latest hand-tracking state.</summary>
        public HandTrackingState HandTrackingState => _lastState;

        public XrInputProvider(IXrSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        /// <summary>Poll the provider for the latest input state.</summary>
        public Result<bool> Poll()
        {
            if (!_connected)
            {
                return Result<bool>.Failure(
                    ErrorCatalogue.XrSessionFailed(
                        Guid.NewGuid().ToString(),
                        new Dictionary<string, object> { { "stage", "Poll" } }).Code,
                    "Input provider is not connected. Start the XR session first.");
            }

            var stateResult = _session.GetHandTrackingState();
            if (!stateResult.IsSuccess)
            {
                return Result<bool>.Failure(stateResult.ErrorCode, stateResult.ErrorMessage);
            }

            _lastState = stateResult.Value;
            return Result<bool>.Success(true);
        }

        /// <summary>
        /// Connect the provider (call after session start). Conceptually binds
        /// to the platform hand-tracking subsystem.
        /// </summary>
        public Result<bool> Connect()
        {
            if (!_session.IsAvailable)
            {
                return Result<bool>.Failure(
                    ErrorCatalogue.XrSessionFailed(
                        Guid.NewGuid().ToString()).Code,
                    "Cannot connect input provider: XR session is not available.");
            }

            _connected = true;
            return Result<bool>.Success(true);
        }

        /// <summary>Disconnect the provider.</summary>
        public Result<bool> Disconnect()
        {
            _connected = false;
            return Result<bool>.Success(true);
        }

        /// <summary>
        /// Internal: raise a pinch-started event. Used by the platform
        /// integration once Unity is installed and by test harnesses.
        /// </summary>
        internal void RaisePinchStarted(HandSide hand, float confidence, System.Numerics.Vector3 position)
        {
            var now = DateTimeOffset.UtcNow.Ticks;
            PinchStarted?.Invoke(this, new PinchEventArgs(hand, confidence, position, now));
        }

        /// <summary>
        /// Internal: raise a pinch-ended event.
        /// </summary>
        internal void RaisePinchEnded(HandSide hand, System.Numerics.Vector3 position)
        {
            var now = DateTimeOffset.UtcNow.Ticks;
            PinchEnded?.Invoke(this, new PinchEventArgs(hand, 0f, position, now));
        }

        /// <summary>
        /// Internal: raise a grab-started event.
        /// </summary>
        internal void RaiseGrabStarted(HandSide hand, System.Numerics.Vector3 position)
        {
            var now = DateTimeOffset.UtcNow.Ticks;
            GrabStarted?.Invoke(this, new GrabEventArgs(hand, position, now));
        }

        /// <summary>
        /// Internal: raise a grab-ended event.
        /// </summary>
        internal void RaiseGrabEnded(HandSide hand, System.Numerics.Vector3 position)
        {
            var now = DateTimeOffset.UtcNow.Ticks;
            GrabEnded?.Invoke(this, new GrabEventArgs(hand, position, now));
        }

        /// <summary>
        /// Internal: raise an open-palm event.
        /// </summary>
        internal void RaiseOpenPalm(HandSide hand, System.Numerics.Vector3 position)
        {
            var now = DateTimeOffset.UtcNow.Ticks;
            OpenPalmDetected?.Invoke(this, new HandEventArgs(hand, position, now));
        }

        /// <summary>
        /// Internal: raise a two-hand scale event.
        /// </summary>
        internal void RaiseTwoHandScale(System.Numerics.Vector3 left, System.Numerics.Vector3 right, float deltaScale)
        {
            var now = DateTimeOffset.UtcNow.Ticks;
            TwoHandScale?.Invoke(this, new TwoHandScaleEventArgs(left, right, deltaScale, now));
        }

        /// <summary>
        /// Internal: raise a point/gaze event.
        /// </summary>
        internal void RaisePointUpdated(
            System.Numerics.Vector3 origin,
            System.Numerics.Vector3 direction,
            System.Numerics.Vector3? hitPosition,
            string hitNodeId)
        {
            var now = DateTimeOffset.UtcNow.Ticks;
            PointUpdated?.Invoke(this, new PointEventArgs(origin, direction, hitPosition, hitNodeId, now));
        }

        /// <summary>
        /// Internal: notify the provider that tracking was lost for a hand.
        /// </summary>
        internal void OnTrackingLost(HandSide hand, bool bothHands)
        {
            if (_session is XrSession simulated) simulated.SimulateTrackingLost(hand, bothHands);
        }

        /// <summary>
        /// Internal: notify the provider that tracking was recovered for a hand.
        /// </summary>
        internal void OnTrackingRecovered(HandSide hand, bool bothHands)
        {
            if (_session is XrSession simulated) simulated.SimulateTrackingRecovered(hand, bothHands);
        }
    }
}
