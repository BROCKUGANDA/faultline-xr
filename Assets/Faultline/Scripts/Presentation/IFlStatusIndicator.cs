namespace Faultline.Presentation
{
    using System;

    /// <summary>
    /// Small status indicator (dot / light / chip) contract.
    ///
    /// Visual states map to health: Normal=healthy, Hover, Selected,
    /// Disabled, Critical=failed. Uses Signal Cyan / Amber / Critical Red /
    /// Recovery Green tokens. Soft pulse on status change, never strobe.
    ///
    /// Pure C# — Unity Image/SpriteRenderer implemented behind this interface.
    /// </summary>
    public interface IFlStatusIndicator
    {
        VisualState CurrentState { get; }

        /// <summary>Short accessible label for the status (screen reader / tooltip).</summary>
        string AccessibleLabel { get; set; }

        event Action<IFlStatusIndicator, VisualState> StateChanged;

        void SetState(VisualState state);

        /// <summary>
        /// Pulse the indicator once (soft, MotionSettings.NodeStatusPulseMs).
        /// Used when status transitions to a new severity.
        /// </summary>
        void PulseOnce();

        void OnPointerEnter();
        void OnPointerExit();

        bool ValidateConfiguration(out string errorMessage);
    }
}
