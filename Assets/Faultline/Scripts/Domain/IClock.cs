namespace Faultline.Domain
{
    using System;

    /// <summary>
    /// Deterministic clock abstraction for the simulation engine and agents.
    /// Production uses <see cref="SystemClock"/>; tests inject
    /// <see cref="FixedClock"/> so the same seed reproduces identical output.
    /// </summary>
    public interface IClock
    {
        DateTimeOffset Now { get; }
    }

    /// <summary>Wall-clock implementation for production runs.</summary>
    public sealed class SystemClock : IClock
    {
        public static readonly SystemClock Instance = new SystemClock();
        public DateTimeOffset Now => DateTimeOffset.UtcNow;
    }

    /// <summary>Fixed clock for deterministic tests. Time only moves when advanced.</summary>
    public sealed class FixedClock : IClock
    {
        private DateTimeOffset _now;

        public FixedClock(DateTimeOffset start)
        {
            _now = start;
        }

        public FixedClock()
            : this(new DateTimeOffset(2026, 11, 9, 0, 0, 0, TimeSpan.Zero))
        {
        }

        public DateTimeOffset Now => _now;

        public void Advance(TimeSpan delta) => _now = _now.Add(delta);

        public void Set(DateTimeOffset value) => _now = value;
    }
}
