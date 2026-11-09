namespace Faultline.Tests.PlayMode
{
    using System.Collections;
    using UnityEngine.TestTools;
    using System;
    using System.Collections.Generic;

    using Faultline.XR;
    using NUnit.Framework;

    /// <summary>
    /// PlayMode tests for interaction cancellation:
    /// cancelling in-flight interactions when tracking is lost.
    ///
    /// Domain logic is pure C#; the tests themselves run as PlayMode
    /// coroutines under Unity's test framework.
    /// </summary>
    [TestFixture]
    public sealed class InteractionCancellationTests
    {
        [UnityTest]
        public IEnumerator OnTrackingLost_CancelsPendingInteractions()
        {
            // Runs one PlayMode frame so the assertions observe a live scene loop.
            yield return null;
            var handler = new TrackingLossHandler();
            bool cancelled = false;
            handler.RegisterPendingInteraction(() => cancelled = true);

            handler.OnTrackingLost(new HashSet<HandSide> { HandSide.Left });

            Assert.That(handler.IsTrackingLost, Is.True);
            Assert.That(handler.LostHands.Contains(HandSide.Left), Is.True);
            Assert.That(cancelled, Is.True);
        }

        [UnityTest]
        public IEnumerator TrackingLossBegan_RaisedOnLoss()
        {
            // Runs one PlayMode frame so the assertions observe a live scene loop.
            yield return null;
            var handler = new TrackingLossHandler();
            bool raised = false;
            handler.TrackingLossBegan += (_, _) => raised = true;

            handler.OnTrackingLost(new HashSet<HandSide> { HandSide.Right });

            Assert.That(raised, Is.True);
        }

        [UnityTest]
        public IEnumerator CompletePendingInteraction_NotCancelledOnTrackingLost()
        {
            // Runs one PlayMode frame so the assertions observe a live scene loop.
            yield return null;
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

        [UnityTest]
        public IEnumerator IsTrackingLost_FalseUntilLossOccurs()
        {
            // Runs one PlayMode frame so the assertions observe a live scene loop.
            yield return null;
            var handler = new TrackingLossHandler();

            Assert.That(handler.IsTrackingLost, Is.False);

            handler.OnTrackingLost(new HashSet<HandSide> { HandSide.Left });

            Assert.That(handler.IsTrackingLost, Is.True);
        }

        [UnityTest]
        public IEnumerator Reset_ClearsTrackingLossState()
        {
            // Runs one PlayMode frame so the assertions observe a live scene loop.
            yield return null;
            var handler = new TrackingLossHandler();
            handler.OnTrackingLost(new HashSet<HandSide> { HandSide.Left, HandSide.Right });

            handler.Reset();

            Assert.That(handler.IsTrackingLost, Is.False);
            Assert.That(handler.LostHands.Count, Is.EqualTo(0));
        }
    }
}
