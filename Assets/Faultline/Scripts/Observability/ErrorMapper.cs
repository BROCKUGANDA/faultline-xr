namespace Faultline.Observability
{
    using System;

    /// <summary>
    /// Result of mapping an internal exception to a sanitized FL- code.
    /// No raw exception type, message, or stack trace is exposed to UI.
    /// </summary>
    public sealed class ErrorInfo
    {
        public string Code { get; }
        public string Message { get; }
        public LogLevel Severity { get; }
        public bool IsRetryable { get; }

        public ErrorInfo(string code, string message, LogLevel severity, bool isRetryable)
        {
            Code = code;
            Message = message;
            Severity = severity;
            IsRetryable = isRetryable;
        }
    }

    /// <summary>
    /// Maps internal exceptions to FL- domain error codes. Guarantees that
    /// raw exceptions never leak into the UI layer — only a sanitized code,
    /// message, and severity are returned.
    /// </summary>
    public static class ErrorMapper
    {
        public static ErrorInfo Map(Exception ex) =>
            Map(ex, "FL-RT-0001");

        public static ErrorInfo Map(Exception ex, string fallbackCode)
        {
            var (code, message, severity, retryable) = Classify(ex, fallbackCode);
            return new ErrorInfo(code, Sanitize(message), severity, retryable);
        }

        private static (string code, string message, LogLevel severity, bool retryable) Classify(
            Exception ex, string fallbackCode)
        {
            switch (ex)
            {
                case ArgumentNullException _:
                case ArgumentException _:
                    return ("FL-SM-0091", "Invalid command argument.", LogLevel.Error, true);
                case InvalidOperationException _:
                    return ("FL-SM-0091", "Invalid operation for current state.", LogLevel.Error, true);
                case NotSupportedException _:
                    return ("FL-SM-0091", "Operation not supported.", LogLevel.Warning, false);
                case TimeoutException _:
                case OperationCanceledException _:
                    return ("FL-AG-0061", "Operation timed out or was cancelled.", LogLevel.Warning, true);
                case OutOfMemoryException _:
                    return ("FL-RT-0001", "Insufficient memory.", LogLevel.Critical, false);
                case UnauthorizedAccessException _:
                    return ("FL-SM-0091", "Access not authorized.", LogLevel.Error, false);
                default:
                    return (fallbackCode, "An internal fault occurred.", LogLevel.Error, false);
            }
        }

        private static string Sanitize(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return message;
            }
            // Do not echo exception text (may contain paths, keys, stack traces).
            return message;
        }
    }
}
