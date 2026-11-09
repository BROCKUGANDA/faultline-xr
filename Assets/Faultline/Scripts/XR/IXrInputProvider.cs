namespace Faultline.XR
{
    using System;

    /// <summary>
    /// Input provider abstraction. Simulated input and real platform input
    /// (Meta XR SDK hand tracking) both implement this contract so the
    /// Interaction module receives the same domain commands regardless of
    /// input source.
    ///
    /// No UnityEngine types are referenced here.
    /// </summary>
    public interface IXrInputProvider
    {
        /// <summary>Whether the provider is currently connected and active.</summary>
        bool IsConnected { get; }

        /// <summary>Latest hand-tracking state.</summary>
        HandTrackingState HandTrackingState { get; }

        /// <summary>Raised when a pinch gesture begins (thumb + index contact).</summary>
        event EventHandler<PinchEventArgs> PinchStarted;

        /// <summary>Raised when a pinch gesture ends (fingers separate).</summary>
        event EventHandler<PinchEventArgs> PinchEnded;

        /// <summary>Raised when a grab (sustained pinch) begins.</summary>
        event EventHandler<GrabEventArgs> GrabStarted;

        /// <summary>Raised when a grab (release) ends.</summary>
        event EventHandler<GrabEventArgs> GrabEnded;

        /// <summary>Raised when an open-palm gesture is detected (deselect/clear).</summary>
        event EventHandler<HandEventArgs> OpenPalmDetected;

        /// <summary>Raised when a two-hand scale gesture is detected (radial change).</summary>
        event EventHandler<TwoHandScaleEventArgs> TwoHandScale;

        /// <summary>Raised when a point/gaze ray targets a new world position.</summary>
        event EventHandler<PointEventArgs> PointUpdated;

        /// <summary>Poll the provider for the latest input state.</summary>
        /// <returns>Success or an FL-XR-* error.</returns>
        Faultline.Domain.Result<bool> Poll();
    }

    /// <summary>Pinch gesture event arguments.</summary>
    public sealed class PinchEventArgs : EventArgs
    {
        public HandSide Hand { get; }
        public float Confidence { get; }
        public System.Numerics.Vector3 Position { get; }
        public long TimestampTicks { get; }

        public PinchEventArgs(HandSide hand, float confidence, System.Numerics.Vector3 position, long timestampTicks)
        {
            Hand = hand;
            Confidence = confidence;
            Position = position;
            TimestampTicks = timestampTicks;
        }
    }

    /// <summary>Grab gesture event arguments.</summary>
    public sealed class GrabEventArgs : EventArgs
    {
        public HandSide Hand { get; }
        public System.Numerics.Vector3 Position { get; }
        public long TimestampTicks { get; }

        public GrabEventArgs(HandSide hand, System.Numerics.Vector3 position, long timestampTicks)
        {
            Hand = hand;
            Position = position;
            TimestampTicks = timestampTicks;
        }
    }

    /// <summary>Hand event arguments (open palm, etc.).</summary>
    public sealed class HandEventArgs : EventArgs
    {
        public HandSide Hand { get; }
        public System.Numerics.Vector3 Position { get; }
        public long TimestampTicks { get; }

        public HandEventArgs(HandSide hand, System.Numerics.Vector3 position, long timestampTicks)
        {
            Hand = hand;
            Position = position;
            TimestampTicks = timestampTicks;
        }
    }

    /// <summary>Two-hand scale event arguments.</summary>
    public sealed class TwoHandScaleEventArgs : EventArgs
    {
        public System.Numerics.Vector3 LeftPosition { get; }
        public System.Numerics.Vector3 RightPosition { get; }
        public float DeltaScale { get; } // >1 = scale up, <1 = scale down
        public long TimestampTicks { get; }

        public TwoHandScaleEventArgs(
            System.Numerics.Vector3 leftPosition,
            System.Numerics.Vector3 rightPosition,
            float deltaScale,
            long timestampTicks)
        {
            LeftPosition = leftPosition;
            RightPosition = rightPosition;
            DeltaScale = deltaScale;
            TimestampTicks = timestampTicks;
        }
    }

    /// <summary>Point/gaze event arguments.</summary>
    public sealed class PointEventArgs : EventArgs
    {
        public System.Numerics.Vector3 Origin { get; }
        public System.Numerics.Vector3 Direction { get; }
        public System.Numerics.Vector3? HitPosition { get; }
        public string HitNodeId { get; }
        public long TimestampTicks { get; }

        public PointEventArgs(
            System.Numerics.Vector3 origin,
            System.Numerics.Vector3 direction,
            System.Numerics.Vector3? hitPosition,
            string hitNodeId,
            long timestampTicks)
        {
            Origin = origin;
            Direction = direction;
            HitPosition = hitPosition;
            HitNodeId = hitNodeId;
            TimestampTicks = timestampTicks;
        }
    }
}
