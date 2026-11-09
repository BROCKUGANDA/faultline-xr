namespace Faultline.Presentation
{
    using System;

    /// <summary>
    /// Infrastructure overview screen contract (PRD §6.1, screen 7).
    /// Entry conditions: environment configured.
    /// Exit conditions: user selects a node or opens an incident.
    /// Interaction model: spatial field of nodes/edges; hand select/grab;
    /// dynamic filtering by status/type.
    /// Back navigation: SpatialEnvironmentScreen.
    /// Accessibility: non-colour-only status (glyph + text + material);
    /// labels at readable distance; minimum 4.5 cm gesture targets.
    /// </summary>
    public interface InfrastructureOverviewScreen : IScreen
    {
        /// <summary>Currently selected node id, or null.</summary>
        string SelectedNodeId { get; }

        /// <summary>Whether filtering is active.</summary>
        bool FilterActive { get; }

        event EventHandler<NodeSelectedEventArgs> NodeSelected;
        event EventHandler<FilterChangedEventArgs> FilterChanged;
        event EventHandler IncidentOpened;
    }

    public sealed class NodeSelectedEventArgs : EventArgs
    {
        public string NodeId { get; }
        public NodeSelectedEventArgs(string nodeId)
        {
            NodeId = nodeId;
        }
    }

    public sealed class FilterChangedEventArgs : EventArgs
    {
        public string Filter { get; }
        public FilterChangedEventArgs(string filter)
        {
            Filter = filter;
        }
    }
}
