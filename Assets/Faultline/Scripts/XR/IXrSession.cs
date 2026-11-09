namespace Faultline.XR
{
    using System;

    /// <summary>
    /// XR session lifecycle contract. Manages the OpenXR / Meta XR SDK session
    /// and exposes hand-tracking availability.
    ///
    /// Implementations live in the XR module. The concrete OpenXR-backed
    /// implementation is added once Unity is installed; this file is the pure
    /// C# abstraction that the rest of the codebase depends on.
    ///
    /// No UnityEngine types are referenced here.
    /// </summary>
    public interface IXrSession
    {
        /// <summary>Whether an XR session is currently active.</summary>
        bool IsAvailable { get; }

        /// <summary>Whether hand tracking is available on this device.</summary>
        bool IsHandTrackingAvailable { get; }

        /// <summary>Initialise the XR subsystem (OpenXR instance, etc.).</summary>
        /// <returns>Success or an FL-XR-* error.</returns>
        Faultline.Domain.Result<bool> Initialize();

        /// <summary>Start the XR session (begin tracking).</summary>
        /// <returns>Success or an FL-XR-* error.</returns>
        Faultline.Domain.Result<bool> StartSession();

        /// <summary>End the XR session (stop tracking, release resources).</summary>
        /// <returns>Success or an FL-XR-* error.</returns>
        Faultline.Domain.Result<bool> EndSession();

        /// <summary>
        /// Get the latest hand-tracking state for both hands. Returns a failure
        /// result (e.g. FL-XR-0011) when hand tracking is not available.
        /// </summary>
        Faultline.Domain.Result<HandTrackingState> GetHandTrackingState();

        /// <summary>Raised when the XR session has started successfully.</summary>
        event EventHandler<XrSessionEventArgs> SessionStarted;

        /// <summary>Raised when the XR session has ended.</summary>
        event EventHandler<XrSessionEventArgs> SessionEnded;

        /// <summary>
        /// Raised when tracking is lost (hand(s) go out of view / occluded).
        /// Carries which hand(s) are lost.
        /// </summary>
        event EventHandler<XrTrackingEventArgs> TrackingLost;

        /// <summary>
        /// Raised when tracking is recovered after a loss.
        /// Carries which hand(s) have recovered.
        /// </summary>
        event EventHandler<XrTrackingEventArgs> TrackingRecovered;
    }

    /// <summary>Arguments for session start/end events.</summary>
    public sealed class XrSessionEventArgs : EventArgs
    {
        public bool Success { get; }
        public string ErrorCode { get; }
        public string ErrorMessage { get; }

        public XrSessionEventArgs(bool success, string errorCode = null, string errorMessage = null)
        {
            Success = success;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
        }
    }

    /// <summary>Arguments for tracking lost/recovered events.</summary>
    public sealed class XrTrackingEventArgs : EventArgs
    {
        public HandSide LostHand { get; }
        public HandSide RecoveredHand { get; }
        public bool BothHands { get; }
        public long TimestampTicks { get; }

        public XrTrackingEventArgs(HandSide lostHand, HandSide recoveredHand, bool bothHands, long timestampTicks)
        {
            LostHand = lostHand;
            RecoveredHand = recoveredHand;
            BothHands = bothHands;
            TimestampTicks = timestampTicks;
        }
    }
}
