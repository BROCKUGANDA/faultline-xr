# FAULTLINE XR: 500-Point Product, Production and Enterprise Engineering Playbook
**Version:** 1.1 (refactored)  |  **Prepared:** 9 November 2026  |  **Refactored:** 9 November 2026  |  **Competition deadline:** 18 November 2026, 12:00 PM Pacific (11:00 PM Uganda time)
**Purpose:** A traceable opportunity backlog, production-readiness cheatlist, distributed-systems blueprint and execution prompt for the FAULTLINE XR coding agent.
> **Reality check:** No feature list or prompt can guarantee a hackathon win. The official competition uses four equally weighted judging dimensions: Innovation & Creativity, Experience Design, Technical Implementation, and Polish & Presentation. This playbook optimizes for demonstrable evidence against those dimensions. It is not an instruction to implement all 500 items before the deadline.
## How to use this document

## Repo reconciliation (2026-11-09) — READ FIRST
This playbook was written against a Unity-native assumption. The repository has since pivoted:
- **Submission path is the WebXR static build in `web/`** (TypeScript, Vite, Three.js; runs in Meta Quest Browser via WebXR hand tracking; desktop preview mode for flow review). See `IMPLEMENTATION_STATUS.md`.
- The C# tree under `Assets/Faultline/` and `Tests/` is retained as reference material only; Unity compile is blocked (no Unity 6 LTS installed) and the Unity path is **not** the submission path.
- Phase 0 and Phase 2 artifacts already exist in `docs/` (`ENVIRONMENT_AUDIT.md`, `SYSTEM_ARCHITECTURE.md`, `DOMAIN_MODEL.md`, `DESIGN_SYSTEM.md`, `ERROR_CATALOGUE.md`, `KNOWN_LIMITATIONS.md`). The missing Phase 1 artifact is `REQUIREMENT_TRACEABILITY.md`.
- Remaining release blockers per `IMPLEMENTATION_STATUS.md`: public HTTPS deployment (GitHub Pages workflow `deploy-pages.yml` exists but runs on a self-hosted runner), public video link, physical Quest headset verification, and the Devpost submission form.
- Consequences: wherever this document says Unity Editor, APK, ARM64, PlayMode tests or Meta XR SDK, translate to the WebXR equivalents (Vite build in `web/dist/`, HTTPS-hosted static site, browser storage persistence, `npm test`, Quest Browser verification). The distributed-systems, domain-integrity, agent-safety and honesty guidance applies unchanged.
1. Treat the **NOW** items as candidate work for the competition release. Even within NOW, ship only what strengthens the complete end-to-end slice.
2. Treat **AFTER CORE** as work to consider only after the P0 journey is stable.
3. Treat **LATER** as post-submission roadmap work. Do not build it at the expense of testing, packaging or the demo.
4. Before implementation, inspect the actual repository and map each item to existing code. Do not duplicate working capabilities.
5. Every item must receive a status: `NOT_ASSESSED`, `PLANNED`, `IN_PROGRESS`, `BLOCKED`, `DONE_VERIFIED`, `DEFERRED`, or `NOT_APPLICABLE`. `DONE_VERIFIED` requires evidence.
6. Keep a traceability matrix: item ID → requirement → code/files → tests → evidence → status.
## Prioritization rules
- **P0 / release blocker:** necessary for eligibility, launch, hands-first use, scenario completion, correctness, safety, judge access or submission.
- **P1 / quality amplifier:** improves the demonstrated loop, clarity, comfort, reliability or polish without destabilizing P0.
- **P2 / post-competition:** enterprise, multiplayer, hardware, integrations and growth features that are not required for a credible competition build.
- **Do not equate item count with quality.** A fully tested 40-feature product with a coherent interaction loop beats 500 half-implemented controls.

## 01. Product thesis, competition fit and scope control [NOW]
001. **[NOW]** Write a one-sentence product promise that states the user, problem, spatial interaction and measurable outcome; reject descriptions that could describe an ordinary dashboard.
002. **[NOW]** Name the primary user as an SRE/platform engineer and document the first job-to-be-done: understand and safely resolve one simulated cascading incident.
003. **[NOW]** Choose Productivity as the primary track and record the rationale against the official track definitions; do not claim eligibility or category awards are guaranteed.
004. **[NOW]** Map the experience explicitly to Innovation, Experience Design, Technical Implementation, and Polish & Presentation; create evidence for each criterion.
005. **[NOW]** Select Best Agentic Interaction as a secondary target because agents investigate context and assist decisions; keep the human in control of consequential actions.
006. **[NOW]** Treat Best First Five Minutes as a design objective: deliver a meaningful incident insight and a satisfying interaction before the fifth minute.
007. **[NOW]** Treat Accessibility Forward as an engineering objective, not a late visual toggle; provide multiple ways to understand state and interact.
008. **[NOW]** Make the reason this must be spatial explicit: dependency topology, reach-based inspection, scale changes, spatial causality and hands-on intervention must matter.
009. **[NOW]** Write down what must work offline: app launch, scenario start, telemetry, investigation, approval, simulation, verification and results.
010. **[NOW]** Create a P0/P1/P2 scope register; P0 is the one complete polished incident loop, P1 improves it, P2 is post-competition.
011. **[NOW]** Maintain a strict feature freeze date and reserve the final days for test, packaging, video, submission and rollback to a known-good build.
012. **[NOW]** Confirm eligibility requirements, Meta Developer Access, Start membership and country eligibility directly from official sources before relying on prize assumptions.
013. **[NOW]** Confirm the required submission format and release-channel steps for the actual engine; document who will perform account-only actions.
014. **[NOW]** Record whether the entry is New or Adapted/Significantly Updated and preserve a truthful development history that satisfies the chosen division.
015. **[NOW]** Create a competition compliance matrix for permitted SDKs, public availability, third-party assets, licences, content restrictions and proof of original work.
016. **[NOW]** Use one primary scenario with deterministic replay instead of several thin scenarios; only add another when the first is complete and tested.
017. **[NOW]** Write three judge personas and the question each will ask; make the application answer those questions through the experience, not only the pitch.
018. **[NOW]** Create a kill list of attractive but deadline-dangerous work such as multiplayer, live cloud control, complex hardware and custom networking.
019. **[NOW]** Define a product boundary: competition simulation is not a production incident-response control plane and must never imply that live infrastructure was changed.
020. **[NOW]** Set measurable success goals for completion time, interaction discoverability, scenario repeatability, crash-free runs and judge comprehension.

## 02. First-run experience, onboarding and core user journey [NOW]
021. **[NOW]** Design a cold-start path that reaches the first meaningful spatial scene quickly and never waits on an optional network service.
022. **[NOW]** Create a one-screen value explanation using a concrete incident rather than a long feature list or generic AI claims.
023. **[NOW]** Build a progressive hand tutorial that teaches target, pinch, release and inspect in context; avoid a wall of text.
024. **[NOW]** Provide an optional skip tutorial route while retaining discoverable contextual hints and a way to replay the tutorial.
025. **[NOW]** Introduce the user to the infrastructure through one clear focal object and progressively reveal supporting services.
026. **[NOW]** Create a first-five-minute script with timed milestones and instrument each milestone for usability analysis.
027. **[NOW]** Make the opening incident visible and understandable before asking the user to learn a complex graph vocabulary.
028. **[NOW]** Give users a safe sandbox state before the first high-impact simulation action; never punish exploratory gestures.
029. **[NOW]** Create a guided first incident that exposes a cause, an impact path, an agent finding and a verified recovery.
030. **[NOW]** Keep the total primary flow completable in five to eight minutes, with a shorter judge path that still shows the product loop.
031. **[NOW]** Add a persistent orientation cue showing current scenario, incident state and current task without dominating the view.
032. **[NOW]** Make Back, Close, Cancel, Reset and Exit behaviour predictable across every modal and interaction state.
033. **[NOW]** Prevent users from becoming trapped in a panel, modal, tutorial or agent wait state; provide an escape route.
034. **[NOW]** Make progress visible through meaningful milestones such as Cause Found, Evidence Confirmed and Recovery Verified.
035. **[NOW]** Provide a repeat-scenario action that resets data and state deterministically without requiring an application restart.
036. **[NOW]** Add a concise end-of-run explanation that states what happened, why the chosen action worked and what remained uncertain.
037. **[NOW]** Ensure the user can complete the full journey seated and without walking around the room.
038. **[NOW]** Use contextual help only when relevant; dismiss it automatically after demonstrated mastery but preserve a help entry point.
039. **[NOW]** Add a first-run preference step for handedness, interaction comfort, text scale and motion sensitivity where practical.
040. **[NOW]** Run a first-time-user test with no developer narration; record every moment of confusion and repair the underlying interaction.

## 03. Hands, gaze, controller-free input and interaction reliability [NOW]
041. **[NOW]** Implement a platform-backed hand input adapter using the installed, version-compatible Meta XR APIs; verify every API against current documentation.
042. **[NOW]** Create a central interaction state machine with Idle, Hovered, Focused, Selected, Held, Released, Cancelled and Lost states.
043. **[NOW]** Debounce pinch transitions and apply hysteresis so tracking noise does not generate repeated selections.
044. **[NOW]** Require a stable target and minimum input confidence before starting a destructive-looking or consequential interaction.
045. **[NOW]** Separate target acquisition from command execution so a hand gesture cannot accidentally approve a remediation.
046. **[NOW]** Provide hover, focus, selection, hold and release feedback through consistent visual and optional haptic cues.
047. **[NOW]** Add a large hand-interactable button alternative for every gesture-dependent critical action.
048. **[NOW]** Implement hand dominance and mirrored placement without duplicating business logic.
049. **[NOW]** Support seated reach envelopes and avoid requiring overhead, far-side or behind-the-user gestures.
050. **[NOW]** Create gaze-assisted target acquisition only where supported by the selected device and SDK; never assume eye tracking exists.
051. **[NOW]** Use dwell only for low-risk focus or navigation; require explicit confirmation for approval and execution.
052. **[NOW]** Implement tracking-loss handling that cancels unsafe pending actions and preserves the user's current task.
053. **[NOW]** On tracking recovery, do not replay a gesture or auto-execute a previously pending command.
054. **[NOW]** Add input calibration and a simple interaction sensitivity setting without exposing confusing engineering controls.
055. **[NOW]** Prevent hand rays, cursors and focus indicators from obscuring evidence or small but important UI labels.
056. **[NOW]** Create interaction timeout behaviour that visibly explains when a hold or pinch was not recognized.
057. **[NOW]** Handle two-hand pinch and scaling only if it is stable, discoverable and not required to finish the scenario.
058. **[NOW]** Test rapid repeated pinches, target switching, occlusion, one-hand operation and hands leaving the tracking volume.
059. **[NOW]** Add an input diagnostics view showing tracking availability and recognized interaction state for developers, not normal users.
060. **[NOW]** Prove the entire P0 flow with controllers unavailable and document which simulator input is only a developer aid.

