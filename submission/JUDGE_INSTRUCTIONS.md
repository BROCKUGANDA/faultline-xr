# Judge Instructions — FAULTLINE XR

## What you are testing

FAULTLINE XR is a deployable **WebXR** experience for Meta Quest. It is not an APK and does not require a developer-mode install.

## Prerequisites

- Meta Quest headset with Meta Quest Browser.
- A secure HTTPS deployment URL for `web/dist/`.
- Hand tracking enabled or permission available.
- No controller required.

## Open the experience

1. Open the deployed URL in **Meta Quest Browser**.
2. Wait for the FAULTLINE XR launch screen.
3. If a VR session has been prepared, tap **Enter VR**.
4. Grant hand-tracking permission when requested.
5. If hand tracking is unavailable, use **Desktop preview** for a basic flow review. This is not a substitute for immersive testing.

## CASCADE scenario

1. Tap **Begin operation**.
2. Optionally open **How pinch works** to review the gesture model.
3. Open **CASCADE** and tap **Begin CASCADE**.
4. Inspect the service graph. Database is down and App Service is failing.
5. Tap **Open incident** to begin the investigation.
6. Dispatch at least two of **ORACLE**, **TRACE**, **SENTRY**, and **FORGE**.
7. Review the findings and confidence values.
8. Tap **Synthesise findings**.
9. Tap **Propose RestartService**.
10. Review the approval card and tap **Approve remediation**.
11. Tap **Execute remediation**.
12. Tap **Verify recovery**.
13. Tap **Resolve incident**.
14. Open the after-action report. Use **Run again** to restart the deterministic scenario.

## Hand interactions

| Action | Gesture |
| --- | --- |
| Select a button or node | Pinch thumb and index finger |
| Drag a panel | Pinch a panel and move the hand |
| Scale the graph | Pinch with both hands and spread them |
| Rotate the graph | Pinch empty space and drag |
| Release | Open the hand |

## Expected behavior

- Every critical workflow action can be completed without a controller.
- The scenario is deterministic and reproducible.
- Two agent findings are required before findings can be synthesised.
- The proposed restart requires an explicit approval.
- Database health recovers after execution and verification.
- The incident can then be resolved and the after-action report viewed.

## Troubleshooting

| Symptom | Likely cause | Action |
| --- | --- | --- |
| No **Enter VR** button | WebXR is not available in the current browser or session preparation failed | Use Meta Quest Browser and confirm the page is served over HTTPS |
| Hand tracking is not visible | Permission was denied or hand joints were not exposed | Grant permission and relaunch the experience |
| Pinch does not register | Hand is too far from the tracked volume or tracking is lost | Hold the hand in the headset view, palm visible |
| Page shows desktop preview unexpectedly | No immersive-vr session was created | Open the URL in Quest Browser rather than a normal desktop browser |
| Static hosting returns a 404 | `web/dist/` contents were not published as the site root | Ensure `index.html` is at the root and `assets/` is alongside it |

## Desktop fallback

Desktop preview uses mouse aim and click. It is useful for reviewing the flow and menus, but the official interaction check should be performed in Quest Browser with hand tracking.
