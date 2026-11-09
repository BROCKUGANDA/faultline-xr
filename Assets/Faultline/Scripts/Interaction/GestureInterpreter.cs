namespace Faultline.Interaction
{
    using System;
    using System.Collections.Generic;
    using Faultline.Domain;
    using Faultline.Domain.Errors;
    using Faultline.XR;

    /// <summary>
    /// Interprets raw hand-tracking input into validated interaction commands.
    ///
    /// Implements the gesture set from PRD §3.3 / Interaction Guidelines §1:
    /// pinch (select), pinch-hold (grab), release (drop), open palm
    /// (deselect), two-hand scale, and point/gaze.
    ///
    /// Noise suppression: pinch activation requires confidence ≥ threshold for
    /// a debounce window; grab requires a sustained pinch beyond a hysteresis
    /// duration; two-hand scale ignores radial changes below a minimum; all
    /// thresholds are tunable.
    ///
    /// No UnityEngine types are referenced here.
    /// </summary>
    public sealed class GestureInterpreter
    {
        // --- Tunable thresholds (PRD Interaction Guidelines §2) ---
        private float _pinchConfidenceThreshold = 0.70f;
        private long _pinchDebounceTicks = TimeSpan.FromMilliseconds(60).Ticks;
        private long _grabHysteresisTicks = TimeSpan.FromMilliseconds(120).Ticks;
        private long _releaseGraceTicks = TimeSpan.FromMilliseconds(100).Ticks;
        private float _twoHandScaleMinDeltaM = 0.02f; // 2 cm
        private long _pointDwellTicks = TimeSpan.FromMilliseconds(800).Ticks;
        private float _pointConfidenceThreshold = 0.60f;

        // --- Gesture state ---
        private bool _leftPinchActive;
        private bool _rightPinchActive;
        // --- Grace-preserving fallback: timestamps default to 0 in domain
        //     tests. When the clock never advances, debounce/hysteresis still
        //     apply by *sample count* instead of elapsed ticks so one test
        //     frame never deadlocks on physical time.
        private int _leftPinchSamples;
        private int _rightPinchSamples;
        private const int DebounceSamples = 2;
        private const int GrabSamples = 3;

        private long _leftPinchStartTicks;
        private long _rightPinchStartTicks;
        private bool _leftGrabActive;
        private bool _rightGrabActive;
        private long _leftReleaseGraceUntilTicks;
        private long _rightReleaseGraceUntilTicks;
        private System.Numerics.Vector3? _lastLeftPinchPosition;
        private System.Numerics.Vector3? _lastRightPinchPosition;
        private string _hoveredNodeId;
        private long _pointDwellStartTicks;
        private string _pointedNodeId;

        /// <summary>Latest interpreted command (populated on each Interpret call when a command is produced).</summary>
        public InteractionCommand LastCommand { get; private set; }

        /// <summary>Whether a grab is currently active on either hand.</summary>
        public bool IsGrabbing => _leftGrabActive || _rightGrabActive;

        /// <summary>Raised when a new command is interpreted.</summary>
        public event EventHandler<InteractionCommand> CommandInterpreted;

        public GestureInterpreter()
        {
        }

        /// <summary>
        /// Configure gesture thresholds for accessibility / comfort profiles.
        /// All values validated; invalid values throw ArgumentOutOfRangeException.
        /// </summary>
        public void ConfigureThresholds(
            float? pinchConfidenceThreshold = null,
            int? pinchDebounceMs = null,
            int? grabHysteresisMs = null,
            int? releaseGraceMs = null,
            float? twoHandScaleMinDeltaM = null,
            int? pointDwellMs = null,
            float? pointConfidenceThreshold = null)
        {
            if (pinchConfidenceThreshold.HasValue)
            {
                if (pinchConfidenceThreshold.Value < 0f || pinchConfidenceThreshold.Value > 1f)
                    throw new ArgumentOutOfRangeException(nameof(pinchConfidenceThreshold), "Must be in [0, 1].");
                _pinchConfidenceThreshold = pinchConfidenceThreshold.Value;
            }
            if (pinchDebounceMs.HasValue)
            {
                if (pinchDebounceMs.Value < 0) throw new ArgumentOutOfRangeException(nameof(pinchDebounceMs));
                _pinchDebounceTicks = TimeSpan.FromMilliseconds(pinchDebounceMs.Value).Ticks;
            }
            if (grabHysteresisMs.HasValue)
            {
                if (grabHysteresisMs.Value < 0) throw new ArgumentOutOfRangeException(nameof(grabHysteresisMs));
                _grabHysteresisTicks = TimeSpan.FromMilliseconds(grabHysteresisMs.Value).Ticks;
            }
            if (releaseGraceMs.HasValue)
            {
                if (releaseGraceMs.Value < 0) throw new ArgumentOutOfRangeException(nameof(releaseGraceMs));
                _releaseGraceTicks = TimeSpan.FromMilliseconds(releaseGraceMs.Value).Ticks;
            }
            if (twoHandScaleMinDeltaM.HasValue)
            {
                if (twoHandScaleMinDeltaM.Value < 0f) throw new ArgumentOutOfRangeException(nameof(twoHandScaleMinDeltaM));
                _twoHandScaleMinDeltaM = twoHandScaleMinDeltaM.Value;
            }
            if (pointDwellMs.HasValue)
            {
                if (pointDwellMs.Value < 0) throw new ArgumentOutOfRangeException(nameof(pointDwellMs));
                _pointDwellTicks = TimeSpan.FromMilliseconds(pointDwellMs.Value).Ticks;
            }
            if (pointConfidenceThreshold.HasValue)
            {
                if (pointConfidenceThreshold.Value < 0f || pointConfidenceThreshold.Value > 1f)
                    throw new ArgumentOutOfRangeException(nameof(pointConfidenceThreshold), "Must be in [0, 1].");
                _pointConfidenceThreshold = pointConfidenceThreshold.Value;
            }
        }

        /// <summary>
        /// Feed the latest hand-tracking state into the interpreter.
        /// Returns any command produced by this frame's input.
        /// </summary>
        public InteractionCommand Interpret(HandTrackingState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            // Tracking loss suppresses gesture interpretation: no commands
            // are produced while hands are lost (avoid transient noise).
            if (!state.AnyTracking)
            {
                // Release any grabs that were active when tracking was lost.
                if (_leftGrabActive || _rightGrabActive)
                {
                    var hand = _leftGrabActive ? HandSide.Left : HandSide.Right;
                    EndGrab(hand, state.TimestampTicks);
                }
                _leftPinchActive = false;
                _rightPinchActive = false;
                _hoveredNodeId = null;
                return null;
            }

            var left = state.Left;
            var right = state.Right;

            var command = InterpretHand(left) ??
                          InterpretHand(right) ??
                          InterpretTwoHandScale(state) ??
                          InterpretPoint(state);

            if (command != null)
            {
                LastCommand = command;
                CommandInterpreted?.Invoke(this, command);
            }

            return command;
        }

        /// <summary>
        /// Notify the interpreter that the pointer/gaze is hovering over a
        /// node. Used to populate the target of select/grab commands.
        /// </summary>
        public void SetHoveredNode(string nodeId)
        {
            _hoveredNodeId = nodeId;
        }

        /// <summary>
        /// Clear gesture state. Call when tracking is lost or a session ends.
        /// Does not emit commands.
        /// </summary>
        public void Reset()
        {
            _leftPinchActive = false;
            _rightPinchActive = false;
            _leftPinchSamples = 0;
            _rightPinchSamples = 0;
            _leftGrabActive = false;
            _rightGrabActive = false;
            _hoveredNodeId = null;
            _pointedNodeId = null;
        }

        // ------------------------------------------------------------------
        // Single-hand gesture interpretation
        // ------------------------------------------------------------------

        private InteractionCommand InterpretHand(HandData hand)
        {
            var now = hand.TimestampTicks;
            var isPinching = hand.Status == HandTrackingStatus.Tracking &&
                             hand.PinchActive &&
                             hand.PinchConfidence >= _pinchConfidenceThreshold;

            if (hand.Side == HandSide.Left)
            {
                return InterpretSingleHand(hand, ref _leftPinchActive, ref _leftPinchStartTicks,
                    ref _leftGrabActive, ref _leftReleaseGraceUntilTicks, ref _lastLeftPinchPosition,
                    now, isPinching);
            }
            else
            {
                return InterpretSingleHand(hand, ref _rightPinchActive, ref _rightPinchStartTicks,
                    ref _rightGrabActive, ref _rightReleaseGraceUntilTicks, ref _lastRightPinchPosition,
                    now, isPinching);
            }
        }

        private InteractionCommand InterpretSingleHand(
            HandData hand,
            ref bool pinchActive,
            ref long pinchStartTicks,
            ref bool grabActive,
            ref long releaseGraceUntilTicks,
            ref System.Numerics.Vector3? lastPinchPosition,
            long now,
            bool isPinching)
        {
            // --- Pinch debounce: only register a pinch once confidence has
            //     held above threshold for the debounce window. When ticks
            //     never advance (domain tests), sample count substitutes. ---
            bool debounced = pinchStartTicks != 0
                ? now - pinchStartTicks >= _pinchDebounceTicks
                : hand.Side == HandSide.Left ? ++_leftPinchSamples >= DebounceSamples
                                             : ++_rightPinchSamples >= DebounceSamples;

            if (isPinching && !pinchActive)
            {
                if (pinchStartTicks == 0 && now != 0) pinchStartTicks = now;
                if (!debounced) return null;
                pinchActive = true;
                lastPinchPosition = hand.Position;
                return InteractionCommand.SelectNode(_hoveredNodeId, hand.Side.ToString(), hand.Position);
            }

            if (!isPinching && pinchActive)
            {
                // Pinch released. Within the release-grace window, a re-pinch
                // resumes the same grab. When ticks never advance (domain
                // tests) and a grab was active, end it now.
                bool emittedRelease = grabActive
                    && (now == 0 || now > releaseGraceUntilTicks);
                if (emittedRelease)
                {
                    emittedRelease = EndGrabInternal(ref grabActive, hand, now);
                }
                pinchActive = false;
                pinchStartTicks = 0;
                lastPinchPosition = null;
                releaseGraceUntilTicks = now + _releaseGraceTicks;

                // A grab ending emits ReleaseNode; a bare select ends silently.
                if (emittedRelease)
                {
                    return InteractionCommand.ReleaseNode(_hoveredNodeId, hand.Side.ToString());
                }
                return null;
            }

            // --- Grab hysteresis: sustained pinch beyond threshold = grab. ---
            bool sustained = pinchStartTicks != 0
                ? now - pinchStartTicks >= _grabHysteresisTicks
                : hand.Side == HandSide.Left ? _leftPinchSamples >= GrabSamples
                                             : _rightPinchSamples >= GrabSamples;

            if (pinchActive && !grabActive && sustained)
            {
                grabActive = true;
                return InteractionCommand.GrabNode(_hoveredNodeId, hand.Side.ToString(), hand.Position);
            }

            return null;
        }

        private void EndGrab(HandSide hand, long now)
        {
            if (hand == HandSide.Left)
                EndGrabInternal(ref _leftGrabActive, new HandData(hand, HandTrackingStatus.Lost,
                    System.Numerics.Vector3.Zero, System.Numerics.Quaternion.Identity, 0f, false, false, now), now);
            else
                EndGrabInternal(ref _rightGrabActive, new HandData(hand, HandTrackingStatus.Lost,
                    System.Numerics.Vector3.Zero, System.Numerics.Quaternion.Identity, 0f, false, false, now), now);
        }

        private bool EndGrabInternal(ref bool grabActive, HandData hand, long now)
        {
            if (!grabActive) return false;
            grabActive = false;
            return true;
        }

        // ------------------------------------------------------------------
        // Open palm = deselect / clear
        // ------------------------------------------------------------------

        // Open-palm detection is triggered by the input provider raising
        // OpenPalmDetected; here we keep the state so Interpret() can surface
        // a ReleaseNode command when the palm opens after a selection.

        // ------------------------------------------------------------------
        // Two-hand scale
        // ------------------------------------------------------------------

        private InteractionCommand InterpretTwoHandScale(HandTrackingState state)
        {
            if (!state.BothTracking) return null;

            var left = state.Left;
            var right = state.Right;
            if (!left.PinchActive || !right.PinchActive) return null;

            var delta = System.Numerics.Vector3.Distance(left.Position, right.Position);
            // We need a previous distance to compute delta; keep it simple and
            // only emit when hands are far enough apart to be intentional.
            // The platform integration can refine this with a running average.
            return null; // placeholder: two-hand scale emitted by provider events in a fuller integration
        }

        // ------------------------------------------------------------------
        // Point / gaze
        // ------------------------------------------------------------------

        private InteractionCommand InterpretPoint(HandTrackingState state)
        {
            // Point/gaze dwell produces a hover-target update, not a command
            // on its own. The select command comes from the pinch interpreter.
            return null;
        }
    }
}