## 04. Spatial infrastructure visualization and topology UX [NOW]
061. **[NOW]** Build a legible spatial topology where node position and edge direction communicate actual dependency relationships.
062. **[NOW]** Give each service a stable ID, display name, service class, health state and selection affordance.
063. **[NOW]** Represent upstream and downstream dependencies with consistent edge direction, arrowheads and labels where needed.
064. **[NOW]** Use spatial depth to distinguish system layers without forcing users to rotate their head excessively.
065. **[NOW]** Provide a compact overview and an expanded inspection mode with a deliberate transition between them.
066. **[NOW]** Use layout rules that minimize edge crossings and node overlap for the known scenario graph.
067. **[NOW]** Add a deterministic auto-layout that preserves recognizable positions across reset and replay.
068. **[NOW]** Implement safe node selection that remains stable when the topology animates or telemetry changes.
069. **[NOW]** Show the dependency path affected by the selected service and de-emphasize unrelated branches.
070. **[NOW]** Make critical path tracing visually distinct from ordinary data flow without relying on colour alone.
071. **[NOW]** Provide node clustering for worker replicas or repeated service instances, with expansion into individual nodes.
072. **[NOW]** Support zoom and scale controls with bounded limits and a clear reset-to-overview action.
073. **[NOW]** Keep essential node labels readable at the supported viewing distance and avoid tiny floating text.
074. **[NOW]** Prevent topology objects from intersecting the user's near field or clipping through contextual panels.
075. **[NOW]** Use line thickness, pattern, shape and animation cadence to distinguish normal, delayed and failed edges.
076. **[NOW]** Show missing, unknown and stale telemetry as different states from healthy; never silently convert unknown to green.
077. **[NOW]** Provide a textual and structured alternative to the visual graph for users who cannot parse spatial topology.
078. **[NOW]** Add a selected-node breadcrumb or focus marker so users understand where they are in a large graph.
079. **[NOW]** Keep graph rendering separate from graph truth; visuals subscribe to validated domain state and cannot mutate it.
080. **[NOW]** Create a topology visual regression scene with known node positions, edge paths and status combinations.

## 05. Design system, materials, glyphs, sound and motion [NOW]
081. **[NOW]** Create a single token source for colour, spacing, typography, panel depth, radius, animation timing and interaction states.
082. **[NOW]** Use the Obsidian Signal palette consistently: dark neutral environment, cool cyan/blue interaction signals, amber warnings, red faults and green verified recovery.
083. **[NOW]** Define contrast-tested semantic states for Healthy, Degraded, Critical, Unknown, Pending, Running, Failed and Verified.
084. **[NOW]** Establish a typography scale with readable body text and restrained technical monospace labels; test in actual headset scale.
085. **[NOW]** Build reusable materials for default nodes, selected nodes, failed nodes, degraded nodes, evidence links and agent activity.
086. **[NOW]** Create a coherent glyph set for services, incidents, telemetry, agents, approval, rejection, warning, recovery and verification.
087. **[NOW]** Pair every critical icon with a text label or accessible semantic name; do not require icon memorization.
088. **[NOW]** Define panel hierarchy through spacing, luminance, depth and typography rather than excessive glass blur.
089. **[NOW]** Create button states for idle, hover, focus, pressed, disabled, loading and error with measurable visual differences.
090. **[NOW]** Define motion curves and durations for focus, panel reveal, selection, node pulse, agent dispatch and recovery transitions.
091. **[NOW]** Make animation state-driven and cancelable; never let a decorative animation determine whether domain work succeeded.
092. **[NOW]** Add reduced-motion behaviour that replaces large spatial movement with subtle opacity or bounded changes.
093. **[NOW]** Use particle effects only to explain flow, propagation or recovery; cap count and overdraw for performance.
094. **[NOW]** Design a restrained sound palette for focus, selection, dispatch, alert, approval and verified recovery.
095. **[NOW]** Keep audio informational and optional; provide visual equivalents and an independent volume setting.
096. **[NOW]** Create a gradient/material library with a small number of semantic gradients instead of arbitrary per-panel effects.
097. **[NOW]** Add a visual QA catalogue showing all component states against every semantic status.
098. **[NOW]** Audit every screen for layout consistency, spacing, alignment, clipping, overlapping, hierarchy and text truncation.
099. **[NOW]** Verify font and icon licences and record source, version and redistribution permissions for every bundled asset.
100. **[NOW]** Create a branded scene transition and boot/loading composition that remains responsive, respects reduced motion and can be skipped when the underlying operation is ready.

## 06. Domain model, graph algorithms and state integrity [NOW]
101. **[NOW]** Define immutable IDs and validated schemas for ServiceNode, DependencyEdge, Incident, Evidence, AgentTask, Proposal and RecoveryResult.
102. **[NOW]** Separate domain entities from Unity MonoBehaviours, scene objects, prefabs and ScriptableObject presentation assets.
103. **[NOW]** Represent dependencies as a directed graph and document whether each relationship means calls, data flow or operational dependency.
104. **[NOW]** Implement iterative breadth-first traversal for impact analysis with visited-set protection against cycles.
105. **[NOW]** Implement depth-first or equivalent cycle detection and return a useful cycle path for diagnostics.
106. **[NOW]** Reject edges whose source or destination node does not exist; report a typed validation error.
107. **[NOW]** Prevent duplicate nodes and duplicate edges using stable composite keys.
108. **[NOW]** Handle disconnected components and isolated nodes without exceptions or misleading health propagation.
109. **[NOW]** Use explicit status transitions and guard clauses rather than arbitrary string assignments.
110. **[NOW]** Define invariants for graph consistency, incident state, proposal lifecycle and recovery verification.
111. **[NOW]** Make domain operations deterministic for a given input state and seed.
112. **[NOW]** Use bounded numeric ranges and finite-value checks for latency, throughput, error rate and health scores.
113. **[NOW]** Version serialized domain schemas and implement a documented migration or safe reset strategy.
114. **[NOW]** Separate wall-clock timestamps from simulation time so replay remains deterministic.
115. **[NOW]** Use value objects for latency, percentage, severity and correlation identifiers where they prevent unit confusion.
116. **[NOW]** Add domain-level result types that carry success or typed failure without swallowing exceptions.
117. **[NOW]** Ensure event handlers cannot mutate the collection currently being enumerated; snapshot or queue changes safely.
118. **[NOW]** Create invariant tests for every state transition and graph mutation.
119. **[NOW]** Add property-based or generated tests for graph sizes, cycles, duplicate IDs and malformed edges if the toolchain supports them.
120. **[NOW]** Document domain ownership and prohibit the presentation layer from bypassing application commands.

## 07. Incident scenarios, telemetry and realistic failure propagation [NOW]
121. **[NOW]** Implement the CASCADE scenario as the mandatory end-to-end slice with a database latency root cause and visible downstream effects.
122. **[NOW]** Define a scenario manifest with ID, title, description, seed, initial graph, telemetry profile, incident trigger and success criteria.
123. **[NOW]** Generate correlated metrics, logs and traces from the same simulation events rather than unrelated decorative sample data.
124. **[NOW]** Model latency percentiles, error rate, request volume, queue depth and saturation with documented units.
125. **[NOW]** Represent normal baselines and anomaly thresholds so users can distinguish a true incident from ordinary variation.
126. **[NOW]** Create timestamps that preserve causal ordering across request, database, queue and worker events.
127. **[NOW]** Model retries and timeouts as contributors to load amplification instead of making failures magically spread.
128. **[NOW]** Demonstrate dependency impact through request volume, timeout propagation and queue backlog changes.
129. **[NOW]** Add a controllable simulation clock with pause, resume and bounded speed changes for replay and judge demos.
130. **[NOW]** Keep random variation seeded and bounded so the demo never produces a different root cause unexpectedly.
131. **[NOW]** Create a scenario reset that restores all graph state, event queues, agent tasks and timeline events.
132. **[NOW]** Add at least two alternate failure profiles only after CASCADE passes every acceptance test.
133. **[NOW]** Model partial failure and degraded operation, not only binary up/down service states.
134. **[NOW]** Include a misleading but plausible symptom so agents must distinguish a root cause from downstream effects.
135. **[NOW]** Add stale, delayed and missing telemetry events to test uncertainty handling.
136. **[NOW]** Make severity calculation deterministic and explain which signals contributed to severity.
137. **[NOW]** Add a blast-radius estimate based on graph reachability and service criticality.
138. **[NOW]** Store scenario fixtures as version-controlled data rather than constructing all content in scene code.
139. **[NOW]** Create golden scenario snapshots for initial state, incident state, proposed fix and verified recovery.
140. **[NOW]** Ensure the recovery path genuinely improves simulated metrics and returns affected dependencies to acceptable state.

