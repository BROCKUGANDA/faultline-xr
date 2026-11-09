namespace Faultline.Presentation
{
    using System;

    /// <summary>
    /// Hand tutorial screen contract (PRD §6.1, screen 4).
    /// Entry conditions: onboarding → hand tutorial selected.
    /// Exit conditions: tutorial gestures demonstrated or skipped.
    /// Interaction model: guided pinch/grab/release practice with live hand
    /// tracking feedback; tracking-status indicator shown.
    /// Back navigation: OnboardingScreen.
    /// Accessibility: visual (non-voice) gesture guidance; tracking-loss
    /// recovery shown via status indicator.
    /// </summary>
    public interface HandTutorialScreen : IScreen
    {
        /// <summary>Current tutorial gesture being demonstrated.</summary>
        string CurrentGesture { get; }

        /// <summary>Whether the user has successfully performed the current gesture.</summary>
        bool GestureCompleted { get; }

        /// <summary>Latest hand-tracking status for display.</summary>
        string LeftHandStatus { get; }
        string RightHandStatus { get; }

        event EventHandler<HandTutorialGestureEventArgs> GestureDemonstrated;
        event EventHandler TutorialCompleted;
        event EventHandler TutorialSkipped;
    }

    public sealed class HandTutorialGestureEventArgs : EventArgs
    {
        public string GestureName { get; }
        public bool Success { get; }
        public HandTutorialGestureEventArgs(string gestureName, bool success)
        {
            GestureName = gestureName;
            Success = success;
        }
    }
}
