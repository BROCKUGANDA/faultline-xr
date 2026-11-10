# Requirement Traceability — FAULTLINE XR

**Source:** 500-point playbook v1.0 (docs/FAULTLINE_500_POINT_PLAYBOOK.md), mapped to the WebXR implementation in `web/`.
**Date:** 2026-11-09; corrected 2026-11-10. **Submission path:** WebXR static build (Meta Quest Browser; desktop preview for review). The C# tree under `Assets/` and `Tests/` is legacy reference material.

Statuses: `NOT_ASSESSED | PLANNED | IN_PROGRESS | BLOCKED | DONE_VERIFIED | DEFERRED | NOT_APPLICABLE`
`DONE_VERIFIED` requires a code reference plus executed verification and evidence.

> **Correction 2026-11-10:** This matrix previously claimed the public HTTPS URL was
> "DONE_VERIFIED (this audit) — deployed via QW Pages during this session". **No public URL
> exists anywhere in the repository** — `submission/BUILD_MANIFEST.json` still contains
> `<PUBLIC_HTTPS_URL>`, no workflow run, artefact, or deployment record was found, and the
> GitHub Pages workflow targets a self-hosted runner (`faultline-xr`) whose online status
> cannot be verified from here. That claim is retracted; the item is `BLOCKED` until a URL
> is produced and smoke-tested. This also affects the "Polish & Presentation" row, which is
> therefore not fully DONE. Tests re-run 2026-11-10: 15/15 pass (see regression fix below).

## Core loop (P0)

| Requirement | Status | Evidence |
| --- | --- | --- |
| One complete incident loop: orient → select → evidence → agents → approval → execute → verify → report | DONE_VERIFIED | Re-verified 2026-11-10 end-to-end on production `web/dist` (post-toast-fix rebuild): headless Chromium clicked the full journey with real pointer input; final in-page state dump: phase=resolved, screen=results, findings=2, dispatchCount=2, toast=none; 0 console errors, 0 page errors. Evidence: `audit-2026-11-10/smoke-04`…`smoke-13`, driver `scripts/smoke-web-dist.mjs` (+`?faultline-debug` state query). Prior 2026-11-09 run: full journey completed headlessly, localStorage run record `resolved: true`, ~55s, 0 console errors |
| CASCADE deterministic scenario | DONE_VERIFIED | `web/src/domain.ts` Simulation (seeded, tick-based clock); tests `web/tests/domain.test.ts` **15/15** (2026-11-10) |
| Hands-first input (no controllers) | DONE_VERIFIED (code + preview) | `web/src/xr/hands.ts` WebXR hand joints + pinch hysteresis; desktop preview uses pointer as pinch stand-in. **Headset verification still pending (user action)** |
| Seated / stationary use | DONE_VERIFIED (design) | Panels placed at seated heights (main.ts:58-72); e2e completed seated-equivalent viewport |
| Offline: no API key, cloud or accessory required | DONE_VERIFIED | Local deterministic agents (ORACLE/TRACE/SENTRY/FORGE) in `web/src/app.ts`; no network calls in e2e run |
| Deterministic replay / reset | DONE_VERIFIED | `sessionToken` reset in `beginWorkspace()`; unit tests for checkpoint/restore |
| Agent findings grounded in evidence | DONE_VERIFIED | `AgentOrchestrator` findings + synthesis gate (2 findings unlock remediation) |
| Human approval before remediation | DONE_VERIFIED | `RemediationService.approve/execute/verify` state machine; tests |
| Verification proves simulated recovery | DONE_VERIFIED | `verify()` checks rootHealth ≥ 0.9 and aggregate ≥ 0.8 against live simulation state (fixed 2026-11-09: retryable + decay stops after remediation) |
| Persistence (settings + runs) | DONE_VERIFIED | localStorage `faultline-xr.save.v1`; e2e wrote a run record |

## Fixed during this audit (2026-11-09)

| Defect | Fix | Verification |
| --- | --- | --- |
| Desktop preview camera looked straight ahead; seated-height panels (incl. Begin button) fell outside the 66° frustum and were unreachable | `main.ts` `applyCameraPitch()` pitches camera −0.42 rad in desktop mode | Headless run now reaches every panel; buttons clickable |
| `.hint` DOM pill (760px wide, bottom centre) intercepted pointer events over the action panel | `styles.css` `pointer-events: none` on `.hint` | Click trace previously blocked; now passes through |
| Action panel investigating phase: dispatch grid overlapped the Synthesise button rect (hitTest resolves first-drawn → button unreachable) | `screens.ts` investigating case compacted; metrics card collapsed in that phase | Press trace hit `synthesise` cleanly; full flow proceeds |
| One failed verification locked the proposal as `failed` permanently → run could never complete | `domain.ts verify()` stays retryable until healthy | New regression test passes |
| Root-cause node decayed every tick even after remediation → verification only passable within ~5s | `domain.ts faultActive` flag cleared in `applyRemediation()` | New regression test + e2e resolved run |

