namespace Faultline.Tests.PlayMode
{
    using System.Collections;
    using UnityEngine.TestTools;
    using Faultline.Interaction;
    using NUnit.Framework;

    /// <summary>
    /// PlayMode tests for controller-free interaction:
    /// every critical action reachable without controllers.
    ///
    /// Domain logic is pure C#; the tests themselves run as PlayMode
    /// coroutines under Unity's test framework.
    /// </summary>
    [TestFixture]
    public sealed class ControllerFreeTests
    {
        [UnityTest]
        public IEnumerator ValidateCoverage_AllCriticalActionsRegistered_Succeeds()
        {
            // Runs one PlayMode frame so the assertions observe a live scene loop.
            yield return null;
            var validator = new ControllerFreeValidator();
            validator.RegisterCriticalAction("SelectNode", "target-node-1");
            validator.RegisterCriticalAction("GrabNode", "target-node-2");
            validator.RegisterCriticalAction("ApproveRemediation", "target-approve");
            validator.RegisterCriticalAction("RejectRemediation", "target-reject");
            validator.RegisterCriticalAction("DispatchAgent", "target-dispatch");
            validator.RegisterCriticalAction("InspectEvidence", "target-evidence");
            validator.RegisterCriticalAction("ClosePanel", "target-close");

            var result = validator.ValidateCoverage();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.True);
        }

        [UnityTest]
        public IEnumerator ValidateCoverage_NoRegistrations_SucceedsWithEmptyCoverage()
        {
            // Runs one PlayMode frame so the assertions observe a live scene loop.
            yield return null;
            var validator = new ControllerFreeValidator();

            var result = validator.ValidateCoverage();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.True);
        }

        [UnityTest]
        public IEnumerator TryGetTarget_ReturnsRegisteredTarget()
        {
            // Runs one PlayMode frame so the assertions observe a live scene loop.
            yield return null;
            var validator = new ControllerFreeValidator();
            validator.RegisterCriticalAction("ClosePanel", "target-close");

            var found = validator.TryGetTarget("ClosePanel", out var targetId);

            Assert.That(found, Is.True);
            Assert.That(targetId, Is.EqualTo("target-close"));
        }

        [UnityTest]
        public IEnumerator TryGetTarget_UnregisteredAction_ReturnsFalse()
        {
            // Runs one PlayMode frame so the assertions observe a live scene loop.
            yield return null;
            var validator = new ControllerFreeValidator();

            var found = validator.TryGetTarget("DoesNotExist", out var targetId);

            Assert.That(found, Is.False);
            Assert.That(targetId, Is.Null);
        }

        [UnityTest]
        public IEnumerator RegisteredCriticalActions_ContainsRegisteredIds()
        {
            // Runs one PlayMode frame so the assertions observe a live scene loop.
            yield return null;
            var validator = new ControllerFreeValidator();
            validator.RegisterCriticalAction("DispatchAgent", "target-dispatch");

            Assert.That(validator.RegisteredCriticalActions.Contains("DispatchAgent"), Is.True);
        }

        [UnityTest]
        public IEnumerator Reset_ClearsRegistrations()
        {
            // Runs one PlayMode frame so the assertions observe a live scene loop.
            yield return null;
            var validator = new ControllerFreeValidator();
            validator.RegisterCriticalAction("SelectNode", "target-node-1");
            validator.Reset();

            Assert.That(validator.RegisteredCriticalActions.Count, Is.EqualTo(0));
        }
    }
}
