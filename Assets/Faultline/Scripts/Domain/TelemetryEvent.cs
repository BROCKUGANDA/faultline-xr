namespace Faultline.Domain
{
    using System.Collections.Generic;

    using System;

    /// <summary>
    /// Severity of an observability event.
    /// </summary>
    public enum TelemetrySeverity
    {
        Verbose,
        Information,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// Immutable domain entity: a timestamped telemetry sample for a service,
    /// carrying a named metric and numeric value.
    /// </summary>
    public sealed class TelemetryEvent : IEquatable<TelemetryEvent>
    {
        public Guid EventId { get; }
        public DateTimeOffset Timestamp { get; }
        public string ServiceId { get; }
        public TelemetrySeverity Severity { get; }
        public string Message { get; }
        public string MetricName { get; }
        public double? MetricValue { get; }

        public TelemetryEvent(Guid eventId, DateTimeOffset timestamp, string serviceId,
            TelemetrySeverity severity, string message, string metricName = null,
            double? metricValue = null)
        {
            EventId = eventId;
            Timestamp = timestamp;
            ServiceId = !string.IsNullOrWhiteSpace(serviceId)
                ? serviceId
                : throw new ArgumentException("Service id is required.", nameof(serviceId));
            Severity = severity;
            Message = message;
            MetricName = metricName;
            MetricValue = metricValue;
        }

        public bool Equals(TelemetryEvent other) =>
            other is not null && EventId.Equals(other.EventId);

        public override bool Equals(object obj) => Equals(obj as TelemetryEvent);

        public override int GetHashCode() => EventId.GetHashCode();

        public static bool operator ==(TelemetryEvent left, TelemetryEvent right) =>
            EqualityComparer<TelemetryEvent>.Default.Equals(left, right);

        public static bool operator !=(TelemetryEvent left, TelemetryEvent right) =>
            !(left == right);

        public override string ToString() =>
            $"TelemetryEvent({ServiceId}, {MetricName}={MetricValue}, {Severity}, {Timestamp:O})";
    }
}
