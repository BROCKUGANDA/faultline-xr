namespace Faultline.Tests.EditMode
{
    using System;

    using Faultline.Domain;
    using NUnit.Framework;

    /// <summary>
    /// EditMode tests for the incident lifecycle state machine.
    ///
    /// Pure C# — no UnityEngine, no UnityEditor.
    /// </summary>
    [TestFixture]
    public sealed class StateMachineTests
    {
        // Valid lifecycle path: DETECTED → TRIAGED → INVESTIGATING → AWAITING_APPROVAL → REMEDIATING → VERIFYING → RESOLVED
        [Test]
        public void ValidIncidentLifecycle_AllTransitionsSucceed()
        {
            var incident = Incident.Create(Guid.NewGuid(), "scenario-1", "database", "Critical", "rule-db",
                DateTimeOffset.UtcNow);

            incident = Step(incident, IncidentLifecycleState.Triaged);
            incident = Step(incident, IncidentLifecycleState.Investigating);
            incident = Step(incident, IncidentLifecycleState.AwaitingApproval);
            incident = Step(incident, IncidentLifecycleState.Remediating);
            incident = Step(incident, IncidentLifecycleState.Verifying);
            incident = Step(incident, IncidentLifecycleState.Resolved);

            Assert.That(incident.State, Is.EqualTo(IncidentLifecycleState.Resolved));
        }

        [Test]
        public void InvalidTransition_ReturnsFlIcError()
        {
            var incident = Incident.Create(Guid.NewGuid(), "scenario-1", "database", "Critical", "rule-db",
                DateTimeOffset.UtcNow);

            // Cannot jump DETECTED -> REMEDIATING
            var result = incident.ValidateTransition(IncidentLifecycleState.Remediating);
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("FL-IC-0050"));
        }

        [Test]
        public void TerminalState_NoFurtherTransitions()
        {
            var incident = Incident.Create(Guid.NewGuid(), "scenario-1", "database", "Critical", "rule-db",
                DateTimeOffset.UtcNow);
            incident = Step(incident, IncidentLifecycleState.Triaged);
            incident = Step(incident, IncidentLifecycleState.Investigating);
            incident = Step(incident, IncidentLifecycleState.AwaitingApproval);
            incident = Step(incident, IncidentLifecycleState.Remediating);
            incident = Step(incident, IncidentLifecycleState.Verifying);
            incident = Step(incident, IncidentLifecycleState.Resolved);

            var result = incident.ValidateTransition(IncidentLifecycleState.Rejected);
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("FL-IC-0052"));
        }

        [Test]
        public void RejectionAllowedFromNonTerminal()
        {
            var incident = Incident.Create(Guid.NewGuid(), "scenario-1", "database", "Critical", "rule-db",
                DateTimeOffset.UtcNow);
            var result = incident.ValidateTransition(IncidentLifecycleState.Rejected);
            Assert.That(result.IsValid, Is.True);
        }

        private static Incident Step(Incident incident, IncidentLifecycleState toState)
        {
            var validation = incident.ValidateTransition(toState);
            Assert.That(validation.IsValid, Is.True, $"Expected valid {incident.State} -> {toState}");
            return incident.TransitionTo(toState, out var _);
        }
    }
}
