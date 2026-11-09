# FAULTLINE XR — Meta VR Start Developer Competition 2026

A hands-first infrastructure-incident remediation experience for Meta Quest and WebXR.

You diagnose a cascading service failure, dispatch four deterministic analyst agents, approve a safe remediation, execute it, verify recovery, and resolve the incident — entirely with hand gestures. No controllers, API key, account, or network connection are required during play.

## Why this submission is playable

The original Unity repository contained design and C# reference code, but no Unity project files, scenes, Meta XR SDK integration, APK, or installed Unity environment. It therefore could not produce the competition-required playable build. FAULTLINE XR now ships as a WebXR experience:

- **Three.js** renders the 3D infrastructure graph, panels, hand rays, and node states.
- **Meta Quest Browser** supplies WebXR immersive-vr sessions and hand-tracking joints.
- **TypeScript** contains the deterministic simulation, incident lifecycle, agent orchestration, remediation service, and persistence.
- **Vite** produces a deployable static build with support for GitHub Pages, Vercel, or another HTTPS host.

The earlier Unity/C# project remains in this repository as an architecture and domain reference. It is **not** a buildable Unity project and must not be described as one.

## Run locally

```powershell
cd "web"
npm install
npm run dev
```

Open the printed local URL. Desktop preview uses a mouse pointer and is suitable for reviewing the flow, but it is not a replacement for testing hand tracking in Quest Browser.

## Test and build

```powershell
cd "web"
npm test -- --run
npm run build
```

The production output is generated at `web/dist/`.

## Deploy for judging

Build once, then deploy `web/dist/` to any HTTPS host that supports WebXR-capable pages. A static host is sufficient; there is no server-side simulation or external API. Keep `index.html` and `assets/` together so relative asset paths continue to work.

For GitHub Pages, publish the contents of `web/dist/` as the site root. For Vercel or another static host, set the output directory to `web/dist`.

## Play on Meta Quest

1. Deploy the built site over HTTPS.
2. Open the deployed URL in **Meta Quest Browser**.
3. Grant hand-tracking permission when requested.
4. Tap **Enter VR** or **Desktop preview**. The VR button is shown after a WebXR session has been prepared.
5. Begin the CASCADE scenario and complete the incident workflow with pinch gestures.

No controller is needed. Desktop preview is a mouse-based review mode only.

## Experience

The complete first-run flow is:

1. **Welcome** — choose whether to learn the gesture model or begin.
2. **Scenario** — open CASCADE, the mandatory production-shaped scenario.
3. **Detected** — inspect the incident and node states in the spatial graph.
4. **Investigating** — dispatch at least two of ORACLE, TRACE, SENTRY, and FORGE.
5. **Findings** — review deterministic evidence, confidence, and recommendations.
6. **Approval** — explicitly approve or reject the proposed `RestartService`.
7. **Execution** — run the checkpointed, reversible remediation.
8. **Verification** — confirm root-node and aggregate health recovery.
9. **Resolved** — open the after-action report and replay if desired.

## Hands-first interaction

- Pinch thumb and index finger to select buttons and graph nodes.
- A two-hand pinch-and-spread gesture scales the infrastructure graph.
- Pinch a panel edge or surface to drag it within the seated reach envelope.
- Drag empty space to rotate the graph.
- Desktop preview uses the mouse pointer and click instead of pinch.

The environment is seated and stationary. Panels and the graph are placed for an approximately two-foot reach envelope and can be reset from the interface.

## Accessibility and comfort

Settings include:

- Text scale: 100%, 115%, and 130%.
- High-contrast panel styling.
- Reduced motion that disables critical-node pulsing and decorative motion.
- Audio-cue toggle.
- Left-handed panel layout.

Settings and completed-run records are stored locally in browser storage. Storage failure degrades gracefully rather than breaking the experience.

## Repository layout

```text
web/
  src/
    app.ts              App state machine and orchestration
    audio.ts            Synthesised WebAudio cues
    domain.ts           Deterministic scenario and services
    main.ts             Renderer, camera, WebXR, and loop
    scene.ts            3D graph and environment
    ui/                 Panel renderer and screens
    xr/                 Hand rig and interaction manager
    styles.css          Browser overlay styling
  tests/                Domain unit tests
  dist/                 Generated static build
Assets/                 C#/Unity reference implementation (not buildable as-is)
docs/                   Architecture, design, QA, and engineering notes
submission/             Competition submission documentation
```

## Competition selection

- **Track:** Productivity — a focused operator workspace that makes incident response faster and clearer anywhere.
- **Division:** New Experience — the WebXR implementation was built during the competition window.
- **Platform:** WebXR / Immersive Web SDK with the WebXR Device API and hand-tracking extension.
- **Submission path:** Static WebXR site rather than an APK.

## Reference implementation

The C# sources under `Assets/Faultline/Scripts/` provide the original domain, scenario, and interaction contracts that informed the TypeScript implementation. However, the repository does not include a Unity `ProjectSettings/` directory, generated project files, scenes, or the Meta XR SDK. It therefore cannot build an APK in its present state.

See:

- `submission/SUBMISSION_CHECKLIST.md`
- `submission/DEMO_SCRIPT.md`
- `submission/JUDGE_INSTRUCTIONS.md`
- `submission/COMPLIANCE_MATRIX.md`
- `submission/DEVPOST_SUBMISSION_FORM.md`

The legacy Unity planning files and C# names are retained only where they document the reference architecture. They are not evidence of a completed Unity build or headset test.
