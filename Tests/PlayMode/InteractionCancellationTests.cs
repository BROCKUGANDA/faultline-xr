namespace Faultline.Tests.PlayMode
{
    using System;
    using System.Collections.Generic;

    using Faultline.XR;
    using NUnit.Framework;

    /// <summary>
    /// PlayMode tests for interaction cancellation:
    /// cancelling in-flight interactions when tracking is lost.
    ///
    /// Pure C# — no UnityEngine, no UnityEditor.
    /// </summary>
    [TestFixture]
    public sealed class InteractionCancellationTests
    {
        [Test]
        public void OnTrackingLost_CancelsPendingInteractions()
        {
            var handler = new TrackingLossHandler();
            bool cancelled = false;
            handler.RegisterPendingInteraction(() => cancelled = true);

            handler.OnTrackingLost(new HashSet<HandSide> { HandSide.Left });

            Assert.That(handler.IsTrackingLost, Is.True);
            Assert.That(handler.LostHands.Contains(HandSide.Left), Is.True);
            Assert.That(cancelled, Is.True);
        }

        [Test]
        public void TrackingLossBegan_RaisedOnLoss()
        {
            var handler = new TrackingLossHandler();
            bool raised = false;
            handler.TrackingLossBegan += (_, _) => raised = true;

            handler.OnTrackingLost(new HashSet<HandSide> { HandSide.Right });

            Assert.That(raised, Is.True);
        }

        [Test]
        public void CompletePendingInteraction_NotCancelledOnTrackingLost()
        {
            var handler = new TrackingLossHandler();
            bool cancelled = false;
            bool completed = false;
            Action cancelAction = () => cancelled = true;
            Action completedAction = () => completed = true;

            handler.RegisterPendingInteraction(cancelAction);
            handler.CompletePendingInteraction(completedAction);

            handler.OnTrackingLost(new HashSet<HandSide> { HandSide.Left });

            Assert.That(cancelled, Is.False);
            Assert.That(completed, Is.False); // not executed, just unregistered
        }

        [Test]
        public void IsTrackingLost_FalseUntilLossOccurs()
        {
            var handler = new TrackingLossHandler();

            Assert.That(handler.IsTrackingLost, Is.False);

            handler.OnTrackingLost(new HashSet<HandSide> { HandSide.Left });

            Assert.That(handler.IsTrackingLost, Is.True);
        }

        [Test]
        public void Reset_ClearsTrackingLossState()
        {
            var handler = new TrackingLossHandler();
            handler.OnTrackingLost(new HashSet<HandSide> { HandSide.Left, HandSide.Right });

            handler.Reset();

            Assert.That(handler.IsTrackingLost, Is.False);
            Assert.That(handler.LostHands.Count, Is.EqualTo(0));
        }
    }
}
