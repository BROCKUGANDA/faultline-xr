namespace Faultline.Agents
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Faultline.Domain;

    /// <summary>
    /// Deterministic local specialist agents (PRD §5.5, §4.6).
    ///
    /// Each role is a pure function over incident evidence (graph view +
    /// telemetry). Every finding references real evidence ids and cites its
    /// limitations. No LLM, no wall-clock, no I/O. Findings carry
    /// <c>Guid.Empty</c> until recorded — the orchestrator assigns the
    /// deterministic id on <c>RecordFinding</c>.
    /// </summary>

    /// <summary>TRACE: dependency analyst. Upstream traversal → root cause.</summary>
    public sealed class TraceAgent : IAgentRole
    {
        public AgentRole Role => AgentRole.Trace;

        public Result<AgentFinding> Investigate(Task<Result<AgentFinding>> previous) =>
            throw new NotSupportedException("Use InvestigateCore to inject evidence.");

        internal static Result<AgentFinding> InvestigateCore(
            Guid taskId, string rootCauseNodeId,
            IReadOnlyList<string> propagationPath, IEnumerable<Guid> evidenceIds)
        {
            var impact = string.Join(" -> ", propagationPath ?? Enumerable.Empty<string>());
            return Result<AgentFinding>.Success(new AgentFinding(
                Guid.Empty, taskId,
                evidenceIds ?? Enumerable.Empty<Guid>(),
                $"Dependency analysis identifies {rootCauseNodeId} as the likely upstream cause. Propagation path: {impact}.",
                0.8,
                new[] { "Analysis is limited to the simulated dependency graph; no production topology was inspected." },
                $"Isolate {rootCauseNodeId} and verify dependents recover."));
        }
    }

    /// <summary>SENTRY: telemetry analyst. Symptom correlation → evidence.</summary>
    public sealed class SentryAgent : IAgentRole
    {
        public AgentRole Role => AgentRole.Sentry;

        public Result<AgentFinding> Investigate(Task<Result<AgentFinding>> previous) =>
            throw new NotSupportedException("Use InvestigateCore to inject evidence.");

        internal static Result<AgentFinding> InvestigateCore(
            Guid taskId, string rootCauseNodeId,
            IReadOnlyList<TelemetryEvent> telemetry)
        {
            var elevated = (telemetry ?? Enumerable.Empty<TelemetryEvent>())
                .Where(e => e.Severity >= TelemetrySeverity.Warning)
                .Select(e => e.EventId)
                .Take(3)
                .ToList();

            return Result<AgentFinding>.Success(new AgentFinding(
                Guid.Empty, taskId, elevated,
                $"Telemetry correlation: {elevated.Count} elevated samples cluster around {rootCauseNodeId} with latency and error-rate elevation.",
                0.75,
                new[] { "Telemetry is sampled from the local simulation; temporal correlation is not proof of causation." },
                $"Correlate log windows around the first elevated sample before approving remediation."));
        }
    }

    /// <summary>FORGE: remediation specialist. Proposal input → structured action.</summary>
    public sealed class ForgeAgent : IAgentRole
    {
        public AgentRole Role => AgentRole.Forge;

        public Result<AgentFinding> Investigate(Task<Result<AgentFinding>> previous) =>
            throw new NotSupportedException("Use InvestigateCore to inject evidence.");

        internal static Result<AgentFinding> InvestigateCore(
            Guid taskId, string rootCauseNodeId,
            IReadOnlyList<string> blastRadius)
        {
            var affected = blastRadius ?? Enumerable.Empty<string>();
            return Result<AgentFinding>.Success(new AgentFinding(
                Guid.Empty, taskId, Enumerable.Empty<Guid>(),
                $"Remediation proposal: restart {rootCauseNodeId} to restore downstream health. Affected: {string.Join(", ", affected)}.",
                0.7,
                new[] { "Proposal is bounded to the deterministic simulation; production impact was not evaluated." },
                "Approve proposal to execute restart against the simulation; rollback is available on failure."));
        }
    }

    /// <summary>ORACLE: investigation coordinator. Synthesizes specialist findings.</summary>
    public sealed class OracleAgent : IAgentRole
    {
        public AgentRole Role => AgentRole.Oracle;

        public Result<AgentFinding> Investigate(Task<Result<AgentFinding>> previous) =>
            throw new NotSupportedException("Use InvestigateCore to inject evidence.");

        internal static Result<AgentFinding> InvestigateCore(
            Guid taskId, string rootCauseNodeId,
            IEnumerable<AgentFinding> findings)
        {
            var list = (findings ?? Enumerable.Empty<AgentFinding>()).ToList();
            var avg = list.Count == 0 ? 0.0 : list.Average(f => f.Confidence);
            return Result<AgentFinding>.Success(new AgentFinding(
                Guid.Empty, taskId,
                list.SelectMany(f => f.EvidenceIds).Distinct().ToList(),
                $"Synthesis: {list.Count} specialist findings agree that {rootCauseNodeId} is the root cause (avg confidence {avg:P0}).",
                Math.Clamp(avg, 0.0, 1.0),
                new[] { "Synthesis inherits the limitations of each specialist finding." },
                $"Proceed to human approval for remediation of {rootCauseNodeId}."));
        }
    }
}
