namespace Faultline.Domain
{
    using System.Collections.Generic;

    /// <summary>
    /// Incident detection and lifecycle contract (PRD §3.3). Implemented in Incidents.
    /// </summary>
    public interface IIncidentService
    {
        IReadOnlyList<string> ActiveIncidentIds { get; }
        Result<string> DetectIncident(string nodeId, string ruleId, string severity);
        Result<bool> TransitionIncident(string incidentId, string toState);
        Result<IDictionary<string, object>> GetIncident(string incidentId);
    }
}
