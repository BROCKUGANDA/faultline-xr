namespace Faultline.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    /// <summary>
    /// Serializable schema for a persisted scenario. All fields are plain C#
    /// value/reference types — no UnityEngine, no UnityEditor.
    ///
    /// Versioning: <see cref="SchemaVersion"/> is incremented on every
    /// breaking schema change. <see cref="SaveSchema.MigrateV0ToV1"/> and any
    /// future migrators translate older payloads into the current shape.
    /// </summary>
    [Serializable]
    public sealed class ScenarioSaveData
    {
        /// <summary>Schema version of this payload (CurrentSchemaVersion = 1).</summary>
        public int SchemaVersion { get; set; }

        /// <summary>User settings captured at save time.</summary>
        public UserSettings UserSettings { get; set; }

        /// <summary>Scenario progress snapshot.</summary>
        public ScenarioProgressEntry ScenarioProgress { get; set; }

        /// <summary>Current incident snapshot (null when none active).</summary>
        public CurrentIncidentEntry CurrentIncident { get; set; }

        /// <summary>Agent findings recorded for this scenario.</summary>
        public IReadOnlyList<AgentFindingEntry> AgentFindings { get; set; }

        /// <summary>Approval decisions recorded for this scenario.</summary>
        public IReadOnlyList<ApprovalDecisionEntry> ApprovalDecisions { get; set; }

        /// <summary>Recovery results recorded for this scenario.</summary>
        public IReadOnlyList<RecoveryResultEntry> RecoveryResults { get; set; }

        /// <summary>Timeline events recorded for this scenario.</summary>
        public IReadOnlyList<TimelineEventEntry> TimelineEvents { get; set; }

        public ScenarioSaveData()
        {
            SchemaVersion = SaveSchema.CurrentSchemaVersion;
            UserSettings = new UserSettings();
            ScenarioProgress = new ScenarioProgressEntry();
            CurrentIncident = null;
            AgentFindings = new ReadOnlyCollection<AgentFindingEntry>(
                new List<AgentFindingEntry>());
            ApprovalDecisions = new ReadOnlyCollection<ApprovalDecisionEntry>(
                new List<ApprovalDecisionEntry>());
            RecoveryResults = new ReadOnlyCollection<RecoveryResultEntry>(
                new List<RecoveryResultEntry>());
            TimelineEvents = new ReadOnlyCollection<TimelineEventEntry>(
                new List<TimelineEventEntry>());
        }

        /// <summary>Scenario progress snapshot.</summary>
        [Serializable]
        public sealed class ScenarioProgressEntry
        {
            public string ScenarioId { get; set; }
            public string SessionId { get; set; }
            public string Status { get; set; }          // e.g. NotStarted, InProgress, Completed
            public double ProgressPct { get; set; }     // 0..100
            public DateTimeOffset StartedAt { get; set; }
            public DateTimeOffset? CompletedAt { get; set; }

            public ScenarioProgressEntry()
            {
                ScenarioId = null;
                SessionId = null;
                Status = "NotStarted";
                ProgressPct = 0.0;
                StartedAt = DateTimeOffset.UtcNow;
                CompletedAt = null;
            }
        }

        /// <summary>Current incident snapshot.</summary>
        [Serializable]
        public sealed class CurrentIncidentEntry
        {
            public string Id { get; set; }
            public string State { get; set; }           // lifecycle state name
            public string NodeId { get; set; }          // node the incident is on
            public DateTimeOffset DetectedAt { get; set; }

            public CurrentIncidentEntry()
            {
                Id = null;
                State = null;
                NodeId = null;
                DetectedAt = DateTimeOffset.UtcNow;
            }
        }

        /// <summary>Serialized agent finding.</summary>
        [Serializable]
        public sealed class AgentFindingEntry
        {
            public string AgentRole { get; set; }
            public string TaskId { get; set; }
            public IReadOnlyList<string> EvidenceIds { get; set; }
            public string Finding { get; set; }
            public double Confidence { get; set; }
            public string Recommendation { get; set; }
            public DateTimeOffset Timestamp { get; set; }

            public AgentFindingEntry()
            {
                AgentRole = null;
                TaskId = null;
                EvidenceIds = new ReadOnlyCollection<string>(new List<string>());
                Finding = null;
                Confidence = 0.0;
                Recommendation = null;
                Timestamp = DateTimeOffset.UtcNow;
            }
        }

        /// <summary>Serialized approval decision.</summary>
        [Serializable]
        public sealed class ApprovalDecisionEntry
        {
            public string ProposalId { get; set; }
            public bool Approved { get; set; }
            public DateTimeOffset Timestamp { get; set; }
            public string ApproverId { get; set; }

            public ApprovalDecisionEntry()
            {
                ProposalId = null;
                Approved = false;
                Timestamp = DateTimeOffset.UtcNow;
                ApproverId = null;
            }
        }

        /// <summary>Serialized recovery result.</summary>
        [Serializable]
        public sealed class RecoveryResultEntry
        {
            public string ProposalId { get; set; }
            public bool Success { get; set; }
            public double HealthDelta { get; set; }
            public DateTimeOffset VerifiedAt { get; set; }

            public RecoveryResultEntry()
            {
                ProposalId = null;
                Success = false;
                HealthDelta = 0.0;
                VerifiedAt = DateTimeOffset.UtcNow;
            }
        }

        /// <summary>Serialized timeline event.</summary>
        [Serializable]
        public sealed class TimelineEventEntry
        {
            public string EventType { get; set; }
            public DateTimeOffset Timestamp { get; set; }
            public string Description { get; set; }
            public string CorrelationId { get; set; }

            public TimelineEventEntry()
            {
                EventType = null;
                Timestamp = DateTimeOffset.UtcNow;
                Description = null;
                CorrelationId = null;
            }
        }
    }
}
