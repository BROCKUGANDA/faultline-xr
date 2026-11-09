namespace Faultline.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    using Faultline.Domain;

    /// <summary>
    /// Read-only view over the infrastructure graph for domain-layer queries.
    /// Wraps <see cref="InfrastructureGraph"/> and exposes the
    /// <see cref="Faultline.Domain.IInfrastructureGraph"/> contract.
    /// </summary>
    public sealed class InfrastructureGraphView : Faultline.Domain.IInfrastructureGraph
    {
        private readonly InfrastructureGraph _graph;

        public InfrastructureGraphView(InfrastructureGraph graph)
        {
            _graph = graph ?? throw new ArgumentNullException(nameof(graph));
        }

        public IReadOnlyList<string> NodeIds => _graph.Nodes.Select(n => n.NodeId).ToList();

        public IReadOnlyList<(string From, string To)> EdgeIds => _graph.Edges
            .Select(e => (e.SourceNodeId, e.TargetNodeId)).ToList();

        public Faultline.Domain.Result<bool> NodeExists(string nodeId) =>
            Faultline.Domain.Result<bool>.Success(_graph.NodeExists(nodeId));

        public Faultline.Domain.Result<string> GetNodeStatus(string nodeId) =>
            _graph.GetNodeStatus(nodeId);

        public IReadOnlyList<string> GetDependents(string nodeId) =>
            _graph.GetDependents(nodeId);

        public IReadOnlyList<string> GetDependencies(string nodeId) =>
            _graph.GetDependencies(nodeId);

        public Faultline.Domain.Result<bool> HasCycle() => _graph.HasCycleResult();
    }
}
