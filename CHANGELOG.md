# Changelog

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
