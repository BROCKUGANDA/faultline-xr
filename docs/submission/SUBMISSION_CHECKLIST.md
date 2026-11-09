# Submission Checklist — FAULTLINE XR

PRD §14 deliverables. Status as of 2026-11-08 (PHASE 1 scaffolding only).
Competition deadline: **18 November 2026**.

| # | Deliverable (PRD §14) | Status | Notes |
|---|-----------------------|--------|-------|
| 1 | Unity project opens in Unity 6 LTS + URP | ⬜ | Unity not installed yet |
| 2 | Android ARM64 APK builds successfully | ⬜ | JDK/NDK + Meta XR SDK needed |
| 3 | APK installs and runs on Meta Quest / Meta VR Glasses | ⬜ | No headset available yet |
| 4 | OpenXR session initializes; hand tracking works | ⬜ | PHASE 7 |
| 5 | Scenario runs end-to-end (incident → agent → remediation → resolved) | ⬜ | PHASE 2–6, 10 |
| 6 | Controller-free interaction (pinch, grab, release, scale, point/gaze) | ⬜ | PHASE 8 |
| 7 | Design system applied (tokens, gradients, typography, icons, motion) | ⬜ | PHASE 10 |
| 8 | Error taxonomy FL-[DOMAIN]-[NUMBER] emitted in logs/events | ⬜ | PHASE 11 |
| 9 | Unit + integration + XR + E2E tests pass | ⬜ | PHASE 14 |
| 10 | Build guide reproduced by a second person | ⬜ | docs/engineering/BUILD_GUIDE.md |
| 11 | Documentation complete (architecture, design, decisions, QA, known limitations) | 🛠️ | PHASE 1 scaffolding done |
| 12 | Submission build archived before 2026-11-18 | ⬜ | Deadline |

## Pre-submission gates

- [ ] APK installs on headset and launches to the scenario HUD
- [ ] No uncaught exceptions in a full scenario run
- [ ] All FL- error codes documented and reachable
- [ ] Build guide steps verified by someone who did not write them
- [ ] CHANGELOG.md updated through submission