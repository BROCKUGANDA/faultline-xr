namespace Faultline.Tests.PlayMode
{
    using System.Collections;
    using UnityEngine.TestTools;
    using Faultline.Domain;
    using Faultline.Interaction;
    using Faultline.XR;
    using NUnit.Framework;

    /// <summary>
    /// PlayMode tests for hand interaction:
    /// pinch selection, grab/release, tracking loss/recovery.
    ///
    /// Domain logic is pure C#; the tests themselves run as PlayMode
    /// coroutines under Unity's test framework.
    /// </summary>
    [TestFixture]
    public sealed class HandInteractionTests
    {
        [UnityTest]
        public IEnumerator PinchSelection_WhenPinchActive_EmitsSelectNodeCommand()
        {
            // Runs one PlayMode frame so the assertions observe a live scene loop.
            yield return null;
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

        [UnityTest]
        public IEnumerator Grab_WhenSustainedPinch_EmitsGrabNodeCommand()
        {
            // Runs one PlayMode frame so the assertions observe a live scene loop.
            yield return null;
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

        [UnityTest]
        public IEnumerator Release_WhenPinchEnds_EmitsReleaseNodeCommand()
        {
            // Runs one PlayMode frame so the assertions observe a live scene loop.
            yield return null;
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

        [UnityTest]
        public IEnumerator TrackingLoss_LeftHandLost_TracksLostEvent()
        {
            // Runs one PlayMode frame so the assertions observe a live scene loop.
            yield return null;
            var session = new XrSession();
            session.Initialize();
            session.StartSession();

            session.SimulateTrackingLost(HandSide.Left, false);

            Assert.That(session.IsHandTrackingAvailable, Is.True);
            var state = session.GetHandTrackingState();
            Assert.That(state.IsSuccess, Is.True);
            Assert.That(state.Value.AnyLost, Is.True);
        }

        [UnityTest]
        public IEnumerator TrackingRecovery_AfterLoss_ReturnsToTracking()
        {
            // Runs one PlayMode frame so the assertions observe a live scene loop.
            yield return null;
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
