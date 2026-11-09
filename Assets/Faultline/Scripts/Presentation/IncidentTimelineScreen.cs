namespace Faultline.Presentation
{
    using System;

    /// <summary>
    /// Incident timeline screen contract (PRD §6.1, screen 18).
    /// Entry conditions: incident has recorded events.
    /// Exit conditions: timeline closed.
    /// Interaction model: chronological event list; hand select to inspect
    /// an event; filter by event type.
    /// Back navigation: IncidentDetailScreen.
    /// Accessibility: event timestamps and descriptions announced; filter
    /// state announced.
    /// </summary>
    public interface IncidentTimelineScreen : IScreen
    {
        /// <summary>Incident whose timeline is shown.</summary>
        string IncidentId { get; }

        /// <summary>Number of timeline events.</summary>
        int EventCount { get; }

        event EventHandler<IncidentIdEventArgs> TimelineOpened;
        event EventHandler<TimelineEventSelectedEventArgs> EventSelected;
        event EventHandler TimelineFilterChanged;
    }

    public sealed class TimelineEventSelectedEventArgs : EventArgs
    {
        public string EventId { get; }
        public TimelineEventSelectedEventArgs(string eventId)
        {
            EventId = eventId;
        }
    }
}
