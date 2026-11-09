namespace Faultline.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using Faultline.Observability;

    /// <summary>
    /// User settings captured as part of a session. Pure C# — no UnityEngine.
    /// </summary>
    [Serializable]
    public sealed class UserSettings
    {
        public bool HandTrackingPreferred { get; set; } = true;
        public bool SeatedModePreferred { get; set; } = true;
        public bool ControllerFreeRequired { get; set; } = true;
        public double ComfortSnapAngleDegrees { get; set; } = 30.0;
        public string LanguageCode { get; set; } = "en";
    }

    /// <summary>
    /// Runtime session state: everything that must survive a save/load round
    /// trip. Serializable and engine-agnostic (no UnityEngine).
    ///
    /// Holds the live scenario/incident references plus the agent findings,
    /// approval decisions, recovery results and timeline events produced
    /// during the session, together with the user settings reference.
    /// </summary>
    [Serializable]
    public sealed class SessionState
    {
        /// <summary>Opaque session identifier.</summary>
        public string SessionId { get; set; }

        /// <summary>Currently active scenario id (null when none active).</summary>
        public string ScenarioId { get; set; }

        /// <summary>Currently active incident id (null when none active).</summary>
        public string IncidentId { get; set; }

        /// <summary>Incident lifecycle state name (null when none active).</summary>
        public string IncidentState { get; set; }

        /// <summary>Node the incident is on (null when none active).</summary>
        public string IncidentNodeId { get; set; }

        /// <summary>When the incident was detected (null when none active).</summary>
        public DateTimeOffset? IncidentDetectedAt { get; set; }

        /// <summary>Scenario progress 0..100 (null when untracked).</summary>
        public double? ProgressPct { get; set; }

        /// <summary>Scenario status, e.g. NotStarted/InProgress/Completed.</summary>
        public string ProgressStatus { get; set; }

        /// <summary>When the scenario started (null when untracked).</summary>
        public DateTimeOffset? StartedAt { get; set; }

        /// <summary>When the scenario completed (null while in progress).</summary>
        public DateTimeOffset? CompletedAt { get; set; }

        /// <summary>User settings reference for this session.</summary>
        public UserSettings UserSettings { get; set; }

        /// <summary>Agent findings recorded during the session.</summary>
        public IReadOnlyList<ScenarioSaveData.AgentFindingEntry> AgentFindings { get; set; }

        /// <summary>Approval decisions recorded during the session.</summary>
        public IReadOnlyList<ScenarioSaveData.ApprovalDecisionEntry> ApprovalDecisions { get; set; }

        /// <summary>Recovery results recorded during the session.</summary>
        public IReadOnlyList<ScenarioSaveData.RecoveryResultEntry> RecoveryResults { get; set; }

        /// <summary>Timeline events recorded during the session.</summary>
        public IReadOnlyList<ScenarioSaveData.TimelineEventEntry> TimelineEvents { get; set; }

        /// <summary>Whether this state represents a fresh/empty session.</summary>
        public bool IsEmpty => string.IsNullOrEmpty(ScenarioId) &&
            AgentFindings.Count == 0 && ApprovalDecisions.Count == 0 &&
            RecoveryResults.Count == 0 && TimelineEvents.Count == 0;

        public SessionState()
        {
            SessionId = CorrelationIdProvider.Create("session");
            ScenarioId = null;
            IncidentId = null;
            IncidentState = null;
            IncidentNodeId = null;
            IncidentDetectedAt = null;
            ProgressPct = null;
            ProgressStatus = null;
            StartedAt = null;
            CompletedAt = null;
            UserSettings = new UserSettings();
            AgentFindings = new ReadOnlyCollection<ScenarioSaveData.AgentFindingEntry>(
                new List<ScenarioSaveData.AgentFindingEntry>());
            ApprovalDecisions = new ReadOnlyCollection<ScenarioSaveData.ApprovalDecisionEntry>(
                new List<ScenarioSaveData.ApprovalDecisionEntry>());
            RecoveryResults = new ReadOnlyCollection<ScenarioSaveData.RecoveryResultEntry>(
                new List<ScenarioSaveData.RecoveryResultEntry>());
            TimelineEvents = new ReadOnlyCollection<ScenarioSaveData.TimelineEventEntry>(
                new List<ScenarioSaveData.TimelineEventEntry>());
        }

        /// <summary>Create an empty/default session state.</summary>
        public static SessionState Empty() => new SessionState();
    }
}
