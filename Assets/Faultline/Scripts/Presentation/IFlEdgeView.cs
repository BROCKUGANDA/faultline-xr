namespace Faultline.Presentation
{
    using System;

    /// <summary>
    /// Dependency edge view contract (directed link between two nodes).
    ///
    /// Visual states: Normal / Hover / Selected / Disabled / Critical.
    /// Renders as a thin world-space line / ribbon; highlights on dependency
    /// fault cascade. Configuration validated before first render.
    ///
    /// Pure C# — Unity LineRenderer implemented behind this interface.
    /// </summary>
    public interface IFlEdgeView
    {
        VisualState CurrentState { get; }

        /// <summary>Immutable edge identifier (Domain EdgeId as string).</summary>
        string EdgeId { get; }

        /// <summary>Source node id.</summary>
        string SourceNodeId { get; }

        /// <summary>Target node id.</summary>
        string TargetNodeId { get; }

        event Action<IFlEdgeView> Selected;
        event Action<IFlEdgeView> Deselected;
        event Action<IFlEdgeView, VisualState> StateChanged;

        void SetState(VisualState state);

        /// <summary>Highlight this edge (e.g. when a dependent node enters Critical).</summary>
        void Highlight();

        /// <summary>Clear highlight.</summary>
        void Unhighlight();

        void OnPointerEnter();
        void OnPointerExit();
        void OnSelect();
        void OnDeselect();

        bool ValidateConfiguration(out string errorMessage);
    }
}
