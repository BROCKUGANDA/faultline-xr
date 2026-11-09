namespace Faultline.Presentation
{
    using System;

    /// <summary>
    /// Service inspection screen contract (PRD §6.1, screen 8).
    /// Entry conditions: a node selected in the infrastructure overview.
    /// Exit conditions: inspection closed or incident opened for this node.
    /// Interaction model: contextual inspector panel (RIGHT zone); hand
    /// targets for InspectEvidence, DispatchAgent.
    /// Back navigation: InfrastructureOverviewScreen.
    /// Accessibility: readable metrics; technical labels monospace; status
    /// via colour + glyph + text.
    /// </summary>
    public interface ServiceInspectionScreen : IScreen
    {
        /// <summary>Id of the inspected node.</summary>
        string NodeId { get; }

        /// <summary>Whether evidence is available for this node.</summary>
        bool EvidenceAvailable { get; }

        event EventHandler<NodeInspectedEventArgs> NodeInspected;
        event EventHandler InspectEvidenceRequested;
        event EventHandler DispatchAgentRequested;
    }

    public sealed class NodeInspectedEventArgs : EventArgs
    {
        public string NodeId { get; }
        public NodeInspectedEventArgs(string nodeId)
        {
            NodeId = nodeId;
        }
    }
}