## 08. Agent orchestration and multi-agent safety [NOW]
141. **[NOW]** Define ORACLE as coordinator, TRACE as dependency analyst, SENTRY as telemetry analyst and FORGE as remediation strategist.
142. **[NOW]** Use typed agent task inputs and outputs; prohibit free-form text from being treated as executable instructions.
143. **[NOW]** Give each agent a narrow capability contract and minimum required evidence set.
144. **[NOW]** Implement a deterministic local agent baseline so the core demo works without external models or credentials.
145. **[NOW]** Make optional LLM providers adapters behind an interface; never couple scene logic to a provider SDK.
146. **[NOW]** Give every task a unique task ID, incident ID, correlation ID, deadline and lifecycle state.
147. **[NOW]** Prevent duplicate dispatch for the same agent, incident and investigation phase using an idempotency key.
148. **[NOW]** Use cancellation tokens or an equivalent cancellation mechanism for timeouts, user cancellation and scene shutdown.
149. **[NOW]** Validate agent output against strict schemas and reject missing fields, invalid enums and impossible numeric values.
150. **[NOW]** Require evidence references for factual claims and label ungrounded hypotheses explicitly as hypotheses.
151. **[NOW]** Separate agent confidence from probability of correctness and document how confidence is calculated.
152. **[NOW]** Prevent the coordinator from treating an agent timeout as agreement or a successful finding.
153. **[NOW]** Implement bounded retries only for transient provider or transport failures; do not retry schema errors blindly.
154. **[NOW]** Use a deterministic fallback response with an explicit 'local rules engine' label when a model is unavailable.
155. **[NOW]** Preserve the original agent result and validation errors for diagnostics without exposing provider secrets.
156. **[NOW]** Prevent agents from invoking arbitrary C# methods, reflection, shell commands or network destinations.
157. **[NOW]** Make agents propose actions only; a separate policy and approval service decides whether a proposal is valid.
158. **[NOW]** Add an agent status panel for Queued, Running, Waiting, Completed, Failed, Cancelled and Fallback states.
159. **[NOW]** Design agent findings to be concise, evidence-linked and readable in spatial UI without giant chat transcripts.
160. **[NOW]** Test conflicting agent findings and require ORACLE to surface disagreement and uncertainty rather than invent consensus.

## 09. Evidence, telemetry interpretation and explainability [NOW]
161. **[NOW]** Create a unified Evidence model with evidence ID, type, source service, event time, observed time and correlation ID.
162. **[NOW]** Support metric, log, trace, dependency and simulation-state evidence types with explicit schemas.
163. **[NOW]** Make every agent finding cite one or more evidence IDs that exist in the active scenario.
164. **[NOW]** Reject evidence references that are missing, stale beyond the scenario policy or belong to another incident.
165. **[NOW]** Show the difference between observed fact, inferred hypothesis, recommendation and completed action.
166. **[NOW]** Add metric trend views that explain units, time window, baseline and threshold.
167. **[NOW]** Provide log filtering by service, severity, time range and correlation ID.
168. **[NOW]** Provide trace inspection with parent-child spans, duration, error status and service boundaries.
169. **[NOW]** Correlate the same request across API, application and database telemetry using a trace or request ID.
170. **[NOW]** Show stale telemetry distinctly and explain when missing evidence limits the conclusion.
171. **[NOW]** Create a causal timeline that orders scenario events and agent findings without hiding clock skew or uncertainty.
172. **[NOW]** Make root-cause explanations state which evidence supports and which evidence contradicts the hypothesis.
173. **[NOW]** Allow users to open an evidence item from a finding and return to the finding without losing context.
174. **[NOW]** Add an evidence completeness indicator that does not imply certainty when evidence volume is high.
175. **[NOW]** Provide a compact evidence summary and a detailed inspection view for different experience levels.
176. **[NOW]** Track provenance from generated fixture to displayed chart and agent interpretation.
177. **[NOW]** Prevent charts from silently smoothing or dropping outliers that matter to incident diagnosis.
178. **[NOW]** Use accessible labels and tables as alternatives to chart-only communication.
179. **[NOW]** Test missing, duplicated, delayed, contradictory and out-of-order evidence records.
180. **[NOW]** Create a deterministic explanation report that can be saved or shown in the final results screen.

## 10. Remediation, approval, rollback and safety controls [NOW]
181. **[NOW]** Represent each remediation proposal as a typed plan with preconditions, expected effects, affected services and reversibility.
182. **[NOW]** Require the user to inspect evidence and explicitly approve a proposal before simulation execution.
183. **[NOW]** Use a separate approval command service that validates proposal state and active incident identity.
184. **[NOW]** Record approval time, approval ID, proposal hash and scenario ID in the audit timeline.
185. **[NOW]** Expire stale proposals when the graph or relevant telemetry changes materially.
186. **[NOW]** Make approve, reject, inspect and cancel controls mutually clear and resistant to accidental activation.
187. **[NOW]** Use a second explicit confirmation for high-impact simulation changes, not a single ambiguous pinch.
188. **[NOW]** Create a simulation checkpoint before executing a remediation action.
189. **[NOW]** Make execution idempotent and reject duplicate execution IDs or already completed proposals.
190. **[NOW]** Define execution preconditions and fail closed when current state differs from the proposal assumptions.
191. **[NOW]** Execute remediation as deterministic domain commands, never direct UI mutation.
192. **[NOW]** Record every state change as an ordered event with before/after values and causal correlation.
193. **[NOW]** Verify postconditions using actual simulated metrics and graph state rather than a success animation.
194. **[NOW]** Rollback to the checkpoint when an action partially succeeds and recovery policy allows rollback.
195. **[NOW]** Report rollback failure separately and preserve enough state to diagnose it.
196. **[NOW]** Prevent external LLM output from supplying an executable command body or arbitrary parameter values.
197. **[NOW]** Keep all competition remediation confined to local simulated state; explicitly label it as a simulation.
198. **[NOW]** Add rejection flows that explain why the proposal is risky, unsupported or stale.
199. **[NOW]** Test double approval, double execution, cancellation during execution and application pause during execution.
200. **[NOW]** Generate a post-incident report containing approved action, evidence, simulated outcome, rollback status and unresolved uncertainty.

## 11. Distributed systems, reliability patterns and failure engineering [NOW]
201. **[NOW]** Model the product's own runtime as a local-first modular monolith; do not split into microservices without an actual operational need.
202. **[NOW]** Document process boundaries between Unity client, optional backend, optional model provider and any hardware bridge.
203. **[NOW]** Define explicit timeouts for every network call and asynchronous operation that can block user-visible work.
204. **[NOW]** Use bounded exponential backoff with jitter for transient network failures and a strict maximum retry budget.
205. **[NOW]** Do not retry non-idempotent writes unless an idempotency key or deduplication strategy makes replay safe.
206. **[NOW]** Use request IDs, correlation IDs and trace context consistently across UI, domain, adapter and backend boundaries.
207. **[NOW]** Design optional backend outages so local simulation remains fully playable.
208. **[NOW]** Apply bulkhead isolation so model-provider failure cannot stall XR frame updates or core simulation.
209. **[NOW]** Use a circuit breaker only around genuinely remote dependencies and expose Open, Half-Open and Closed states to diagnostics.
210. **[NOW]** Separate fast interaction commands from slower agent work using asynchronous task orchestration.
211. **[NOW]** Bound queue length and define overload behaviour instead of allowing unbounded task accumulation.
212. **[NOW]** Define backpressure behaviour for telemetry generation, event dispatch and agent task creation.
213. **[NOW]** Make event handlers idempotent where delivery or replay can duplicate an event.
214. **[NOW]** Use an outbox pattern only if a backend must atomically persist domain changes and publish events; do not add it to an entirely local MVP.
215. **[NOW]** Use inbox deduplication or processed-event keys if external event delivery can be repeated.
216. **[NOW]** Document ordering guarantees and partition keys before introducing any message broker.
217. **[NOW]** Use monotonic simulation sequence numbers for causal ordering; do not rely solely on wall clocks.
218. **[NOW]** Define graceful degradation levels for model offline, backend offline, persistence unavailable and XR tracking degraded.
219. **[NOW]** Add fault-injection switches for timeout, dropped event, duplicate event, invalid response and persistence failure.
220. **[NOW]** Run chaos-style tests in the deterministic simulator and verify system recovery without confusing simulation faults with app faults.

## 12. API contracts, integration adapters and backend boundaries [AFTER CORE]
221. **[AFTER CORE]** Define versioned API contracts before implementing any optional backend endpoint.
222. **[AFTER CORE]** Use consistent request and response envelopes with request ID, correlation ID and schema version.
223. **[AFTER CORE]** Validate all inbound payloads at the boundary before converting them into domain commands.
224. **[AFTER CORE]** Use explicit HTTP semantics and typed application errors rather than returning HTTP 200 for all failures.
225. **[AFTER CORE]** Define stable error codes and safe user-facing messages for authentication, validation, rate limit, timeout and dependency failures.
226. **[AFTER CORE]** Implement client-side request deadlines and cancellation for every optional remote request.
227. **[AFTER CORE]** Use bounded connection pools and avoid creating a new HTTP client for each request.
228. **[AFTER CORE]** Handle 429 responses using provider guidance and bounded retry scheduling; never spin in a tight loop.
229. **[AFTER CORE]** Handle 5xx responses as potentially transient while treating 4xx validation and permission errors as non-retryable by default.
230. **[AFTER CORE]** Version provider adapters independently from core domain contracts.
231. **[AFTER CORE]** Keep secrets in a secure configuration channel and never bake API credentials into the Unity APK.
232. **[AFTER CORE]** Use local development configuration that is safe to commit and separate from personal secret files.
233. **[AFTER CORE]** Define integration health checks that do not expose credentials or sensitive payloads.
234. **[AFTER CORE]** Use mock adapters for tests and deterministic local adapters for judge demos.
235. **[AFTER CORE]** Validate provider output schema and content size before storing or displaying it.
236. **[AFTER CORE]** Redact tokens, authorization headers, personal information and sensitive provider payloads from logs.
237. **[AFTER CORE]** Document API rate limits, usage budgets, model costs and provider fallback behaviour.
238. **[AFTER CORE]** Protect the user experience from DNS failure, TLS errors, captive portals and transient connectivity loss.
239. **[AFTER CORE]** Add contract tests for every adapter and a compatibility test for the active provider version.
240. **[AFTER CORE]** Do not add Kubernetes, Kafka, Redis or cloud databases to the competition slice unless a demonstrated requirement justifies each dependency.

