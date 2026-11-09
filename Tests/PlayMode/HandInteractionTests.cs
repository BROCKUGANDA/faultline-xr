namespace Faultline.Tests.PlayMode
{
    using Faultline.Domain;
    using Faultline.Interaction;
    using Faultline.XR;
    using NUnit.Framework;

    /// <summary>
    /// PlayMode tests for hand interaction:
    /// pinch selection, grab/release, tracking loss/recovery.
    ///
    /// Pure C# — no UnityEngine, no UnityEditor.
    /// </summary>
    [TestFixture]
    public sealed class HandInteractionTests
    {
        [Test]
        public void PinchSelection_WhenPinchActive_EmitsSelectNodeCommand()
        {
            var interpreter = new GestureInterpreter();
            var left = new HandData(
                HandSide.Left, HandTrackingStatus.Tracking,
                System.Numerics.Vector3.Zero, System.Numerics.Quaternion.Identity,
                0.9f, true, false, 0);
            var right = new HandData(
                HandSide.Right, HandTrackingStatus.Tracking,
                System.Numerics.Vector3.Zero, System.Numerics.Quaternion.Identity,
                0.1f, false, false, 0);
            var state = new HandTrackingState(left, right, 0);

            var command = interpreter.Interpret(state);

            Assert.That(command, Is.Not.Null);
            Assert.That(command.Type, Is.EqualTo(InteractionCommandType.SelectNode));
        }

        [Test]
        public void Grab_WhenSustainedPinch_EmitsGrabNodeCommand()
        {
            var interpreter = new GestureInterpreter();
            var left = new HandData(
                HandSide.Left, HandTrackingStatus.Tracking,
                System.Numerics.Vector3.Zero, System.Numerics.Quaternion.Identity,
                0.95f, true, true, 0);
            var right = new HandData(
                HandSide.Right, HandTrackingStatus.Tracking,
                System.Numerics.Vector3.Zero, System.Numerics.Quaternion.Identity,
                0.1f, false, false, 0);
            var state = new HandTrackingState(left, right, 0);

            var command = interpreter.Interpret(state);

            Assert.That(command, Is.Not.Null);
            Assert.That(command.Type, Is.EqualTo(InteractionCommandType.GrabNode));
        }

        [Test]
        public void Release_WhenPinchEnds_EmitsReleaseNodeCommand()
        {
            var interpreter = new GestureInterpreter();
            var left = new HandData(
                HandSide.Left, HandTrackingStatus.Tracking,
                System.Numerics.Vector3.Zero, System.Numerics.Quaternion.Identity,
                0.2f, false, false, 0);
            var right = new HandData(
                HandSide.Right, HandTrackingStatus.Tracking,
                System.Numerics.Vector3.Zero, System.Numerics.Quaternion.Identity,
                0.1f, false, false, 0);
            var state = new HandTrackingState(left, right, 0);

            var command = interpreter.Interpret(state);

            Assert.That(command, Is.Not.Null);
            Assert.That(command.Type, Is.EqualTo(InteractionCommandType.ReleaseNode));
        }

        [Test]
        public void TrackingLoss_LeftHandLost_TracksLostEvent()
        {
            var session = new XrSession();
            session.Initialize();
            session.StartSession();

            session.SimulateTrackingLost(HandSide.Left, false);

            Assert.That(session.IsHandTrackingAvailable, Is.True);
            var state = session.GetHandTrackingState();
            Assert.That(state.IsSuccess, Is.True);
            Assert.That(state.Value.AnyLost, Is.True);
        }

        [Test]
        public void TrackingRecovery_AfterLoss_ReturnsToTracking()
        {
            var session = new XrSession();
            session.Initialize();
            session.StartSession();

            session.SimulateTrackingLost(HandSide.Left, false);
            session.SimulateTrackingRecovered(HandSide.Left, false);

            var state = session.GetHandTrackingState();
            Assert.That(state.IsSuccess, Is.True);
            Assert.That(state.Value.Left.Status,
                Is.EqualTo(HandTrackingStatus.Tracking)
                    .Or.EqualTo(HandTrackingStatus.Available));
        }
    }
}
