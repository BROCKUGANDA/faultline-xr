namespace Faultline.Presentation
{
    using System;

    /// <summary>
    /// Infrastructure node view contract (graph node in world space).
    ///
    /// Visual states: Normal / Hover / Selected / Disabled / Critical.
    /// Renders an FL_* glyph, status indicator, and label. Configuration
    /// validated before first render.
    ///
    /// Pure C# — Unity transform/mesh implemented behind this interface.
    /// </summary>
    public interface IFlNodeView
    {
        VisualState CurrentState { get; }

        /// <summary>Immutable node identifier (Domain NodeId as string).</summary>
        string NodeId { get; }

        /// <summary>Glyph token name, e.g. FL_NODE, FL_DATABASE.</summary>
        string GlyphName { get; set; }

        /// <summary>Primary label shown beneath the glyph.</summary>
        string Label { get; set; }

        event Action<IFlNodeView> Selected;
        event Action<IFlNodeView> Deselected;
        event Action<IFlNodeView, VisualState> StateChanged;
        event Action<IFlNodeView, System.Numerics.Vector3> Dragged;

        void SetState(VisualState state);

        /// <summary>Begin a grab/drag on this node (sustained pinch ≥ hysteresis threshold).</summary>
        void OnGrabBegin();

        /// <summary>Continue the drag, following the hand with damping.</summary>
        void OnDrag(System.Numerics.Vector3 worldDelta);

        /// <summary>End the grab; drop at current position, snap to nearest anchor if within threshold.</summary>
        void OnGrabEnd();

        void OnPointerEnter();
        void OnPointerExit();
        void OnSelect();
        void OnDeselect();

        bool ValidateConfiguration(out string errorMessage);
    }
}