## 13. Persistence, event history, replay and data lifecycle [AFTER CORE]
241. **[AFTER CORE]** Choose the simplest durable local persistence mechanism compatible with the Unity runtime and document its trade-offs.
242. **[AFTER CORE]** Persist user preferences separately from scenario execution state.
243. **[AFTER CORE]** Version save data and validate it before deserialization into domain objects.
244. **[AFTER CORE]** Write saves atomically using a temporary file or supported transactional mechanism before replacing the previous valid save.
245. **[AFTER CORE]** Handle missing, truncated, corrupted, incompatible and partially written save files safely.
246. **[AFTER CORE]** Provide a recoverable reset path that preserves user preferences unless the user explicitly chooses a full reset.
247. **[AFTER CORE]** Store a scenario seed, scenario version and simulation sequence number for deterministic replay.
248. **[AFTER CORE]** Record ordered domain events needed to reconstruct the incident timeline and recovery outcome.
249. **[AFTER CORE]** Define retention limits for local sessions, evidence and diagnostics so storage does not grow without bound.
250. **[AFTER CORE]** Separate ephemeral telemetry from durable incident summary data.
251. **[AFTER CORE]** Use schema migration tests for every persisted format change.
252. **[AFTER CORE]** Prevent stale async save operations from overwriting a newer session state.
253. **[AFTER CORE]** Use checksums or equivalent integrity detection for critical local save envelopes where useful.
254. **[AFTER CORE]** Avoid storing secrets, raw provider credentials or unnecessary personal information in saves.
255. **[AFTER CORE]** Provide export of a sanitized incident report if it materially helps the product demonstration.
256. **[AFTER CORE]** Make report export resilient to missing optional evidence and invalid filenames.
257. **[AFTER CORE]** Test app suspend/resume during an active incident and restore a coherent state.
258. **[AFTER CORE]** Test low storage and permission-related persistence failure without crashing the simulation.
259. **[AFTER CORE]** Document what is in-memory, what is persisted and what is regenerated from scenario fixtures.
260. **[AFTER CORE]** Create replay tests that compare event sequence and final state hashes for identical scenario seeds.

## 14. Security, privacy, safety and supply-chain assurance [NOW]
261. **[NOW]** Create a lightweight threat model covering local app, external model provider, optional backend, user input, saved data and hardware bridge.
262. **[NOW]** Define trust boundaries and ensure AI-generated text is treated as untrusted data.
263. **[NOW]** Validate lengths, enums, numeric ranges, IDs and nesting depth for every user- or provider-controlled payload.
264. **[NOW]** Never execute model-generated code, shell commands, SQL fragments or arbitrary reflection calls.
265. **[NOW]** Do not expose live infrastructure credentials or production endpoints in the competition build.
266. **[NOW]** Use least privilege for any optional backend credential and restrict it to the minimum required operations.
267. **[NOW]** Keep test credentials, developer tokens and private URLs out of source control and build logs.
268. **[NOW]** Scan dependencies for known vulnerabilities and record package versions and source provenance.
269. **[NOW]** Verify asset licences and third-party SDK redistribution rules before shipping.
270. **[NOW]** Add safe deserialization rules and reject unexpected object types or oversized inputs.
271. **[NOW]** Prevent log injection by normalizing untrusted strings and bounding message size.
272. **[NOW]** Ensure the UI never displays stack traces, access tokens or internal exception dumps.
273. **[NOW]** Provide explicit consent before using a network AI provider and clearly show when remote processing is active.
274. **[NOW]** Use synthetic telemetry only in the demo and label it as simulated; do not present it as real customer infrastructure data.
275. **[NOW]** Minimize analytics collection and avoid collecting voice, gaze or room data unless required and explicitly disclosed.
276. **[NOW]** Provide a privacy notice for any collected diagnostics and document retention and deletion behaviour.
277. **[NOW]** Ensure any voice input path has a visible status and does not record continuously without a clear reason and permission.
278. **[NOW]** Add safe defaults for optional IoT or Bluetooth hardware integration and reject unauthenticated control messages.
279. **[NOW]** Create a release security checklist covering secrets, permissions, debug builds, logs and dependency inventory.
280. **[NOW]** Run a final secret scan and inspect the packaged APK configuration before submission.

## 15. Observability, structured logs, errors and diagnostics [NOW]
281. **[NOW]** Define a central error taxonomy with stable codes, severity, recoverability and user guidance.
282. **[NOW]** Use a consistent error structure containing code, message, correlation ID, timestamp and safe diagnostic context.
283. **[NOW]** Create structured logs with event names and key-value fields rather than relying on free-form text only.
284. **[NOW]** Use a single correlation ID across incident, agent tasks, evidence retrieval, remediation and verification.
285. **[NOW]** Separate user-facing status messages from diagnostic details and raw exception data.
286. **[NOW]** Create an in-app developer diagnostics panel that can be disabled or hidden in the judge-facing experience.
287. **[NOW]** Track scenario start, investigation completion, approval, execution and verification durations.
288. **[NOW]** Track agent success, timeout, fallback and schema rejection counts.
289. **[NOW]** Track graph node and edge counts, active particles and scene object counts for performance diagnosis.
290. **[NOW]** Add a bounded log ring buffer with severity filters and export of sanitized diagnostics.
291. **[NOW]** Define warning and error thresholds that do not spam users or hide repeated failures.
292. **[NOW]** Ensure every exception path either recovers safely or reaches a deliberate fatal-error boundary.
293. **[NOW]** Add explicit loading state timeouts and a useful failure message when a task exceeds the expected duration.
294. **[NOW]** Distinguish a domain failure from an XR runtime failure, an external integration failure and a rendering failure.
295. **[NOW]** Provide an error recovery action such as Retry, Return to Overview, Reset Scenario or Exit where appropriate.
296. **[NOW]** Do not show Retry when the operation is non-retryable or would duplicate a side effect.
297. **[NOW]** Record build version, SDK version, device profile and scenario ID in sanitized diagnostics.
298. **[NOW]** Create a known-error reproduction catalogue with steps and expected recovery.
299. **[NOW]** Test error mapping for unknown exceptions and ensure a safe generic message is shown.
300. **[NOW]** Produce a final QA report that includes known limitations instead of claiming universal reliability.

## 16. XR performance, rendering and resource management [NOW]
301. **[NOW]** Use the official competition performance target as a hard profiling reference; the judging page specifies at least 60 FPS on Quest hardware.
302. **[NOW]** Profile the actual target build and device profile rather than treating Unity Editor frame rate as proof.
303. **[NOW]** Track CPU frame time, GPU frame time, memory, garbage collection and missed frame intervals.
304. **[NOW]** Avoid per-frame allocations in hand interaction, graph updates and telemetry visualizers.
305. **[NOW]** Pool repeated nodes, edge effects, particles and transient UI objects where lifecycle churn is measurable.
306. **[NOW]** Batch compatible geometry and reuse materials to reduce draw calls and state changes.
307. **[NOW]** Limit transparent surfaces and particle overdraw, especially across large spatial panels.
308. **[NOW]** Use level-of-detail or simplified rendering for distant topology nodes when useful.
309. **[NOW]** Throttle noncritical telemetry visualization updates independently of XR frame rendering.
310. **[NOW]** Move parsing, report generation and optional network work off the main thread where Unity API rules permit.
311. **[NOW]** Marshal all Unity object changes to the correct thread and prevent background callbacks from touching destroyed objects.
312. **[NOW]** Use cancellation and lifecycle guards when async tasks outlive scenes or application sessions.
313. **[NOW]** Set a maximum number of simultaneously visible evidence rows, nodes, labels and agent activity effects.
314. **[NOW]** Avoid excessive dynamic lights, shadow casters and expensive post-processing in the headset build.
315. **[NOW]** Use fixed or bounded simulation steps and prevent a long pause from causing a huge catch-up spike.
316. **[NOW]** Measure cold start, scenario load, panel open and first interaction latency.
317. **[NOW]** Implement memory cleanup for scene unload, scenario reset and repeated demo loops.
318. **[NOW]** Test thermal and sustained-session behaviour on available hardware or clearly state simulator-only validation.
319. **[NOW]** Create a performance budget per subsystem and identify the owner when a budget is exceeded.
320. **[NOW]** Capture before/after profiler evidence for every claimed optimization.

## 17. Accessibility, comfort, ergonomics and inclusive interaction [NOW]
321. **[NOW]** Make all P0 tasks usable while seated and within a comfortable reachable interaction volume.
322. **[NOW]** Offer high-contrast status indicators and never communicate failure solely through red or green colour.
323. **[NOW]** Provide text labels, semantic names and non-colour visual symbols for critical states.
324. **[NOW]** Support adjustable text scale without causing essential labels to clip or overlap.
325. **[NOW]** Keep important content within a practical field of view and avoid requiring frequent extreme head turns.
326. **[NOW]** Use stable horizon and bounded spatial transitions; avoid forced camera movement.
327. **[NOW]** Provide reduced-motion mode and avoid flashing or rapidly oscillating alert effects.
328. **[NOW]** Make critical controls large enough to target reliably with hands and provide forgiving hit areas.
329. **[NOW]** Offer alternative controls for users who cannot perform a particular pinch or hold gesture.
330. **[NOW]** Explore gaze or voice alternatives only when supported by hardware, permission and tested API behaviour.
331. **[NOW]** Ensure all audio cues have visual equivalents and all visual alerts can be understood without sound.
332. **[NOW]** Provide pause and resume behaviour that does not penalize users with slower interaction speed.
333. **[NOW]** Avoid time pressure in the core investigation unless it is an explicitly optional challenge mode.
334. **[NOW]** Use plain language for critical explanations and progressively reveal technical terminology.
335. **[NOW]** Avoid colour combinations that become indistinguishable under common colour-vision differences.
336. **[NOW]** Keep interaction hints consistent and remove redundant instruction after the user has mastered a gesture.
337. **[NOW]** Test with different handedness, reach distances, seated postures and hand tracking conditions.
338. **[NOW]** Provide a comfort reset that repositions panels without resetting scenario progress.
339. **[NOW]** Document accessibility limitations honestly and do not claim certification without assessment.
340. **[NOW]** Run an accessibility review of every P0 screen before final release.

