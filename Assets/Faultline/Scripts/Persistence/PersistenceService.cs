namespace Faultline.Persistence
{
    using System;
    using System.IO;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using Faultline.Domain;
    using Faultline.Domain.Errors;
    using Faultline.Observability;

    /// <summary>
    /// Default implementation of <see cref="IPersistenceService"/>.
    ///
    /// Uses System.Text.Json for safe serialization. All failure modes
    /// (missing file, corrupted JSON, IO errors, schema mismatch) degrade
    /// gracefully to default/empty session state — this service never throws
    /// on load and never crashes the host.
    ///
    /// Pure C# — no UnityEngine. <see cref="SaveFilePath"/> is abstracted as
    /// a string so the service is testable without Unity; the Unity-backed
    /// integration points it at Application.persistentDataPath.
    /// </summary>
    public sealed class PersistenceService : IPersistenceService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };

        private readonly string _saveFilePath;
        private readonly FaultlineLogger _logger;

        /// <summary>Create the service with a given save path.</summary>
        /// <param name="saveFilePath">Full path to the save file (abstracted
        /// from UnityEngine for testability).</param>
        /// <param name="logger">Optional observability logger.</param>
        public PersistenceService(string saveFilePath, FaultlineLogger logger = null)
        {
            if (string.IsNullOrWhiteSpace(saveFilePath))
            {
                throw new ArgumentException("Save file path is required.", nameof(saveFilePath));
            }

            _saveFilePath = saveFilePath;
            _logger = logger;
        }

        /// <summary>Filesystem path the save file lives at.</summary>
        public string SaveFilePath => _saveFilePath;

        /// <summary>Persist the given session state atomically.</summary>
        public Result<bool> Save(SessionState state)
        {
            if (state == null)
            {
                return Result<bool>.Failure("FL-PS-0100",
                    "Save failed: state is null.",
                    diagnosticContext: new Dictionary<string, object> { { "path", _saveFilePath } });
            }

            try
            {
                var directory = Path.GetDirectoryName(_saveFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var payload = ToPayload(state);
                var json = JsonSerializer.Serialize(payload, JsonOptions);

                // Atomic write: write to a temp file then move into place so
                // a crash mid-write never leaves a partially-written save.
                var tempPath = _saveFilePath + ".tmp";
                File.WriteAllText(tempPath, json);
                if (File.Exists(_saveFilePath))
                {
                    File.Replace(tempPath, _saveFilePath, null);
                }
                else
                {
                    File.Move(tempPath, _saveFilePath);
                }

                _logger?.Log(LogLevel.Info, "Scenario state saved.",
                    CorrelationIdProvider.Create("ps"), new Dictionary<string, object>
                    {
                        { "path", _saveFilePath },
                        { "scenarioId", state.ScenarioId },
                        { "schemaVersion", payload.SchemaVersion }
                    });

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                var correlationId = CorrelationIdProvider.Create("ps");
                _logger?.Log(LogLevel.Error, $"Save failed: {ex.Message}.",
                    correlationId, new Dictionary<string, object> { { "path", _saveFilePath } });

                return Result<bool>.Failure("FL-PS-0100",
                    $"Save failed: {ex.Message}.",
                    diagnosticContext: new Dictionary<string, object>
                    {
                        { "path", _saveFilePath },
                        { "scenarioId", state.ScenarioId },
                        { "exceptionType", ex.GetType().Name }
                    });
            }
        }

        /// <summary>Load session state from disk. Never throws.</summary>
        public Result<SessionState> Load()
        {
            try
            {
                if (!File.Exists(_saveFilePath))
                {
                    _logger?.Log(LogLevel.Debug, "No save data found; returning empty state.",
                        CorrelationIdProvider.Create("ps"), new Dictionary<string, object>
                        {
                            { "path", _saveFilePath }
                        });

                    return Result<SessionState>.Failure("FL-PS-0101",
                        "Load failed: no save data found.",
                        diagnosticContext: new Dictionary<string, object> { { "path", _saveFilePath } });
                }

                var json = File.ReadAllText(_saveFilePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return Corrupted("Save data is empty.", "FL-PS-0101");
                }

                var payload = JsonSerializer.Deserialize<ScenarioSaveData>(json, JsonOptions);
                if (payload == null)
                {
                    return Corrupted("Deserialization returned null.", "FL-PS-0101");
                }

                try
                {
                    if (!SaveSchema.IsValid(payload, out var reason))
                    {
                        return Corrupted($"Invalid save data: {reason}", "FL-PS-0101");
                    }

                    if (payload.SchemaVersion < SaveSchema.CurrentSchemaVersion)
                    {
                        SaveSchema.MigrateV0ToV1(payload);
                    }

                    var state = ToState(payload);
                    _logger?.Log(LogLevel.Info, "Scenario state loaded.",
                        CorrelationIdProvider.Create("ps"), new Dictionary<string, object>
                        {
                            { "path", _saveFilePath },
                            { "scenarioId", state.ScenarioId },
                            { "schemaVersion", payload.SchemaVersion }
                        });

                    return Result<SessionState>.Success(state);
                }
                catch (Exception ex)
                {
                    return Corrupted($"Save data failed validation: {ex.Message}.", "FL-PS-0101");
                }
            }
            catch (JsonException ex)
            {
                return Corrupted($"Save data is corrupted (invalid JSON): {ex.Message}.", "FL-PS-0101");
            }
            catch (Exception ex)
            {
                var correlationId = CorrelationIdProvider.Create("ps");
                _logger?.Log(LogLevel.Error, $"Load failed: {ex.Message}.",
                    correlationId, new Dictionary<string, object> { { "path", _saveFilePath } });

                return Corrupted($"Load failed: {ex.Message}.", "FL-PS-0101", correlationId);
            }
        }

        /// <summary>Delete any persisted save data.</summary>
        public Result<bool> Reset()
        {
            try
            {
                if (File.Exists(_saveFilePath))
                {
                    File.Delete(_saveFilePath);
                }

                var tempPath = _saveFilePath + ".tmp";
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }

                _logger?.Log(LogLevel.Info, "Save data reset.",
                    CorrelationIdProvider.Create("ps"), new Dictionary<string, object>
                    {
                        { "path", _saveFilePath }
                    });

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure("FL-PS-0100",
                    $"Reset failed: {ex.Message}.",
                    diagnosticContext: new Dictionary<string, object> { { "path", _saveFilePath } });
            }
        }

        /// <summary>Whether a valid save file exists on disk.</summary>
        public bool HasSaveData()
        {
            try
            {
                if (!File.Exists(_saveFilePath))
                {
                    return false;
                }

                var json = File.ReadAllText(_saveFilePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return false;
                }

                var payload = JsonSerializer.Deserialize<ScenarioSaveData>(json, JsonOptions);
                return payload != null && SaveSchema.IsValid(payload, out _);
            }
            catch
            {
                // Any read/deserialization problem means no usable save data.
                return false;
            }
        }

        // ------------------------------------------------------------------
        // Internal mapping between runtime state and the serializable schema.
        // ------------------------------------------------------------------

        private ScenarioSaveData ToPayload(SessionState state)
        {
            return new ScenarioSaveData
            {
                SchemaVersion = SaveSchema.CurrentSchemaVersion,
                UserSettings = state.UserSettings ?? new UserSettings(),
                ScenarioProgress = new ScenarioSaveData.ScenarioProgressEntry
                {
                    ScenarioId = state.ScenarioId,
                    SessionId = state.SessionId,
                    Status = state.ProgressStatus ?? (string.IsNullOrEmpty(state.ScenarioId) ? "NotStarted" : "InProgress"),
                    ProgressPct = state.ProgressPct ?? 0.0,
                    StartedAt = state.StartedAt ?? DateTimeOffset.UtcNow,
                    CompletedAt = state.CompletedAt
                },
                CurrentIncident = string.IsNullOrEmpty(state.IncidentId)
                    ? null
                    : new ScenarioSaveData.CurrentIncidentEntry
                    {
                        Id = state.IncidentId,
                        State = state.IncidentState,
                        NodeId = state.IncidentNodeId,
                        DetectedAt = state.IncidentDetectedAt ?? DateTimeOffset.UtcNow
                    },
                AgentFindings = MapFindings(state.AgentFindings),
                ApprovalDecisions = MapApprovals(state.ApprovalDecisions),
                RecoveryResults = MapRecoveries(state.RecoveryResults),
                TimelineEvents = MapTimeline(state.TimelineEvents)
            };
        }

        private SessionState ToState(ScenarioSaveData payload)
        {
            return new SessionState
            {
                SessionId = payload.ScenarioProgress?.SessionId
                    ?? CorrelationIdProvider.Create("session"),
                ScenarioId = payload.ScenarioProgress?.ScenarioId,
                IncidentId = payload.CurrentIncident?.Id,
                IncidentState = payload.CurrentIncident?.State,
                IncidentNodeId = payload.CurrentIncident?.NodeId,
                IncidentDetectedAt = payload.CurrentIncident?.DetectedAt,
                ProgressPct = payload.ScenarioProgress?.ProgressPct,
                ProgressStatus = payload.ScenarioProgress?.Status,
                StartedAt = payload.ScenarioProgress?.StartedAt,
                CompletedAt = payload.ScenarioProgress?.CompletedAt,
                UserSettings = payload.UserSettings ?? new UserSettings(),
                AgentFindings = MapFindingsBack(payload.AgentFindings),
                ApprovalDecisions = MapApprovalsBack(payload.ApprovalDecisions),
                RecoveryResults = MapRecoveriesBack(payload.RecoveryResults),
                TimelineEvents = MapTimelineBack(payload.TimelineEvents)
            };
        }

        private static List<ScenarioSaveData.AgentFindingEntry> MapFindings(
            IReadOnlyList<ScenarioSaveData.AgentFindingEntry> findings) =>
            findings?.ToList() ?? new List<ScenarioSaveData.AgentFindingEntry>();

        private static List<ScenarioSaveData.ApprovalDecisionEntry> MapApprovals(
            IReadOnlyList<ScenarioSaveData.ApprovalDecisionEntry> decisions) =>
            decisions?.ToList() ?? new List<ScenarioSaveData.ApprovalDecisionEntry>();

        private static List<ScenarioSaveData.RecoveryResultEntry> MapRecoveries(
            IReadOnlyList<ScenarioSaveData.RecoveryResultEntry> results) =>
            results?.ToList() ?? new List<ScenarioSaveData.RecoveryResultEntry>();

        private static List<ScenarioSaveData.TimelineEventEntry> MapTimeline(
            IReadOnlyList<ScenarioSaveData.TimelineEventEntry> events) =>
            events?.ToList() ?? new List<ScenarioSaveData.TimelineEventEntry>();

        private static IReadOnlyList<ScenarioSaveData.AgentFindingEntry> MapFindingsBack(
            IReadOnlyList<ScenarioSaveData.AgentFindingEntry> findings) =>
            findings ?? new List<ScenarioSaveData.AgentFindingEntry>();

        private static IReadOnlyList<ScenarioSaveData.ApprovalDecisionEntry> MapApprovalsBack(
            IReadOnlyList<ScenarioSaveData.ApprovalDecisionEntry> decisions) =>
            decisions ?? new List<ScenarioSaveData.ApprovalDecisionEntry>();

        private static IReadOnlyList<ScenarioSaveData.RecoveryResultEntry> MapRecoveriesBack(
            IReadOnlyList<ScenarioSaveData.RecoveryResultEntry> results) =>
            results ?? new List<ScenarioSaveData.RecoveryResultEntry>();

        private static IReadOnlyList<ScenarioSaveData.TimelineEventEntry> MapTimelineBack(
            IReadOnlyList<ScenarioSaveData.TimelineEventEntry> events) =>
            events ?? new List<ScenarioSaveData.TimelineEventEntry>();

        private Result<SessionState> Corrupted(string reason, string errorCode,
            string correlationId = null)
        {
            _logger?.Log(LogLevel.Warning, reason,
                correlationId ?? CorrelationIdProvider.Create("ps"),
                new Dictionary<string, object> { { "path", _saveFilePath } });

            return Result<SessionState>.Failure(errorCode,
                $"Load failed: {reason}",
                diagnosticContext: new Dictionary<string, object> { { "path", _saveFilePath } });
        }
    }
}
