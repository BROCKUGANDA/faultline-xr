namespace Faultline.Presentation
{
    using System;

    /// <summary>
    /// Elevated panel container contract (PRD §7 component contract).
    ///
    /// Visual states: Normal / Hover / Selected / Disabled / Critical.
    /// Panels use PanelDepth gradient (Void → Obsidian → Graphite) and Ice/Mist
    /// text. Configuration validated before first render.
    ///
    /// Pure C# — Unity RectTransform/CanvasGroup implemented behind this
    /// interface in the Presentation layer.
    /// </summary>
    public interface IFlPanel
    {
        VisualState CurrentState { get; }

        /// <summary>Panel heading text.</summary>
        string Heading { get; set; }

        /// <summary>Whether the panel is open / visible.</summary>
        bool IsOpen { get; }

        /// <summary>Whether the panel is pinned in world space (not dismissed by gesture).</summary>
        bool IsPinned { get; set; }

        event Action<IFlPanel> Opened;
        event Action<IFlPanel> Closed;
        event Action<IFlPanel, VisualState> StateChanged;

        void SetState(VisualState state);

        /// <summary>Open the panel with the configured motion duration.</summary>
        void Open();

        /// <summary>Close the panel with the configured motion duration.</summary>
        void Close();

        /// <summary>Pin/unpin the panel in world space.</summary>
        void SetPinned(bool pinned);

        void OnPointerEnter();
        void OnPointerExit();
        void OnSelect();
        void OnDeselect();

        bool ValidateConfiguration(out string errorMessage);
    }
}