## 18. Automated testing, scenario acceptance and regression prevention [NOW]
341. **[NOW]** Create Unity EditMode tests for domain invariants, graph traversal, state transitions and validation errors.
342. **[NOW]** Create PlayMode tests for scene lifecycle, UI flow, spatial components and scenario orchestration.
343. **[NOW]** Create contract tests for agent input/output schemas and evidence references.
344. **[NOW]** Test dependency graphs with zero nodes, one node, disconnected components, cycles and large bounded graphs.
345. **[NOW]** Test incident state transitions for valid paths, invalid jumps and duplicate commands.
346. **[NOW]** Test remediation double-submit, stale proposal, approval expiry and rollback failure.
347. **[NOW]** Test malformed agent output, timeout, cancellation, provider unavailable and deterministic fallback.
348. **[NOW]** Test persistence corruption, schema version mismatch, interrupted save and reset behaviour.
349. **[NOW]** Test tracking loss during hover, pinch, grab, approval and recovery confirmation.
350. **[NOW]** Automate the CASCADE journey to assert final domain state and not merely the presence of UI text.
351. **[NOW]** Create a golden test that verifies the exact initial graph and post-recovery graph for the same seed.
352. **[NOW]** Add build smoke tests that assert the startup scene exists and all required scenes are included.
353. **[NOW]** Run a clean-clone build to detect hidden dependencies on local machine state.
354. **[NOW]** Run tests after each critical subsystem change and before every release candidate.
355. **[NOW]** Use a regression matrix that maps each bug to a test preventing recurrence.
356. **[NOW]** Test repeat-run behaviour for at least ten scenario resets in one app session.
357. **[NOW]** Test pause, resume, focus loss, XR session termination and app shutdown during asynchronous work.
358. **[NOW]** Record test command, environment, build identifier, timestamp and result in machine-readable output.
359. **[NOW]** Mark unexecuted tests as Not Run; do not report them as passing because the test code exists.
360. **[NOW]** Make any failing P0 acceptance test a release blocker unless an explicit documented exception is approved by the user.

## 19. Build engineering, CI, release packaging and submission readiness [NOW]
361. **[NOW]** Pin Unity Editor, package and SDK versions in a reproducible project setup document.
362. **[NOW]** Use a Unity version control strategy that handles scene, prefab and asset metadata safely.
363. **[NOW]** Configure Android ARM64 and only the XR features and permissions actually required by the app.
364. **[NOW]** Set a stable application identifier, product version and build number strategy.
365. **[NOW]** Create development, test and release build configurations without hardcoding sensitive environment values.
366. **[NOW]** Verify all required scenes are included in the build and the correct startup scene is selected.
367. **[NOW]** Create a release build script that logs tool versions, git commit, build settings and output path.
368. **[NOW]** Compute SHA-256 for every release APK and include it in the build manifest.
369. **[NOW]** Keep release artifacts immutable after a successful validation run; create a new version for changes.
370. **[NOW]** Build from a clean working tree or record all uncommitted changes in the manifest.
371. **[NOW]** Run package inspection for permissions, debug flags, symbols and embedded secrets.
372. **[NOW]** Test install, launch, scenario completion and app restart on the closest available target environment.
373. **[NOW]** Maintain a known-good release candidate that can be restored if a late change breaks the build.
374. **[NOW]** Create a release checklist for upload, release channel, tester invitation and judge access.
375. **[NOW]** Verify the invite link using a separate test account or documented equivalent before submission.
376. **[NOW]** Create a submission freeze procedure and ensure no changes are made after the competition deadline.
377. **[NOW]** Retain the exact submitted APK, checksum, manifest and submission evidence for auditability.
378. **[NOW]** Automate lint, compile, unit test and basic packaging checks in CI when the repository environment permits.
379. **[NOW]** Add build failure diagnostics and avoid scripts that swallow Unity command-line exit codes.
380. **[NOW]** Create a release rollback plan that returns to the last fully tested APK before the submission cutoff.

## 20. Enterprise architecture, governance and operational readiness [AFTER CORE]
381. **[AFTER CORE]** Document the intended enterprise deployment topology without implementing unnecessary production infrastructure in the competition build.
382. **[AFTER CORE]** Define tenant boundaries before any multi-customer data model is introduced.
383. **[AFTER CORE]** Define role-based access concepts for viewer, investigator, approver, administrator and auditor.
384. **[AFTER CORE]** Separate read permissions, proposal permissions and execution permissions in the future live-operations architecture.
385. **[AFTER CORE]** Create an audit event schema that records actor, action, object, time, correlation ID and result.
386. **[AFTER CORE]** Define retention, deletion, export and access policies for future customer telemetry and incident records.
387. **[AFTER CORE]** Document data residency and regional processing questions for enterprise deployments without claiming compliance prematurely.
388. **[AFTER CORE]** Create an integration threat model for future Kubernetes, cloud, observability and ticketing connectors.
389. **[AFTER CORE]** Require explicit environment allowlists and read-only defaults for future live infrastructure connectors.
390. **[AFTER CORE]** Design a policy engine boundary that can evaluate allowed actions independently of an AI agent.
391. **[AFTER CORE]** Require human approval and change-window checks for future production remediation.
392. **[AFTER CORE]** Define break-glass access, emergency disable and audit requirements before live write capability exists.
393. **[AFTER CORE]** Create an enterprise SLO model covering availability, task completion latency, data freshness and recovery correctness.
394. **[AFTER CORE]** Define an incident-response plan for the FAULTLINE service itself if the product becomes a hosted platform.
395. **[AFTER CORE]** Document backup and restore objectives for future server-side customer data.
396. **[AFTER CORE]** Create a vendor and subprocessor inventory when external providers are introduced.
397. **[AFTER CORE]** Design feature flags and staged rollout controls for enterprise connectors.
398. **[AFTER CORE]** Create a migration and compatibility policy for customer scenario definitions and agent contracts.
399. **[AFTER CORE]** Define a security review gate before adding any write-capable production connector.
400. **[AFTER CORE]** Keep enterprise claims evidence-based; distinguish roadmap intent from tested, deployed and certified capabilities.

## 21. Collaboration, shared sessions and multiplayer extension [LATER]
401. **[LATER]** Design a future session model with host, participant, role and shared scenario identity.
402. **[LATER]** Separate local interaction state from authoritative shared incident state.
403. **[LATER]** Define which spatial objects are shared and which are private to each participant.
404. **[LATER]** Create a conflict-resolution rule for two users attempting to approve or modify the same proposal.
405. **[LATER]** Use server-authoritative approval state if collaboration is introduced; do not trust client-only approval claims.
406. **[LATER]** Define presence events and participant connection state with graceful reconnect behaviour.
407. **[LATER]** Make participant identity and role visible without cluttering the main topology.
408. **[LATER]** Support shared annotations anchored to services, traces or evidence items.
409. **[LATER]** Add a turn-taking or ownership mechanism for high-impact shared interactions.
410. **[LATER]** Design a spectator mode for trainers, mentors or judges without granting control permissions.
411. **[LATER]** Synchronize simulation time and event sequence across participants with explicit drift handling.
412. **[LATER]** Handle one participant disconnecting during investigation without corrupting the session.
413. **[LATER]** Provide a host transfer or session recovery strategy if the host leaves unexpectedly.
414. **[LATER]** Protect collaboration channels against malformed events, spam and unauthorized action attempts.
415. **[LATER]** Add session invitation and access control only after authentication and authorization are implemented.
416. **[LATER]** Test latency, jitter, packet loss and reconnect scenarios before claiming real-time collaboration.
417. **[LATER]** Avoid making multiplayer a dependency for the single-user judge demo.
418. **[LATER]** Create a cooperative exercise where one user investigates telemetry and another reviews remediation evidence.
419. **[LATER]** Evaluate asymmetric roles only where spatial interaction creates real value rather than novelty.
420. **[LATER]** Treat multiplayer as a future special-award opportunity, not a last-minute competition feature.

## 22. Progression, training scenarios and reason to return [LATER]
421. **[LATER]** Create a scenario mastery record based on demonstrated investigation and verification, not time spent alone.
422. **[LATER]** Provide difficulty levels by changing evidence ambiguity and failure complexity while preserving causal correctness.
423. **[LATER]** Add guided training scenarios that teach one distributed-systems concept at a time.
424. **[LATER]** Create scenario debriefs that explain why a tempting incorrect diagnosis failed.
425. **[LATER]** Store personal bests for diagnosis quality, evidence coverage and recovery verification if progression is added.
426. **[LATER]** Avoid rewarding unsafe remediation or speed at the expense of correctness.
427. **[LATER]** Create optional challenge constraints such as limited evidence or reduced telemetry without compromising the core demo.
428. **[LATER]** Add scenario unlocks only when additional content exists and has been tested.
429. **[LATER]** Provide a visible learning path from basic dependency tracing to advanced failure analysis.
430. **[LATER]** Create a replay mode that lets users inspect the event timeline after completing an incident.
431. **[LATER]** Add a compare-run view that shows differences between two investigation strategies.
432. **[LATER]** Use anonymized aggregate learning metrics only with appropriate consent and privacy safeguards.
433. **[LATER]** Create repeat-use value through new scenario packs, team exercises and evolving infrastructure models.
434. **[LATER]** Avoid artificial streaks, manipulative scarcity or pressure mechanics in professional training software.
435. **[LATER]** Offer an instructor mode with learning objectives, hints and assessment rubrics in a future edition.
436. **[LATER]** Add scenario authoring as a later capability with validation and preview before publication.
437. **[LATER]** Version scenario packs so training outcomes remain reproducible.
438. **[LATER]** Provide difficulty explanations and accessible alternatives for each challenge mechanic.
439. **[LATER]** Measure whether progression improves understanding through user tests rather than assuming engagement equals learning.
440. **[LATER]** Do not implement progression until the core incident loop is complete and compelling.

