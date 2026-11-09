namespace Faultline.Observability
{
    using System;

    /// <summary>
    /// Generates correlation IDs for request tracing across subsystems.
    /// IDs are opaque, URL-safe, and unique per call.
    /// </summary>
    public static class CorrelationIdProvider
    {
        /// <summary>Create a new correlation ID (32 lowercase hex chars).</summary>
        public static string Create() => Guid.NewGuid().ToString("N");

        /// <summary>
        /// Create a correlation ID prefixed with a subsystem tag, e.g.
        /// "xr-4a2b...". Useful for log correlation across module boundaries.
        /// </summary>
        public static string Create(string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix))
            {
                return Create();
            }
            return $"{prefix}-{Create()}";
        }
    }
}
