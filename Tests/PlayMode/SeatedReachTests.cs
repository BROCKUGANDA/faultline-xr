namespace Faultline.Tests.PlayMode
{
    using System.Collections;
    using UnityEngine.TestTools;
    using Faultline.Presentation;
    using Faultline.Spatial;
    using NUnit.Framework;

    /// <summary>
    /// PlayMode tests for seated reach and panel placement:
    /// seated reach envelope, panel placement.
    ///
    /// Domain logic is pure C#; the tests themselves run as PlayMode
    /// coroutines under Unity's test framework.
    /// </summary>
    [TestFixture]
    public sealed class SeatedReachTests
    {
        [UnityTest]
        public IEnumerator CoordinateSystem_DefinesSeatedReachEnvelope()
        {
            // Runs one PlayMode frame so the assertions observe a live scene loop.
            yield return null;
            Assert.That(CoordinateSystem.NearReachM, Is.EqualTo(0.40f));
            Assert.That(CoordinateSystem.FarReachM, Is.EqualTo(0.70f));
            Assert.That(CoordinateSystem.LabelMinDistanceM, Is.EqualTo(0.30f));
        }

        [UnityTest]
        public IEnumerator IsWithinComfortableReach_NearPosition_ReturnsTrue()
        {
            // Runs one PlayMode frame so the assertions observe a live scene loop.
            yield return null;
            var position = -0.50f * System.Numerics.Vector3.UnitZ; // 0.5 m forward
            var within = CoordinateSystem.IsWithinComfortableReach(position);

            Assert.That(within, Is.True);
        }

        [UnityTest]
        public IEnumerator IsWithinComfortableReach_TooFar_ReturnsFalse()
        {
            // Runs one PlayMode frame so the assertions observe a live scene loop.
            yield return null;
            var position = -2.0f * System.Numerics.Vector3.UnitZ; // 2 m forward
            var within = CoordinateSystem.IsWithinComfortableReach(position);

            Assert.That(within, Is.False);
        }

        [UnityTest]
        public IEnumerator ClampToReachEnvelope_PositionBeyondFarReach_IsClamped()
        {
            // Runs one PlayMode frame so the assertions observe a live scene loop.
            yield return null;
            var position = -2.0f * System.Numerics.Vector3.UnitZ;
            var (clamped, wasClamped) = CoordinateSystem.ClampToReachEnvelope(position);

            Assert.That(wasClamped, Is.True);
            var distance = System.Numerics.Vector3.Distance(CoordinateSystem.Origin, clamped);
            Assert.That(distance, Is.LessThanOrEqualTo(CoordinateSystem.FarReachM + 0.001f));
        }

        [UnityTest]
        public IEnumerator ClampToReachEnvelope_PositionWithinEnvelope_IsNotClamped()
        {
            // Runs one PlayMode frame so the assertions observe a live scene loop.
            yield return null;
            var position = -0.50f * System.Numerics.Vector3.UnitZ;
            var (clamped, wasClamped) = CoordinateSystem.ClampToReachEnvelope(position);

            Assert.That(wasClamped, Is.False);
        }

        [UnityTest]
        public IEnumerator SpatialZones_DefaultPositions_AreWithinReach()
        {
            // Runs one PlayMode frame so the assertions observe a live scene loop.
            yield return null;
            foreach (var zone in new[]
            {
                SpatialZones.Centre,
                SpatialZones.Left,
                SpatialZones.Right
            })
            {
                var distance = System.Numerics.Vector3.Distance(
                    CoordinateSystem.Origin, zone.DefaultPosition);
                Assert.That(distance, Is.LessThanOrEqualTo(CoordinateSystem.FarReachM + 0.001f),
                    $"Zone {zone.Zone} is outside the seated reach envelope.");
            }
        }

        [UnityTest]
        public IEnumerator PanelAnchor_CreatesAnchor_InAssignedZone()
        {
            // Runs one PlayMode frame so the assertions observe a live scene loop.
            yield return null;
            var anchor = new PanelAnchor(
                "panel-incident-detail",
                SpatialZone.Right,
                System.Numerics.Vector3.Zero);

            Assert.That(anchor.AnchorId, Is.EqualTo("panel-incident-detail"));
            Assert.That(anchor.Zone, Is.EqualTo(SpatialZone.Right));
        }

        [UnityTest]
        public IEnumerator PanelAnchorManager_RegistersAndRetrievesAnchor()
        {
            // Runs one PlayMode frame so the assertions observe a live scene loop.
            yield return null;
            var manager = new PanelAnchorManager();
            var anchor = new PanelAnchor(
                "panel-evidence",
                SpatialZone.Left,
                System.Numerics.Vector3.Zero);

            manager.Register(anchor);
            var retrieved = manager.Get("panel-evidence");

            Assert.That(retrieved, Is.Not.Null);
            Assert.That(retrieved.AnchorId, Is.EqualTo("panel-evidence"));
        }
    }
}
