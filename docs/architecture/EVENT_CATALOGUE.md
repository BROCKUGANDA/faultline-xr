# Event Catalogue — FAULTLINE XR

Typed domain events from PRD §9.5. Events are append-only, carry globally
unique ids, and are the only way Presentation and other edge modules observe
domain state changes.

| Event | Raised by | Key payload | Idempotency key |
|-------|-----------|-------------|-----------------|
| ScenarioStarted | Domain (scenario service) | scenarioId, startedAtUtc | scenarioId |
| ServiceStatusChanged | Infrastructure | nodeId, previousStatus, newStatus | nodeId + status + occurredAt |
| IncidentDetected | Incidents | incidentId, nodeId, severity, ruleId | incidentId |
| AgentDispatched | Agents | taskId, agentType, incidentId | taskId |
| AgentTaskCompleted | Agents | taskId, status, findingIds | taskId |
| EvidenceCollected | Agents / Observability | taskId, evidenceId, type, ref | evidenceId |
| RemediationProposed | Remediation | planId, incidentId, actionType, proposedAtUtc | planId |
| RemediationApproved | Remediation | planId, approvedBy, approvedAtUtc | planId |
| RemediationRejected | Remediation | planId, rejectedBy, reason | planId |
| RemediationExecuted | Remediation | planId, succeeded, stepResults, executedAtUtc | planId |
| VerificationCompleted | Remediation / Incidents | planId, incidentId, verified | planId |
| IncidentResolved | Incidents | incidentId, resolvedBy, resolvedAtUtc | incidentId |

## Idempotency

Each event carries a stable idempotency key (right-hand column). Handlers
must deduplicate by key before applying effects: redelivery of the same event
must not double-count metrics, re-dispatch agents, or re-execute remediation.
Events are never mutated after publication; corrections are published as new
events. This makes the event stream replay-safe for tests and persistence
recovery.