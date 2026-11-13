# FAULTLINE XR

**Hands-first infrastructure incident remediation for Meta Quest and WebXR.**

Diagnose a cascading service failure, dispatch four deterministic analyst agents,
review the evidence, approve a safe remediation, execute it, verify recovery, and
resolve the incident — entirely with hand gestures. No controllers, no API keys,
no accounts, no network calls at runtime.

**Live:** https://faultline.aiazimara.com

---

## Play it

| Path | How |
| --- | --- |
| **Meta Quest (judged path)** | Open the URL in **Quest Browser**, allow hand tracking, tap **Enter VR**, then complete the CASCADE scenario with pinch gestures. |
| **Phone / tablet** | Open the URL. You get the desktop-preview flow (touch = pointer). Android WebView has no WebXR, so hand tracking is not available here. |
| **Desktop** | Open the URL, click **Desktop preview**. Mouse = pointer; useful for flow review, not a substitute for headset testing. |

No install, no account, no keys.

---

## The experience

A ~5 minute complete loop:

1. **Welcome** — choose the gesture tutorial or begin.
2. **Scenario** — open CASCADE (7 services, 6 dependencies).
3. **Detected** — inspect the incident and node health in the spatial graph.
4. **Investigating** — dispatch at least two of ORACLE, TRACE, SENTRY, FORGE.
5. **Findings** — review deterministic evidence, confidence, recommendations.
6. **Approval** — explicitly approve or reject `RestartService`.
7. **Execution** — checkpointed, reversible remediation.
8. **Verification** — root-node and aggregate health must recover.
9. **Resolved** — after-action report, with run history persisted locally.

### Hands-first interaction

- **Pinch** thumb + index to select buttons, nodes, and cards.
- **Two-hand pinch-and-spread** scales the infrastructure graph.
- **Pinch a panel edge** to drag it within the seated reach envelope.
- **Drag empty space** to rotate the graph.

Seated, stationary, ~2 ft reach. Panels can be reset from the interface.

---

## Accessibility

Settings (persisted in `localStorage`, with graceful in-memory fallback):

- Text scale — 100% / 115% / 130%
- High contrast
- Reduced motion (disables pulsing and decorative motion)
- Audio cue toggle
- Left-handed panel layout

---

## Architecture

```
web/
  src/
    app.ts            App state machine and orchestration
    domain.ts         Deterministic simulation, incident, agents, remediation, persistence
    main.ts           Renderer, camera, WebXR session, animation loop
    scene.ts          3D infrastructure graph and environment
    audio.ts          Synthesised WebAudio cues (no audio files)
    ui/               Canvas panel painter, screens, theme
    xr/               Hand rig, pinch detection, interaction manager
  tests/              Unit tests
android-twa/          Android WebView launcher (review build, not an XR runtime)
deploy/               Cloudflare Tunnel + nginx compose, deployment notes
scripts/              Build, e2e, and verification tooling
submission/           Competition submission docs and build manifest
```

**Stack:** TypeScript, Three.js, Vite. Static output in `web/dist/` — no server
component, no runtime network dependency.

**Submission path:** WebXR static site, not an APK.

---

## Develop

```powershell
cd web
npm install
npm run dev        # local dev server
npm test -- --run  # unit tests
npm run build      # -> web/dist/
```

### Verification

```powershell
# unit tests (22)
cd web; npm test -- --run

# end-to-end against a served build (44 assertions)
npx vite preview --port 4173 --host 127.0.0.1
node scripts/e2e-journey.mjs  --url "http://127.0.0.1:4173/?faultline-debug"
node scripts/e2e-settings.mjs --url "http://127.0.0.1:4173/?faultline-debug"
node scripts/e2e-edges.mjs    --url "http://127.0.0.1:4173/?faultline-debug"

# against the live public deployment (8 assertions)
node scripts/e2e-public.mjs --url https://faultline.aiazimara.com/
```

The `?faultline-debug` flag exposes read-only app state to the test drivers. It is
inert during normal play and never required at runtime.

---

## Deploy

The live site is a static nginx container behind a **dedicated Cloudflare Tunnel**.
It is isolated from the other services on the host — its own network, its own
tunnel, no change to the production edge.

```bash
cd /opt/faultline-xr
docker compose -f docker-compose.faultline.yml up -d
curl -s http://127.0.0.1:8088/healthz
curl -sSI https://faultline.aiazimara.com/
```

See `deploy/DEPLOYMENT_STATUS.md` for the full topology and the tunnel/DNS
configuration.

---

## Android APK

`android-twa/` builds a signed APK that loads the live URL. Build it with:

```powershell
node scripts/build-twa-apk.mjs --host faultline.aiazimara.com --keypass <password> --out submission\build
```

Fully offline — `aapt2 → javac → d8 → zipalign → apksigner`. No Gradle, no network.

> **Important limitation, stated plainly:** this is a **launcher, not an XR
> runtime.** Android WebView does not implement the WebXR Device API, so the
> in-app view shows the desktop-preview flow. The "Open in VR browser" button
> hands off to a WebXR-capable browser. The judged experience is the WebXR site
> itself; the APK is a convenience for phone review.

---

## Repository notes

- The Unity/C# tree (`Assets/`, `Tests/`) is **reference material only**. It is
  not a buildable Unity project, was never compiled here, and is not the
  submission path.
- The signing keystore is git-ignored. **Never commit it** — a leaked keystore
  lets anyone ship an update signed as this app.
- CI workflows live in the public repository. This private repo is a mirror and
  intentionally has no Actions configured.