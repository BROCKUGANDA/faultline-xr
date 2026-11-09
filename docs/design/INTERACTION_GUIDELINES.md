# Interaction Guidelines — FAULTLINE XR

Controller-free hand interaction for Meta Quest / Meta VR Glasses. Derived from
PRD §3.3 (contracts) and the XR/Interaction module responsibilities.

## 1. Hand gestures

| Gesture | Action | Behaviour |
|---------|--------|-----------|
| Pinch (thumb + index touch) | Select / activate | Brief pinch = tap equivalent on nearest hover target. Confirms button, selects node, opens panel. |
| Pinch-hold | Grab / drag | Hold pinch on a grabbable (node, panel edge, anchor) to move it in world space; follows hand with damping. |
| Release (open fingers) | Drop / let go | Ends grab; drops node/panel at current position; snaps to nearest valid anchor if within threshold. |
| Open palm (fingers extended) | Deselect / clear | Clears current selection; closes transient hover menus; resets grab state. |
| Two-hand scale | Resize / zoom | Pinch with both hands and move apart/together to scale a panel or the infrastructure graph view. |
| Point / gaze | Aim / hover | Index finger extended or head gaze ray highlights nearest target; dwell triggers tooltip after confidence threshold. |

## 2. Hysteresis, debounce, confidence

- Pinch activation requires confidence ≥ 0.7 for ≥ 60 ms (debounce) to avoid false taps from hand jitter.
- Grab requires sustained pinch ≥ 120 ms before translating to drag (hysteresis between select and grab).
- Release-to-drop has a 100 ms grace window; re-pinch within it resumes the same grab.
- Two-hand scale ignores radial changes < 2 cm to suppress micro-tremor.
- Point/gaze dwell: 800 ms at ≥ 0.6 confidence before tooltip appears.
- All thresholds tunable per accessibility profile (comfort mode lowers sensitivity).

## 3. Tracking loss behaviour

- When hand tracking is lost: active grab auto-releases (drops held object); selection clears; UI shows a transient "tracking lost" hint.
- No state mutation occurs during tracking loss — commands submitted before loss complete or are rejected cleanly.
- On tracking regain: previous hover target is not auto-reacquired; user must re-pinch to reselect.
- Falls back gracefully to controller input if a paired controller is present (controller-free remains the primary requirement).

## 4. Controller-free requirement

- Primary input: hand tracking via Meta XR SDK (OpenXR).
- No gameplay interaction depends on a controller. Controllers, if present, are secondary/alternative only.
- All interactive targets meet the 4.5 cm minimum gesture target at arm's length and provide visual hover feedback.