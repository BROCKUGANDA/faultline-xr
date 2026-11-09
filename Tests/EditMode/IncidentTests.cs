namespace Faultline.Tests.EditMode
{
    using System.Linq;

    using Faultline.Domain;
    using Faultline.Incidents;
    using NUnit.Framework;

    /// <summary>
    /// EditMode tests for incident detection and transitions, including
    /// FL-IC error mapping and duplicate detection.
    ///
    /// Pure C# — no UnityEngine.
    /// </summary>
    [TestFixture]
    public sealed class IncidentTests
    {
        [Test]
        public void DetectIncident_AddsActiveIncident()
        {
            var service = new IncidentService(new FixedClock());
            var result = service.DetectIncident("database", "rule-db-01", "Critical");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(service.ActiveIncidentIds.Count, Is.EqualTo(1));
        }

        [Test]
        public void DetectIncident_Duplicate_ReturnsFlIc()
        {
            var service = new IncidentService(new FixedClock());
            var first = service.DetectIncident("database", "rule-db-01", "Critical");
            Assert.That(first.IsSuccess, Is.True);

            var second = service.DetectIncident("database", "rule-db-01", "Critical");
            Assert.That(second.IsSuccess, Is.False);
            Assert.That(second.ErrorCode, Is.EqualTo("FL-IC-0052"));
            Assert.That(service.ActiveIncidentIds.Count, Is.EqualTo(1));
        }

        [Test]
        public void TransitionIncident_InvalidState_ReturnsFlIc()
        {
            var service = new IncidentService(new FixedClock());
            service.DetectIncident("database", "rule-db-01", "Critical");
            var incidentId = service.ActiveIncidentIds.First();

            // Jumping DETECTED -> RESOLVED is invalid.
            var result = service.TransitionIncident(incidentId, "Resolved");
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("FL-IC-0050"));
        }

        [Test]
        public void TransitionIncident_ValidPath_Succeeds()
        {
            var service = new IncidentService(new FixedClock());
            service.DetectIncident("database", "rule-db-01", "Critical");
            var incidentId = service.ActiveIncidentIds.First();

            foreach (var state in new[] { "Triaged", "Investigating", "AwaitingApproval", "Remediating", "Verifying", "Resolved" })
            {
                var result = service.TransitionIncident(incidentId, state);
                Assert.That(result.IsSuccess, Is.True, $"Failed at {state}: {result.ErrorMessage}");
            }

            var incident = service.GetIncident(incidentId);
            Assert.That(incident.Value["state"].ToString(), Is.EqualTo("Resolved"));
        }

        [Test]
        public void TransitionIncident_UnknownIncident_ReturnsFlIc()
        {
            var service = new IncidentService(new FixedClock());
            var result = service.TransitionIncident("nonexistent", "Triaged");
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("FL-IC-0051"));
        }
    }
}
