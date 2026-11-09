namespace Faultline.Presentation
{
    using System;

    /// <summary>
    /// Base screen contract (PRD §6.1). Every one of the 22 screens implements
    /// this contract, declaring entry conditions, exit conditions, loading /
    /// empty / error states, interaction model, back navigation, focus
    /// behaviour, and accessibility requirements.
    ///
    /// Screens never mutate domain state directly — they capture user intent
    /// and submit validated commands (see Faultline.Interaction.InteractionCommand)
    /// to the owning domain service, then react to domain events.
    ///
    /// Pure C# — no UnityEngine. Concrete views implement this behind
    /// Unity UI Toolkit / uGUI once Unity is installed.
    /// </summary>
    public interface IScreen
    {
        /// <summary>Stable screen identifier, e.g. "Boot", "Welcome".</summary>
        string ScreenId { get; }

        /// <summary>Human-readable screen title (accessible).</summary>
        string Title { get; }

        /// <summary>Current lifecycle state.</summary>
        ScreenState State { get; }

        /// <summary>Whether the screen may be entered right now (entry conditions met).</summary>
        bool CanEnter { get; }

        /// <summary>Whether the screen may be exited right now (exit conditions met).</summary>
        bool CanExit { get; }

        /// <summary>
        /// Conditions that must hold before this screen may be entered.
        /// Evaluated by the navigator; returns a human-readable description
        /// of any unmet condition.
        /// </summary>
        string EntryConditions { get; }

        /// <summary>
        /// Conditions that must hold before this screen may be exited.
        /// </summary>
        string ExitConditions { get; }

        /// <summary>
        /// Interaction model summary for this screen (gesture targets,
        /// gaze, controller-free affordances). Used by accessibility tooling
        /// and the ControllerFreeValidator.
        /// </summary>
        string InteractionModel { get; }

        /// <summary>
        /// Back-navigation target screen id, or null if this screen has no
        /// back target (e.g. Boot).
        /// </summary>
        string BackScreenId { get; }

        /// <summary>Whether focus should wrap within this screen.</summary>
        bool FocusWraps { get; }

        /// <summary>Accessible description announced when the screen opens.</summary>
        string AccessibleDescription { get; }

        /// <summary>Raised when the screen state changes.</summary>
        event EventHandler<ScreenStateChangedEventArgs> StateChanged;

        /// <summary>Enter the screen (transition from previous).</summary>
        /// <returns>Success or an FL-UI-* error.</returns>
        Faultline.Domain.Result<bool> Enter();

        /// <summary>Exit the screen (transition to next/back).</summary>
        /// <returns>Success or an FL-UI-* error.</returns>
        Faultline.Domain.Result<bool> Exit();

        /// <summary>Set the screen to a loading state.</summary>
        void SetLoading();

        /// <summary>Set the screen to an empty state with guidance text.</summary>
        void SetEmpty(string guidance);

        /// <summary>Set the screen to content state.</summary>
        void SetContent();

        /// <summary>Set the screen to an error state carrying an FL- code.</summary>
        void SetError(string errorCode, string userGuidance, bool canRetry);
    }

    /// <summary>Arguments for screen state changes.</summary>
    public sealed class ScreenStateChangedEventArgs : EventArgs
    {
        public string ScreenId { get; }
        public ScreenState PreviousState { get; }
        public ScreenState NewState { get; }
        public long TimestampTicks { get; }

        public ScreenStateChangedEventArgs(
            string screenId,
            ScreenState previousState,
            ScreenState newState,
            long timestampTicks)
        {
            ScreenId = screenId;
            PreviousState = previousState;
            NewState = newState;
            TimestampTicks = timestampTicks;
        }
    }
}