## 23. Commercial validation, pricing, product analytics and customer discovery [LATER]
441. **[LATER]** Interview SRE and platform-engineering practitioners about how they currently understand dependency failures and incident propagation.
442. **[LATER]** Validate whether spatial topology improves diagnosis or training compared with an ordinary 2D graph.
443. **[LATER]** Identify the economic buyer separately from the daily user for enterprise training and operations use cases.
444. **[LATER]** Test initial segments such as SRE training, university labs and enterprise incident simulation before broadening the market.
445. **[LATER]** Write a value hypothesis tied to reduced training time, faster mental-model formation or improved incident readiness.
446. **[LATER]** Avoid promising production mean-time-to-recovery reductions without customer evidence.
447. **[LATER]** Test pricing hypotheses for individual learning, team training and enterprise licensing.
448. **[LATER]** Build a simple customer-discovery landing page only after the value proposition is clear.
449. **[LATER]** Track qualified interest, demo completion and pilot willingness rather than vanity page views alone.
450. **[LATER]** Create a pilot proposal with scope, success criteria, data requirements and clear non-production boundaries.
451. **[LATER]** Define what a paid proof of concept would include and what support obligations it creates.
452. **[LATER]** Identify competing observability, incident-management and training products and document actual differentiation.
453. **[LATER]** Create a competitor matrix based on verified features, not unsupported marketing claims.
454. **[LATER]** Estimate cost of external model usage and infrastructure before selecting a subscription or usage-based price.
455. **[LATER]** Create an enterprise deployment-cost model including support, compliance, integration and maintenance.
456. **[LATER]** Document intellectual property ownership and third-party licence obligations before fundraising or commercial distribution.
457. **[LATER]** Separate pilot pipeline, signed commitments, paying customers and hypothetical interest in all reporting.
458. **[LATER]** Prepare a concise investor narrative around the product problem, spatial advantage, agentic workflow and commercial path.
459. **[LATER]** Identify measurable validation milestones for the next 30, 60 and 90 days after competition submission.
460. **[LATER]** Do not build billing, payments or full CRM functionality into the competition demo.

## 24. Demo craft, judge narrative, submission assets and evidence [NOW]
461. **[NOW]** Write a three-minute maximum demo script that opens with the visible failure and quickly shows why spatial interaction matters.
462. **[NOW]** Record authentic application footage from the target experience or supported simulator; do not substitute concept art for working product footage.
463. **[NOW]** Show hands or gaze interaction clearly enough that judges can understand the input and outcome.
464. **[NOW]** Demonstrate one real agent investigation with visible evidence references and honest deterministic/LLM labelling.
465. **[NOW]** Show a human approval decision and the actual simulated state transition that follows it.
466. **[NOW]** Show post-remediation metrics improving and verification passing; do not rely on a green success card alone.
467. **[NOW]** Keep the first 15 seconds visually and narratively compelling without hiding the product purpose.
468. **[NOW]** Avoid long loading sequences, developer console views and unexplained jargon in the public video.
469. **[NOW]** Create clean screenshots of the welcome scene, topology, evidence inspection, agent findings and recovery state.
470. **[NOW]** Create a concise project description naming the audience, problem, spatial innovation and demonstrated result.
471. **[NOW]** Explain exactly which device profiles were tested and distinguish simulator testing from physical hardware testing.
472. **[NOW]** List SDKs, APIs, engines and external services actually used; do not include aspirational integrations as completed work.
473. **[NOW]** Prepare a judge quick-start guide with installation, launch, hand interaction and scenario replay instructions.
474. **[NOW]** Test judge access through the submitted release channel or required hosting path before final submission.
475. **[NOW]** Create a troubleshooting section for tracking initialization, app launch and scenario reset.
476. **[NOW]** Review the Devpost form field by field and make sure the video and build links are publicly or judge-accessible as required.
477. **[NOW]** Check team, organization, ownership, contributor and third-party asset credits for accuracy.
478. **[NOW]** Preserve screenshots, logs, test reports and build checksums that substantiate claims in the submission.
479. **[NOW]** Ask an uninvolved reviewer to watch the video once and explain the product; rewrite confusing moments.
480. **[NOW]** Freeze the submission build before the deadline and do not modify the entry during the judging period if the rules prohibit it.

## 25. Hardware, advanced spatial features and future platform expansion [LATER]
481. **[LATER]** Prototype an ESP32 incident beacon only after the standalone XR experience is complete and stable.
482. **[LATER]** Use an LED ring to mirror simulated incident severity, agent investigation and verified recovery states.
483. **[LATER]** Add a vibration motor only with a safe intensity limit and a user-controlled disable option.
484. **[LATER]** Define a small authenticated message protocol between the Unity app and a local hardware bridge.
485. **[LATER]** Use sequence numbers and acknowledgements to detect missing or duplicated hardware events.
486. **[LATER]** Handle USB or Bluetooth disconnects without blocking the core XR session.
487. **[LATER]** Show hardware connection status and keep every hardware-dependent feature optional.
488. **[LATER]** Avoid exposing raw Bluetooth pairing or arbitrary command execution to an AI agent.
489. **[LATER]** Create a simulator-only hardware adapter so the UI can be tested without physical electronics.
490. **[LATER]** Document power, thermal and enclosure safety for a physical prototype.
491. **[LATER]** Explore purposeful passthrough by using a desk or tabletop as a meaningful topology workspace if supported by the selected device.
492. **[LATER]** Use room understanding and plane anchoring only through officially supported public APIs available to the entrant.
493. **[LATER]** Ensure spatial content adapts to unknown room dimensions and gracefully falls back when surfaces are unavailable.
494. **[LATER]** Persist spatial anchors only if the device and platform APIs support the required lifetime and permission model.
495. **[LATER]** Explore spatial audio to indicate directional service dependency or incident propagation without making audio essential.
496. **[LATER]** Consider eye gaze for target acquisition and hand confirmation when supported, with a non-gaze alternative.
497. **[LATER]** Prototype shared spatial incident rooms only after single-user state ownership and conflict semantics are documented.
498. **[LATER]** Investigate future live read-only connectors for Kubernetes and observability platforms behind a strict adapter boundary.
499. **[LATER]** Require policy checks, explicit environment allowlists and human approval before any future write-capable live connector.
500. **[LATER]** Publish an expansion roadmap that clearly separates shipped, tested, experimental and proposed capabilities.

# Production cheatlist: the non-negotiable operating standard

### Product and experience
- [ ] One sentence explains the customer, job, spatial advantage and outcome.
- [ ] One primary track is selected; every major feature supports that story.
- [ ] The first five minutes prove value without developer narration.
- [ ] The complete P0 scenario works seated and without controllers.
- [ ] Every required screen has loading, empty, success, error and recovery behaviour where applicable.
- [ ] Every visible control performs a real action or is clearly disabled/marked as unavailable.
- [ ] No fake live telemetry, fake agent autonomy or unlabelled simulated data.
- [ ] The experience can be restarted and replayed deterministically.

### Architecture and code quality
- [ ] Domain logic is independent of Unity scenes and presentation objects.
- [ ] Module dependencies point inward; no cyclic assembly references.
- [ ] State changes pass through validated commands or application services.
- [ ] Entities have stable IDs, explicit ownership and documented lifecycle.
- [ ] Async work has cancellation, timeout and lifecycle guards.
- [ ] No unbounded queues, retries, recursion or unbounded in-memory histories.
- [ ] No swallowed exceptions, empty catch blocks or unexplained TODOs in P0 paths.
- [ ] Configuration is validated at startup and missing mandatory configuration fails clearly.

### Distributed systems and resilience
- [ ] Local-first deterministic simulation is the source of truth for the competition build.
- [ ] Remote dependencies are optional and isolated from XR frame updates.
- [ ] Timeouts are set at every network and async boundary.
- [ ] Retries are bounded, jittered and limited to transient failures.
- [ ] Non-idempotent writes have idempotency protection or are not retried.
- [ ] Duplicate events and duplicate commands are handled safely.
- [ ] Ordering and causality use sequence/correlation IDs, not only wall-clock time.
- [ ] Backpressure and overload behaviour are defined for every queue or stream.
- [ ] Circuit breakers are used only where they solve a real remote dependency problem.
- [ ] Recovery and rollback paths are tested, not merely documented.

### AI and agentic safety
- [ ] Agent inputs and outputs are typed, bounded and schema-validated.
- [ ] Every material finding is grounded in evidence IDs.
- [ ] Uncertainty and disagreement are displayed honestly.
- [ ] Local deterministic fallback remains functional without API keys.
- [ ] LLMs never run arbitrary code or invoke unrestricted tools.
- [ ] Agents propose; policy validates; user approves; domain service executes.
- [ ] Provider timeouts, rate limits, malformed outputs and outages have tested handling.
- [ ] Model/provider usage, cost and data handling are disclosed and bounded.

### Security and privacy
- [ ] No secrets, tokens, private keys or production credentials in source, APK or logs.
- [ ] Inputs, IDs, paths, numeric ranges and serialized payloads are validated.
- [ ] Untrusted model output is never executed or trusted as policy.
- [ ] Debug-only permissions, endpoints and logging are absent from the release build.
- [ ] Third-party SDKs and assets have recorded licences and versions.
- [ ] Synthetic demo data is clearly described as simulated.
- [ ] User data collection is minimized, disclosed and controllable.
- [ ] Security limitations are documented; do not claim certifications without evidence.

