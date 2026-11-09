namespace Faultline.Presentation
{
    /// <summary>
    /// Visual state shared by every design-system component contract.
    /// Normal / Hover / Selected / Disabled / Critical.
    /// Pure C# — no UnityEngine.
    /// </summary>
    public enum VisualState
    {
        Normal,
        Hover,
        Selected,
        Disabled,
        Critical
    }
}
