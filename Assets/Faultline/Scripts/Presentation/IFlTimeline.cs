namespace Faultline.Presentation
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Timeline contract — ordered list of events (incident, remediation,
    /// agent findings) scrubbable in world space.
    ///
    /// Visual states: Normal / Hover / Selected / Disabled / Critical.
    /// Critical when the timeline contains an unresolved critical incident.
    ///
    /// Pure C# — Unity ScrollRect/List implemented behind this interface.
    /// </summary>
    public interface IFlTimeline
    {
        VisualState CurrentState { get; }

        /// <summary>Timeline title.</summary>
        string Title { get; set; }

        /// <summary>Ids of events currently displayed, oldest first.</summary>
        IReadOnlyList<string> EventIds { get; }

        event Action<IFlTimeline> Scrubbed;
        event Action<IFlTimeline, string> EventSelected;
        event Action<IFlTimeline, VisualState> StateChanged;

        void SetState(VisualState state);

        /// <summary>Replace the displayed event sequence.</summary>
        void SetEvents(IReadOnlyList<string> eventIds);

        /// <summary>Scrub the timeline to a position (0..1).</summary>
        void ScrubTo(float normalizedPosition);

        /// <summary>Select a specific event on the timeline.</summary>
        void SelectEvent(string eventId);

        void OnPointerEnter();
        void OnPointerExit();

        bool ValidateConfiguration(out string errorMessage);
    }
}
