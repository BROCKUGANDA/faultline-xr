namespace Faultline.Tests.PlayMode
{
    using Faultline.Interaction;
    using NUnit.Framework;

    /// <summary>
    /// PlayMode tests for controller-free interaction:
    /// every critical action reachable without controllers.
    ///
    /// Pure C# — no UnityEngine, no UnityEditor.
    /// </summary>
    [TestFixture]
    public sealed class ControllerFreeTests
    {
        [Test]
        public void ValidateCoverage_AllCriticalActionsRegistered_Succeeds()
        {
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

        [Test]
        public void ValidateCoverage_NoRegistrations_SucceedsWithEmptyCoverage()
        {
            var validator = new ControllerFreeValidator();

            var result = validator.ValidateCoverage();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.True);
        }

        [Test]
        public void TryGetTarget_ReturnsRegisteredTarget()
        {
            var validator = new ControllerFreeValidator();
            validator.RegisterCriticalAction("ClosePanel", "target-close");

            var found = validator.TryGetTarget("ClosePanel", out var targetId);

            Assert.That(found, Is.True);
            Assert.That(targetId, Is.EqualTo("target-close"));
        }

        [Test]
        public void TryGetTarget_UnregisteredAction_ReturnsFalse()
        {
            var validator = new ControllerFreeValidator();

            var found = validator.TryGetTarget("DoesNotExist", out var targetId);

            Assert.That(found, Is.False);
            Assert.That(targetId, Is.Null);
        }

        [Test]
        public void RegisteredCriticalActions_ContainsRegisteredIds()
        {
            var validator = new ControllerFreeValidator();
            validator.RegisterCriticalAction("DispatchAgent", "target-dispatch");

            Assert.That(validator.RegisteredCriticalActions.Contains("DispatchAgent"), Is.True);
        }

        [Test]
        public void Reset_ClearsRegistrations()
        {
            var validator = new ControllerFreeValidator();
            validator.RegisterCriticalAction("SelectNode", "target-node-1");
            validator.Reset();

            Assert.That(validator.RegisteredCriticalActions.Count, Is.EqualTo(0));
        }
    }
}
