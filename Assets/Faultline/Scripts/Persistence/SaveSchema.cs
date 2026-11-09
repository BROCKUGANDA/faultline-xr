namespace Faultline.Persistence
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Save schema versioning and migration.
    ///
    /// <see cref="CurrentSchemaVersion"/> is the version this build writes.
    /// Migration stubs translate older payloads into the current shape.
    /// Validation checks a payload is well-formed before it is trusted.
    /// </summary>
    public static class SaveSchema
    {
        /// <summary>Current save schema version. Increment on breaking changes.</summary>
        public const int CurrentSchemaVersion = 1;

        /// <summary>
        /// Migrate a schema-version-0 payload into the current shape.
        /// Version 0 had no <see cref="ScenarioSaveData.SchemaVersion"/> field
        /// and no <see cref="ScenarioSaveData.UserSettings"/> block; this
        /// stub back-fills defaults so the payload round-trips safely.
        /// </summary>
        /// <param name="data">Payload to migrate (mutated in place).</param>
        public static void MigrateV0ToV1(ScenarioSaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (data.SchemaVersion == 0 || data.SchemaVersion < 1)
            {
                data.SchemaVersion = CurrentSchemaVersion;

                if (data.UserSettings == null)
                {
                    data.UserSettings = new UserSettings();
                }

                if (data.ScenarioProgress == null)
                {
                    data.ScenarioProgress = new ScenarioSaveData.ScenarioProgressEntry();
                }

                if (data.AgentFindings == null)
                {
                    data.AgentFindings = new List<ScenarioSaveData.AgentFindingEntry>();
                }

                if (data.ApprovalDecisions == null)
                {
                    data.ApprovalDecisions = new List<ScenarioSaveData.ApprovalDecisionEntry>();
                }

                if (data.RecoveryResults == null)
                {
                    data.RecoveryResults = new List<ScenarioSaveData.RecoveryResultEntry>();
                }

                if (data.TimelineEvents == null)
                {
                    data.TimelineEvents = new List<ScenarioSaveData.TimelineEventEntry>();
                }
            }
        }

        /// <summary>
        /// Validate a deserialized payload. Returns true when the payload is
        /// well-formed enough to trust; false when it should be treated as
        /// corrupted and replaced with default state.
        /// </summary>
        /// <param name="data">Payload to validate.</param>
        /// <param name="reason">Human-readable reason when false.</param>
        public static bool IsValid(ScenarioSaveData data, out string reason)
        {
            reason = null;

            if (data == null)
            {
                reason = "Save data is null.";
                return false;
            }

            if (data.SchemaVersion < 0 || data.SchemaVersion > CurrentSchemaVersion)
            {
                reason = $"Unsupported schema version {data.SchemaVersion} " +
                         $"(current {CurrentSchemaVersion}).";
                return false;
            }

            if (data.ScenarioProgress == null)
            {
                reason = "ScenarioProgress is missing.";
                return false;
            }

            if (data.UserSettings == null)
            {
                reason = "UserSettings is missing.";
                return false;
            }

            if (data.AgentFindings == null ||
                data.ApprovalDecisions == null ||
                data.RecoveryResults == null ||
                data.TimelineEvents == null)
            {
                reason = "One or more list collections are null.";
                return false;
            }

            return true;
        }
    }
}
