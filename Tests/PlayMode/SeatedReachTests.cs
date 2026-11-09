namespace Faultline.Tests.PlayMode
{
    using Faultline.Presentation;
    using Faultline.Spatial;
    using NUnit.Framework;

    /// <summary>
    /// PlayMode tests for seated reach and panel placement:
    /// seated reach envelope, panel placement.
    ///
    /// Pure C# — no UnityEngine, no UnityEditor.
    /// </summary>
    [TestFixture]
    public sealed class SeatedReachTests
    {
        [Test]
        public void CoordinateSystem_DefinesSeatedReachEnvelope()
        {
            Assert.That(CoordinateSystem.NearReachM, Is.EqualTo(0.40f));
            Assert.That(CoordinateSystem.FarReachM, Is.EqualTo(0.70f));
            Assert.That(CoordinateSystem.LabelMinDistanceM, Is.EqualTo(0.30f));
        }

        [Test]
        public void IsWithinComfortableReach_NearPosition_ReturnsTrue()
        {
            var position = -0.50f * System.Numerics.Vector3.UnitZ; // 0.5 m forward
            var within = CoordinateSystem.IsWithinComfortableReach(position);

            Assert.That(within, Is.True);
        }

        [Test]
        public void IsWithinComfortableReach_TooFar_ReturnsFalse()
        {
            var position = -2.0f * System.Numerics.Vector3.UnitZ; // 2 m forward
            var within = CoordinateSystem.IsWithinComfortableReach(position);

            Assert.That(within, Is.False);
        }

        [Test]
        public void ClampToReachEnvelope_PositionBeyondFarReach_IsClamped()
        {
            var position = -2.0f * System.Numerics.Vector3.UnitZ;
            var (clamped, wasClamped) = CoordinateSystem.ClampToReachEnvelope(position);

            Assert.That(wasClamped, Is.True);
            var distance = System.Numerics.Vector3.Distance(CoordinateSystem.Origin, clamped);
            Assert.That(distance, Is.LessThanOrEqualTo(CoordinateSystem.FarReachM + 0.001f));
        }

        [Test]
        public void ClampToReachEnvelope_PositionWithinEnvelope_IsNotClamped()
        {
            var position = -0.50f * System.Numerics.Vector3.UnitZ;
            var (clamped, wasClamped) = CoordinateSystem.ClampToReachEnvelope(position);

            Assert.That(wasClamped, Is.False);
        }

        [Test]
        public void SpatialZones_DefaultPositions_AreWithinReach()
        {
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

        [Test]
        public void PanelAnchor_CreatesAnchor_InAssignedZone()
        {
            var anchor = new PanelAnchor(
                "panel-incident-detail",
                SpatialZone.Right,
                System.Numerics.Vector3.Zero);

            Assert.That(anchor.AnchorId, Is.EqualTo("panel-incident-detail"));
            Assert.That(anchor.Zone, Is.EqualTo(SpatialZone.Right));
        }

        [Test]
        public void PanelAnchorManager_RegistersAndRetrievesAnchor()
        {
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
