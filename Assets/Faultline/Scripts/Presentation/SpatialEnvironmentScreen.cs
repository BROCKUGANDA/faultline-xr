namespace Faultline.Presentation
{
    using System;

    /// <summary>
    /// Spatial environment screen contract (PRD §6.1, screen 6).
    /// Entry conditions: scenario selected; XR session active.
    /// Exit conditions: spatial anchors / environment configured.
    /// Interaction model: place/adjust infrastructure field origin; zone
    /// assignment (CENTRE / LEFT / RIGHT).
    /// Back navigation: ScenarioSelectionScreen.
    /// Accessibility: seated reach envelope respected; anchors within
    /// comfortable reach.
    /// </summary>
    public interface SpatialEnvironmentScreen : IScreen
    {
        /// <summary>Current spatial zone being configured.</summary>
        SpatialZone ActiveZone { get; }

        /// <summary>Whether anchors are within comfortable reach.</summary>
        bool AnchorsInReach { get; }

        event EventHandler<SpatialZoneChangedEventArgs> ZoneChanged;
        event EventHandler EnvironmentConfigured;
    }

    public sealed class SpatialZoneChangedEventArgs : EventArgs
    {
        public SpatialZone PreviousZone { get; }
        public SpatialZone NewZone { get; }
        public SpatialZoneChangedEventArgs(SpatialZone previousZone, SpatialZone newZone)
        {
            PreviousZone = previousZone;
            NewZone = newZone;
        }
    }
}
