namespace Faultline.Presentation
{
    using System;

    /// <summary>
    /// Evidence card contract — shows an AgentFinding with source event ids.
    ///
    /// Visual states: Normal / Hover / Selected / Disabled / Critical.
    /// Expands on select to reveal the full finding; collapses on dismiss.
    /// Configuration validated before first render.
    ///
    /// Pure C# — Unity Panel/Text implemented behind this interface.
    /// </summary>
    public interface IFlEvidenceCard
    {
        VisualState CurrentState { get; }

        /// <summary>Immutable finding identifier (Domain FindingId as string).</summary>
        string FindingId { get; }

        /// <summary>Short summary shown in collapsed state.</summary>
        string Summary { get; set; }

        /// <summary>Whether the card is expanded (full detail visible).</summary>
        bool IsExpanded { get; }

        event Action<IFlEvidenceCard> Expanded;
        event Action<IFlEvidenceCard> Collapsed;
        event Action<IFlEvidenceCard> Dismissed;
        event Action<IFlEvidenceCard, VisualState> StateChanged;

        void SetState(VisualState state);

        /// <summary>Expand the card to reveal full evidence (motion: EvidenceCardRevealMs).</summary>
        void Expand();

        /// <summary>Collapse the card to summary-only.</summary>
        void Collapse();

        /// <summary>Dismiss the card from the view.</summary>
        void Dismiss();

        void OnPointerEnter();
        void OnPointerExit();
        void OnSelect();
        void OnDeselect();

        bool ValidateConfiguration(out string errorMessage);
    }
}
