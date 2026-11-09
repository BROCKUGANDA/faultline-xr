namespace Faultline.Domain
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    /// <summary>
    /// Role of a deterministic local agent.
    /// </summary>
    public enum AgentRole
    {
        Oracle,
        Trace,
        Sentry,
        Forge
    }

    /// <summary>
    /// Status of an agent task.
    /// </summary>
    public enum AgentTaskStatus
    {
        Dispatched,
        InProgress,
        Completed,
        Failed,
        Cancelled,
        TimedOut
    }

    /// <summary>
    /// Immutable domain entity: a task assigned to a deterministic local agent,
    /// producing AgentFinding entries.
    /// </summary>
    public sealed class AgentTask : IEquatable<AgentTask>
    {
        public Guid TaskId { get; }
        public AgentRole Role { get; }
        public AgentTaskStatus Status { get; }
        public IDictionary<string, object> Context { get; }
        public object Result { get; }
        public TimeSpan Timeout { get; }
        public DateTimeOffset DispatchedAt { get; }
        public DateTimeOffset? CompletedAt { get; }
        public IReadOnlyList<Guid> FindingIds { get; }

        private readonly IClock _clock;

        public AgentTask(Guid taskId, AgentRole role, AgentTaskStatus status,
            IDictionary<string, object> context,
            TimeSpan timeout, object result = null,
            DateTimeOffset? dispatchedAt = null, DateTimeOffset? completedAt = null,
            IEnumerable<Guid> findingIds = null, IClock clock = null)
        {
            TaskId = taskId;
            Role = role;
            Status = status;
            Context = context != null
                ? new ReadOnlyDictionary<string, object>(new Dictionary<string, object>(context))
                : new ReadOnlyDictionary<string, object>(new Dictionary<string, object>());
            Result = result;
            Timeout = timeout;
            _clock = clock ?? SystemClock.Instance;
            DispatchedAt = dispatchedAt ?? _clock.Now;
            CompletedAt = completedAt;
            FindingIds = new ReadOnlyCollection<Guid>((findingIds ?? Enumerable.Empty<Guid>()).ToList());
        }

        public AgentTask(Guid taskId, AgentRole role, IDictionary<string, object> context,
            TimeSpan timeout, object result = null,
            DateTimeOffset? dispatchedAt = null, DateTimeOffset? completedAt = null,
            IEnumerable<Guid> findingIds = null, IClock clock = null)
            : this(taskId, role, AgentTaskStatus.Dispatched, context, timeout, result,
                dispatchedAt, completedAt, findingIds, clock)
        {
        }

        public static AgentTask Create(Guid taskId, AgentRole role,
            IDictionary<string, object> context, TimeSpan timeout, IClock clock = null) =>
            new AgentTask(taskId, role, context, timeout, clock: clock);

        public AgentTask WithStatus(AgentTaskStatus status) =>
            new AgentTask(TaskId, Role, status, Context, Timeout, Result, DispatchedAt,
                status == AgentTaskStatus.Completed || status == AgentTaskStatus.Failed
                    || status == AgentTaskStatus.Cancelled || status == AgentTaskStatus.TimedOut
                        ? _clock.Now
                        : CompletedAt, FindingIds, _clock);

        public AgentTask WithResult(object result) =>
            new AgentTask(TaskId, Role, Status, Context, Timeout, result, DispatchedAt, CompletedAt, FindingIds, _clock);

        public AgentTask WithFindingIds(IEnumerable<Guid> findingIds) =>
            new AgentTask(TaskId, Role, Status, Context, Timeout, Result, DispatchedAt, CompletedAt, findingIds, _clock);

        public bool Equals(AgentTask other) =>
            other is not null && TaskId.Equals(other.TaskId);

        public override bool Equals(object obj) => Equals(obj as AgentTask);

        public override int GetHashCode() => TaskId.GetHashCode();

        public static bool operator ==(AgentTask left, AgentTask right) =>
            EqualityComparer<AgentTask>.Default.Equals(left, right);

        public static bool operator !=(AgentTask left, AgentTask right) =>
            !(left == right);

        public override string ToString() =>
            $"AgentTask({Role}, {Status}, task={TaskId})";
    }
}
