namespace Faultline.Domain
{
    using System;

    /// <summary>
    /// Unit type for fallible operations that carry no success value, used with
    /// <see cref="Result{Unit}"/>. Pure C# — no UnityEngine dependency.
    /// </summary>
    public readonly struct Unit : IEquatable<Unit>
    {
        public static Unit Instance { get; } = default;

        public bool Equals(Unit other) => true;

        public override bool Equals(object obj) => obj is Unit;

        public override int GetHashCode() => 0;

        public static bool operator ==(Unit left, Unit right) => true;

        public static bool operator !=(Unit left, Unit right) => false;

        public override string ToString() => "Unit";
    }
}