### XR performance and comfort
- [ ] Profile on the actual target build/device profile; Editor numbers are not proof.
- [ ] Use the official judging minimum of 60 FPS on Quest as a performance gate.
- [ ] No avoidable per-frame allocations in hot interaction/render paths.
- [ ] Rendering, particles, transparent surfaces and dynamic lights have explicit budgets.
- [ ] All important interactions are within seated reach and FoV-aware.
- [ ] Tracking loss cancels unsafe pending interactions and never replays them.
- [ ] Reduced-motion and non-colour-only status communication are available.
- [ ] No forced camera movement or essential information at extreme FoV edges.

### Testing and verification
- [ ] Unit tests cover graph, state machine, simulation, evidence and remediation invariants.
- [ ] Integration tests cover scenario → agents → approval → execution → verification.
- [ ] XR tests cover controller-free input, tracking loss/recovery and seated interaction.
- [ ] End-to-end test asserts domain state and final outcomes, not just UI visibility.
- [ ] Clean-clone build proves there are no hidden local dependencies.
- [ ] Repeated reset/replay test proves state does not leak between runs.
- [ ] Corrupted save, missing config, offline provider and app pause/resume are tested.
- [ ] Unexecuted tests are marked Not Run, never Passed.

### Release and submission
- [ ] Eligibility, Start membership, Developer Access and country requirements are confirmed by the entrant.
- [ ] Entry division and development history are truthful and supported by records.
- [ ] Build settings, SDK versions, package identity, scenes and permissions are verified.
- [ ] Release APK is installed/launched and the critical journey is smoke-tested.
- [ ] Release manifest includes version, commit, timestamp, toolchain and SHA-256.
- [ ] Competition release channel and judge invitation/access are verified using official workflow.
- [ ] Public demo video is under three minutes and shows real product footage.
- [ ] Devpost text, screenshots, video, build link and credits are complete and consistent.
- [ ] The exact submitted build is archived and checksummed.
- [ ] Do not make submission changes during the judging period where official rules prohibit them.

