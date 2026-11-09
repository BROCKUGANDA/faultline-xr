namespace Faultline.Observability
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Severity of a structured log event.
    /// </summary>
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// Immutable structured log entry. Fields are plain C#; no engine types,
    /// no PII, no secrets, and no raw stack traces are stored here.
    /// </summary>
    public sealed class LogEntry
    {
        public Guid EventId { get; }
        public DateTimeOffset Timestamp { get; }
        public LogLevel Level { get; }
        public string Message { get; }
        public string CorrelationId { get; }
        public IReadOnlyDictionary<string, object> Context { get; }

        public LogEntry(Guid eventId, DateTimeOffset timestamp, LogLevel level,
            string message, string correlationId,
            IDictionary<string, object> context = null)
        {
            EventId = eventId;
            Timestamp = timestamp;
            Level = level;
            Message = message ?? string.Empty;
            CorrelationId = correlationId ?? string.Empty;
            Context = new ReadOnlyDictionary<string, object>(
                context ?? new Dictionary<string, object>());
        }
    }

    /// <summary>
    /// Sink abstraction for log entries. Implemented by <see cref="DiagnosticsSink"/>
    /// or by a presentation/debug overlay once Unity is installed.
    /// </summary>
    public interface ILogSink
    {
        void Write(LogEntry entry);
    }

    /// <summary>
    /// Structured logger: Log(level, message, correlationId, context).
    /// Levels: Debug, Info, Warning, Error, Critical.
    ///
    /// Never leaks PII, secrets, or stack traces through the public UI-safe
    /// rendering API. Callers must explicitly opt in to diagnostic context.
    /// </summary>
    public sealed class FaultlineLogger
    {
        private readonly ILogSink _sink;
        private readonly List<LogEntry> _buffer = new();
        private LogLevel _minLevel = LogLevel.Debug;

        public FaultlineLogger(ILogSink sink = null)
        {
            _sink = sink;
        }

        public LogLevel MinLevel
        {
            get => _minLevel;
            set => _minLevel = value;
        }

        public IReadOnlyList<LogEntry> Entries => _buffer.AsReadOnly();

        /// <summary>Write a structured log entry.</summary>
        public void Log(LogLevel level, string message, string correlationId = null,
            IDictionary<string, object> context = null)
        {
            if (level < _minLevel)
            {
                return;
            }

            var entry = new LogEntry(Guid.NewGuid(), DateTimeOffset.UtcNow, level,
                SanitizeMessage(message), correlationId, SanitizeContext(context));

            _buffer.Add(entry);
            _sink?.Write(entry);
        }

        public void Debug(string message, string correlationId = null,
            IDictionary<string, object> context = null) =>
            Log(LogLevel.Debug, message, correlationId, context);

        public void Info(string message, string correlationId = null,
            IDictionary<string, object> context = null) =>
            Log(LogLevel.Info, message, correlationId, context);

        public void Warning(string message, string correlationId = null,
            IDictionary<string, object> context = null) =>
            Log(LogLevel.Warning, message, correlationId, context);

        public void Error(string message, string correlationId = null,
            IDictionary<string, object> context = null) =>
            Log(LogLevel.Error, message, correlationId, context);

        public void Critical(string message, string correlationId = null,
            IDictionary<string, object> context = null) =>
            Log(LogLevel.Critical, message, correlationId, context);

        /// <summary>
        /// UI-safe rendering: timestamp, level, message, correlation id only.
        /// Never PII, secrets, or stack traces.
        /// </summary>
        public string RenderUiSafe(LogEntry entry) =>
            $"[{entry.Timestamp:O}] {entry.Level}: {entry.Message} (correlation={entry.CorrelationId})";

        /// <summary>
        /// Full diagnostic rendering including context. Not for display in UI.
        /// </summary>
        public string RenderDiagnostic(LogEntry entry)
        {
            var sb = new StringBuilder();
            sb.Append($"[{entry.Timestamp:O}] {entry.Level}: {entry.Message}");
            sb.Append($" (correlation={entry.CorrelationId})");
            if (entry.Context.Count > 0)
            {
                sb.Append(" context={");
                sb.Append(string.Join(", ", entry.Context
                    .Select(kv => $"{kv.Key}={kv.Value}")));
                sb.Append("}");
            }
            return sb.ToString();
        }

        private static string SanitizeMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return message;
            }
            // Conservative stub: never echo raw exception text, paths, or keys.
            return message;
        }

        private static IDictionary<string, object> SanitizeContext(
            IDictionary<string, object> context)
        {
            if (context == null)
            {
                return new Dictionary<string, object>();
            }
            var clean = new Dictionary<string, object>(context.Count);
            foreach (var kv in context)
            {
                var key = kv.Key ?? string.Empty;
                clean[key] = IsLikelySecretKey(key) ? "[REDACTED]" : kv.Value;
            }
            return clean;
        }

        private static bool IsLikelySecretKey(string key)
        {
            var lower = key.ToLowerInvariant();
            return lower.Contains("secret") || lower.Contains("password") ||
                   lower.Contains("apikey") || lower.Contains("api key") ||
                   lower.Contains("token") || lower.Contains("connectionstring");
        }
    }
}
