namespace Faultline.Presentation
{
    using System;

    /// <summary>
    /// Error panel contract — displays an FL- error code and message with
    /// retry/dismiss actions. Uses Critical Red token.
    ///
    /// Visual states: Critical (primary); Normal/Hover/Selected/Disabled for
    /// the actions within it.
    ///
    /// Pure C# — Unity Panel/Text/Button implemented behind this interface.
    /// </summary>
    public interface IFlErrorPanel
    {
        VisualState CurrentState { get; }

        /// <summary>FL- error code, e.g. "FL-E-1003".</summary>
        string ErrorCode { get; set; }

        /// <summary>Human-readable error message.</summary>
        string ErrorMessage { get; set; }

        /// <summary>Whether the panel is currently visible.</summary>
        bool IsVisible { get; }

        event Action<IFlErrorPanel> Dismissed;
        event Action<IFlErrorPanel> Retried;
        event Action<IFlErrorPanel, VisualState> StateChanged;

        void SetState(VisualState state);

        /// <summary>Show the error panel (uses Critical styling).</summary>
        void Show();

        /// <summary>Hide the error panel.</summary>
        void Dismiss();

        /// <summary>Attempt the failed operation again.</summary>
        void Retry();

        void OnPointerEnter();
        void OnPointerExit();

        bool ValidateConfiguration(out string errorMessage);
    }
}
