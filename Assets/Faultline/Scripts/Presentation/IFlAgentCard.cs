namespace Faultline.Presentation
{
    using System;

    /// <summary>
    /// Agent card contract — shows an AgentTask lifecycle and findings.
    ///
    /// Visual states: Normal / Hover / Selected / Disabled / Critical.
    /// Critical when the agent task failed. Reveals on assign with motion.
    ///
    /// Pure C# — Unity Panel/Text implemented behind this interface.
    /// </summary>
    public interface IFlAgentCard
    {
        VisualState CurrentState { get; }

        /// <summary>Immutable task identifier (Domain TaskId as string).</summary>
        string TaskId { get; }

        /// <summary>Agent name / role shown on the card.</summary>
        string AgentName { get; set; }

        /// <summary>Current task lifecycle phase (dispatched / in_progress / completed / failed).</summary>
        string Phase { get; set; }

        event Action<IFlAgentCard> ViewDetailsRequested;
        event Action<IFlAgentCard, VisualState> StateChanged;

        void SetState(VisualState state);

        /// <summary>Reveal the card when a task is assigned (motion: AgentCardRevealMs).</summary>
        void Reveal();

        /// <summary>Mark the card as complete / archived.</summary>
        void Archive();

        void OnPointerEnter();
        void OnPointerExit();
        void OnSelect();
        void OnDeselect();

        bool ValidateConfiguration(out string errorMessage);
    }
}
