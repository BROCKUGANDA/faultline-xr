namespace Faultline.Interaction
{
    using System;

    /// <summary>
    /// Lifecycle states of an interactable element (node, panel, anchor).
    ///
    /// Valid transitions:
    ///   IDLE -> HOVERED (pointer/ray enters)
    ///   HOVERED -> IDLE (pointer/ray exits)
    ///   HOVERED -> SELECTED (brief pinch / tap)
    ///   SELECTED -> HOVERED (deselect / release without grab)
    ///   SELECTED -> GRABBED (sustained pinch / grab-hold)
    ///   GRABBED -> SELECTED (release drag without dropping — reselect)
    ///   GRABBED -> IDLE (drop / release)
    ///   SELECTED -> RELEASED (explicit release / deselect)
    ///   GRABBED -> RELEASED (explicit release after drag)
    ///   RELEASED -> IDLE (cooldown complete)
    ///   IDLE -> GRABBED (direct grab on grabbable without prior hover)
    ///   HOVERED -> GRABBED (direct grab from hover)
    ///
    /// Invalid transitions are rejected by <see cref="TryTransition"/>.
    /// </summary>
    public enum InteractionState
    {
        Idle,
        Hovered,
        Selected,
        Grabbed,
        Released
    }

    /// <summary>
    /// Validates and performs state transitions on an interactable element.
    /// </summary>
    public sealed class InteractionStateMachine
    {
        private InteractionState _state = InteractionState.Idle;

        /// <summary>Current interaction state.</summary>
        public InteractionState Current => _state;

        /// <summary>Raised on every successful transition.</summary>
        public event EventHandler<InteractionTransitionEventArgs> Transitioned;

        /// <summary>
        /// Attempt a transition to <paramref name="nextState"/>.
        /// Returns true when the transition is valid and applied; false when
        /// the transition is invalid for the current state (no side effects).
        /// </summary>
        public bool TryTransition(InteractionState nextState, string reason = null)
        {
            if (!IsValidTransition(_state, nextState))
            {
                Transitioned?.Invoke(this, new InteractionTransitionEventArgs(
                    _state, nextState, false,
                    $"Invalid transition from {_state} to {nextState}{(!string.IsNullOrEmpty(reason) ? ": " + reason : "")}."));
                return false;
            }

            var from = _state;
            _state = nextState;
            Transitioned?.Invoke(this, new InteractionTransitionEventArgs(
                from, nextState, true, reason));
            return true;
        }

        /// <summary>Force-set the state without validation (use sparingly, e.g. on tracking loss reset).</summary>
        public void ForceSet(InteractionState state, string reason = null)
        {
            var from = _state;
            _state = state;
            Transitioned?.Invoke(this, new InteractionTransitionEventArgs(
                from, state, true, reason ?? "Forced state change."));
        }

        /// <summary>Reset to Idle. Used when tracking is lost or a session ends.</summary>
        public void Reset(string reason = null)
        {
            ForceSet(InteractionState.Idle, reason ?? "Reset to idle.");
        }

        private static bool IsValidTransition(InteractionState from, InteractionState to)
        {
            if (from == to) return true; // no-op transitions are allowed

            return from switch
            {
                InteractionState.Idle =>
                    to == InteractionState.Hovered ||
                    to == InteractionState.Selected ||
                    to == InteractionState.Grabbed,

                InteractionState.Hovered =>
                    to == InteractionState.Idle ||
                    to == InteractionState.Selected ||
                    to == InteractionState.Grabbed,

                InteractionState.Selected =>
                    to == InteractionState.Hovered ||
                    to == InteractionState.Idle ||
                    to == InteractionState.Grabbed ||
                    to == InteractionState.Released,

                InteractionState.Grabbed =>
                    to == InteractionState.Selected ||
                    to == InteractionState.Idle ||
                    to == InteractionState.Released,

                InteractionState.Released =>
                    to == InteractionState.Idle,

                _ => false
            };
        }
    }

    /// <summary>Arguments for interaction state transitions.</summary>
    public sealed class InteractionTransitionEventArgs : EventArgs
    {
        public InteractionState FromState { get; }
        public InteractionState ToState { get; }
        public bool IsValid { get; }
        public string Reason { get; }

        public InteractionTransitionEventArgs(
            InteractionState fromState,
            InteractionState toState,
            bool isValid,
            string reason)
        {
            FromState = fromState;
            ToState = toState;
            IsValid = isValid;
            Reason = reason;
        }
    }
}
