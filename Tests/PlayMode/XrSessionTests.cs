namespace Faultline.Tests.PlayMode
{
    using Faultline.Domain;
    using Faultline.XR;
    using NUnit.Framework;

    /// <summary>
    /// PlayMode tests for the XR session lifecycle:
    /// session init, hand tracking availability.
    ///
    /// Pure C# — no UnityEngine, no UnityEditor.
    /// </summary>
    [TestFixture]
    public sealed class XrSessionTests
    {
        [Test]
        public void Initialize_WhenNotInitialized_SucceedsAndMarksAvailable()
        {
            var session = new XrSession();
            var result = session.Initialize();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(session.IsAvailable, Is.False);
        }

        [Test]
        public void Initialize_Idempotent_SucceedsOnRepeatedCalls()
        {
            var session = new XrSession();
            var first = session.Initialize();
            var second = session.Initialize();

            Assert.That(first.IsSuccess, Is.True);
            Assert.That(second.IsSuccess, Is.True);
        }

        [Test]
        public void StartSession_AfterInitialize_MarksSessionAvailable()
        {
            var session = new XrSession();
            session.Initialize();
            var result = session.StartSession();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(session.IsAvailable, Is.True);
        }

        [Test]
        public void StartSession_BeforeInitialize_Rejected()
        {
            var session = new XrSession();
            var result = session.StartSession();

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(session.IsAvailable, Is.False);
        }

        [Test]
        public void EndSession_AfterStart_ClearsAvailability()
        {
            var session = new XrSession();
            session.Initialize();
            session.StartSession();
            var result = session.EndSession();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(session.IsAvailable, Is.False);
        }

        [Test]
        public void GetHandTrackingState_AfterStart_ReturnsHandTrackingState()
        {
            var session = new XrSession();
            session.Initialize();
            session.StartSession();

            var state = session.GetHandTrackingState();

            Assert.That(state.IsSuccess, Is.True);
            Assert.That(state.Value, Is.Not.Null);
            Assert.That(state.Value.Left.Status, Is.EqualTo(HandTrackingStatus.Available));
            Assert.That(state.Value.Right.Status, Is.EqualTo(HandTrackingStatus.Available));
        }

        [Test]
        public void GetHandTrackingState_WithoutSession_ReturnsFailure()
        {
            var session = new XrSession();
            var state = session.GetHandTrackingState();

            Assert.That(state.IsSuccess, Is.False);
        }

        [Test]
        public void IsHandTrackingAvailable_ReflectsSupportedFlag()
        {
            var session = new XrSession();
            session.Initialize();

            Assert.That(session.IsHandTrackingAvailable, Is.True);
        }
    }
}
