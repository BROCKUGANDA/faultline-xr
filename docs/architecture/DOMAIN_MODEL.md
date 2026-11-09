# Domain Model — FAULTLINE XR

Entities from PRD §5.1. Every entity has an immutable ID; state transitions are
guarded and published as domain events (docs/architecture/EVENT_CATALOGUE.md).
Pure C# — no UnityEngine dependencies; lives in Faultline.Domain.

## Entity catalogue

| Entity | Immutable ID | Lifecycle | Responsibilities |
|--------|-------------|-----------|------------------|
| ServiceNode | NodeId | registered → healthy → degraded → failed → recovered | A deployable service in the simulated infrastructure graph; owns health and status. |
| DependencyEdge | EdgeId | created → active → broken → removed | Directed dependency from one ServiceNode to another; broken edges cascade fault injection. |
| Incident | IncidentId | detected → investigating → mitigating → resolved → closed | Raised when a node or edge breaches an SLO; carries severity (Info/Warning/Critical). |
| TelemetryEvent | EventId | emitted | Numeric sample (latency, error rate, CPU, memory). |
| LogEvent | EventId | emitted | Structured log line with level and message. |
| TraceEvent | EventId | emitted | Distributed-trace span event (traceId, spanId, operation, duration). |
| RemediationAction | ActionId | proposed → approved → rejected → executed → verified | One executable remediation step (restart, rollback, scale, drain, drain-node). |
| AgentTask | TaskId | dispatched → in_progress → completed → failed | Task assigned to a deterministic local agent; produces AgentFinding entries. |
| AgentFinding | FindingId | created → reviewed → dismissed → acted_on | Evidence/finding produced during an agent task; references source event ids. |
| RecoveryResult | ResultId | pending → succeeded → failed | Outcome of executing remediation against an incident; captures before/after node health. |

## Lifecycle rules

- IDs are immutable once assigned; never reused.
- Transitions are executed through validated commands, not direct property writes (Presentation never mutates domain state directly).
- Invalid transitions return `Result.Failure` with an FL- error code (docs/engineering/ERROR_CATALOGUE.md).
- Every terminal transition publishes a typed domain event with an idempotency key.
- Aggregates enforce consistency: an Incident owns its events, a RemediationAction owns its execution steps.