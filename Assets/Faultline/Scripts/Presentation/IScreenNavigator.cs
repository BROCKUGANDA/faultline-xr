namespace Faultline.Presentation
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Screen navigator contract (PRD §6.1 / §4.1).
    ///
    /// Manages screen transitions with explicit application state. No
    /// arbitrary delays — transitions are explicit and state-driven.
    ///
    /// The navigator never mutates domain state directly; screens capture
    /// user intent and submit commands to domain services, and the navigator
    /// reacts to screen state changes.
    ///
    /// Pure C# — no UnityEngine. The concrete implementation drives Unity
    /// scene / canvas activation behind this interface.
    /// </summary>
    public interface IScreenNavigator
    {
        /// <summary>Current application state (explicit per PRD §4.1).</summary>
        ApplicationState CurrentState { get; }

        /// <summary>The currently active screen, or null if none.</summary>
        IScreen CurrentScreen { get; }

        /// <summary>Stack of visited screens (for back navigation).</summary>
        IReadOnlyList<IScreen> BackStack { get; }

        /// <summary>Raised when the application state changes.</summary>
        event EventHandler<ApplicationStateChangedEventArgs> StateChanged;

        /// <summary>Raised when the active screen changes.</summary>
        event EventHandler<ScreenChangedEventArgs> ScreenChanged;

        /// <summary>
        /// Transition to a new application state and activate its screen.
        /// Validates that the target state is reachable from the current state.
        /// </summary>
        /// <returns>Success or an FL-UI-* error.</returns>
        Faultline.Domain.Result<bool> TransitionTo(ApplicationState targetState);

        /// <summary>
        /// Navigate directly to a screen by id (within the current state).
        /// </summary>
        /// <returns>Success or an FL-UI-* error.</returns>
        Faultline.Domain.Result<bool> NavigateToScreen(string screenId);

        /// <summary>
        /// Navigate back to the previous screen (pop the back stack).
        /// </summary>
        /// <returns>Success or an FL-UI-* error.</returns>
        Faultline.Domain.Result<bool> GoBack();

        /// <summary>
        /// Register a screen implementation under its screen id.
        /// </summary>
        void RegisterScreen(string screenId, IScreen screen);

        /// <summary>
        /// Whether a transition from current state to <paramref name="targetState"/>
        /// is allowed (entry conditions met).
        /// </summary>
        bool CanTransitionTo(ApplicationState targetState);
    }

    /// <summary>
    /// Explicit application states (PRD §4.1). The application is always in
    /// exactly one state; screens are activated within states.
    /// </summary>
    public enum ApplicationState
    {
        /// <summary>Application is booting (BootScreen active).</summary>
        Booting,

        /// <summary>Application is ready; welcome/scenario selection active.</summary>
        Ready,

        /// <summary>A scenario is active; spatial field and investigation available.</summary>
        ScenarioActive,

        /// <summary>The scenario is paused (PauseMenuScreen active).</summary>
        Paused,

        /// <summary>An error requires user recovery action (ErrorRecoveryScreen active).</summary>
        ErrorRecovery,

        /// <summary>Application is shutting down.</summary>
        ShuttingDown
    }

    /// <summary>Arguments for application state changes.</summary>
    public sealed class ApplicationStateChangedEventArgs : EventArgs
    {
        public ApplicationState PreviousState { get; }
        public ApplicationState NewState { get; }
        public long TimestampTicks { get; }

        public ApplicationStateChangedEventArgs(
            ApplicationState previousState,
            ApplicationState newState,
            long timestampTicks)
        {
            PreviousState = previousState;
            NewState = newState;
            TimestampTicks = timestampTicks;
        }
    }

    /// <summary>Arguments for screen changes.</summary>
    public sealed class ScreenChangedEventArgs : EventArgs
    {
        public string PreviousScreenId { get; }
        public string NewScreenId { get; }
        public long TimestampTicks { get; }

        public ScreenChangedEventArgs(
            string previousScreenId,
            string newScreenId,
            long timestampTicks)
        {
            PreviousScreenId = previousScreenId;
            NewScreenId = newScreenId;
            TimestampTicks = timestampTicks;
        }
    }
}
