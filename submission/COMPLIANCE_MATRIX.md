# Compliance Matrix — FAULTLINE XR

Last reviewed: 2026-11-09 against the official Meta VR Start Developer Competition 2026 page.

| Competition requirement | FAULTLINE XR response | Status | Evidence |
| --- | --- | --- | --- |
| Build for Meta VR devices | Deploy `web/dist/` as an HTTPS WebXR site and open it in Meta Quest Browser | Implemented | `web/src/main.ts`, `web/index.html` |
| Choose one track | Productivity | Selected | `submission/DEVPOST_SUBMISSION_FORM.md` |
| Choose one division | New Experience | Selected | `submission/DEVPOST_SUBMISSION_FORM.md` |
| Complete the experience with hands | WebXR hand joints and pinch gestures are the primary input | Implemented | `web/src/xr/hands.ts`, `web/src/xr/interaction.ts` |
| Controllers not required | Every workflow action is reachable by pinch or panel drag | Implemented | `web/src/ui/screens.ts`, `web/src/app.ts` |
| Seated, stationary use | Spatial layout is designed for seated operation and a limited reach envelope | Implemented | `web/src/app.ts`, `web/src/scene.ts` |
| Fast, complete short session | The full CASCADE workflow is designed to complete in approximately five minutes | Implemented | `web/src/app.ts` |
| Original, entrant-built experience | Simulation, agents, interaction, and presentation are implemented in this codebase | Implemented | `web/src/domain.ts`, `web/src/scene.ts` |
| No wrapper around a third-party service | Runtime simulation is local and deterministic; no external API or cloud call is used | Implemented | `web/src/domain.ts` |
| Playable build | Vite generates `web/dist/` with `index.html` and bundled assets | Built locally | `web/package.json`, `web/dist/` |
| WebXR submission path | Static HTTPS deployment is the submission path | Implemented | `README.md`, `submission/JUDGE_INSTRUCTIONS.md` |
| Demonstration video | Script is prepared; public upload is still required | Pending | `submission/DEMO_SCRIPT.md` |
| Complete submission form | Copy is prepared for Devpost | Pending submission | `submission/DEVPOST_SUBMISSION_FORM.md` |
| Track and division fields | Product copy specifies Productivity / New Experience | Prepared | `submission/DEVPOST_SUBMISSION_FORM.md` |
| Inspiration, build approach, and future plans | Submission form copy is written within the requested length | Prepared | `submission/DEVPOST_SUBMISSION_FORM.md` |
| Hand-interaction explanation | Submission form copy describes the implementation | Prepared | `submission/DEVPOST_SUBMISSION_FORM.md` |
| Public video link | Must be supplied after recording | Pending | `submission/BUILD_MANIFEST.json` |
| Public HTTPS build URL | Must be supplied after deployment | Pending | `submission/BUILD_MANIFEST.json` |
| Team details | Must be supplied by the entrant | Pending | `submission/DEVPOST_SUBMISSION_FORM.md` |
| Physical headset verification | WebXR code is present, but no physical Quest test has yet been recorded | Pending | `docs/qa/KNOWN_LIMITATIONS.md` |

## Legacy Unity path

The prior Unity/C# tree is not a buildable Unity project and has no APK. It is not the competition submission path and should not be presented as one. The WebXR implementation is the source of the submittable product.

## Remaining submission checklist

1. Complete a full flow test in Quest Browser.
2. Deploy `web/dist/` to HTTPS.
3. Record and upload the demo video.
4. Add public URLs to the submission form and build manifest.
5. Add team member names, emails, and roles.
6. Submit before the competition deadline.
