# Design System — FAULTLINE XR

Tokens, gradients, typography, icons, motion. Applied to XR panels/HUD per module
Presentation (§10). Referenced by assets once Unity is installed.

## 1. Colour tokens

| Token | Usage | Hex |
|-------|-------|-----|
| Void | Deep background, void space | #070B12 |
| Obsidian | Panel base, elevated surfaces | #0D1420 |
| Graphite | Secondary panels, subtle dividers | #152131 |
| Slate | Surface borders, input fields | #26364A |
| Ice | Primary text on dark | #EAF3FF |
| Mist | Secondary text, disabled states | #91A4BA |
| Signal Cyan | Success, healthy nodes, info accents | #41D9F5 |
| Electric Blue | Primary actions, selected state | #537EFF |
| Violet | Agent/automation accents | #9C83FF |
| Amber | Warning, degraded nodes, cautions | #FFBE55 |
| Critical Red | Critical incidents, failures | #FF526A |
| Recovery Green | Resolved, healthy-after-recovery | #54D99A |

### Semantic mapping

- Status healthy → Signal Cyan / Recovery Green
- Status degraded → Amber
- Status failed / critical → Critical Red
- Primary CTA → Electric Blue
- Agent/automation hint → Violet
- Body text on panel → Ice / Mist

## 2. Gradient system

Gradients are used sparingly: panel headers, glow hints, and progress fills.
Registered definitions live in `Faultline.DesignSystem.GradientRegistry`.

| Gradient | Stops | Angle | Use |
|----------|-------|-------|-----|
| Panel depth | Void → Obsidian → Graphite | 90° | Elevated panel tops |
| Primary signal | Signal Cyan → Electric Blue | 135° | Primary action glow |
| Intelligence | Violet → Electric Blue | 135° | Agent/automation hint |
| Critical incident | Critical Red → Void | 90° | Incident alert pulse |
| Recovery | Recovery Green → Signal Cyan | 135° | Resolved / healthy fill |
| Healthy Pulse | Signal Cyan (20%) → transparent | — | Healthy node glow |
| Critical Glow | Critical Red (30%) → transparent | — | Incident alert glow |
| Ambient Void | Void → Obsidian | 90° | World-space background fade |

Gradients are data structures (start colour, end colour, optional mid stop,
angle); Presentation builds the actual Unity gradient asset at runtime.

## 3. Typography scale

| Token | Size | Weight | Font family | Use |
|-------|------|--------|-------------|-----|
| Hero | 36–44 pt | SemiBold (700) | Space Grotesk | Scene titles, large HUD headers |
| Screen title | 28–32 pt | SemiBold (600) | Space Grotesk | Panel headings, screen titles |
| Section | 22–24 pt | SemiBold (600) | Inter | Section titles within panels |
| Panel | 18–20 pt | Medium (500) | Inter | Panel body labels, list items |
| Body | 16–18 pt | Regular (400) | Inter | Primary body text |
| Secondary | 14–16 pt | Regular (400) | Inter | Secondary body text, captions |
| Technical labels | 12–14 pt | Regular (400) | JetBrains Mono | Technical labels, timestamps, data values |

All text: sans-serif, high legibility at distance in VR (avoid thin weights below 400).

## 4. Icon glyph list

FL_* icon glyphs, each rendered per visual state (Normal / Hover / Selected /
Disabled / Critical) via signed-distance-field font or sprite atlas. The full
registry lives in `Assets/Faultline/Data/GlyphRegistry.json` and
`Faultline.DesignSystem.GlyphRegistry`.

| Glyph | Meaning |
|-------|---------|
| FL_NODE | Infrastructure service node |
| FL_DATABASE | Database / data store node |
| FL_GATEWAY | Gateway / entry point |
| FL_AUTH | Authentication / identity |
| FL_QUEUE | Message queue |
| FL_WORKER | Worker / background process |
| FL_NETWORK | Network link / dependency edge |
| FL_TRACE | Distributed trace span |
| FL_METRIC | Metric sample / gauge |
| FL_LOG | Log event |
| FL_AGENT | Autonomous agent |
| FL_ORACLE | Oracle / external data feed |
| FL_SENTRY | Sentry / watcher alert |
| FL_FORGE | Forge / build or deploy action |
| FL_APPROVE | Approve action |
| FL_REJECT | Reject action |
| FL_WARNING | Warning / degraded status |
| FL_FAILURE | Failure / critical status |
| FL_RECOVERY | Recovery / remediated status |
| FL_VERIFY | Verify / check action |
| FL_TIMELINE | Timeline / event history |
| FL_SETTINGS | Settings / configuration |
| FL_ACCESSIBILITY | Accessibility / comfort controls |
| FL_PAUSE | Pause / suspend action |

Icons scale with distance; minimum touch/gesture target 4.5 cm at arm's length.

## 5. Motion principles

- No camera shake; movement is user-driven (head/hands) only.
- Fade-in/fade-out for panels: 200–300 ms ease-out.
- Node status transitions: soft pulse, never strobe.
- Glow hints for interactable targets: 1.5 s slow breathe on first appearance.
- Spatial UI locks to world anchors; follows head with slight lag (deadzone) to avoid nausea.
- All motion can be reduced via an accessibility toggle (comfort mode).