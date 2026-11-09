namespace Faultline.Tests.PlayMode
{
    using System.Collections;
    using UnityEngine.TestTools;
    using Faultline.Domain;
    using Faultline.XR;
    using NUnit.Framework;

    /// <summary>
    /// PlayMode tests for the XR session lifecycle:
    /// session init, hand tracking availability.
    ///
    /// Domain logic is pure C#; the tests themselves run as PlayMode
    /// coroutines under Unity's test framework.
    /// </summary>
    [TestFixture]
    public sealed class XrSessionTests
    {
        [UnityTest]
        public IEnumerator Initialize_WhenNotInitialized_SucceedsAndMarksAvailable()
        {
            // Runs one PlayMode frame so the assertions observe a live scene loop.
            yield return null;
            var session = new XrSession();
            var result = session.Initialize();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(session.IsAvailable, Is.False);
        }

        [UnityTest]
        public IEnumerator Initialize_Idempotent_SucceedsOnRepeatedCalls()
        {
            // Runs one PlayMode frame so the assertions observe a live scene loop.
            yield return null;
            var session = new XrSession();
            var first = session.Initialize();
            var second = session.Initialize();

            Assert.That(first.IsSuccess, Is.True);
            Assert.That(second.IsSuccess, Is.True);
        }

        [UnityTest]
        public IEnumerator StartSession_AfterInitialize_MarksSessionAvailable()
        {
            // Runs one PlayMode frame so the assertions observe a live scene loop.
            yield return null;
            var session = new XrSession();
            session.Initialize();
            var result = session.StartSession();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(session.IsAvailable, Is.True);
        }

        [UnityTest]
        public IEnumerator StartSession_BeforeInitialize_Rejected()
        {
            // Runs one PlayMode frame so the assertions observe a live scene loop.
            yield return null;
            var session = new XrSession();
            var result = session.StartSession();

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(session.IsAvailable, Is.False);
        }

        [UnityTest]
        public IEnumerator EndSession_AfterStart_ClearsAvailability()
        {
            // Runs one PlayMode frame so the assertions observe a live scene loop.
            yield return null;
            var session = new XrSession();
            session.Initialize();
            session.StartSession();
            var result = session.EndSession();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(session.IsAvailable, Is.False);
        }

        [UnityTest]
        public IEnumerator GetHandTrackingState_AfterStart_ReturnsHandTrackingState()
        {
            // Runs one PlayMode frame so the assertions observe a live scene loop.
            yield return null;
            var session = new XrSession();
            session.Initialize();
            session.StartSession();

            var state = session.GetHandTrackingState();

            Assert.That(state.IsSuccess, Is.True);
            Assert.That(state.Value, Is.Not.Null);
            Assert.That(state.Value.Left.Status, Is.EqualTo(HandTrackingStatus.Available));
            Assert.That(state.Value.Right.Status, Is.EqualTo(HandTrackingStatus.Available));
        }

        [UnityTest]
        public IEnumerator GetHandTrackingState_WithoutSession_ReturnsFailure()
        {
            // Runs one PlayMode frame so the assertions observe a live scene loop.
            yield return null;
            var session = new XrSession();
            var state = session.GetHandTrackingState();

            Assert.That(state.IsSuccess, Is.False);
        }

        [UnityTest]
        public IEnumerator IsHandTrackingAvailable_ReflectsSupportedFlag()
        {
            // Runs one PlayMode frame so the assertions observe a live scene loop.
            yield return null;
            var session = new XrSession();
            session.Initialize();

            Assert.That(session.IsHandTrackingAvailable, Is.True);
        }
    }
}
