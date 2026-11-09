namespace Faultline.Presentation
{
    using System;

    /// <summary>
    /// Transient notification toast contract.
    ///
    /// Visual states: Normal / Hover / Selected / Disabled / Critical
    /// (Critical for error toasts, uses Critical Red).
    /// Auto-dismisses after a configured duration; dismissible by gesture.
    ///
    /// Pure C# — Unity CanvasGroup implemented behind this interface.
    /// </summary>
    public interface IFlToast
    {
        VisualState CurrentState { get; }

        /// <summary>Toast body text.</summary>
        string Message { get; set; }

        /// <summary>Whether the toast is currently visible.</summary>
        bool IsVisible { get; }

        event Action<IFlToast> Appeared;
        event Action<IFlToast> Dismissed;
        event Action<IFlToast, VisualState> StateChanged;

        void SetState(VisualState state);

        /// <summary>Show the toast (motion: ToastAppearMs).</summary>
        void Show();

        /// <summary>Dismiss the toast (motion: ToastDismissMs).</summary>
        void Dismiss();

        void OnPointerEnter();
        void OnPointerExit();

        bool ValidateConfiguration(out string errorMessage);
    }
}
