namespace Faultline.Domain
{
    using System.Collections.Generic;

    /// <summary>
    /// Result of a fallible domain operation. Success carries a value;
    /// failure carries an FL- error code and message (ADR-006).
    /// No UnityEngine dependency; pure C#.
    /// </summary>
    /// <typeparam name="T">Success value type.</typeparam>
    public sealed class Result<T>
    {
        public bool IsSuccess { get; }
        public T Value { get; }
        public string ErrorCode { get; }
        public string ErrorMessage { get; }
        public IReadOnlyDictionary<string, object> DiagnosticContext { get; }

        private Result(T value, bool isSuccess, string errorCode, string errorMessage,
            IReadOnlyDictionary<string, object> diagnosticContext)
        {
            Value = value;
            IsSuccess = isSuccess;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
            DiagnosticContext = diagnosticContext;
        }

        public static Result<T> Success(T value) =>
            new(value, true, null, null, new Dictionary<string, object>());

        public static Result<T> Failure(string errorCode, string errorMessage,
            IDictionary<string, object> diagnosticContext = null) =>
            new(default, false, errorCode, errorMessage,
                diagnosticContext != null
                    ? new Dictionary<string, object>(diagnosticContext)
                    : new Dictionary<string, object>());
    }
}