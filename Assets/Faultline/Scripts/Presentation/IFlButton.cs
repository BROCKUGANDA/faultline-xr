namespace Faultline.Presentation
{
    using System;

    /// <summary>
    /// Pressable button contract (PRD §7 component contract).
    ///
    /// Visual states: Normal / Hover / Selected / Disabled / Critical.
    /// Configuration must be validated before first render.
    /// Interaction behaviour: pinch/tap activates; hover previews; disabled
    /// ignores input; critical state conveys a destructive or blocking action.
    ///
    /// Pure C# — Unity types (Button, OnClick, etc.) are implemented behind
    /// this interface in the Presentation layer.
    /// </summary>
    public interface IFlButton
    {
        /// <summary>Current visual state.</summary>
        VisualState CurrentState { get; }

        /// <summary>Human-readable label shown on the button.</summary>
        string Label { get; set; }

        /// <summary>Tooltip / accessible description.</summary>
        string AccessibleDescription { get; set; }

        /// <summary>Whether the button is currently interactable.</summary>
        bool Interactable { get; set; }

        /// <summary>Raised when the button is activated (pinch/tap/click).</summary>
        event Action<IFlButton> Activated;

        /// <summary>Raised when the visual state changes.</summary>
        event Action<IFlButton, VisualState> StateChanged;

        /// <summary>
        /// Transition to a new visual state. Presentation plays the matching
        /// motion (per MotionSettings) and updates colour/glyph.
        /// </summary>
        void SetState(VisualState state);

        /// <summary>
        /// Pointer/ray entered the target area (hover begin).
        /// </summary>
        void OnPointerEnter();

        /// <summary>
        /// Pointer/ray left the target area (hover end).
        /// </summary>
        void OnPointerExit();

        /// <summary>
        /// Selection began (pinch-hold or grab on this target).
        /// </summary>
        void OnSelect();

        /// <summary>
        /// Selection ended (release).
        /// </summary>
        void OnDeselect();

        /// <summary>
        /// Activation confirmed (brief pinch/tap). Triggers Activated event.
        /// </summary>
        void OnActivate();

        /// <summary>
        /// Validate configuration (label present, colour/glyph tokens resolve,
        /// motion settings sane). Returns true and an empty message when valid.
        /// </summary>
        bool ValidateConfiguration(out string errorMessage);
    }
}
