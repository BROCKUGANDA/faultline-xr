namespace Faultline.XR
{
    using System;
    using System.Collections.Generic;
    using Faultline.Domain;
    using Faultline.Domain.Errors;

    /// <summary>
    /// Pure-C# XR session implementation skeleton.
    ///
    /// Conceptually wraps OpenXR / Meta XR SDK: reports session availability,
    /// hand-tracking availability, and tracking loss/recovery. No OpenXR or
    /// UnityEngine calls are made here — when Unity is installed, the
    /// OpenXR-backed integration replaces the platform probes below while
    /// keeping this same contract.
    ///
    /// All state transitions are validated: StartSession requires Initialize,
    /// EndSession requires StartSession, GetHandTrackingState requires an
    /// active session.
    /// </summary>
    public sealed class XrSession : IXrSession
    {
        private bool _initialized;
        private bool _started;
        private bool _handTrackingSupported;
        private HandTrackingStatus _leftStatus = HandTrackingStatus.Available;
        private HandTrackingStatus _rightStatus = HandTrackingStatus.Available;

        /// <summary>Whether an XR session is currently active.</summary>
        public bool IsAvailable => _started;

        /// <summary>Whether hand tracking is available on this device.</summary>
        public bool IsHandTrackingAvailable => _handTrackingSupported;

        public event EventHandler<XrSessionEventArgs> SessionStarted;
        public event EventHandler<XrSessionEventArgs> SessionEnded;
        public event EventHandler<XrTrackingEventArgs> TrackingLost;
        public event EventHandler<XrTrackingEventArgs> TrackingRecovered;

        /// <summary>
        /// Initialise the XR subsystem. Probes platform capabilities
        /// (conceptually OpenXR instance creation + hand-extension check).
        /// Safe to call multiple times; idempotent.
        /// </summary>
        public Result<bool> Initialize()
        {
            if (_initialized)
            {
                return Result<bool>.Success(true);
            }

            // Conceptual probe: on a real device this would call into the
            // OpenXR/Meta XR SDK to query XR_EXT_hand_tracking support.
            // Here we mark hand tracking as supported so the abstraction
            // can be exercised end-to-end without a headset.
            _handTrackingSupported = true;
            _initialized = true;

            return Result<bool>.Success(true);
        }

        /// <summary>Start the XR session (begin tracking).</summary>
        public Result<bool> StartSession()
        {
            if (!_initialized)
            {
                return Result<bool>.Failure(
                    ErrorCatalogue.XrSessionFailed(
                        Guid.NewGuid().ToString(),
                        new Dictionary<string, object> { { "stage", "StartSession" } }).Code,
                    "XR session must be initialized before StartSession.");
            }

            if (_started)
            {
                return Result<bool>.Success(true);
            }

            _started = true;
            _leftStatus = HandTrackingStatus.Available;
            _rightStatus = HandTrackingStatus.Available;

            SessionStarted?.Invoke(this, new XrSessionEventArgs(true));
            return Result<bool>.Success(true);
        }

        /// <summary>End the XR session (stop tracking, release resources).</summary>
        public Result<bool> EndSession()
        {
            if (!_started)
            {
                return Result<bool>.Success(true);
            }

            _started = false;
            _leftStatus = HandTrackingStatus.NotAvailable;
            _rightStatus = HandTrackingStatus.NotAvailable;

            SessionEnded?.Invoke(this, new XrSessionEventArgs(true));
            return Result<bool>.Success(true);
        }

        /// <summary>
        /// Get the latest hand-tracking state for both hands.
        /// Returns FL-XR-0011 when hand tracking is unavailable.
        /// </summary>
        public Result<HandTrackingState> GetHandTrackingState()
        {
            if (!_started)
            {
                return Result<HandTrackingState>.Failure(
                    ErrorCatalogue.XrSessionFailed(
                        Guid.NewGuid().ToString(),
                        new Dictionary<string, object> { { "stage", "GetHandTrackingState" } }).Code,
                    "Cannot get hand-tracking state: XR session is not active.");
            }

            if (!_handTrackingSupported)
            {
                return Result<HandTrackingState>.Failure(
                    ErrorCatalogue.XrHandTrackingUnavailable(
                        Guid.NewGuid().ToString()).Code,
                    "Hand tracking is not available on this device.");
            }

            var now = DateTimeOffset.UtcNow.Ticks;
            var left = new HandData(
                HandSide.Left, _leftStatus,
                System.Numerics.Vector3.Zero, System.Numerics.Quaternion.Identity,
                0f, false, false, now);
            var right = new HandData(
                HandSide.Right, _rightStatus,
                System.Numerics.Vector3.Zero, System.Numerics.Quaternion.Identity,
                0f, false, false, now);

            return Result<HandTrackingState>.Success(
                new HandTrackingState(left, right, now));
        }

        /// <summary>
        /// Internal: simulate tracking loss for a hand. Used by tests and by
        /// the platform integration once Unity is installed.
        /// </summary>
        internal void SimulateTrackingLost(HandSide hand, bool bothHands)
        {
            if (hand == HandSide.Left || bothHands)
                _leftStatus = HandTrackingStatus.Lost;
            if (hand == HandSide.Right || bothHands)
                _rightStatus = HandTrackingStatus.Lost;

            TrackingLost?.Invoke(this, new XrTrackingEventArgs(
                hand == HandSide.Left ? hand : HandSide.Right,
                hand == HandSide.Right ? hand : HandSide.Left,
                bothHands, DateTimeOffset.UtcNow.Ticks));
        }

        /// <summary>
        /// Internal: simulate tracking recovery for a hand. Used by tests and
        /// by the platform integration once Unity is installed.
        /// </summary>
        internal void SimulateTrackingRecovered(HandSide hand, bool bothHands)
        {
            if (hand == HandSide.Left || bothHands)
                _leftStatus = HandTrackingStatus.Tracking;
            if (hand == HandSide.Right || bothHands)
                _rightStatus = HandTrackingStatus.Tracking;

            TrackingRecovered?.Invoke(this, new XrTrackingEventArgs(
                HandSide.Left, HandSide.Right, bothHands, DateTimeOffset.UtcNow.Ticks));
        }
    }
}
