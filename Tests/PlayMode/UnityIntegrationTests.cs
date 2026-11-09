namespace Faultline.Tests.PlayMode
{
    using System.Collections;
    using System.Threading.Tasks;

    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;

    using Faultline.App;
    using Faultline.Domain;
    using Faultline.Presentation;
    using Faultline.Runtime;
    using Faultline.Runtime.UI;

    /// <summary>
    /// PlayMode coverage for the Unity layer itself: the composition root, the
    /// screen registry, the spatial topology projection, navigation validity and
    /// the end-to-end CASCADE journey asserted against domain state rather than
    /// rendered text.
    /// </summary>
    [TestFixture]
    public sealed class UnityIntegrationTests
    {
        private GameObject _root;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("FaultlineTestRoot");
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.Destroy(_root);
        }

        [UnityTest]
        public IEnumerator ApplicationRoot_Boots_AndRegistersAllTwentyThreeScreens()
        {
            var app = _root.AddComponent<FaultlineUnityApp>();

            yield return WaitFor(() => app.IsBooted, 10f, "application root never finished booting");

            Assert.That(app.BootError, Is.Null.Or.Empty,
                $"boot reported an error: {app.BootError}");
            Assert.That(app.RegisteredScreenCount, Is.EqualTo(ScreenIds.All.Length),
                "screen registry must install every PRD §6.1 screen");
            Assert.That(app.Navigator.CurrentState, Is.EqualTo(ApplicationState.Ready));
            Assert.That(app.Navigator.CurrentScreen.ScreenId, Is.EqualTo(ScreenIds.Welcome));
        }

        [UnityTest]
        public IEnumerator ApplicationRoot_RegistersEveryCriticalAction_ControllerFree()
        {
            var app = _root.AddComponent<FaultlineUnityApp>();
            yield return WaitFor(() => app.IsBooted, 10f, "application root never finished booting");

            var coverage = app.Validator.ValidateCoverage();

            Assert.That(coverage.IsSuccess, Is.True,
                "every consequential action must have a hand-reachable target");
            CollectionAssert.Contains(app.Validator.RegisteredCriticalActions, "approve");
            CollectionAssert.Contains(app.Validator.RegisteredCriticalActions, "execute");
            CollectionAssert.Contains(app.Validator.RegisteredCriticalActions, "resolve");
        }

        [UnityTest]
        public IEnumerator TopologyView_ProjectsTheSevenServiceCascadeGraph()
        {
            var app = _root.AddComponent<FaultlineUnityApp>();
            yield return WaitFor(() => app.IsBooted, 10f, "boot did not complete");

            var start = app.BeginScenarioAndOpenIncident();
            Assert.That(start.IsSuccess, Is.True, start.ErrorMessage);

            yield return null;

            Assert.That(app.Topology.NodeCount, Is.EqualTo(7), "CASCADE defines seven services");
            Assert.That(app.Topology.EdgeCount, Is.EqualTo(6), "CASCADE defines six dependencies");
        }

        [UnityTest]
        public IEnumerator CascadeJourney_ReachesResolved_AndSimulatedHealthActuallyImproves()
        {
            var app = _root.AddComponent<FaultlineUnityApp>();
            yield return WaitFor(() => app.IsBooted, 10f, "boot did not complete");

            Assert.That(app.BeginScenarioAndOpenIncident().IsSuccess, Is.True);
            var controller = app.Controller;
            var healthBefore = controller.Engine.AggregateHealth;
            var rootBefore = controller.Engine.Nodes[controller.RootCauseNodeId].HealthScore;

            Assert.That(controller.OpenIncident().IsSuccess, Is.True);
            yield return RunTask(app, () => app.InvestigateAsync(), "investigating");
            Assert.That(controller.Phase, Is.EqualTo(InvestigationPhase.Findings), controller.LastError);
            Assert.That(controller.Report, Is.Not.Null);
            Assert.That(controller.Report.Findings.Count, Is.GreaterThanOrEqualTo(2),
                "the journey requires at least two grounded agent findings");

            Assert.That(app.ProposeRemediation().IsSuccess, Is.True, controller.LastError);
            Assert.That(app.ApproveRemediation().IsSuccess, Is.True, controller.LastError);
            yield return RunTask(app, () => app.ExecuteRemediationAsync(), "executing");
            Assert.That(controller.Phase, Is.EqualTo(InvestigationPhase.Executed), controller.LastError);
            Assert.That(controller.Recovery, Is.Not.Null);
            Assert.That(controller.Recovery.HealthDelta, Is.GreaterThan(0d));

            Assert.That(app.VerifyRemediation().IsSuccess, Is.True, controller.LastError);
            Assert.That(app.ResolveIncident().IsSuccess, Is.True, controller.LastError);
            Assert.That(controller.Phase, Is.EqualTo(InvestigationPhase.Resolved));

            var healthAfter = controller.Engine.AggregateHealth;
            var rootAfter = controller.Engine.Nodes[controller.RootCauseNodeId].HealthScore;

            Assert.That(healthAfter, Is.GreaterThan(healthBefore),
                "recovery must change simulation truth, not just the UI");
            Assert.That(rootAfter, Is.GreaterThanOrEqualTo(0.9d),
                "the root service must return to healthy after the remediation");

            var incident = app.Controller.App.Incidents.GetIncidentEntity(controller.IncidentId);
            Assert.That(incident.IsSuccess, Is.True);
            Assert.That(incident.Value.State, Is.EqualTo(IncidentLifecycleState.Resolved));
        }

        [UnityTest]
        public IEnumerator Navigator_RejectsAnInvalidApplicationTransition()
        {
            var app = _root.AddComponent<FaultlineUnityApp>();
            yield return WaitFor(() => app.IsBooted, 10f, "boot did not complete");

            var illegal = app.Navigator.TransitionTo(ApplicationState.Booting);

            Assert.That(illegal.IsSuccess, Is.False,
                "Ready → Booting is not a permitted transition and must be refused");
            Assert.That(illegal.ErrorCode, Is.EqualTo("FL-UI-0021"));
            Assert.That(app.Navigator.CurrentState, Is.EqualTo(ApplicationState.Ready),
                "a refused transition must leave state untouched");
        }

        [UnityTest]
        public IEnumerator EveryScreen_CanBeNavigatedTo_AndReportsAContentState()
        {
            var app = _root.AddComponent<FaultlineUnityApp>();
            yield return WaitFor(() => app.IsBooted, 10f, "boot did not complete");

            foreach (var screenId in ScreenIds.All)
            {
                if (!app.Navigator.TryGetScreen(screenId, out var screen))
                {
                    Assert.Fail($"screen '{screenId}' was not registered");
                }

                if (!screen.CanEnter)
                {
                    Assert.That(screen.EntryConditions, Is.Not.Null.And.Not.Empty,
                        $"'{screenId}' blocks entry but does not explain why");
                    continue;
                }

                var enter = app.Navigator.NavigateToScreen(screenId);
                Assert.That(enter.IsSuccess, Is.True, $"{screenId} failed to enter: {enter.ErrorMessage}");

                yield return null;

                Assert.That(screen.Title, Is.Not.Null.And.Not.Empty, $"{screenId} has no title");
                Assert.That(screen.AccessibleDescription, Is.Not.Null.And.Not.Empty,
                    $"{screenId} has no accessible description");
            }
        }

        [UnityTest]
        public IEnumerator Session_FallsBackToLabelledSimulationWhenNoDeviceIsPresent_AndStillDrivesCommands()
        {
            var app = _root.AddComponent<FaultlineUnityApp>();
            yield return WaitFor(() => app.IsBooted, 10f, "boot did not complete");

            // Editor/CI runs have no hand-tracking runtime; the app must say so
            // rather than pretend a device is attached.
            Assert.That(app.ProviderKind, Is.EqualTo(XrProviderKind.Simulated).Or.EqualTo(XrProviderKind.Device));

            var state = app.Session.GetHandTrackingState();
            Assert.That(state.IsSuccess, Is.True,
                "the selected provider must be able to report hand state or fail loudly");
            Assert.That(app.Session.IsHandTrackingAvailable, Is.True);
        }

        private static IEnumerator WaitFor(System.Func<bool> condition, float timeoutSeconds, string failureMessage)
        {
            var deadline = Time.unscaledTime + timeoutSeconds;
            while (!condition())
            {
                if (Time.unscaledTime > deadline)
                    Assert.Fail($"{failureMessage} (timed out after {timeoutSeconds}s)");
                yield return null;
            }
        }

        /// <summary>
        /// Drive an app command to completion frame-by-frame. Polls completion
        /// instead of blocking so a stalled task surfaces as a test failure
        /// rather than a hung editor.
        /// </summary>
        private static IEnumerator RunTask(FaultlineUnityApp app, System.Func<Task<Result<bool>>> work,
            string label)
        {
            var task = work();
            var deadline = Time.unscaledTime + 20f;
            while (!task.IsCompleted)
            {
                if (Time.unscaledTime > deadline)
                    Assert.Fail($"{label} did not complete within 20s");
                yield return null;
            }

            if (task.IsFaulted)
                Assert.Fail($"{label} faulted: {task.Exception?.GetBaseException().Message}");

            Assert.That(task.Result.IsSuccess, Is.True, $"{label} failed: {task.Result.ErrorMessage}");
        }
    }
}