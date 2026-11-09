namespace Faultline.Domain
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    /// <summary>
    /// Operational status of a simulated service node.
    /// </summary>
    public enum OperationalStatus
    {
        Healthy,
        Degraded,
        Failing,
        Down,
        Recovering
    }

    /// <summary>
    /// Immutable domain entity: a deployable service in the simulated
    /// infrastructure graph. Owns health, status, latency, error rate,
    /// throughput, dependency relationships, recent events and active alerts.
    /// Identity is a stable human-readable <see cref="NodeId"/> (e.g.
    /// "database", "api-gateway") shared with <see cref="DependencyEdge"/>
    /// endpoints, so scenario, graph, and telemetry agree on keys.
    /// </summary>
    public sealed class ServiceNode : IEquatable<ServiceNode>
    {
        public string NodeId { get; }
        public string DisplayName { get; }
        public string ServiceType { get; }
        public OperationalStatus Status { get; }
        public double HealthScore { get; }      // 0..1
        public TimeSpan Latency { get; }
        public double ErrorRate { get; }        // 0..1
        public double Throughput { get; }       // requests/sec
        public IReadOnlyList<string> Dependencies { get; }   // node IDs this node depends upon
        public IReadOnlyList<string> RecentEvents { get; }
        public IReadOnlyList<string> ActiveAlerts { get; }

        public ServiceNode(string nodeId, string displayName, string serviceType,
            OperationalStatus status = OperationalStatus.Healthy,
            double healthScore = 1.0,
            TimeSpan latency = default,
            double errorRate = 0.0,
            double throughput = 0.0,
            IEnumerable<string> dependencies = null,
            IEnumerable<string> recentEvents = null,
            IEnumerable<string> activeAlerts = null)
        {
            NodeId = !string.IsNullOrWhiteSpace(nodeId)
                ? nodeId
                : throw new ArgumentException("Node id is required.", nameof(nodeId));
            DisplayName = !string.IsNullOrWhiteSpace(displayName)
                ? displayName
                : throw new ArgumentException("Display name is required.", nameof(displayName));
            ServiceType = !string.IsNullOrWhiteSpace(serviceType)
                ? serviceType
                : throw new ArgumentException("Service type is required.", nameof(serviceType));
            Status = status;
            HealthScore = Math.Clamp(healthScore, 0.0, 1.0);
            Latency = latency;
            ErrorRate = Math.Clamp(errorRate, 0.0, 1.0);
            Throughput = throughput < 0.0 ? 0.0 : throughput;
            Dependencies = new ReadOnlyCollection<string>(
                (dependencies ?? Enumerable.Empty<string>()).Distinct().ToList());
            RecentEvents = new ReadOnlyCollection<string>(
                (recentEvents ?? Enumerable.Empty<string>()).ToList());
            ActiveAlerts = new ReadOnlyCollection<string>(
                (activeAlerts ?? Enumerable.Empty<string>()).ToList());
        }

        public ServiceNode WithStatus(OperationalStatus status) =>
            new ServiceNode(NodeId, DisplayName, ServiceType, status, HealthScore, Latency,
                ErrorRate, Throughput, Dependencies, RecentEvents, ActiveAlerts);

        public ServiceNode WithHealth(double healthScore) =>
            new ServiceNode(NodeId, DisplayName, ServiceType, Status, healthScore, Latency,
                ErrorRate, Throughput, Dependencies, RecentEvents, ActiveAlerts);

        public ServiceNode WithLatency(TimeSpan latency) =>
            new ServiceNode(NodeId, DisplayName, ServiceType, Status, HealthScore, latency,
                ErrorRate, Throughput, Dependencies, RecentEvents, ActiveAlerts);

        public ServiceNode WithErrorRate(double errorRate) =>
            new ServiceNode(NodeId, DisplayName, ServiceType, Status, HealthScore, Latency,
                errorRate, Throughput, Dependencies, RecentEvents, ActiveAlerts);

        public ServiceNode WithThroughput(double throughput) =>
            new ServiceNode(NodeId, DisplayName, ServiceType, Status, HealthScore, Latency,
                ErrorRate, throughput, Dependencies, RecentEvents, ActiveAlerts);

        public ServiceNode WithDependencies(IEnumerable<string> dependencies) =>
            new ServiceNode(NodeId, DisplayName, ServiceType, Status, HealthScore, Latency,
                ErrorRate, Throughput, dependencies, RecentEvents, ActiveAlerts);

        public ServiceNode WithRecentEvents(IEnumerable<string> recentEvents) =>
            new ServiceNode(NodeId, DisplayName, ServiceType, Status, HealthScore, Latency,
                ErrorRate, Throughput, Dependencies, recentEvents, ActiveAlerts);

        public ServiceNode WithActiveAlerts(IEnumerable<string> activeAlerts) =>
            new ServiceNode(NodeId, DisplayName, ServiceType, Status, HealthScore, Latency,
                ErrorRate, Throughput, Dependencies, RecentEvents, activeAlerts);

        public bool Equals(ServiceNode other) =>
            other is not null && NodeId.Equals(other.NodeId);

        public override bool Equals(object obj) => Equals(obj as ServiceNode);

        public override int GetHashCode() => NodeId.GetHashCode();

        public static bool operator ==(ServiceNode left, ServiceNode right) =>
            EqualityComparer<ServiceNode>.Default.Equals(left, right);

        public static bool operator !=(ServiceNode left, ServiceNode right) =>
            !(left == right);

        public override string ToString() =>
            $"ServiceNode({DisplayName}, {Status}, health={HealthScore:P0}, latency={Latency.TotalMilliseconds:F0}ms)";
    }
}
