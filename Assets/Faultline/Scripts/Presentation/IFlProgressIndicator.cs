namespace Faultline.Presentation
{
    using System;

    /// <summary>
    /// Progress indicator contract (bar or spinner) for tasks/completion.
    ///
    /// Visual states: Normal / Hover / Selected / Disabled / Critical
    /// (Critical when the tracked operation failed).
    /// Fills with Progress Fill gradient (Electric Blue → Signal Cyan).
    ///
    /// Pure C# — Unity Slider/Image implemented behind this interface.
    /// </summary>
    public interface IFlProgressIndicator
    {
        VisualState CurrentState { get; }

        /// <summary>Progress label, e.g. "Remediating node...".</summary>
        string Label { get; set; }

        /// <summary>Progress fraction 0..1.</summary>
        float Progress { get; }

        /// <summary>Whether the operation is indeterminate (spinner mode).</summary>
        bool IsIndeterminate { get; set; }

        event Action<IFlProgressIndicator, float> ProgressChanged;
        event Action<IFlProgressIndicator> Completed;
        event Action<IFlProgressIndicator> Failed;
        event Action<IFlProgressIndicator, VisualState> StateChanged;

        void SetState(VisualState state);

        /// <summary>Set progress fraction (0..1); fires Completed at 1.</summary>
        void SetProgress(float progress);

        /// <summary>Start indeterminate spin.</summary>
        void StartIndeterminate();

        /// <summary>Stop indeterminate spin.</summary>
        void StopIndeterminate();

        void OnPointerEnter();
        void OnPointerExit();

        bool ValidateConfiguration(out string errorMessage);
    }
}
