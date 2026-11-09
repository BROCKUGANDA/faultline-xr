# Test Strategy — FAULTLINE XR

Per PRD §13. Four test layers; Unity not installed yet, so no tests are written or run in PHASE 1.

## 1. Unit tests (EditMode)

**Scope:** Pure C# domain engine — Result<T>, entities, commands, incident lifecycle, remediation flow, agent task state machine.

**Location:** `Tests/EditMode/` → `Faultline.Tests.EditMode.asmdef`.

**References:** Faultline.Domain, Faultline.Infrastructure, Faultline.Incidents, Faultline.Agents, Faultline.Remediation, Faultline.Observability, Faultline.Persistence.

**Criteria:** Every domain transition has a pass and a fail case; every invalid command returns a named FL- error code; Result<T> failure never throws.

## 2. Integration tests (EditMode / PlayMode)

**Scope:** Module seams — Domain↔Infrastructure graph queries, Incidents←Infrastructure detection, Agents→Incidents dispatch, Remediation→Incidents execute/verify, Persistence save/load round-trip.

**Location:** `Tests/EditMode/` and `Tests/PlayMode/` → both asmdefs.

**Criteria:** Graph integrity checks pass; incident detection fires on SLO breach; remediation plan executes end-to-end; saved scenario reloads with identical state.

## 3. XR tests (PlayMode, device)

**Scope:** OpenXR session start, hand tracking availability, gesture recognition (pinch/grab/release/scale/point), tracking-loss behaviour, controller-free fallback.

**Location:** `Tests/PlayMode/` → `Faultline.Tests.PlayMode.asmdef` (requires device / Meta Quest).

**Criteria:** Session initializes on ARM64 Android; pinch registers with confidence threshold; tracking loss releases grab without state corruption; no controller required for core flows.

## 4. End-to-end tests (PlayMode, device)

**Scope:** Full scenario: start scenario → incident detected → agent dispatched → finding collected → remediation proposed/approved/executed → incident resolved, all through the Presentation HUD.

**Location:** `Tests/PlayMode/` → `Faultline.Tests.PlayMode.asmdef`.

**Criteria:** Scenario completes without unhandled exceptions; every event in docs/architecture/EVENT_CATALOGUE.md is observable; final state consistent.

## 5. Test environment notes

- Unity 6 LTS + Test Framework package required (not installed yet).
- EditMode tests can run headless once Unity is installed.
- PlayMode/XR/E2E require a physical Meta Quest or remote device lab.
- No test code exists yet; assemblies are placeholders ready for PHASE 14.