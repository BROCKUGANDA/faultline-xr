namespace Faultline.Domain
{
    using System;

    /// <summary>
    /// Immutable domain entity: a structured log line for a service with a
    /// level and message.
    /// </summary>
    public sealed class LogEvent : IEquatable<LogEvent>
    {
        public Guid EventId { get; }
        public DateTimeOffset Timestamp { get; }
        public string ServiceId { get; }
        public TelemetrySeverity Level { get; }
        public string Message { get; }

        public LogEvent(Guid eventId, DateTimeOffset timestamp, string serviceId,
            TelemetrySeverity level, string message)
        {
            EventId = eventId;
            Timestamp = timestamp;
            ServiceId = !string.IsNullOrWhiteSpace(serviceId)
                ? serviceId
                : throw new ArgumentException("Service id is required.", nameof(serviceId));
            Level = level;
            Message = message;
        }

        public bool Equals(LogEvent other) =>
            other is not null && EventId.Equals(other.EventId);

        public override bool Equals(object obj) => Equals(obj as LogEvent);

        public override int GetHashCode() => EventId.GetHashCode();

        public static bool operator ==(LogEvent left, LogEvent right) =>
            EqualityComparer<LogEvent>.Default.Equals(left, right);

        public static bool operator !=(LogEvent left, LogEvent right) =>
            !(left == right);

        public override string ToString() =>
            $"LogEvent({ServiceId}, {Level}, {Message}, {Timestamp:O})";
    }
}
