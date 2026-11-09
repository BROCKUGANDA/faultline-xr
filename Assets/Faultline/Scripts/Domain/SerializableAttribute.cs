namespace Faultline.Domain
{
    /// <summary>
    /// Lightweight Serializable marker attribute — pure C#, no UnityEngine.
    ///
    /// Faultline.Domain must remain engine-agnostic (headless-testable). The
    /// Presentation layer marks its settings classes with this attribute so
    /// they can later be mapped onto Unity's SerializedObject / ScriptableObject
    /// once Unity is installed, without the Domain layer referencing Unity.
    /// </summary>
    [System.AttributeUsage(
        System.AttributeTargets.Class |
        System.AttributeTargets.Struct |
        System.AttributeTargets.Field,
        Inherited = false)]
    public sealed class SerializableAttribute : System.Attribute
    {
    }
}
