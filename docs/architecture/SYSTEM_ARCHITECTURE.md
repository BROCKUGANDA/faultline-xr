# System Architecture — FAULTLINE XR

Module map, responsibilities, and dependency direction.
Derived from PRD §3.3 (contracts), §5.1 (domain model), §9.3 (modules).

## 1. Dependency direction (acyclic)

Logical flow:

    App → Domain → Infrastructure → Incidents → Agents → Remediation → XR → Interaction → Spatial → Presentation
                                                                   ↳ Observability (leaf, cross-cutting)
                                                                   ↳ Persistence
                                                                   ↳ Integrations

Every arrow is a "depends on" relationship enforced at compile time by Unity
assembly definitions (.asmdef references). There are no cycles.

## 2. Module table

| # | Module | Responsibilities | Depends on |
|---|--------|------------------|------------|
| 1 | App (Composition Root) | Bootstraps all modules, wires interfaces to implementations, owns application lifetime. Intentionally thin — no god object, no direct domain mutation. | Domain, Infrastructure, Incidents, Agents, Remediation, XR, Interaction, Spatial, Presentation, Observability, Persistence, Integrations |
| 2 | Domain | Pure C# domain engine: entities, value objects, immutable IDs, validated commands, Result<T>. No UnityEngine types; testable headless. | Observability (logging interface only) |
| 3 | Infrastructure | Simulated infrastructure graph: service nodes, dependency edges, graph queries, cycle/integrity checks. | Domain, Observability |
| 4 | Incidents | Incident detection, severity, lifecycle (detected → investigating → mitigating → resolved → closed). | Domain, Infrastructure, Observability |
| 5 | Agents | Deterministic local agent baseline: dispatch agent tasks, task lifecycle, findings collection. | Domain, Infrastructure, Incidents, Observability |
| 6 | Remediation | Remediation plans: propose → approve/reject → execute → verify. | Domain, Incidents, Agents, Observability |
| 7 | XR | OpenXR + Meta XR SDK integration: XR session, hand tracking, device capabilities, controller-free input. | Domain, Observability |
| 8 | Interaction | Hand gestures (pinch, grab, release, open palm, two-hand scale, point/gaze), debounce, confidence, tracking-loss behaviour. | XR, Domain, Observability |
| 9 | Spatial | Spatial anchors, scene understanding, world-locked placement of UI and nodes. | Domain, Infrastructure, XR, Interaction, Observability |
| 10 | Presentation | HUD, panels, menus, visual feedback per design system. Never mutates domain state directly — submits validated commands and reacts to events. | Spatial, Domain, Incidents, Agents, Remediation, Interaction, Observability |
| 11 | Observability | Logging, tracing, event sink. Leaf module: depended on by all, depends on none. | — |
| 12 | Persistence | Save/load of scenario state, incident history, remediation records. | Domain, Observability |
| 13 | Integrations | External adapters (telemetry export, crash reporting, future LLM/cloud hooks). | Domain, Observability |

## 3. Key architectural rules

1. **No cycles.** Enforced by .asmdef reference lists (see assembly map below).
2. **Domain independence.** Domain contains no Unity scene or Engine references; business logic runs headless.
3. **Presentation is read-only toward domain.** Presentation captures user intent (gesture, press, gaze) and submits a command. State changes happen only inside Domain/Incidents/Agents/Remediation via validated commands, and Presentation learns about them through domain events (see docs/architecture/EVENT_CATALOGUE.md).
4. **One assembly per module.** Each module is a Unity assembly definition; test assemblies reference the modules under test.
5. **App composes, App does not orchestrate logic.** App wires interfaces at startup; runtime behaviour lives in the modules.

## 4. Assembly dependency map

| Assembly | References (assembly names) |
|----------|-----------------------------|
| Faultline.App | Faultline.Domain, Faultline.Infrastructure, Faultline.Incidents, Faultline.Agents, Faultline.Remediation, Faultline.XR, Faultline.Interaction, Faultline.Spatial, Faultline.Presentation, Faultline.Observability, Faultline.Persistence, Faultline.Integrations |
| Faultline.Domain | Faultline.Observability |
| Faultline.Infrastructure | Faultline.Domain, Faultline.Observability |
| Faultline.Incidents | Faultline.Domain, Faultline.Infrastructure, Faultline.Observability |
| Faultline.Agents | Faultline.Domain, Faultline.Infrastructure, Faultline.Incidents, Faultline.Observability |
| Faultline.Remediation | Faultline.Domain, Faultline.Incidents, Faultline.Agents, Faultline.Observability |
| Faultline.XR | Faultline.Domain, Faultline.Observability |
| Faultline.Interaction | Faultline.XR, Faultline.Domain, Faultline.Observability |
| Faultline.Spatial | Faultline.Domain, Faultline.Infrastructure, Faultline.XR, Faultline.Interaction, Faultline.Observability |
| Faultline.Presentation | Faultline.Spatial, Faultline.Domain, Faultline.Incidents, Faultline.Agents, Faultline.Remediation, Faultline.Interaction, Faultline.Observability |
| Faultline.Observability | (none) |
| Faultline.Persistence | Faultline.Domain, Faultline.Observability |
| Faultline.Integrations | Faultline.Domain, Faultline.Observability |
| Faultline.Tests.EditMode | Faultline.Domain, Faultline.Infrastructure, Faultline.Incidents, Faultline.Agents, Faultline.Remediation, Faultline.XR, Faultline.Interaction, Faultline.Spatial, Faultline.Presentation, Faultline.Observability, Faultline.Persistence, Faultline.Integrations |
| Faultline.Tests.PlayMode | Faultline.Domain, Faultline.Infrastructure, Faultline.Incidents, Faultline.Agents, Faultline.Remediation, Faultline.XR, Faultline.Interaction, Faultline.Spatial, Faultline.Presentation, Faultline.Observability, Faultline.Persistence, Faultline.Integrations |

## 5. Layering summary

Composition (App) → Experience (Presentation, Interaction, Spatial, XR) → Logic (Domain, Incidents, Agents, Remediation) → Adapters (Infrastructure, Persistence, Integrations) ← Cross-cutting leaf (Observability).