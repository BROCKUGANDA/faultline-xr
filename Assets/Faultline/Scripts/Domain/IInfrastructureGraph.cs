namespace Faultline.Domain
{
    using System.Collections.Generic;

    /// <summary>
    /// Simulated infrastructure graph contract (PRD §3.3). Queryable graph of
    /// service nodes and dependency edges. Implemented in Infrastructure.
    /// </summary>
    public interface IInfrastructureGraph
    {
        IReadOnlyList<string> NodeIds { get; }
        IReadOnlyList<(string From, string To)> EdgeIds { get; }
        Result<bool> NodeExists(string nodeId);
        Result<string> GetNodeStatus(string nodeId);
        IReadOnlyList<string> GetDependents(string nodeId);
        IReadOnlyList<string> GetDependencies(string nodeId);
        Result<bool> HasCycle();
    }
}