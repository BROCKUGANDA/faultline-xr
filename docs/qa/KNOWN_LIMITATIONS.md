# Known Limitations — FAULTLINE XR

**2026-11-10 (WebXR reality — authoritative):** The submission is the WebXR static build in `web/` (Meta Quest Browser), not the Unity APK described in the older sections below. Current honest state:

- **No Quest Browser / device test has been performed** — no headset is available; hand-tracking, pinch hysteresis, render comfort and 60+ FPS on Quest are **unverified**.
- **Desktop preview is a review aid only** — mouse pointer stands in for a pinch; layout/perf there do not validate headset comfort.
- **Public HTTPS deployment is pending** — no URL exists yet; the Pages self-hosted runner could not be verified.
- **`scripts/smoke-steps.json` and `scripts/smoke-web-dist.mjs` are dev/QA tooling** committed to the repo; they are not part of the shipped bundle (but they expect `puppeteer-core` at `web/node_modules` or `video/node_modules`).
- **Read-only `?faultline-debug` URL hook** in `web/src/main.ts` exposes live app state (`window.__faultlineApp.state`) for automated e2e verification. It is invisible in normal use, never required at runtime, and may be removed for a frozen release candidate.
- **Probes for a public HTTPS URL (2026-11-10)** all reached the same wall: GitHub redirects every github.io project URL on the `BROCKUGANDA` account to `securevoiceai.me` (account-level verified domain of an unrelated live product; A record 57.130.80.158). GitHub Pages for `faultline-xr` is configured and built green from branch `gh-pages` — but github.io 308-redirects there. Entry fix: account Settings → Pages → remove/change the verified domain (then `https://brockuganda.github.io/faultline-xr/` serves immediately) or deploy via an authorized Caddy route on securevoiceai.me.
- **Stray public repo `BROCKUGANDA/faultline-xr-demo`** (created 2026-11-10 diagnosing the same issue; cannot be deleted via API — token 403, GraphQL `deleteRepository` unavailable). README inside marks it stale. Delete via web UI (Settings → Danger Zone) if unwanted.
- **Toast overlay bug (fixed 2026-11-10):** a visible toast made the workspace action panel render blank until the toast expired. Fixed; verified by the full headless journey. Any regression in toast/panel rendering should be treated as P1.
- **Unity/C# tree is reference-only** — never compiled here, never a build input; treat its claims accordingly.
- **Save data is `localStorage`** in the browser; failures degrade to in-memory defaults (FL-PS-0100) and never block the journey.

---

Current state as of 2026-11-08 (PHASE 0–14 code complete, pending Unity install for compile/build). Tracked here so nothing is forgotten before PHASE 15 submission.

## Honest build state

- **No C# compilation has been attempted** — Unity 6 LTS is not installed in this environment.
- **No APK has been built** — Android ARM64 build requires Unity 6 LTS + Android Build Support + JDK 17 + NDK + Meta XR SDK.
- **No device testing has been performed** — no physical Meta Quest / Meta VR Glasses headset is available.

| # | Limitation | Impact | Planned resolution |
|---|------------|--------|--------------------|
| 1 | Unity 6 LTS not installed | Cannot open project, compile C#, or build APK | Install Unity Hub + Unity 6 LTS + Android Build Support |
| 2 | JDK / Android NDK not installed | Android ARM64 builds fail | Install OpenJDK 17 / NDK via Unity Hub |
| 3 | No physical Meta Quest / Meta VR Glasses headset | Cannot verify XR session, hand tracking, or device performance | Acquire headset or use remote device lab; emulator limited |
| 4 | No APK built yet | No submission build exists | Follow `docs/engineering/BUILD_GUIDE.md` once tooling is present |
| 5 | Meta XR SDK All-in-One v207+ not imported | OpenXR + hand tracking unavailable at runtime | Import package after Unity install |
| 6 | Windows-only host noted | No macOS/Linux build verification planned | Android target only; host OS irrelevant to APK output |

## What is NOT a limitation (PHASE 0–14 complete)

The following were listed as limitations in the PHASE 1 state but are now **implemented** (pure C#, pending Unity compile):

| # | Area | Status | Where |
|---|------|--------|-------|
| A | Domain engine & Result pattern | Code complete | `Assets/Faultline/Scripts/Domain/` |
| B | Infrastructure graph | Code complete | `Assets/Faultline/Scripts/Infrastructure/` |
| C | Incident detection (CASCADE scenario) | Code complete | `Assets/Faultline/Scripts/Incidents/` |
| D | Agent orchestration | Code complete | `Assets/Faultline/Scripts/Agents/` |
| E | Remediation & approvals | Code complete | `Assets/Faultline/Scripts/Remediation/` |
| F | XR integration abstractions | Code complete | `Assets/Faultline/Scripts/XR/` |
| G | Interaction (controller-free gestures) | Code complete | `Assets/Faultline/Scripts/Interaction/` |
| H | Spatial topology | Code complete | `Assets/Faultline/Scripts/Spatial/` |
| I | Presentation (23 screen contracts) | Code complete | `Assets/Faultline/Scripts/Presentation/` |
| J | Observability | Code complete | `Assets/Faultline/Scripts/Observability/` |
| K | Persistence (save/load, graceful degrade) | Code complete | `Assets/Faultline/Scripts/Persistence/` |
| L | Integrations seam | Code complete | `Assets/Faultline/Scripts/Integrations/` |
| M | QA tests (EditMode + PlayMode contracts) | Code complete | `Tests/EditMode/`, `Tests/PlayMode/` |
| N | Build script + submission docs | Code complete | `scripts/`, `submission/` |

## Remaining limitations (PHASE 15 only)

Only the items in rows 1–6 above remain. None block PHASE 0–14 deliverables; all are tracked for PHASE 15 (submission build, due 2026-11-18).
