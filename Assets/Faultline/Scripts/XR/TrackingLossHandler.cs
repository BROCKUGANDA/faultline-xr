namespace Faultline.XR
{
    using System;
    using System.Collections.Generic;
    using Faultline.Domain;
    using Faultline.Domain.Errors;

    /// <summary>
    /// Handles XR hand-tracking loss and recovery behaviour (PRD §8, §3.3;
    /// Interaction Guidelines §3).
    ///
    /// On tracking lost:
    ///   - Cancel unsafe pending interactions (in-flight grabs are released,
    ///     selections are cleared).
    ///   - Preserve interaction state so it can be resumed after recovery.
    ///   - Show a tracking-status indicator (via the provided status callback).
    ///   - Allow recovery: on tracking regain, the previous hover target is
    ///     NOT auto-reacquired; the user must re-pinch to reselect.
    ///   - Do NOT auto-execute any action after resume.
    ///
    /// No UnityEngine types are referenced here; the status indicator callback
    /// is implemented by the Presentation layer.
    /// </summary>
    public sealed class TrackingLossHandler
    {
        private bool _trackingWasActive;
        private readonly HashSet<HandSide> _lostHands = new();
        private readonly List<Action> _pendingInteractions = new();

        /// <summary>Whether tracking is currently lost on any hand.</summary>
        public bool IsTrackingLost => _lostHands.Count > 0;

        /// <summary>Which hands are currently lost.</summary>
        public IReadOnlyCollection<HandSide> LostHands => _lostHands;

        /// <summary>Raised when tracking loss begins for any hand.</summary>
        public event EventHandler<TrackingLossEventArgs> TrackingLossBegan;

        /// <summary>Raised when tracking is recovered for all previously lost hands.</summary>
        public event EventHandler<TrackingRecoveryEventArgs> TrackingRecovered;

        /// <summary>
        /// Register an interaction that is in progress and should be cancelled
        /// if tracking is lost before it completes.
        /// </summary>
        public void RegisterPendingInteraction(Action cancelAction)
        {
            if (cancelAction == null)
            {
                throw new ArgumentNullException(nameof(cancelAction));
            }
            _pendingInteractions.Add(cancelAction);
        }

        /// <summary>
        /// Mark a pending interaction as completed (safe to execute; will not
        /// be cancelled by tracking loss).
        /// </summary>
        public void CompletePendingInteraction(Action completedAction)
        {
            _pendingInteractions.Remove(completedAction);
        }

        /// <summary>
        /// Notify the handler that tracking was lost for the given hand(s).
        /// Cancels unsafe pending interactions and raises TrackingLossBegan.
        /// </summary>
        /// <param name="lostHands">The hand(s) that lost tracking.</param>
        /// <param name="statusIndicator">Callback to show the tracking-status
        /// indicator in the Presentation layer. May be null.</param>
        public void OnTrackingLost(IEnumerable<HandSide> lostHands, Action<string> statusIndicator = null)
        {
            foreach (var hand in lostHands)
            {
                _lostHands.Add(hand);
            }

            _trackingWasActive = true;

            // Cancel unsafe pending interactions (in-flight grabs, etc.).
            var cancelled = _pendingInteractions.Count;
            foreach (var cancel in _pendingInteractions)
            {
                try { cancel(); }
                catch { /* Cancellation must not throw. */ }
            }
            _pendingInteractions.Clear();

            statusIndicator?.Invoke(
                $"Tracking lost: {string.Join(", ", _lostHands)} hand(s). Re-establish tracking to resume.");

            TrackingLossBegan?.Invoke(this, new TrackingLossEventArgs(
                new HashSet<HandSide>(_lostHands), cancelled, DateTimeOffset.UtcNow.Ticks));
        }

        /// <summary>
        /// Notify the handler that tracking was recovered for the given hand(s).
        /// Does NOT auto-execute any action. The user must re-pinch to reselect.
        /// </summary>
        /// <param name="recoveredHands">The hand(s) that recovered tracking.</param>
        /// <param name="statusIndicator">Callback to dismiss the tracking-status
        /// indicator. May be null.</param>
        public void OnTrackingRecovered(IEnumerable<HandSide> recoveredHands, Action<string> statusIndicator = null)
        {
            foreach (var hand in recoveredHands)
            {
                _lostHands.Remove(hand);
            }

            bool fullyRecovered = _lostHands.Count == 0 && _trackingWasActive;
            if (fullyRecovered)
            {
                _trackingWasActive = false;
                statusIndicator?.Invoke("Tracking recovered. Re-pinch to reselect your target.");
                TrackingRecovered?.Invoke(this, new TrackingRecoveryEventArgs(
                    new HashSet<HandSide>(recoveredHands), DateTimeOffset.UtcNow.Ticks));
            }
        }

        /// <summary>Clear all state. Call when the XR session ends.</summary>
        public void Reset()
        {
            _lostHands.Clear();
            _pendingInteractions.Clear();
            _trackingWasActive = false;
        }
    }

    /// <summary>Arguments for tracking-loss-began events.</summary>
    public sealed class TrackingLossEventArgs : EventArgs
    {
        public IReadOnlyCollection<HandSide> LostHands { get; }
        public int PendingInteractionsCancelled { get; }
        public long TimestampTicks { get; }

        public TrackingLossEventArgs(
            IReadOnlyCollection<HandSide> lostHands,
            int pendingInteractionsCancelled,
            long timestampTicks)
        {
            LostHands = lostHands;
            PendingInteractionsCancelled = pendingInteractionsCancelled;
            TimestampTicks = timestampTicks;
        }
    }

    /// <summary>Arguments for tracking-recovered events.</summary>
    public sealed class TrackingRecoveryEventArgs : EventArgs
    {
        public IReadOnlyCollection<HandSide> RecoveredHands { get; }
        public long TimestampTicks { get; }

        public TrackingRecoveryEventArgs(
            IReadOnlyCollection<HandSide> recoveredHands,
            long timestampTicks)
        {
            RecoveredHands = recoveredHands;
            TimestampTicks = timestampTicks;
        }
    }
}
