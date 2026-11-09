namespace Faultline.Presentation
{
    using System;

    /// <summary>
    /// Settings screen contract (PRD §6.1, screen 19).
    /// Entry conditions: any screen → settings opened (pause or menu).
    /// Exit conditions: settings closed (back to previous screen).
    /// Interaction model: category list; hand select opens sub-category.
    /// Back navigation: previous screen (context-dependent).
    /// Accessibility: categories announced; high contrast; text scaling.
    /// </summary>
    public interface SettingsScreen : IScreen
    {
        /// <summary>Available settings category ids.</summary>
        System.Collections.Generic.IReadOnlyList<string> Categories { get; }

        /// <summary>Currently selected category id, or null.</summary>
        string SelectedCategory { get; }

        event EventHandler<SettingsCategoryEventArgs> CategorySelected;
        event EventHandler SettingsClosed;
    }

    public sealed class SettingsCategoryEventArgs : EventArgs
    {
        public string CategoryId { get; }
        public SettingsCategoryEventArgs(string categoryId)
        {
            CategoryId = categoryId;
        }
    }
}
