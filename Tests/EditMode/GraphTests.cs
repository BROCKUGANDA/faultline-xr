namespace Faultline.Tests.EditMode
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Faultline.Domain;
    using Faultline.Infrastructure;
    using NUnit.Framework;

    /// <summary>
    /// EditMode tests for the infrastructure graph: traversal, cycle
    /// detection, duplicate-edge prevention, missing-node detection,
    /// disconnected-node handling, impact analysis.
    ///
    /// Pure C# — no UnityEngine.
    /// </summary>
    [TestFixture]
    public sealed class GraphTests
    {
        [Test]
        public void CycleDetection_ReturnsTrueForCycle()
        {
            var graph = new InfrastructureGraph();
            graph.AddNode(new ServiceNode("A", "A", "Service", OperationalStatus.Healthy));
            graph.AddNode(new ServiceNode("B", "B", "Service", OperationalStatus.Healthy));
            graph.AddNode(new ServiceNode("C", "C", "Service", OperationalStatus.Healthy));

            graph.AddEdge(new DependencyEdge(Guid.NewGuid(), "A", "B",
                Infrastructure.DependencyDirection.Downstream,
                Infrastructure.DependencyType.ApiCall, TimeSpan.FromMilliseconds(10)));
            graph.AddEdge(new DependencyEdge(Guid.NewGuid(), "B", "C",
                Infrastructure.DependencyDirection.Downstream,
                Infrastructure.DependencyType.ApiCall, TimeSpan.FromMilliseconds(10)));
            graph.AddEdge(new DependencyEdge(Guid.NewGuid(), "C", "A",
                Infrastructure.DependencyDirection.Downstream,
                Infrastructure.DependencyType.ApiCall, TimeSpan.FromMilliseconds(10)));

            var (hasCycle, path) = graph.DetectCycle();
            Assert.That(hasCycle, Is.True);
            Assert.That(path, Is.Not.Null);
            Assert.That(path.Count, Is.GreaterThan(0));
        }

        [Test]
        public void NoCycle_AcyclicGraph_ReturnsFalse()
        {
            var graph = new InfrastructureGraph();
            graph.AddNode(new ServiceNode("A", "A", "Service", OperationalStatus.Healthy));
            graph.AddNode(new ServiceNode("B", "B", "Service", OperationalStatus.Healthy));
            graph.AddEdge(new DependencyEdge(Guid.NewGuid(), "A", "B",
                Infrastructure.DependencyDirection.Downstream,
                Infrastructure.DependencyType.ApiCall, TimeSpan.FromMilliseconds(10)));

            var (hasCycle, _) = graph.DetectCycle();
            Assert.That(hasCycle, Is.False);
        }

        [Test]
        public void DuplicateEdge_Prevented()
        {
            var graph = new InfrastructureGraph();
            graph.AddNode(new ServiceNode("A", "A", "Service", OperationalStatus.Healthy));
            graph.AddNode(new ServiceNode("B", "B", "Service", OperationalStatus.Healthy));

            var edge1 = new DependencyEdge(Guid.NewGuid(), "A", "B",
                Infrastructure.DependencyDirection.Downstream,
                Infrastructure.DependencyType.ApiCall, TimeSpan.FromMilliseconds(10));
            var edge2 = new DependencyEdge(Guid.NewGuid(), "A", "B",
                Infrastructure.DependencyDirection.Downstream,
                Infrastructure.DependencyType.ApiCall, TimeSpan.FromMilliseconds(10));

            var r1 = graph.AddEdge(edge1);
            var r2 = graph.AddEdge(edge2);

            Assert.That(r1.IsSuccess, Is.True);
            Assert.That(r2.IsSuccess, Is.False);
            Assert.That(r2.ErrorCode, Is.EqualTo("FL-GR-0042"));
        }

        [Test]
        public void MissingNode_Rejected()
        {
            var graph = new InfrastructureGraph();
            graph.AddNode(new ServiceNode("A", "A", "Service", OperationalStatus.Healthy));

            var edge = new DependencyEdge(Guid.NewGuid(), "A", "unknown",
                Infrastructure.DependencyDirection.Downstream,
                Infrastructure.DependencyType.ApiCall, TimeSpan.FromMilliseconds(10));

            var result = graph.AddEdge(edge);
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("FL-GR-0041"));
        }

        [Test]
        public void DownstreamTraversal_CycleSafe_NoInfiniteRecursion()
        {
            var graph = new InfrastructureGraph();
            graph.AddNode(new ServiceNode("A", "A", "Service", OperationalStatus.Healthy));
            graph.AddNode(new ServiceNode("B", "B", "Service", OperationalStatus.Healthy));
            graph.AddNode(new ServiceNode("C", "C", "Service", OperationalStatus.Healthy));
            graph.AddEdge(new DependencyEdge(Guid.NewGuid(), "A", "B",
                Infrastructure.DependencyDirection.Downstream,
                Infrastructure.DependencyType.ApiCall, TimeSpan.FromMilliseconds(10)));
            graph.AddEdge(new DependencyEdge(Guid.NewGuid(), "B", "C",
                Infrastructure.DependencyDirection.Downstream,
                Infrastructure.DependencyType.ApiCall, TimeSpan.FromMilliseconds(10)));
            graph.AddEdge(new DependencyEdge(Guid.NewGuid(), "C", "A",
                Infrastructure.DependencyDirection.Downstream,
                Infrastructure.DependencyType.ApiCall, TimeSpan.FromMilliseconds(10)));

            var visited = graph.TraverseDownstream("A");
            // Visited set prevents infinite recursion; each node appears once.
            Assert.That(visited.Count, Is.EqualTo(3));
            Assert.That(visited.Distinct().Count(), Is.EqualTo(visited.Count));
        }

        [Test]
        public void DisconnectedNode_EmptyTraversal()
        {
            var graph = new InfrastructureGraph();
            graph.AddNode(new ServiceNode("A", "A", "Service", OperationalStatus.Healthy));
            graph.AddNode(new ServiceNode("B", "B", "Service", OperationalStatus.Healthy));

            var visited = graph.TraverseDownstream("A");
            Assert.That(visited.Count, Is.EqualTo(1));
            Assert.That(visited.Contains("A"), Is.True);
        }

        [Test]
        public void AnalyzeImpact_ReturnsBlastRadius()
        {
            var graph = new InfrastructureGraph();
            graph.AddNode(new ServiceNode("A", "A", "Service", OperationalStatus.Healthy));
            graph.AddNode(new ServiceNode("B", "B", "Service", OperationalStatus.Healthy));
            graph.AddEdge(new DependencyEdge(Guid.NewGuid(), "A", "B",
                Infrastructure.DependencyDirection.Downstream,
                Infrastructure.DependencyType.ApiCall, TimeSpan.FromMilliseconds(10)));

            var result = graph.AnalyzeImpact("A");
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.BlastRadius.Contains("A"), Is.True);
            Assert.That(result.Value.BlastRadius.Contains("B"), Is.True);
        }

        [Test]
        public void AnalyzeImpact_UnknownNode_Failure()
        {
            var graph = new InfrastructureGraph();
            var result = graph.AnalyzeImpact("nonexistent");
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("FL-GR-0043"));
        }
    }
}