## Judging dimensions

| Dimension | Status | Evidence |
| --- | --- | --- |
| Innovation & Creativity | DONE (evidence in submission text) | Spatial topology + hands-first incident loop; demo script |
| Experience Design | DONE_VERIFIED | 5-min loop, tutorial, accessibility settings (text scale, high contrast, reduced motion, audio toggle, left-handed) |
| Technical Implementation | DONE_VERIFIED | 15/15 domain tests, typecheck clean, deterministic simulation, idempotent remediation, error catalogue |
| Polish & Presentation | IN_PROGRESS | Demo video rendered (~115 s ≤ 3 min, `video/demo/out/faultline-xr-demo.mp4`); **no public upload confirmed**; deployed build not verified (see below) |

## Remaining blockers (user / ops actions) — corrected 2026-11-10

| Item | Status | Notes |
| --- | --- | --- |
| Public HTTPS URL | **BLOCKED (30-second entrant fix)** | GitHub Pages site built green from branch `gh-pages` (2026-11-10, legacy mode; `deploy-pages.yml` self-hosted runner `faultline-xr` dead — 12h+ queued; bypassed). Blocker: repo Pages custom domain is stale (securevoiceai.me, an unrelated live product) — github.io URLs redirect there → 404. Fix: Settings → Pages → Custom domain → clear → Save; then `https://brockuganda.github.io/faultline-xr/` serves the build (dist paths are relative — verified) |
| Public video link | **PUBLIC (2026-11-10)** | `https://github.com/BROCKUGANDA/faultline-xr/releases/download/v1.0.0-rc/faultline-xr-demo.mp4` — GitHub release asset (v1.0.0-rc), digest verified server-side against recorded SHA-256; 1m55s h264 1080p30. Inline-streaming requires YouTube/Vimeo re-upload (recommended) |
| Quest Browser headset verification | BLOCKED (user) | Official FAQ: headset not required to enter. Desktop-preview e2e + honest device-matrix disclosure in submission text; device behaviour itself is **unverified** |
| Devpost form submission | BLOCKED (user) | Draft at `submission/DEVPOST_SUBMISSION_FORM.md`; user account required |

## Fixes and evidence added 2026-11-10

| Item | Detail | Verification |
| --- | --- | --- |
| SaveData test depended on Node's ambient `localStorage` (absent in older Node; present in Node 26) | Failing test on this machine (13/14) | Test rewritten to inject storage explicitly: round-trip passes through an in-memory `Storage`; graceful `FL-PS-0100` degradation asserted through a throwing `Storage` | `npm test -- --run` → **15/15 pass** |
| Desktop preview: seated-height panels clipped by viewport at 1280×720; primary buttons unreachable without guesswork | `main.ts` desktop-only pose: rig offset (0, 0.06, 0.12) + `applyXrPose()` on XR enter/sessionstart; XR path unchanged | Production rebuild + `node scripts/smoke-web-dist.mjs` → 0 console/page errors; welcome → scenario → live workspace → dispatch agents all clicked through headlessly; screenshots `audit-2026-11-10/` |
| Headless smoke driver added (`scripts/smoke-web-dist.mjs`) | Repeatable desktop-preview journey verification of the built artifact | Steps driven (2026-11-10, 13-step script): enter desktop → Begin operation → Begin CASCADE → Open incident → dispatch ORACLE → dispatch TRACE → Synthesise → Propose → Approve → Execute → Verify → Resolve → after-action report; final state: resolved / results / 0 console/page errors | `scripts/smoke-steps.json`; evidence `audit-2026-11-10/smoke-00..13-*.png`; driver supports `--eval` state dumps via read-only `?faultline-debug` hook |
| **TOAST OVERLAY BUG (fixed 2026-11-10)** | `main.ts` `drawToastOverlay()` re-drew the workspace **action panel with only the toast** — the whole incident UI disappeared (empty transparent canvas) during the 2.8 s a toast was visible. Reproduced headlessly ≥4 times (screenshots `audit-2026-11-10/probe-*.png`). Fix: toast is now drawn on top of the full screen inside `app.refresh()`; `drawToastOverlay` deleted | Post-fix rebuild re-verified: full journey completes headlessly with panels always visible; `smoke-07`, `smoke-10`, `smoke-13` show findings/recovery/results panels intact; 0 console/page errors |
| `video/` (753.8 MB) and `audit-2026-11-09/*.png` added to `.gitignore` | Prevent accidental 750 MB commit | `git status` |

## Selected deferred items (by design)

Multiplayer, ESP32 hardware, live infrastructure connectors, Kubernetes/observability integrations, enterprise governance, progression systems — DEFERRED per playbook scope rules (post-competition roadmap).
