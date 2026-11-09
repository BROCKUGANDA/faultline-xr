namespace Faultline.Presentation
{
    /// <summary>
    /// Lifecycle state of every screen (PRD §6.1).
    ///
    ///   - Loading: screen is entering / fetching content; show a progress
    ///     indicator, block interaction.
    ///   - Empty: screen entered but has no content to show; show empty-state
    ///     guidance and a clear primary action.
    ///   - Content: screen fully populated and interactive.
    ///   - Error: screen encountered an error; show the error panel with the
    ///     FL- code, user guidance, and a retry / back path.
    ///
    /// Pure C# — no UnityEngine.
    /// </summary>
    public enum ScreenState
    {
        Loading,
        Empty,
        Content,
        Error
    }
}
