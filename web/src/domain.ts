/**
 * FAULTLINE XR - deterministic domain engine.
 *
 * Ported from the Unity/C# reference implementation. Pure TypeScript with no
 * rendering or XR dependencies so it can be unit-tested headlessly and reused
 * by both the WebXR client and future builds.
 */

export type OperationalStatus = 'healthy' | 'degraded' | 'failing' | 'down';
export type Severity = 'info' | 'warning' | 'critical' | 'success';

export interface ServiceNode {
  id: string;
  name: string;
  type: string;
  status: OperationalStatus;
  health: number;
  latencyMs: number;
  errorRate: number;
  throughput: number;
  x: number;
  y: number;
  z: number;
}

export interface DependencyEdge {
  from: string;
  to: string;
  kind: string;
  latencyMs: number;
}

export interface ServiceGraph {
  scenarioId: string;
  rootCauseNodeId: string;
  nodes: ServiceNode[];
  edges: DependencyEdge[];
}

export interface TimelineEvent {
  id: string;
  tick: number;
  at: number;
  code: string;
  title: string;
  detail: string;
  severity: Severity;
}

export interface Finding {
  id: string;
  taskId: string;
  role: AgentRole;
  title: string;
  detail: string;
  confidence: number;
  evidence: string[];
  recommendation: string;
}

export type AgentRole = 'ORACLE' | 'TRACE' | 'SENTRY' | 'FORGE';
export type AgentTaskStatus = 'queued' | 'running' | 'completed' | 'cancelled' | 'timedOut';

export interface AgentTask {
  id: string;
  role: AgentRole;
  incidentId: string;
  status: AgentTaskStatus;
  startedAt: number;
  findingId?: string;
}

export type IncidentState =
  | 'detected'
  | 'triaged'
  | 'investigating'
  | 'awaitingApproval'
  | 'remediating'
  | 'verifying'
  | 'resolved'
  | 'rejected'
  | 'cancelled'
  | 'failed';

export interface Incident {
  id: string;
  scenarioId: string;
  nodeId: string;
  severity: Severity;
  state: IncidentState;
  createdAt: number;
  transitions: { from: IncidentState; to: IncidentState; at: number }[];
}

export type PlanStatus = 'proposed' | 'approved' | 'rejected' | 'executed' | 'verified' | 'failed';

export interface RecoveryResult {
  success: boolean;
  healthDelta: number;
  verified: boolean;
  at: number;
}

export interface RemediationProposal {
  id: string;
  incidentId: string;
  nodeId: string;
  action: string;
  status: PlanStatus;
  createdAt: number;
  expiresAt: number;
  approvedBy?: string;
  approvedAt?: number;
  executionId?: string;
  recovery?: RecoveryResult;
}

export type Result<T> =
  | { ok: true; value: T }
  | { ok: false; code: string; message: string };

export const ok = <T>(value: T): Result<T> => ({ ok: true, value });
export const fail = <T>(code: string, message: string): Result<T> => ({
  ok: false,
  code,
  message,
});

let idCounter = 0;

export function makeId(prefix: string): string {
  idCounter += 1;
  // Deterministic counter with a monotonic sequence prefix — no wall-clock
  // randomness. IDs are equal to their creation order so re-runs of the same
  // seeded scenario produce identical identifiers.
  return `${prefix}-${idCounter.toString(36).padStart(4, "0")}`;
}

export function resetIdCounter(): void {
  idCounter = 0;
}

