# Architecture Decision Records — FAULTLINE XR

Decisions made during PHASE 0/PHASE 1. Each entry: context, decision, consequence.

## ADR-001 — Modular monolith (one assembly per module)

**Context:** 13 logical modules from PRD §9.3 need compile-time boundaries without the overhead of separate repositories or packages.

**Decision:** Unity assembly definitions (.asmdef), one per module, with an explicit acyclic reference graph. App is the composition root referencing every module; Observability is the leaf cross-cutting module depended on by all.

**Consequence:** Module boundaries are enforced by the compiler; no cyclic dependencies; tests reference modules under test; no god GameManager — App wires interfaces only.

## ADR-002 — Deterministic local agent baseline

**Context:** Agents must diagnose incidents and produce findings. Cloud/LLM agents introduce latency, cost, and non-determinism unsuitable for a competition build on-device.

**Decision:** Ship a deterministic local agent baseline (rule/state-machine based) in the Agents module. Future cloud/LLM integration lives strictly behind the Integrations seam; the domain never depends on it.

**Consequence:** Agent behaviour is reproducible offline and unit-testable; swapping in a cloud agent later requires only an integration adapter, no domain change.

## ADR-003 — C# domain engine independent of Unity

**Context:** Business logic (scenarios, incidents, remediation, agents) should be testable without launching the Unity Editor or a device.

**Decision:** Domain module contains pure C# only — no UnityEngine, no MonoBehaviour, no scene references. Interfaces (IScenarioService, IInfrastructureGraph, etc.) live in Domain; Unity-specific implementations live in XR/Interaction/Spatial/Presentation.

**Consequence:** Domain tests run headless (EditMode); the engine can be reasoned about and unit-tested before Unity is installed.

## ADR-004 — Commands as the only domain write path

**Context:** Presentation must never mutate domain state directly (architecture rule).

**Decision:** All state changes go through validated commands handled by Domain/Incidents/Agents/Remediation. Presentation captures intent and submits commands; learns results via domain events.

**Consequence:** Single audit trail of mutations; Presentation is thin and replaceable; domain invariants are centralized.

## ADR-005 — Error taxonomy FL-[DOMAIN]-[NUMBER]

**Context:** Errors need stable, searchable codes across modules for QA and support.

**Decision:** 13 domains (RT, XR, IN, UI, GR, IC, AG, EV, RM, SM, PS, IT, SC) with per-domain numeric codes; catalogue in docs/engineering/ERROR_CATALOGUE.md.

**Consequence:** Consistent error surfacing; logs and events carry codes; tests can assert on specific codes.

## ADR-006 — Result<T> pattern for fallible operations

**Context:** Avoid exceptions for control flow in the domain engine.

**Decision:** Domain defines `Result<T>` (success value or failure with error code/message). Commands and queries return Result<T>.

**Consequence:** Explicit failure handling; no silent swallowed failures; easy to map to FL- error codes and observability events.