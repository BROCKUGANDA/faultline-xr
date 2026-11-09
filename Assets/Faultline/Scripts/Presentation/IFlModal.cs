namespace Faultline.Presentation
{
    using System;

    /// <summary>
    /// Blocking modal dialog contract.
    ///
    /// Visual states: Normal / Hover / Selected / Disabled / Critical
    /// (Critical for error/confirm-destructive modals).
    /// Presents title, body, and up to two actions (confirm / cancel).
    ///
    /// Pure C# — Unity Canvas/Panel implemented behind this interface.
    /// </summary>
    public interface IFlModal
    {
        VisualState CurrentState { get; }

        /// <summary>Modal title.</summary>
        string Title { get; set; }

        /// <summary>Modal body text.</summary>
        string Body { get; set; }

        /// <summary>Confirm (primary) action label.</summary>
        string ConfirmLabel { get; set; }

        /// <summary>Cancel (secondary) action label; null = no cancel shown.</summary>
        string CancelLabel { get; set; }

        /// <summary>Whether the modal is currently open.</summary>
        bool IsOpen { get; }

        event Action<IFlModal> Opened;
        event Action<IFlModal> Confirmed;
        event Action<IFlModal> Cancelled;
        event Action<IFlModal> Dismissed;
        event Action<IFlModal, VisualState> StateChanged;

        void SetState(VisualState state);

        /// <summary>Open the modal (motion: ModalAppearMs).</summary>
        void Open();

        /// <summary>Close without confirming (motion: ModalDismissMs).</summary>
        void Dismiss();

        /// <summary>Confirm the primary action.</summary>
        void Confirm();

        /// <summary>Cancel the action.</summary>
        void Cancel();

        void OnPointerEnter();
        void OnPointerExit();

        bool ValidateConfiguration(out string errorMessage);
    }
}
