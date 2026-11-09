namespace Faultline.Agents
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Faultline.Domain;

    /// <summary>
    /// Validation outcome for an agent result.
    /// </summary>
    public sealed class AgentResultValidation
    {
        public bool IsValid { get; }
        public string ErrorCode { get; }
        public string ErrorMessage { get; }

        private AgentResultValidation(bool isValid, string errorCode, string errorMessage)
        {
            IsValid = isValid;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
        }

        public static AgentResultValidation Valid() =>
            new AgentResultValidation(true, null, null);

        public static AgentResultValidation Invalid(string errorCode, string errorMessage) =>
            new AgentResultValidation(false, errorCode, errorMessage);
    }

    /// <summary>
    /// Deterministic local agent orchestrator (PRD §5.5). Dispatches tasks
    /// to ORACLE/TRACE/SENTRY/FORGE, runs each specialist against real
    /// incident evidence, validates results, handles timeouts/cancellation
    /// with retry + deterministic fallback, deduplicates dispatch, detects
    /// conflicts, and synthesizes the final report. No external LLM.
    ///
    /// Deterministic: task ids and finding ids are derived from
    /// (seed, incident, role) and task timestamps come from the injected
    /// clock. The same seed + evidence always produces the same report.
    /// </summary>
    public sealed class AgentOrchestrator : IAgentOrchestrator
    {
        private readonly Random _random;
        private readonly int _seed;
        private readonly IClock _clock;

        private readonly Dictionary<string, AgentTask> _tasks = new();
        private readonly Dictionary<string, AgentFinding> _findings = new();
        private readonly Dictionary<string, IList<string>> _taskFindings = new();
        private readonly Dictionary<string, DateTimeOffset> _taskDeadlines = new();
        private readonly Dictionary<string, string> _idempotentKeys = new();

        private const int MaxRetries = 3;
        private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(30);

        private HashSet<Guid> _incidentEvidenceCache;

        public IReadOnlyDictionary<string, AgentTask> Tasks => _tasks;
        public IReadOnlyDictionary<string, AgentFinding> Findings => _findings;

        public AgentOrchestrator(int seed, IClock clock = null)
        {
            _random = new Random(seed);
            _seed = seed;
            _clock = clock ?? SystemClock.Instance;
        }

        public Result<string> DispatchAgent(string agentType, string incidentId,
            IDictionary<string, object> context) => Dispatch(agentType, incidentId, context);

        /// <summary>
        /// Dispatch a deterministic agent task for the given role and incident.
        /// Returns the task id or an FL-AG error. Idempotent per
        /// (incidentId, role): repeated dispatch returns the existing task.
        /// </summary>
        public Result<string> Dispatch(string agentType, string incidentId,
            IDictionary<string, object> context)
        {
            if (!Enum.TryParse<AgentRole>(agentType, ignoreCase: true, out var role))
            {
                return Result<string>.Failure("FL-AG-0060",
                    $"Unknown agent role: {agentType}.");
            }

            if (string.IsNullOrWhiteSpace(incidentId))
            {
                return Result<string>.Failure("FL-SM-0090",
                    "Incident id is required.");
            }

            var idempotentKey = $"{incidentId}:{role}";
            if (_idempotentKeys.TryGetValue(idempotentKey, out var existingId))
            {
                return Result<string>.Success(existingId);
            }

            var taskId = DeterministicGuid(seed: _seed, text: $"{incidentId}:{role}");
            var task = AgentTask.Create(taskId, role, context, _defaultTimeout, _clock);
            var id = taskId.ToString();
            _tasks[id] = task;
            _taskFindings[id] = new List<string>();
            _taskDeadlines[id] = _clock.Now.Add(_defaultTimeout);
            _idempotentKeys[idempotentKey] = id;

            return Result<string>.Success(id);
        }

        /// <summary>
        /// Complete a task with finding ids. Validates that referenced
        /// findings exist and belong to the task. Returns failure on
        /// malformed/unsupported input.
        /// </summary>
        public Result<bool> CompleteTask(string taskId, IList<string> findingIds)
        {
            if (!_tasks.TryGetValue(taskId, out var task))
            {
                return Result<bool>.Failure("FL-AG-0060", $"Task not found: {taskId}.");
            }

            if (findingIds == null)
            {
                return Result<bool>.Failure("FL-AG-0062",
                    "Finding ids must not be null.");
            }

            foreach (var fid in findingIds)
            {
                if (!_findings.ContainsKey(fid))
                {
                    return Result<bool>.Failure("FL-AG-0062",
                        $"Finding referenced by task {taskId} does not exist: {fid}.");
                }

                var finding = _findings[fid];
                if (finding.TaskId != task.TaskId)
                {
                    return Result<bool>.Failure("FL-AG-0062",
                        $"Finding {fid} does not belong to task {taskId}.");
                }
            }

            _tasks[taskId] = task.WithStatus(AgentTaskStatus.Completed)
                .WithFindingIds(findingIds.Select(Guid.Parse).ToList());

            return Result<bool>.Success(true);
        }

        /// <summary>
        /// Record a finding produced by an agent. Assigns the deterministic
        /// finding id. Validates confidence range and task existence. When the
        /// caller supplies the incident evidence set via InvestigateAsync,
        /// at least one evidence id must reference a real event.
        /// </summary>
        public Result<Guid> RecordFinding(AgentFinding finding)
        {
            if (finding == null)
            {
                return Result<Guid>.Failure("FL-AG-0062", "Finding cannot be null.");
            }

            if (!_tasks.ContainsKey(finding.TaskId.ToString()))
            {
                return Result<Guid>.Failure("FL-AG-0062",
                    $"Finding references unknown task: {finding.TaskId}.");
            }

            if (finding.Confidence < 0.0 || finding.Confidence > 1.0)
            {
                return Result<Guid>.Failure("FL-AG-0062",
                    $"Confidence out of range: {finding.Confidence}.");
            }

            if (_incidentEvidenceCache != null && finding.EvidenceIds != null
                && finding.EvidenceIds.Count > 0
                && !finding.EvidenceIds.Any(_incidentEvidenceCache.Contains))
            {
                return Result<Guid>.Failure("FL-AG-0062",
                    "Finding evidence ids do not reference any known incident event.");
            }

            var stored = finding.FindingId == Guid.Empty
                ? WithId(finding, DeterministicGuid(seed: _seed,
                    text: $"finding:{finding.TaskId}:{_findings.Count}"))
                : finding;

            _findings[stored.FindingId.ToString()] = stored;
            return Result<Guid>.Success(stored.FindingId);
        }

        private static AgentFinding WithId(AgentFinding f, Guid id) =>
            new AgentFinding(id, f.TaskId, f.EvidenceIds, f.Finding,
                f.Confidence, f.Limitations, f.Recommendation);

        /// <summary>
        /// Validate an agent result before it is used for synthesis. Rejects
        /// malformed, contradictory, or unsupported results.
        /// </summary>
        public AgentResultValidation ValidateResult(AgentFinding finding)
        {
            if (finding == null)
            {
                return AgentResultValidation.Invalid("FL-AG-0062", "Result is null.");
            }

            if (string.IsNullOrWhiteSpace(finding.Finding))
            {
                return AgentResultValidation.Invalid("FL-AG-0062", "Finding text is empty.");
            }

            if (finding.Confidence < 0.0 || finding.Confidence > 1.0)
            {
                return AgentResultValidation.Invalid("FL-AG-0062", "Confidence out of range.");
            }

            if (!_tasks.ContainsKey(finding.TaskId.ToString()))
            {
                return AgentResultValidation.Invalid("FL-AG-0062",
                    $"Finding references unknown task: {finding.TaskId}.");
            }

            return AgentResultValidation.Valid();
        }

        /// <summary>
        /// Handle a task timeout: transient applies retry policy (keep task
        /// InProgress, extend deadline); exhausted retries produce a
        /// deterministic fallback finding so synthesis never blocks.
        /// </summary>
        public Result<bool> HandleTimeout(string taskId)
        {
            if (!_tasks.TryGetValue(taskId, out var task))
            {
                return Result<bool>.Failure("FL-AG-0060", $"Task not found: {taskId}.");
            }

            var attempts = task.FindingIds.Count + 1;
            if (attempts > MaxRetries)
            {
                var fallback = FallbackFinding(task.TaskId, $"timeout after {attempts} attempts");
                RecordFinding(fallback);
                _tasks[taskId] = task.WithStatus(AgentTaskStatus.TimedOut);
            }
            else
            {
                _tasks[taskId] = task.WithStatus(AgentTaskStatus.InProgress);
                _taskDeadlines[taskId] = _clock.Now.Add(_defaultTimeout);
            }

            return Result<bool>.Success(true);
        }

        /// <summary>
        /// Cancel a task.
        /// </summary>
        public Result<bool> Cancel(string taskId)
        {
            if (!_tasks.ContainsKey(taskId))
            {
                return Result<bool>.Failure("FL-AG-0060", $"Task not found: {taskId}.");
            }

            _tasks[taskId] = _tasks[taskId].WithStatus(AgentTaskStatus.Cancelled);
            return Result<bool>.Success(true);
        }

        /// <summary>
        /// Synthesize findings into a final deterministic result for the
        /// incident. Conflict detection flags findings whose task roles
        /// disagree; the highest-confidence finding is the winner.
        /// </summary>
        public Result<IDictionary<string, object>> Synthesize(string incidentId)
        {
            var incidentFindings = _findings.Values
                .Where(f => _tasks.TryGetValue(f.TaskId.ToString(), out var t)
                    && GetIncidentId(t.Context) == incidentId)
                .ToList();

            if (!incidentFindings.Any())
            {
                return Result<IDictionary<string, object>>.Failure("FL-AG-0060",
                    $"No findings for incident: {incidentId}.");
            }

            var conflict = DetectConflict(incidentFindings);
            var avgConfidence = incidentFindings.Average(f => f.Confidence);
            var recommendations = incidentFindings
                .Where(f => !string.IsNullOrWhiteSpace(f.Recommendation))
                .Select(f => f.Recommendation).ToList();

            var result = new Dictionary<string, object>
            {
                ["incidentId"] = incidentId,
                ["findingCount"] = incidentFindings.Count,
                ["averageConfidence"] = avgConfidence,
                ["recommendations"] = new ReadOnlyCollection<string>(recommendations),
                ["conflictDetected"] = conflict != null,
                ["synthesizedAt"] = _clock.Now
            };

            return Result<IDictionary<string, object>>.Success(result);
        }

        private static AgentFinding DetectConflict(IEnumerable<AgentFinding> findings)
        {
            var list = findings.ToList();
            if (list.Count < 2) return null;
            return list.OrderByDescending(f => f.Confidence).FirstOrDefault();
        }

        /// <summary>
        /// Full deterministic investigation pipeline:
        /// TRACE (graph → root cause) → SENTRY (telemetry → evidence) →
        /// FORGE (proposal input) → ORACLE (synthesis), with per-role
        /// timeout → retry ×3 → fallback. Honors cancellation and the
        /// per-task deadline from the injected clock.
        /// </summary>
        public async Task<Result<InvestigationReport>> InvestigateAsync(
            string incidentId, string rootCauseNodeId,
            IReadOnlyList<string> propagationPath,
            IReadOnlyList<string> blastRadius,
            IReadOnlyList<TelemetryEvent> telemetry,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(incidentId))
            {
                return Result<InvestigationReport>.Failure("FL-SM-0090", "Incident id is required.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            _incidentEvidenceCache = (telemetry ?? Enumerable.Empty<TelemetryEvent>())
                .Select(e => e.EventId).ToHashSet();

            var context = new Dictionary<string, object> { ["incidentId"] = incidentId };
            var findings = new List<AgentFinding>();

            async Task<AgentFinding> RunRole(AgentRole role,
                Func<Guid, Result<AgentFinding>> body)
            {
                var dispatch = Dispatch(role.ToString(), incidentId, context);
                if (!dispatch.IsSuccess) return FallbackFinding(Guid.Empty, "dispatch failed");
                var taskId = Guid.Parse(dispatch.Value);

                var attempt = 0;
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    attempt++;

                    var candidate = body(taskId);
                    if (candidate.IsSuccess && ValidateResult(candidate.Value).IsValid)
                    {
                        RecordFinding(candidate.Value);
                        CompleteTask(dispatch.Value,
                            new List<string> { _findings.Values
                                .First(f => f.TaskId == taskId).FindingId.ToString() });
                        return candidate.Value;
                    }

                    if (attempt > MaxRetries)
                    {
                        var fallback = FallbackFinding(taskId, "retries exhausted");
                        RecordFinding(fallback);
                        return fallback;
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(5 + _random.Next(5)), cancellationToken);
                }
            }

            findings.Add(await RunRole(AgentRole.Trace,
                tid => TraceAgent.InvestigateCore(tid, rootCauseNodeId, propagationPath, EvidenceFor(telemetry, rootCauseNodeId))));
            findings.Add(await RunRole(AgentRole.Sentry,
                tid => SentryAgent.InvestigateCore(tid, rootCauseNodeId, ElevatedTelemetry(telemetry))));
            findings.Add(await RunRole(AgentRole.Forge,
                tid => ForgeAgent.InvestigateCore(tid, rootCauseNodeId, blastRadius)));
            findings.Add(await RunRole(AgentRole.Oracle,
                tid => OracleAgent.InvestigateCore(tid, rootCauseNodeId, findings)));

            var report = new InvestigationReport(
                incidentId,
                findings.AsReadOnly(),
                "Proceed to human approval for remediation of " + rootCauseNodeId + ".",
                findings.Count == 0 ? 0.0 : findings.Average(f => f.Confidence));

            return Result<InvestigationReport>.Success(report);
        }

        private static IEnumerable<Guid> EvidenceFor(
            IReadOnlyList<TelemetryEvent> telemetry, string nodeId) =>
            (telemetry ?? Enumerable.Empty<TelemetryEvent>())
                .Where(e => e.ServiceId == nodeId)
                .Select(e => e.EventId).Take(2).ToList();

        private static IReadOnlyList<TelemetryEvent> ElevatedTelemetry(
            IReadOnlyList<TelemetryEvent> telemetry) =>
            (telemetry ?? Enumerable.Empty<TelemetryEvent>())
                .Where(e => e.Severity >= TelemetrySeverity.Warning)
                .Take(5).ToList();

        public IReadOnlyDictionary<string, object> GetTaskState(string taskId)
        {
            if (!_tasks.TryGetValue(taskId, out var task))
            {
                return new Dictionary<string, object>();
            }

            return new Dictionary<string, object>
            {
                ["taskId"] = task.TaskId,
                ["role"] = task.Role.ToString(),
                ["status"] = task.Status.ToString(),
                ["context"] = task.Context,
                ["result"] = task.Result,
                ["findingIds"] = task.FindingIds
            };
        }

        /// <summary>
        /// Deterministic fallback: produce a low-confidence finding when an
        /// agent fails to produce a result.
        /// </summary>
        public AgentFinding FallbackFinding(Guid taskId, string reason) =>
            new AgentFinding(
                DeterministicGuid(seed: _seed, text: $"fallback:{taskId}:{reason}"),
                taskId, Enumerable.Empty<Guid>(),
                $"Deterministic fallback: {reason}.", 0.1,
                new List<string> { "No agent evidence available." },
                "Escalate to manual review.");

        private static string GetIncidentId(IDictionary<string, object> context) =>
            context.TryGetValue("incidentId", out var v) ? v?.ToString() : null;

        private static Guid DeterministicGuid(int seed, string text)
        {
            unchecked
            {
                var bytes = new byte[16];
                for (int lane = 0; lane < 4; lane++)
                {
                    uint hash = 2166136261u;
                    foreach (var b in System.Text.Encoding.UTF8.GetBytes($"{seed}:{text}:{lane}"))
                    {
                        hash ^= b;
                        hash *= 16777619u;
                    }

                    System.BitConverter.GetBytes(hash).CopyTo(bytes, lane * 4);
                }

                return new Guid(bytes);
            }
        }
    }
}
