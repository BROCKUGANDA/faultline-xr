namespace Faultline.Runtime.Xr
{
    using System;
    using System.Collections.Generic;

    using UnityEngine;
    using UnityEngine.XR.Hands;
    using UnityEngine.XR.Management;

    using Faultline.Domain;
    using Faultline.Domain.Errors;
    using Faultline.XR;

    /// <summary>
    /// Device-backed <see cref="IXrSession"/> implementation. Drives the OpenXR
    /// session that the Meta XR runtime provides on Quest through Unity's XR
    /// Management loader stack, and reads hand tracking through the XR Hands
    /// subsystem that the Meta SDK depends on.
    ///
    /// The pure-C# <see cref="Faultline.XR.XrSession"/> remains the simulator
    /// fallback; presentation code only ever sees <see cref="IXrSession"/>, so
    /// swapping providers does not touch the domain or the views.
    /// </summary>
    public sealed class MetaXrSession : IXrSession
    {
        private readonly List<XRHandSubsystem> _subsystems = new();

        private bool _initialized;
        private bool _started;
        private HandTrackingStatus _leftStatus = HandTrackingStatus.Available;
        private HandTrackingStatus _rightStatus = HandTrackingStatus.Available;

        public bool IsAvailable => _started;

        public bool IsHandTrackingAvailable => HandSubsystem != null;

        /// <summary>The live hand subsystem, or null when unavailable.</summary>
        public XRHandSubsystem HandSubsystem
        {
            get
            {
                if (!_started) return null;
                SubsystemManager.GetSubsystems(_subsystems);
                return _subsystems.Count > 0 ? _subsystems[0] : null;
            }
        }

        public event EventHandler<XrSessionEventArgs> SessionStarted;
        public event EventHandler<XrSessionEventArgs> SessionEnded;
        public event EventHandler<XrTrackingEventArgs> TrackingLost;
        public event EventHandler<XrTrackingEventArgs> TrackingRecovered;

        public Result<bool> Initialize()
        {
            if (_initialized) return Result<bool>.Success(true);

            var manager = XRGeneralSettings.Instance?.Manager;
            if (manager == null)
            {
                return SessionFailure("XR Management settings are not available.");
            }

            if (!manager.isInitializationComplete && !manager.InitializeLoaderSync())
            {
                return SessionFailure("The XR loader failed to initialise. Check that the OpenXR loader is enabled for Android.");
            }

            _initialized = true;
            return Result<bool>.Success(true);
        }

        public Result<bool> StartSession()
        {
            if (!_initialized)
            {
                return SessionFailure("Initialize must succeed before StartSession.");
            }

            if (_started) return Result<bool>.Success(true);

            var manager = XRGeneralSettings.Instance?.Manager;
            if (manager == null || manager.activeLoader == null)
            {
                return SessionFailure("No active XR loader to start.");
            }

            manager.StartSubsystems();
            _started = true;
            _leftStatus = HandTrackingStatus.Available;
            _rightStatus = HandTrackingStatus.Available;

            SessionStarted?.Invoke(this, new XrSessionEventArgs(true));
            return Result<bool>.Success(true);
        }

        public Result<bool> EndSession()
        {
            if (!_started) return Result<bool>.Success(true);

            XRGeneralSettings.Instance?.Manager?.StopSubsystems();
            _started = false;
            _leftStatus = HandTrackingStatus.NotAvailable;
            _rightStatus = HandTrackingStatus.NotAvailable;

            SessionEnded?.Invoke(this, new XrSessionEventArgs(true));
            return Result<bool>.Success(true);
        }

        public Result<HandTrackingState> GetHandTrackingState()
        {
            if (!_started)
            {
                return Result<HandTrackingState>.Failure(
                    ErrorCatalogue.XrSessionFailed(NewCorrelationId()).Code,
                    "Cannot read hand tracking: the XR session is not active.");
            }

            var subsystem = HandSubsystem;
            if (subsystem == null)
            {
                return Result<HandTrackingState>.Failure(
                    ErrorCatalogue.XrHandTrackingUnavailable(NewCorrelationId()).Code,
                    "Hand tracking subsystem is not available on this device.");
            }

            var left = XrHandPoseReader.ReadHand(subsystem, HandSide.Left, _leftPinchActive);
            var right = XrHandPoseReader.ReadHand(subsystem, HandSide.Right, _rightPinchActive);
            var now = DateTimeOffset.UtcNow.Ticks;
            return Result<HandTrackingState>.Success(new HandTrackingState(left, right, now));
        }

        /// <summary>
        /// Per-frame update raised by the application driver: publishes
        /// TrackingLost / TrackingRecovered exactly on transitions so panels can
        /// cancel pending gestures without replaying them on recovery.
        /// </summary>
        public void UpdateTrackingEvents()
        {
            if (!_started) return;

            var subsystem = HandSubsystem;
            var left = XrHandPoseReader.ReadHand(subsystem, HandSide.Left, _leftPinchActive);
            var right = XrHandPoseReader.ReadHand(subsystem, HandSide.Right, _rightPinchActive);

            _leftPinchActive = left.PinchActive;
            _rightPinchActive = right.PinchActive;

            RaiseIfChanged(ref _leftStatus, left.Status, HandSide.Left);
            RaiseIfChanged(ref _rightStatus, right.Status, HandSide.Right);
        }

        private bool _leftPinchActive;
        private bool _rightPinchActive;

        private void RaiseIfChanged(ref HandTrackingStatus previous, HandTrackingStatus current, HandSide side)
        {
            if (previous == current) return;
            var wasTracking = previous == HandTrackingStatus.Tracking;
            var isTracking = current == HandTrackingStatus.Tracking;
            previous = current;

            var ticks = DateTimeOffset.UtcNow.Ticks;
            if (wasTracking && !isTracking)
            {
                TrackingLost?.Invoke(this, new XrTrackingEventArgs(side, Other(side), false, ticks));
            }
            else if (!wasTracking && isTracking)
            {
                TrackingRecovered?.Invoke(this, new XrTrackingEventArgs(Other(side), side, false, ticks));
            }
        }

        private static HandSide Other(HandSide side) =>
            side == HandSide.Left ? HandSide.Right : HandSide.Left;

        private static Result<bool> SessionFailure(string message) =>
            Result<bool>.Failure(
                ErrorCatalogue.XrSessionFailed(NewCorrelationId()).Code,
                message);

        private static string NewCorrelationId() => Guid.NewGuid().ToString("N");
    }
}