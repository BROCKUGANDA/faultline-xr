namespace Faultline.Runtime.UI
{
    using System;

    using UnityEngine;

    using Faultline.Presentation;
    using Faultline.XR;

    /// <summary>
    /// Developer-input bridge used only when no hand-tracking device is
    /// present (Editor play mode). It raises the same
    /// <see cref="IXrInputProvider"/> events the device provider raises, so
    /// every screen and command path under test is the production path.
    ///
    /// This is a development aid, never a substitute for device validation:
    /// the release journey is only claimed after a headset run.
    /// </summary>
    public sealed class SimulatedInputProvider : IXrInputProvider
    {
        private readonly IXrSession _session;
        private HandTrackingState _lastState;
        private bool _pinchDown;
        private long _pinchStartTicks;

        public SimulatedInputProvider(IXrSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public bool IsConnected => _session.IsAvailable;
        public HandTrackingState HandTrackingState => _lastState;

        public event EventHandler<PinchEventArgs> PinchStarted;
        public event EventHandler<PinchEventArgs> PinchEnded;
        public event EventHandler<GrabEventArgs> GrabStarted;
        public event EventHandler<GrabEventArgs> GrabEnded;
        public event EventHandler<HandEventArgs> OpenPalmDetected;
        public event EventHandler<TwoHandScaleEventArgs> TwoHandScale;
        public event EventHandler<PointEventArgs> PointUpdated;

        /// <summary>Pointer pose supplied by the caller (mouse ray in the Editor).</summary>
        public Func<(Vector3 origin, Vector3 direction)> EditorPointerRay { get; set; }

        public Result<bool> Poll()
        {
            var state = _session.GetHandTrackingState();
            if (!state.IsSuccess) return Result<bool>.Failure(state.ErrorCode, state.ErrorMessage);
            _lastState = state.Value;

            var now = DateTimeOffset.UtcNow.Ticks;
#if UNITY_EDITOR || UNITY_STANDALONE
            var pointer = EditorPointerRay != null ? EditorPointerRay() : (Vector3.zero, Vector3.forward);
            PointUpdated?.Invoke(this, new PointEventArgs(
                new System.Numerics.Vector3(pointer.origin.x, pointer.origin.y, pointer.origin.z),
                new System.Numerics.Vector3(pointer.direction.x, pointer.direction.y, pointer.direction.z),
                null, null, now));

            if (Input.GetMouseButtonDown(0))
            {
                _pinchDown = true;
                _pinchStartTicks = now;
                PinchStarted?.Invoke(this, new PinchEventArgs(HandSide.Right, 1f,
                    new System.Numerics.Vector3(pointer.origin.x, pointer.origin.y, pointer.origin.z), now));
            }
            else if (_pinchDown && Input.GetMouseButtonUp(0))
            {
                _pinchDown = false;
                PinchEnded?.Invoke(this, new PinchEventArgs(HandSide.Right, 1f,
                    new System.Numerics.Vector3(pointer.origin.x, pointer.origin.y, pointer.origin.z), now));
            }

            if (_pinchDown && !GrabActive &&
                TimeSpan.FromTicks(now - _pinchStartTicks).TotalSeconds >= 0.12f)
            {
                GrabActive = true;
                GrabStarted?.Invoke(this, new GrabEventArgs(HandSide.Right,
                    new System.Numerics.Vector3(pointer.origin.x, pointer.origin.y, pointer.origin.z), now));
            }
#endif
            return Result<bool>.Success(true);
        }

        internal bool GrabActive { get; set; }

        internal void RaiseOpenPalm(HandSide side, System.Numerics.Vector3 position) =>
            OpenPalmDetected?.Invoke(this, new HandEventArgs(side, position, DateTimeOffset.UtcNow.Ticks));

        internal void RaiseTwoHandScale(float delta) =>
            TwoHandScale?.Invoke(this, new TwoHandScaleEventArgs(
                System.Numerics.Vector3.Zero, System.Numerics.Vector3.Zero, delta,
                DateTimeOffset.UtcNow.Ticks));
    }
}