/** Small deterministic PRNG so a run can be reproduced from its seed. */
export function mulberry32(seed: number): () => number {
  let a = seed >>> 0;
  return () => {
    a |= 0;
    a = (a + 0x6d2b79f5) | 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

export function statusForHealth(health: number): OperationalStatus {
  if (health >= 0.9) return 'healthy';
  if (health >= 0.6) return 'degraded';
  if (health >= 0.3) return 'failing';
  return 'down';
}

export function clamp01(value: number): number {
  return Math.min(1, Math.max(0, value));
}

/* ------------------------------------------------------------------ */
/* CASCADE scenario                                                     */
/* ------------------------------------------------------------------ */

const CASCADE_NODES: Omit<ServiceNode, 'status'>[] = [
  { id: 'api-gateway', name: 'API Gateway', type: 'Gateway', health: 1.0, latencyMs: 12, errorRate: 0.01, throughput: 1200, x: -0.22, y: 0.14, z: 0.0 },
  { id: 'auth-service', name: 'Auth Service', type: 'Identity', health: 1.0, latencyMs: 18, errorRate: 0.01, throughput: 900, x: -0.3, y: -0.02, z: -0.06 },
  { id: 'app-service', name: 'App Service', type: 'Application', health: 0.55, latencyMs: 95, errorRate: 0.12, throughput: 450, x: 0.0, y: 0.05, z: 0.02 },
  { id: 'database', name: 'Database', type: 'Data Store', health: 0.25, latencyMs: 240, errorRate: 0.35, throughput: 120, x: 0.02, y: -0.15, z: 0.08 },
  { id: 'message-broker', name: 'Message Broker', type: 'Queue', health: 1.0, latencyMs: 8, errorRate: 0.01, throughput: 2000, x: 0.22, y: 0.03, z: -0.02 },
  { id: 'worker-cluster', name: 'Worker Cluster', type: 'Worker', health: 0.6, latencyMs: 110, errorRate: 0.08, throughput: 300, x: 0.28, y: -0.13, z: 0.03 },
  { id: 'monitoring', name: 'Monitoring', type: 'Observability', health: 1.0, latencyMs: 5, errorRate: 0.01, throughput: 500, x: 0.0, y: 0.23, z: -0.11 },
];

const CASCADE_EDGES: DependencyEdge[] = [
  { from: 'api-gateway', to: 'auth-service', kind: 'api', latencyMs: 25 },
  { from: 'api-gateway', to: 'app-service', kind: 'api', latencyMs: 35 },
  { from: 'app-service', to: 'database', kind: 'database', latencyMs: 40 },
  { from: 'app-service', to: 'message-broker', kind: 'queue', latencyMs: 15 },
  { from: 'message-broker', to: 'worker-cluster', kind: 'queue', latencyMs: 20 },
  { from: 'database', to: 'message-broker', kind: 'database', latencyMs: 10 },
];

export function createCascade(seed = 20261109): ServiceGraph {
  const nodes = CASCADE_NODES.map((node) => ({
    ...node,
    status: statusForHealth(node.health),
  }));
  return {
    scenarioId: `cascade-${seed}`,
    rootCauseNodeId: 'database',
    nodes,
    edges: CASCADE_EDGES.map((edge) => ({ ...edge })),
  };
}

/* ------------------------------------------------------------------ */
/* Simulation                                                           */
/* ------------------------------------------------------------------ */

export interface SimulationSnapshot {
  tick: number;
  health: Record<string, number>;
}

export class Simulation {
  readonly graph: ServiceGraph;
  readonly rootCauseNodeId: string;
  readonly scenarioId: string;
  private readonly random: () => number;
  private health = new Map<string, number>();
  private tickCount = 0;
  private snapshot: SimulationSnapshot | null = null;
  readonly events: TimelineEvent[] = [];

  constructor(seed = 20261109) {
    this.graph = createCascade(seed);
    this.rootCauseNodeId = this.graph.rootCauseNodeId;
    this.scenarioId = this.graph.scenarioId;
    this.random = mulberry32(seed);
    for (const node of this.graph.nodes) this.health.set(node.id, node.health);
    this.log('FL-SM-0001', 'Scenario initialised', `${this.scenarioId} · root cause on ${this.rootCauseNodeId}`, 'info');
  }

  get tick(): number {
    return this.tickCount;
  }

  get nodes(): ServiceNode[] {
    return this.graph.nodes.map((node) => this.hydrate(node));
  }

  node(id: string): ServiceNode | undefined {
    const source = this.graph.nodes.find((n) => n.id === id);
    return source ? this.hydrate(source) : undefined;
  }

  get aggregateHealth(): number {
    const all = this.graph.nodes.map((n) => this.health.get(n.id) ?? 0);
    return all.reduce((a, b) => a + b, 0) / Math.max(1, all.length);
  }

  get rootHealth(): number {
    return this.health.get(this.rootCauseNodeId) ?? 0;
  }

  dependenciesOf(nodeId: string): string[] {
    return this.graph.edges.filter((e) => e.from === nodeId).map((e) => e.to);
  }

  dependentsOf(nodeId: string): string[] {
    return this.graph.edges.filter((e) => e.to === nodeId).map((e) => e.from);
  }

  tickOnce(): void {
    this.tickCount += 1;
    const next = new Map(this.health);

    for (const node of this.graph.nodes) {
      const current = this.health.get(node.id) ?? 0;
      const dependencies = this.dependenciesOf(node.id);
      let worst = 1;
      for (const dep of dependencies) worst = Math.min(worst, this.health.get(dep) ?? 1);

      let value = current;
      if (node.id === this.rootCauseNodeId) {
        value = Math.max(0, current - 0.015 * this.random() - 0.004);
      } else if (worst < 0.55) {
        value = Math.max(0, current - 0.08 * (1 - worst) * (0.6 + this.random() * 0.6));
      } else if (current < 1 && worst >= 0.95) {
        value = Math.min(1, current + 0.018);
      }
      next.set(node.id, clamp01(value));
    }

    this.health = next;
  }

  checkpoint(): void {
    this.snapshot = {
      tick: this.tickCount,
      health: Object.fromEntries(this.health),
    };
    this.log('FL-SM-0010', 'Checkpoint created', `tick ${this.tickCount}`, 'info');
  }

  restore(): Result<boolean> {
    if (!this.snapshot) return fail('FL-RM-0084', 'No simulation checkpoint available to restore.');
    this.tickCount = this.snapshot.tick;
    this.health = new Map(Object.entries(this.snapshot.health));
    this.log('FL-SM-0011', 'Checkpoint restored', `rollback to tick ${this.tickCount}`, 'warning');
    return ok(true);
  }

  applyRemediation(nodeId: string, delta: number): Result<number> {
    if (!this.health.has(nodeId)) return fail('FL-RM-0080', `Unknown node: ${nodeId}`);
    const before = this.health.get(nodeId) ?? 0;
    const after = Math.max(clamp01(before + delta), 0.96);
    this.health.set(nodeId, after);

    for (const dependent of this.dependentsOf(nodeId)) {
      const current = this.health.get(dependent) ?? 0;
      this.health.set(dependent, Math.max(clamp01(current + delta * 0.6), 0.9));
    }
    return ok(after - before);
  }

  log(code: string, title: string, detail: string, severity: Severity): TimelineEvent {
    const event: TimelineEvent = {
      id: makeId('ev'),
      tick: this.tickCount,
      at: this.tickCount * 10_000,
      code,
      title,
      detail,
      severity,
    };
    this.events.push(event);
    return event;
  }

  private hydrate(node: ServiceNode): ServiceNode {
    const health = this.health.get(node.id) ?? 0;
    return { ...node, health, status: statusForHealth(health) };
  }
}

/* ------------------------------------------------------------------ */
/* Incident lifecycle                                                   */
/* ------------------------------------------------------------------ */

const ALLOWED: Record<IncidentState, IncidentState[]> = {
  detected: ['triaged', 'rejected', 'cancelled'],
  triaged: ['investigating', 'rejected', 'cancelled'],
  investigating: ['awaitingApproval', 'rejected', 'cancelled'],
  awaitingApproval: ['remediating', 'rejected', 'cancelled'],
  remediating: ['verifying', 'failed', 'cancelled'],
  verifying: ['resolved', 'failed', 'cancelled'],
  resolved: [],
  rejected: [],
  cancelled: [],
  failed: [],
};

export class IncidentLifecycle {
  readonly incident: Incident;

  constructor(incident: Incident) {
    this.incident = incident;
  }

  static create(nodeId: string, scenarioId: string, severity: Severity = 'critical'): IncidentLifecycle {
    return new IncidentLifecycle({
      id: makeId('inc'),
      scenarioId,
      nodeId,
      severity,
      state: 'detected',
      createdAt: 0,
      transitions: [],
    });
  }

  canTransition(to: IncidentState): boolean {
    return (ALLOWED[this.incident.state] ?? []).includes(to);
  }

  transition(to: IncidentState): Result<Incident> {
    if (!this.canTransition(to)) {
      return fail('FL-IC-0050', `Transition not allowed: ${this.incident.state} -> ${to}`);
    }
    const from = this.incident.state;
    this.incident.state = to;
    this.incident.transitions.push({ from, to, at: Date.now() });
    return ok(this.incident);
  }
}

/* ------------------------------------------------------------------ */
/* Agents                                                               */
/* ------------------------------------------------------------------ */

export const AGENT_PROFILES: Record<AgentRole, { name: string; blurb: string; color: string }> = {
  ORACLE: { name: 'ORACLE', blurb: 'Topology & blast radius', color: '#41D9F5' },
  TRACE: { name: 'TRACE', blurb: 'Latency & error traces', color: '#537EFF' },
  SENTRY: { name: 'SENTRY', blurb: 'SLO breach correlation', color: '#FFBE55' },
  FORGE: { name: 'FORGE', blurb: 'Safe remediation plan', color: '#9C83FF' },
};

const FINDING_TEMPLATES: Record<AgentRole, Omit<Finding, 'id' | 'taskId' | 'role'>> = {
  ORACLE: {
    title: 'Blast radius mapped',
    detail:
      'Database sits upstream of App Service. Two dependents inherit elevated error rates; the other five nodes are isolated.',
    confidence: 0.84,
    evidence: ['topology: app-service → database', 'topology: 2 dependent services'],
    recommendation: 'Contain the failure at Database before it reaches the gateway.',
  },
  TRACE: {
    title: 'Root cause in trace',
    detail:
      'p99 latency on Database rose from 240 ms to 3.1 s. The slow span is the write path; retries amplify load on App Service.',
    confidence: 0.93,
    evidence: ['trace: db-write-9f2a (3.1 s)', 'trace: app-service retry storm'],
    recommendation: 'Restart Database to clear the blocked write pool.',
  },
  SENTRY: {
    title: 'SLO breach correlated',
    detail:
      'Six alerts inside the same 90-second window all point at Database availability. Error rate is 35% versus a 1% SLO.',
    confidence: 0.88,
    evidence: ['alert: DB-AVAIL-01', 'metric: error_rate 0.35', 'window: 90 s'],
    recommendation: 'Treat as a single incident, not six separate alerts.',
  },
  FORGE: {
    title: 'Restart is safe',
    detail:
      'Database is stateless for this scenario and has no replica lag. Restart is reversible and needs no data migration.',
    confidence: 0.9,
    evidence: ['policy: restart-db allowed', 'check: replica lag 0 ms'],
    recommendation: 'Approve RestartService on Database; verify health afterwards.',
  },
};

export class AgentOrchestrator {
  private tasks = new Map<string, AgentTask>();
  private findings = new Map<string, Finding>();
  private dispatchedRoles = new Set<AgentRole>();
  private seq = 0;

  dispatch(role: AgentRole, incidentId: string): Result<AgentTask> {
    if (!incidentId) return fail('FL-SM-0090', 'Incident id is required.');
    if (this.dispatchedRoles.has(role)) {
      const existing = [...this.tasks.values()].find((t) => t.role === role && t.incidentId === incidentId);
      if (existing) return ok(existing);
    }
    this.seq += 1;
    const task: AgentTask = {
      id: makeId('task'),
      role,
      incidentId,
      status: 'running',
      startedAt: this.seq,
    };
    this.tasks.set(task.id, task);
    this.dispatchedRoles.add(role);
    return ok(task);
  }

  hasDispatched(role: AgentRole): boolean {
    return this.dispatchedRoles.has(role);
  }

  complete(taskId: string): Result<Finding> {
    const task = this.tasks.get(taskId);
    if (!task) return fail('FL-AG-0060', `Task not found: ${taskId}`);
    if (task.status === 'completed' && task.findingId) {
      const existing = this.findings.get(task.findingId);
      return existing ? ok(existing) : fail('FL-AG-0062', 'Finding missing for completed task.');
    }
    const template = FINDING_TEMPLATES[task.role];
    const finding: Finding = {
      ...template,
      id: makeId('find'),
      taskId: task.id,
      role: task.role,
    };
    this.findings.set(finding.id, finding);
    task.status = 'completed';
    task.findingId = finding.id;
    return ok(finding);
  }

  get allFindings(): Finding[] {
    return [...this.findings.values()];
  }

  get allTasks(): AgentTask[] {
    return [...this.tasks.values()];
  }

  averageConfidence(): number {
    const list = this.allFindings;
    if (!list.length) return 0;
    return list.reduce((sum, f) => sum + f.confidence, 0) / list.length;
  }

  reset(): void {
    this.tasks.clear();
    this.findings.clear();
    this.dispatchedRoles.clear();
  }
}

/* ------------------------------------------------------------------ */
/* Remediation                                                          */
/* ------------------------------------------------------------------ */

export class RemediationService {
  private proposals = new Map<string, RemediationProposal>();
  private now = () => 0;

  /** Inject a clock for deterministic tests. Defaults to a fixed epoch so approvals never expire spuriously. */
  setClock(fn: () => number): void {
    this.now = fn;
  }

  propose(incidentId: string, nodeId: string, action = 'RestartService'): Result<RemediationProposal> {
    if (!incidentId) return fail('FL-SM-0090', 'Incident id is required.');
    const proposal: RemediationProposal = {
      id: makeId('prop'),
      incidentId,
      nodeId,
      action,
      status: 'proposed',
      createdAt: this.now(),
      expiresAt: this.now() + 10 * 60_000,
    };
    this.proposals.set(proposal.id, proposal);
    return ok(proposal);
  }

  get(id: string): RemediationProposal | undefined {
    return this.proposals.get(id);
  }

  approve(id: string, approvedBy: string): Result<RemediationProposal> {
    const proposal = this.proposals.get(id);
    if (!proposal) return fail('FL-RM-0080', `Proposal not found: ${id}`);
    if (proposal.status !== 'proposed') {
      return fail('FL-RM-0081', `Proposal ${id} cannot be approved from status ${proposal.status}.`);
    }
    if (this.now() > proposal.expiresAt) return fail('FL-RM-0082', 'Approval window expired.');
    if (!approvedBy.trim()) return fail('FL-SM-0091', 'Approver identity is required.');
    proposal.status = 'approved';
    proposal.approvedBy = approvedBy;
    proposal.approvedAt = this.now();
    return ok(proposal);
  }

  reject(id: string, _rejectedBy: string, _reason: string): Result<RemediationProposal> {
    const proposal = this.proposals.get(id);
    if (!proposal) return fail('FL-RM-0080', `Proposal not found: ${id}`);
    if (proposal.status !== 'proposed' && proposal.status !== 'approved') {
      return fail('FL-RM-0080', `Proposal ${id} is already ${proposal.status}.`);
    }
    proposal.status = 'rejected';
    return ok(proposal);
  }

  execute(id: string, simulation: Simulation): Result<RecoveryResult> {
    const proposal = this.proposals.get(id);
    if (!proposal) return fail('FL-RM-0080', `Proposal not found: ${id}`);
    if (proposal.status === 'executed' || proposal.status === 'verified') {
      return fail('FL-RM-0083', `Proposal ${id} has already been executed.`);
    }
    if (proposal.status !== 'approved') {
      return fail('FL-RM-0080', `Proposal ${id} is not executable (status=${proposal.status}).`);
    }
    simulation.checkpoint();
    proposal.executionId = makeId('exec');
    const delta = simulation.applyRemediation(proposal.nodeId, 0.8);
    if (!delta.ok) {
      simulation.restore();
      return fail('FL-RM-0084', delta.message);
    }
    proposal.status = 'executed';
    const recovery: RecoveryResult = {
      success: true,
      healthDelta: delta.value,
      verified: false,
      at: this.now(),
    };
    proposal.recovery = recovery;
    return ok(recovery);
  }

  verify(id: string, simulation: Simulation): Result<boolean> {
    const proposal = this.proposals.get(id);
    if (!proposal) return fail('FL-RM-0080', `Proposal not found: ${id}`);
    if (proposal.status !== 'executed') {
      return fail('FL-RM-0080', `Proposal ${id} has not been executed.`);
    }
    const rootHealth = simulation.rootHealth;
    const healthy = rootHealth >= 0.9 && simulation.aggregateHealth >= 0.8;
    proposal.status = healthy ? 'verified' : 'failed';
    if (proposal.recovery) proposal.recovery.verified = healthy;
    return ok(healthy);
  }

  get all(): RemediationProposal[] {
    return [...this.proposals.values()];
  }
}

/* ------------------------------------------------------------------ */
/* Persistence                                                          */
/* ------------------------------------------------------------------ */

export interface RunRecord {
  at: number;
  scenarioId: string;
  resolved: boolean;
  durationMs: number;
  agentsDispatched: number;
  averageConfidence: number;
  healthRecovered: number;
  incidentId: string;
}

export interface Settings {
  textScale: number;
  highContrast: boolean;
  reducedMotion: boolean;
  audio: boolean;
  leftHanded: boolean;
}

export const DEFAULT_SETTINGS: Settings = {
  textScale: 1,
  highContrast: false,
  reducedMotion: false,
  audio: true,
  leftHanded: false,
};

export interface SaveData {
  version: number;
  settings: Settings;
  runs: RunRecord[];
}

const STORAGE_KEY = 'faultline-xr.save.v1';

export function defaultSave(): SaveData {
  return { version: 1, settings: { ...DEFAULT_SETTINGS }, runs: [] };
}

export function loadSave(storage?: Storage): SaveData {
  try {
    const store = storage ?? (typeof localStorage !== 'undefined' ? localStorage : undefined);
    if (!store) return defaultSave();
    const raw = store.getItem(STORAGE_KEY);
    if (!raw) return defaultSave();
    const parsed = JSON.parse(raw) as Partial<SaveData>;
    if (!parsed || parsed.version !== 1) return defaultSave();
    return {
      version: 1,
      settings: { ...DEFAULT_SETTINGS, ...(parsed.settings ?? {}) },
      runs: Array.isArray(parsed.runs) ? parsed.runs.slice(0, 25) : [],
    };
  } catch {
    return defaultSave();
  }
}

export function writeSave(data: SaveData, storage?: Storage): Result<boolean> {
  try {
    const store = storage ?? (typeof localStorage !== 'undefined' ? localStorage : undefined);
    if (!store) return fail('FL-PS-0100', 'Storage unavailable; continuing in memory.');
    store.setItem(STORAGE_KEY, JSON.stringify(data));
    return ok(true);
  } catch {
    return fail('FL-PS-0100', 'Save failed; continuing in memory.');
  }
}
