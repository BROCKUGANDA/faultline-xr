namespace Faultline.Presentation
{
    using System;

    /// <summary>
    /// Agent control panel screen contract (PRD §6.1, screen 10).
    /// Entry conditions: investigation started; agent roles available.
    /// Exit conditions: agent task dispatched or panel closed.
    /// Interaction model: dispatch agent to target; view agent task status.
    /// Back navigation: IncidentDetailScreen.
    /// Accessibility: agent roles announced; dispatch requires explicit
    /// confirm (no hidden gesture).
    /// </summary>
    public interface AgentControlPanelScreen : IScreen
    {
        /// <summary>Available agent role ids.</summary>
        System.Collections.Generic.IReadOnlyList<string> AgentRoles { get; }

        /// <summary>Id of the target node/incident for dispatch.</summary>
        string TargetId { get; }

        event EventHandler<AgentDispatchRequestedEventArgs> AgentDispatchRequested;
        event EventHandler AgentTaskCompleted;
    }

    public sealed class AgentDispatchRequestedEventArgs : EventArgs
    {
        public string AgentRole { get; }
        public string TargetId { get; }
        public AgentDispatchRequestedEventArgs(string agentRole, string targetId)
        {
            AgentRole = agentRole;
            TargetId = targetId;
        }
    }
}
