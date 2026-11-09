# Submission Checklist — FAULTLINE XR

## 1. Product description

FAULTLINE XR is a hands-first infrastructure-incident remediation experience for Meta Quest, built as deployable WebXR. The operator diagnoses a cascading service failure in a 3D spatial graph, dispatches four deterministic analyst agents, reviews evidence, approves a safe remediation, executes it, verifies recovery, and resolves the incident.

## 2. Track and division

- **Track:** Productivity
- **Division:** New Experience
- **Platform:** WebXR / Immersive Web SDK
- **Submission format:** Static website

## 3. Build and deployment

### Local development

```powershell
cd "web"
npm install
npm run dev
```

### Test and production build

```powershell
cd "web"
npm test -- --run
npm run build
```

The generated site is in `web/dist/`. Keep `index.html` and `assets/` together.

### Deployment

Deploy `web/dist/` to an HTTPS host that serves a static website. Suitable examples include:

- GitHub Pages — publish the contents of `web/dist/` as the site root.
- Vercel — set the output directory to `web/dist`.
- Another static HTTPS host.

The experience runs entirely in the browser. It has no server-side dependency and does not call an API at runtime.

## 4. Judge instructions

See `submission/JUDGE_INSTRUCTIONS.md`.

Judge path:

1. Open the deployed URL in Meta Quest Browser.
2. Grant hand-tracking permission.
3. Enter VR.
4. Complete the CASCADE scenario using pinch gestures only.
5. No controller, API key, account, or test build installation is required.

## 5. Feature checklist

| Feature | Status | Notes |
| --- | --- | --- |
| WebXR immersive-vr session | ✅ | Implemented in `web/src/main.ts` |
| WebXR hand tracking | ✅ | Hand joints, pinch hysteresis, rays, and joint feedback |
| First-run tutorial | ✅ | Gesture model and seated reach guidance |
| CASCADE scenario | ✅ | Deterministic seven-service dependency graph |
| Deterministic simulation | ✅ | Event timeline and health propagation |
| Incident lifecycle | ✅ | Validated state machine |
| Agent investigation | ✅ | Four deterministic agents; two findings required |
| Remediation approval | ✅ | Explicit approve/reject decision |
| Checkpointed execution | ✅ | Execution, recovery, verification, and checkpoint/rollback |
| After-action report | ✅ | Time, agents, confidence, and service health |
| Accessibility settings | ✅ | Text scale, high contrast, reduced motion, audio, left-handed layout |
| Local persistence | ✅ | Settings and run history in browser storage |
| Desktop preview | ✅ | Mouse review mode only |
| Static production build | ✅ | Generated in `web/dist/` |
| HTTPS deployment URL | ⬜ | Must be added before submission |
| Demonstration video | ⬜ | Must be recorded and uploaded before submission |
| Physical headset verification | ⬜ | Must be completed before submission |

## 6. Test report

Verified locally in the implementation environment:

- TypeScript compile check — passed.
- Domain test suite — **12/12 passed**.
- Production build — passed.
- Browser smoke test — not yet performed.
- Meta Quest Browser headset test — not yet performed.

Relevant test file:

- `web/tests/domain.test.ts`

## 7. Build manifest

`submission/BUILD_MANIFEST.json` describes the WebXR build. Before submission, replace the placeholder URL values in a copy or the file itself with:

- Public HTTPS URL.
- Video URL.
- Build timestamp.
- Commit or release identifier.
- Production asset checksum if desired.

## 8. Known limitations

- Physical Quest Browser behavior has not yet been verified.
- Desktop preview is not equivalent to immersive hand tracking.
- The C#/Unity tree is design reference material only and is not a buildable APK path.
- Browser storage may be unavailable in private browsing or restricted contexts; the app falls back to in-memory state.
- The production JavaScript chunk is larger than 500 KB before compression and triggers a Vite warning, although the gzipped size is approximately 157 KB.

## 9. Screenshots and video

Prepare the following before submission:

1. Welcome screen.
2. Scenario selection.
3. 3D CASCADE workspace.
4. Agent findings.
5. Remediation approval.
6. Incident resolved / after-action report.

Record a public demonstration video under three minutes. Upload it to YouTube or Vimeo and add the public URL to the submission form and build manifest.

## 10. Submission assets

- `web/dist/` — deployable static build.
- `submission/JUDGE_INSTRUCTIONS.md`
- `submission/DEMO_SCRIPT.md`
- `submission/COMPLIANCE_MATRIX.md`
- `submission/DEVPOST_SUBMISSION_FORM.md`
- `submission/BUILD_MANIFEST.json`
- `CHANGELOG.md`

## 11. Release information

- Version: 1.0.0
- Platform: WebXR / Meta Quest Browser
- Input model: hand tracking; controllers not required
- Competition deadline: 18 November 2026
- License: internal / competition submission
