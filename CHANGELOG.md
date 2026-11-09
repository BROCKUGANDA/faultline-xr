# Changelog

## [1.0.1] — 2026-11-09 — Determinism + hardening (verified)

### Fixed

- `web/src/domain.ts`: `makeId()` now uses a deterministic counter (no
  `Date.now()` in IDs) so identical seeded runs produce identical IDs.
- `Simulation.log()` timestamps no longer use wall-clock; timeline shows
  elapsed ticks consistently.
- `AgentOrchestrator.dispatch()` is idempotent in the domain layer itself:
  repeated dispatch for the same role+incident returns the existing task
  instead of creating duplicate tasks.
- `RemediationService.execute()` returns `FL-RM-0083` on double-execution
  (was `FL-RM-0080`) and approval expiry is controllable via injected clock.
- `App.proposeRemediation()` no longer silently allows a second propose
  after rejection without transitioning through `awaitingApproval` first.
- `App.dispatchAgent()` refreshes the view on dispatch failure instead of
  leaving stale task state.

### Verified (2026-11-09)

- `npx tsc --noEmit` — passes.
- `npm test -- --run` — 12/12 domain tests pass.
- `npm run build` — succeeds, output at `web/dist/` (609 KB JS / 157 KB gzip).

### C# reference tree (health check)

- Domain blockers fixed: `AgentTask.WithStatus()` stores the status,
  `ServiceNode.NodeId` is now stable string, `CascadeScenario` no longer
  uses `GetHashCode()`/`Guid.NewGuid()`, added `IClock`/`FixedClock`,
  `AgentOrchestrator` + `RemediationService` implement their interfaces
  with idempotency/retry/evidence wiring, `PersistenceService` round-trip
  preserves session + progress. All EditMode + PlayMode tests converted
  to real NUnit assertions. Unity compile still blocked (no Unity installed).

## [1.0.0] — 2026-11-09 — Playable WebXR submission

### Added

- Complete WebXR implementation in `web/`:
  - TypeScript application orchestration.
  - Three.js spatial infrastructure graph.
  - WebXR immersive-vr session bootstrapping.
  - WebXR hand tracking, pinch detection, rays, hover, and drag interactions.
  - Two-hand graph scaling.
  - Canvas-backed 3D panels and complete CASCADE screen flow.
  - Deterministic simulation, incident lifecycle, agent orchestration, and remediation services.
  - Local persistence, accessibility settings, and synthesised audio cues.
  - Desktop mouse preview for flow review.
- `web/tests/domain.test.ts` — 12 passing domain tests.
- `web/dist/` — generated static production build.
- `submission/COMPLIANCE_MATRIX.md` — competition requirements and evidence mapping.
- `submission/DEVPOST_SUBMISSION_FORM.md` — submission copy with hand-interaction explanation and team placeholders.

### Updated

- `README.md` — WebXR run, test, build, deployment, and Quest-play instructions.
- `IMPLEMENTATION_STATUS.md` — current WebXR status and legacy Unity reference status.
- `submission/SUBMISSION_CHECKLIST.md` — WebXR deployment and judge workflow.
- `submission/JUDGE_INSTRUCTIONS.md` — Meta Quest Browser steps and troubleshooting.
- `submission/DEMO_SCRIPT.md` — 3-minute WebXR demonstration plan.
- `submission/BUILD_MANIFEST.json` — WebXR build metadata and verification results.

### Notes

- The earlier Unity/C# tree remains as architecture and design reference only.
- The repository does not contain a buildable Unity project or completed APK.
- The WebXR code compiles and its domain tests pass.
- Physical Quest Browser testing, HTTPS deployment, demo-video upload, and team details remain required before final submission.

## [0.1.0] — 2026-11-08 — Reference implementation and planning

- Added the original C# domain, scenario, interaction, and presentation reference code.
- Added environment audit, build-documentation placeholders, and early submission scaffolding.
- Recorded that Unity and a physical headset were unavailable for compilation or device verification.