# Architecture blueprint: production-minded without hackathon overengineering
## Recommended competition architecture
Use a **local-first modular monolith**. For the current submission path this lives in the WebXR app under `web/src/` (TypeScript); keep the domain engine deterministic and independent of the Three.js view. The optional network edges are adapters, not required dependencies. Do not deploy Kubernetes, Kafka, a fleet of microservices or a cloud database merely to look enterprise-grade. (The C# mirror under `Assets/Faultline/` is reference material only.)
```text
WebXR / Quest Browser (immersive-vr, hand-tracking) — desktop preview for review
                  |
           Input & Interaction Layer
                 |
       Presentation / Spatial Topology
                 | commands + projections
             Application Layer
   +-------------+--------------+----------------+
   |             |              |                |
Incident      Agent         Remediation      Scenario
Service       Orchestrator   Policy/Approval  Simulator
   |             |              |                |
   +------------- Domain Model / Event Stream ---+
                 |
        Local Persistence + Replay
                 |
       Optional Adapter Boundaries
        /          |            \
   LLM Provider  Optional API  Hardware Bridge
```
## Suggested module boundaries
- **Domain:** Entities, value objects, invariants, graph algorithms, state machines; no UnityEngine dependency.
- **Application:** Use cases and command handlers; owns orchestration and validates transitions.
- **Simulation:** Seeded telemetry, failure propagation, clock, checkpoints and recovery verification.
- **Agents:** Role definitions, task lifecycle, output schemas, evidence grounding and fallback.
- **Remediation:** Policy, proposal validation, approval, idempotent execution, rollback and audit events.
- **XR/Input:** Meta/OpenXR adapters, hand/gaze signals, interaction state machine and tracking health.
- **Spatial/Presentation:** Unity scenes, prefabs, materials, panels, topology projections and motion.
- **Persistence:** Versioned local saves, session history, replay metadata and safe migration.
- **Integrations:** Optional model, backend and hardware adapters; secrets stay outside the APK.
- **Observability:** Error taxonomy, structured logs, correlation IDs, diagnostics and performance sampling.

## Distributed-systems patterns: what to use, and when
- **Idempotency:** Use stable command/event keys for approval, remediation and any remote write. Store the outcome of a processed key so duplicate delivery returns the original result.
- **Retries:** Use bounded exponential backoff with jitter only for transient failures. Stop on validation, authorization and other permanent errors. Never retry an uncertain write without idempotency protection.
- **Timeouts and cancellation:** Set an end-to-end deadline and a cancellation path for every remote operation. A cancelled UI task must not leave a background callback mutating a destroyed scene.
- **Bulkheads:** Keep model-provider work, persistence and hardware communication off the XR frame loop. A failure in one adapter must not block the simulator.
- **Circuit breaker:** Apply only to a remote dependency that repeatedly fails. Expose state and recovery policy; do not add a circuit breaker around local pure functions.
- **Backpressure:** Bound event/task queues. Coalesce replaceable telemetry updates, preserve critical incident events, and surface overload rather than dropping silently.
- **Event ordering:** Use per-session sequence numbers and correlation IDs. Wall-clock timestamps help humans but are not a reliable total ordering guarantee.
- **Outbox/inbox:** Use only when a backend database transaction must reliably publish events or consume duplicate-prone external events. It is unnecessary for a fully local demo.
- **Saga/compensation:** For multi-step future live workflows, define explicit compensating actions and human approval. The local competition simulation can use a checkpoint and deterministic rollback.
- **Observability:** Carry correlation IDs across UI, agent, adapter and simulation boundaries; record duration, outcome and failure code without leaking secrets.

## Error envelope and code catalogue
Use a stable structure such as `ErrorCode`, `Severity`, `MessageKey`, `CorrelationId`, `OccurredAt`, `Recoverable`, `Retryable`, and sanitized `Context`. Keep user text localized/presentable and keep stack traces in local developer diagnostics only.
Recommended codes: `FL-RT-001` initialization; `FL-XR-001` XR session unavailable; `FL-XR-002` hand tracking lost; `FL-IN-001` invalid gesture; `FL-GR-001` missing graph endpoint; `FL-GR-002` cycle detected; `FL-IC-001` invalid incident transition; `FL-AG-001` agent timeout; `FL-AG-002` invalid agent result; `FL-EV-001` evidence unavailable; `FL-RM-001` invalid approval; `FL-RM-002` remediation execution failed; `FL-SM-001` invalid simulation state; `FL-PS-001` persistence failed; `FL-IT-001` optional integration unavailable; `FL-SC-001` unauthorized operation. Codes are stable contracts; never recycle a code for a different meaning.

# Master coding-agent execution prompt
Copy this prompt into the terminal coding agent after placing this playbook in the repository as `docs/FAULTLINE_500_POINT_PLAYBOOK.md`. The agent must inspect the actual repo before changing it. (A standalone copy ships as `FAULTLINE_XR_Master_Coding_Agent_Prompt.txt`; keep the two in sync or treat the repo copy as the single source and regenerate the standalone file from it.)

```text
You are the Staff Software Engineer, Forward Deployed Engineer, XR Engineer, distributed-systems architect, AI systems engineer, product designer and release owner for FAULTLINE XR.

MISSION
Deliver a genuinely functioning, original, hands-first spatial incident investigation product for the Meta VR Start Developer Competition. The target is a polished, deterministic end-to-end incident experience, not a broad collection of unverified features.

AUTHORITATIVE INPUTS
1. Read the existing repository and all project instructions before editing.
2. Read `docs/FAULTLINE_500_POINT_PLAYBOOK.md` in full.
3. Read the PRD/specification already present in the repository. Where this playbook and the existing project differ, inspect the code and document the reconciliation in an architecture decision record.
4. Verify current official competition rules, official Meta XR documentation, package release notes and the exact installed Unity/Meta SDK versions. Do not invent APIs or use unreleased tools.
5. Treat the official judging criteria as four equally weighted areas: Innovation & Creativity, Experience Design, Technical Implementation, and Polish & Presentation. Produce evidence for each.

NON-NEGOTIABLE HONESTY
- No prompt guarantees a win. Do not write claims such as guaranteed winner, 100% bug-free or production-certified.
- Never claim a feature works because a class, prefab, screen or TODO exists.
- Never claim a test passed unless it was actually executed and its result inspected.
- Distinguish simulator testing from physical-device testing.
- Distinguish deterministic local agents from remote LLM agents; label fallbacks honestly.
- Distinguish simulated infrastructure from live customer infrastructure.
- Never claim Meta approval, eligibility, account setup, release upload or judge access without direct evidence.
- Do not fabricate screenshots, logs, metrics, test reports, user research or customer demand.

SCOPE STRATEGY
The P0 deliverable is one excellent, controller-free, seated, repeatable scenario:
launch → orient → select failing service → inspect evidence → dispatch specialist agents → review grounded findings → inspect remediation → explicitly approve → execute deterministic simulation → verify recovery → review incident report.

The core scenario is CASCADE: database latency causes downstream application degradation and worker/queue symptoms. Use deterministic seeded telemetry. The app must complete the core flow without an external network connection, API key, cloud service or physical accessory. Optional integrations must fail open to a labelled local fallback without blocking the XR session.

DO NOT attempt all 500 backlog items. Treat the 500-point list as an idea/coverage inventory. Assess items, identify what already exists, then select the smallest set that maximizes judging evidence and product coherence. P0 correctness beats P1 breadth; P1 quality beats P2 enterprise theatre. Defer multiplayer, physical hardware, live production control, microservices, Kubernetes, Kafka and cloud operations unless there is a tested user-facing reason and enough time after P0 is stable.

SAFETY AND PLATFORM BOUNDARIES
- All competition remediation is local simulation only. No live production infrastructure write operations.
- AI agents may inspect approved scenario data and produce typed proposals. They cannot execute arbitrary code, shell, SQL, reflection, network destinations or Unity methods.
- Only a validated application service can alter domain state. Human approval is required before simulated remediation.
- No secrets in source control, logs, ScriptableObjects, assets or APK. External provider keys must remain outside the client and be optional.
- Keep privacy minimal. Label generated telemetry and scenario data as simulated.
- Do not make an unsafe gesture execute an approval or remediation.

EXECUTION PROTOCOL
TIME BUDGET: 9 days remain until the 18 November 2026 deadline. The repository is already substantially built. Prioritize closing the release blockers (HTTPS deployment, video, headset verification, Devpost form) over new feature work.

PHASE 0 — RECONNAISSANCE (LARGELY DONE)
`docs/engineering/ENVIRONMENT_AUDIT.md` and `environment-report.json` already exist. Re-verify git status, uncommitted changes (several `Assets/Faultline/` files are currently modified — commit or record them), the `web/` build (`npx tsc --noEmit`, `npm test -- --run`, `npm run build`), and whether the self-hosted GitHub Actions runner for `deploy-pages.yml` is reachable. Produce updates, not a duplicate audit. If a mandatory interactive installation or account action is required, identify the exact blocker and continue with independent work.

PHASE 1 — REQUIREMENT TRACEABILITY (MISSING — DO THIS FIRST)
Create `docs/engineering/REQUIREMENT_TRACEABILITY.md`. It does not yet exist in the repository. Map candidate backlog items to the implemented WebXR build using `IMPLEMENTATION_STATUS.md` as the starting inventory. Use statuses NOT_ASSESSED, PLANNED, IN_PROGRESS, BLOCKED, DONE_VERIFIED, DEFERRED or NOT_APPLICABLE. DONE_VERIFIED requires code reference, executed verification and evidence. Do not create phantom completions. Mark Unity-only items as DEFERRED or NOT_APPLICABLE for the WebXR submission path.

PHASE 2 — ARCHITECTURE AND DESIGN AUDIT (DONE — MAINTAIN)
`SYSTEM_ARCHITECTURE.md`, `DOMAIN_MODEL.md`, `DESIGN_SYSTEM.md` and `ERROR_CATALOGUE.md` already exist under `docs/`. Verify they still match the WebXR implementation, update where drifted, and record new decisions in `docs/engineering/DECISIONS.md`. Domain logic must remain view-framework independent.

PHASE 3 — CLOSE P0 DOMAIN GAPS
Implement stable entities and IDs; directed dependency graph; cycle-safe traversal; incident state machine; deterministic scenario clock; seeded telemetry; causal event timeline; typed evidence; and scenario reset/replay. Add invariants and tests before wiring visual effects. The simulator is the source of truth; visual objects are projections.

PHASE 4 — AGENTIC INVESTIGATION
Implement ORACLE, TRACE, SENTRY and FORGE with narrow roles and typed schemas. Ground factual findings in valid evidence IDs. Display uncertainty and disagreement. Implement timeout, cancellation, bounded retry for transient errors, malformed-output rejection and deterministic fallback. No external API is required for the demo. Make task status explicit and prevent duplicate task dispatch.

PHASE 5 — REMEDIATION AND VERIFICATION
Build proposal → validation → approval → execution → verification. Use proposal expiry, precondition checks, unique execution IDs, idempotency, checkpoint/rollback and audit events. Prove that the simulated metrics and graph state actually improve. Do not use a success animation as proof.

PHASE 6 — XR INPUT AND SPATIAL UX
Build on the WebXR APIs actually in use (`immersive-vr` session with `hand-tracking` feature, hand joints, pinch hysteresis). Maintain hand-first selection and manipulation, stable targets, debounce/hysteresis, tracking-loss cancellation and recovery, seated reach, focus feedback and explicit controls. Gaze only if a profile/API actually supports it. Desktop mouse preview may assist developers but must not be required for the final user flow. Do not fake hand tracking with keyboard input.

PHASE 7 — DESIGN AND PRESENTATION
Apply the FAULTLINE Obsidian Signal token system. Build reusable components and semantic states. All controls must be functional or clearly unavailable. Implement loading, empty, error, retry, cancellation, success and recovery states. Keep typography legible, status meaning non-colour-only, animations purposeful and motion comfortable. Remove generic Unity placeholder UI from the final path.

PHASE 8 — HARDENING
Test app cold start, scene transition, missing scenario, malformed graph, cycles, missing evidence, agent timeout, provider offline, duplicate events, double approval, duplicate execution, rollback failure, corrupted save, tracking loss, tracking recovery, pause/resume, scene unload and repeated scenario reset. Check for hidden local dependencies, unbounded tasks, swallowed exceptions, TODOs in P0 paths and dead buttons.

PHASE 9 — PERFORMANCE AND ACCESSIBILITY
Profile the production `web/dist` build on the target device/profile, not just the desktop preview or `vite dev`. The official judging guidance calls for at least 60 FPS on Quest hardware; in a browser build that means watching frame time in Quest Browser, bundle size (current dist is 609 KB minified / 157 KB gzip), allocations in the render loop, draw calls and particle count. Optimize actual bottlenecks. Validate seated reach, field-of-view placement, readable type, high contrast, reduced motion, non-audio alternatives and controller-free completion. Report simulator/preview-only limitations honestly.

PHASE 10 — RELEASE CANDIDATE
The release artifact is the production static build in `web/dist/` deployed over HTTPS via the GitHub Pages workflow (`deploy-pages.yml`, self-hosted runner — verify the runner is online and the workflow is green on `main`). Produce the build manifest, toolchain versions, commit ID, test report and known limitations (see `submission/BUILD_MANIFEST.json`). Smoke-test the deployed URL end-to-end. Preserve the last known-good deployment; do not overwrite it with an unverified build. Note the currently uncommitted changes in the working tree must be committed (or recorded) before the release build.

PHASE 11 — SUBMISSION PACKAGE
Prepare concise project description, honest feature list, screenshots, judge quick-start guide, device/profile test matrix, three-minute-or-shorter real product demo script, public video checklist, release-channel instructions and Devpost checklist. User/account-only tasks must be reported as actions for the entrant; do not claim them complete.

ENGINEERING RULES
- Prefer small, reviewed, compileable changes over giant rewrites.
- Inspect files before editing and preserve working behaviour.
- Do not use placeholder implementations in P0 paths.
- No silent catch blocks, fake success values, hardcoded test bypasses, empty event handlers or dead buttons.
- No arbitrary sleeps as a substitute for readiness or process completion.
- Async operations need cancellation, deadlines and lifecycle safety.
- All external inputs are bounded and validated.
- All state changes have a single authoritative owner.
- Every user-visible operation has loading, failure and recovery behaviour as appropriate.
- Every important change is followed by compilation and targeted tests.
- Keep changes reversible and record architecture decisions.
- If a command fails, inspect the actual output, identify the root cause and do not repeat the same failing command without changing the cause.
- If blocked, document exact error, impact, attempted fix, workaround and the next required user action. Continue non-blocked work.

PROGRESS REPORT FORMAT
After every phase, update `IMPLEMENTATION_STATUS.md` and report:
1. phase and objective;
2. files created/modified;
3. actual commands executed;
4. build/compile result;
5. tests run, passed, failed and not run;
6. simulator/device profile used;
7. known defects and remaining blockers;
8. evidence paths;
9. next phase and reason it is safe to proceed.

STOP CONDITIONS
Stop feature expansion and stabilize when any of these occurs: P0 regression, build instability, unresolved XR initialization failure, controller-free interaction failure, evidence not matching agent claims, recovery not changing simulation truth, major frame-time regression, missing release access or imminent submission freeze. Fix P0 and preserve the known-good build before adding features.

FINAL DEFINITION OF DONE
A clean first-time user can launch the release, use hands without controllers, understand the topology, investigate CASCADE, inspect real scenario evidence, see specialist agents produce grounded findings, explicitly approve a remediation, watch the simulation state recover, verify the outcome and replay the scenario. The build is reproducible, critical tests have actually run, known limitations are disclosed, the submission materials are accurate and the exact release artifact is checksummed.

BEGIN NOW WITH PHASE 0. Do not reply with a generic plan. Inspect the terminal and repository, create the audit artifacts, and execute the work in the safest order. Ask the user only for a genuinely blocking account, credential, installation, hardware or destructive-action decision.
```

# Competition-specific verification and dates
- Competition entry deadline shown by Devpost: **18 November 2026, 12:00 PM Pacific Time**. Uganda is UTC+3 and Pacific Standard Time is UTC-8 on this date, making the corresponding Uganda deadline **11:00 PM on 18 November 2026**. Verify the displayed deadline again on Devpost before final submission.
- The official FAQ says a physical Quest headset is not required; Developer Access and Start membership must be in place by submission, and all tools/features must be public or available to Start members.
- Official judging criteria are equally weighted: Innovation & Creativity, Experience Design, Technical Implementation, and Polish & Presentation. The judging page calls out a minimum 60 FPS on Quest hardware.
- The official special awards include Best Agentic Interaction, Best First Five Minutes, Best Accessibility Forward Experience and Boldest Original Concept. Targeting them requires real evidence; adding an AI label or an accessibility toggle is not sufficient.
- Devpost states the public demo should honestly show real gameplay/application footage; judges are not required to watch beyond three minutes.
- Do not make changes during the judging period where the official rules prohibit updates.

## Official sources
- https://start-developer-competition-26.devpost.com/rules
- https://start-developer-competition-26.devpost.com/details/judging
- https://start-developer-competition-26.devpost.com/details/special-awards
- https://start-developer-competition-26.devpost.com/details/faqs
- https://developers.meta.com/blog/meta-connect-2026-vr-start-developer-competition/
- https://developers.meta.com/vr/resources/meta-start-developer-program/

---
**Operating principle:** Build the smallest complete product that makes the spatial advantage obvious, the agentic reasoning verifiable, the recovery real within the simulation, and the release dependable. Enterprise maturity is disciplined boundaries and evidence, not the number of infrastructure logos in the architecture diagram.
