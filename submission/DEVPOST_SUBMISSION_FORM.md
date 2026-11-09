# Devpost Submission Form — FAULTLINE XR

Copy the fields below into the official submission form. Replace the placeholders before submitting.

## Submission name

FAULTLINE XR

## Submission tagline

A hands-first incident-response simulator that turns a cascading infrastructure failure into a five-minute spatial rescue.

## Track

Productivity

## Division

New Experience

## Public WebXR URL

`[PUBLIC_HTTPS_URL]`

## Demonstration video URL

`[PUBLIC_YOUTUBE_OR_VIMEO_URL]`

## Description

FAULTLINE XR is a hands-first incident-response experience for Meta Quest. You are placed in front of a spatial infrastructure graph during a cascading service failure. Database is down, App Service is failing, and dependent services are beginning to degrade. Your job is to diagnose the failure, gather corroborating evidence, approve a safe repair, verify recovery, and close the incident.

The full loop is deliberately short and complete. You open the incident, dispatch at least two of four deterministic analyst agents, review evidence and confidence, synthesise the findings, propose a `RestartService`, explicitly approve or reject it, execute it, verify the recovery, and resolve the incident. An after-action report summarizes run time, agents used, confidence, and final service health.

The experience is built around natural hand interaction rather than controllers. You pinch to select nodes and controls, drag panels within a seated reach envelope, rotate the graph, and spread two pinched hands to scale it. Accessibility settings offer text scaling, high contrast, reduced motion, optional audio cues, and a left-handed layout.

FAULTLINE XR is implemented as a deployable WebXR experience using TypeScript, Vite, and Three.js. The simulation is local and deterministic, so no API key, account, external LLM, or network dependency is required during play.

## How hand interactions are implemented

The renderer requests an `immersive-vr` session with the WebXR `hand-tracking` feature. WebXR hand joints provide thumb-tip, index-tip, finger-proximal, and wrist positions. A distance-based pinch detector uses separate activation and release thresholds, producing a stable pinch ray from the index finger.

Each frame, the interaction manager raycasts from the active hand origin along its direction. Hits on canvas-backed panels are mapped from intersection UVs into panel-local coordinates for button targeting; hits on graph nodes select those services. The same coordinator supports hover feedback, button activation, panel dragging, empty-space graph rotation, and two-hand graph scaling. A mouse pointer is included only for desktop review and is not part of the immersive interaction model.

Tracking loss suppresses interaction; an untracked hand does not trigger an action. The seated layout keeps panels and controls within a limited reach envelope.

## Why this matters

Infrastructure incidents are cognitively demanding and time-sensitive. FAULTLINE XR turns that environment into a spatial reasoning exercise: the graph shows blast radius, dependency direction, and recovery at a glance. Making approval and verification explicit also reflects responsible incident operations rather than a game that auto-resolves the problem.

The deterministic local agents make the experience reproducible and offline-friendly. The next iteration can add more scenarios, richer agent roles, and deeper evidence types while retaining the same compact, hands-first interaction model.

## Target launch date

To be confirmed by the entrant.

## Team members

| Name | Email | Role |
| --- | --- | --- |
| `[NAME]` | `[EMAIL]` | `[ROLE]` |

## Adapted / significantly updated summary

Not applicable. This is a **New Experience** submission.

## Legacy implementation note

Older C# and Unity-oriented planning files remain in the repository as design reference. They do not represent a compiled Unity build or an APK. The WebXR implementation in `web/` is the competition submission.

## Screenshot captions

1. Welcome screen with the hands-first entry point.
2. CASCADE scenario selection.
3. Spatial infrastructure workspace during the incident.
4. Agent findings and confidence.
5. Remediation approval.
6. Resolved incident and after-action report.
