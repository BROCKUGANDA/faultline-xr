# Error Catalogue — FAULTLINE XR

Error taxonomy per PRD §8.4. Every error carries a stable code:
`FL-[DOMAIN]-[NUMBER]`. Codes appear in logs, domain events, and Remediation
verification results.

## Domains

| Code | Domain | Meaning |
|------|--------|---------|
| RT | Runtime | Core runtime / engine faults |
| XR | XR / OpenXR / Meta XR SDK | Session, hand tracking, device capability |
| IN | Interaction | Gestures, debounce, tracking loss, controller input |
| UI | Presentation / HUD / panels | UI state, layout, input routing |
| GR | Graph / Infrastructure | Service nodes, dependency edges, cycle detection |
| IC | Incidents | Detection, severity, lifecycle transitions |
| AG | Agents | Agent dispatch, task lifecycle, findings |
| EV | Events / Observability | Event publishing, idempotency, tracing |
| RM | Remediation | Propose, approve, execute, verify flows |
| SM | State management / Domain model | Commands, entity transitions, Result failures |
| PS | Persistence | Save/load, serialization, migration |
| IT | Integrations | External adapters, telemetry export, cloud hooks |
| SC | Scenario | Scenario definition, start/stop, scripting |

## Example codes

| Code | Domain | Example condition |
|------|--------|-------------------|
| FL-RT-0001 | Runtime | Unhandled exception in domain command handler |
| FL-XR-0010 | XR | OpenXR session failed to start on device |
| FL-XR-0011 | XR | Hand tracking not available / not supported |
| FL-IN-0020 | Interaction | Pinch confidence below threshold; gesture rejected |
| FL-IN-0021 | Interaction | Tracking lost during active grab; object dropped |
| FL-UI-0030 | Presentation | Panel requested with no registered view |
| FL-GR-0040 | Infrastructure | Cycle detected in dependency graph |
| FL-GR-0041 | Infrastructure | Dependency edge references unknown node |
| FL-IC-0050 | Incidents | Incident transition not allowed from current state |
| FL-AG-0060 | Agents | Agent task dispatch failed; no agent available |
| FL-EV-0070 | Observability | Duplicate event idempotency key rejected |
| FL-RM-0080 | Remediation | Remediation action not approved; execution blocked |
| FL-SM-0090 | Domain model | Invalid command: entity not found |
| FL-PS-0100 | Persistence | Save failed; serialization error |
| FL-IT-0110 | Integrations | External adapter unreachable; fallback used |
| FL-SC-0120 | Scenario | Scenario definition invalid; cannot start |

## Usage rules

- Every module maps its failures to its domain code; never leak raw exceptions across module boundaries.
- `Result<T>` failures carry the FL- code and a human message (ADR-006).
- Observability logs every FL- error with module, code, timestamp, and idempotency key where applicable.
- QA tests assert on specific FL- codes (docs/qa/TEST_STRATEGY.md).