namespace Faultline.Domain
{
    using System;

    /// <summary>
    /// Immutable domain entity: a distributed-trace span event for a service.
    /// </summary>
    public sealed class TraceEvent : IEquatable<TraceEvent>
    {
        public Guid EventId { get; }
        public DateTimeOffset Timestamp { get; }
        public string ServiceId { get; }
        public string TraceId { get; }
        public string SpanId { get; }
        public string Operation { get; }
        public TimeSpan Duration { get; }
        public TelemetrySeverity Severity { get; }

        public TraceEvent(Guid eventId, DateTimeOffset timestamp, string serviceId,
            string traceId, string spanId, string operation, TimeSpan duration,
            TelemetrySeverity severity = TelemetrySeverity.Information)
        {
            EventId = eventId;
            Timestamp = timestamp;
            ServiceId = !string.IsNullOrWhiteSpace(serviceId)
                ? serviceId
                : throw new ArgumentException("Service id is required.", nameof(serviceId));
            TraceId = !string.IsNullOrWhiteSpace(traceId)
                ? traceId
                : throw new ArgumentException("Trace id is required.", nameof(traceId));
            SpanId = !string.IsNullOrWhiteSpace(spanId)
                ? spanId
                : throw new ArgumentException("Span id is required.", nameof(spanId));
            Operation = !string.IsNullOrWhiteSpace(operation)
                ? operation
                : throw new ArgumentException("Operation is required.", nameof(operation));
            Duration = duration;
            Severity = severity;
        }

        public bool Equals(TraceEvent other) =>
            other is not null && EventId.Equals(other.EventId);

        public override bool Equals(object obj) => Equals(obj as TraceEvent);

        public override int GetHashCode() => EventId.GetHashCode();

        public static bool operator ==(TraceEvent left, TraceEvent right) =>
            EqualityComparer<TraceEvent>.Default.Equals(left, right);

        public static bool operator !=(TraceEvent left, TraceEvent right) =>
            !(left == right);

        public override string ToString() =>
            $"TraceEvent({ServiceId}, {Operation}, {TraceId}/{SpanId}, {Duration.TotalMilliseconds:F0}ms)";
    }
}
