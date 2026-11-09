namespace Faultline.Observability
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    /// <summary>
    /// Session info snapshot collected by the diagnostics sink.
    /// </summary>
    public sealed class SessionInfo
    {
        public string SessionId { get; }
        public DateTimeOffset StartedAt { get; }
        public DateTimeOffset? EndedAt { get; }
        public string ScenarioId { get; }
        public int LogCount { get; }
        public int MetricCount { get; }

        public SessionInfo(string sessionId, DateTimeOffset startedAt,
            DateTimeOffset? endedAt, string scenarioId, int logCount, int metricCount)
        {
            SessionId = sessionId;
            StartedAt = startedAt;
            EndedAt = endedAt;
            ScenarioId = scenarioId;
            LogCount = logCount;
            MetricCount = metricCount;
        }
    }

    /// <summary>
    /// Named performance metric sample.
    /// </summary>
    public sealed class MetricSample
    {
        public string Name { get; }
        public double Value { get; }
        public string Unit { get; }
        public DateTimeOffset Timestamp { get; }

        public MetricSample(string name, double value, string unit,
            DateTimeOffset? timestamp = null)
        {
            Name = name;
            Value = value;
            Unit = unit;
            Timestamp = timestamp ?? DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Collects event history, session info, and performance metrics.
    /// Implements <see cref="ILogSink"/> so <see cref="FaultlineLogger"/> can
    /// write directly into it.
    /// </summary>
    public sealed class DiagnosticsSink : ILogSink
    {
        private List<LogEntry> _logs = new();
        private List<MetricSample> _metrics = new();
        private SessionInfo _session;

        public DiagnosticsSink(string sessionId, string scenarioId = null)
        {
            _session = new SessionInfo(sessionId, DateTimeOffset.UtcNow, null,
                scenarioId, 0, 0);
        }

        public void Write(LogEntry entry)
        {
            _logs.Add(entry);
        }

        public void RecordMetric(string name, double value, string unit = null)
        {
            _metrics.Add(new MetricSample(name, value, unit ?? string.Empty));
        }

        public IReadOnlyList<LogEntry> GetLogs(LogLevel? minLevel = null,
            DateTimeOffset? since = null)
        {
            var query = _logs.AsEnumerable();
            if (minLevel.HasValue)
            {
                query = query.Where(l => l.Level >= minLevel.Value);
            }
            if (since.HasValue)
            {
                query = query.Where(l => l.Timestamp >= since.Value);
            }
            return query.ToList().AsReadOnly();
        }

        public IReadOnlyList<MetricSample> GetMetrics(string name = null)
        {
            if (name == null)
            {
                return _metrics.AsReadOnly();
            }
            return _metrics.Where(m => m.Name == name).ToList().AsReadOnly();
        }

        public SessionInfo GetSessionInfo()
        {
            return new SessionInfo(_session.SessionId, _session.StartedAt,
                _session.EndedAt, _session.ScenarioId, _logs.Count, _metrics.Count);
        }

        public void EndSession()
        {
            _session = new SessionInfo(_session.SessionId, _session.StartedAt,
                DateTimeOffset.UtcNow, _session.ScenarioId, _logs.Count, _metrics.Count);
        }

        public void Clear()
        {
            _logs = new List<LogEntry>();
            _metrics = new List<MetricSample>();
        }
    }
}
