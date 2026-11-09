import { describe, expect, it } from 'vitest';
import {
  AgentOrchestrator,
  IncidentLifecycle,
  RemediationService,
  Simulation,
  createCascade,
  defaultSave,
  loadSave,
  statusForHealth,
  writeSave,
  type SaveData,
} from '../src/domain';

describe('CASCADE scenario', () => {
  it('seeds database as the root cause with six dependent edges', () => {
    const graph = createCascade();
    expect(graph.rootCauseNodeId).toBe('database');
    expect(graph.nodes).toHaveLength(7);
    expect(graph.edges).toHaveLength(6);
    expect(graph.nodes.find((node) => node.id === 'database')?.status).toBe('down');
    expect(graph.nodes.find((node) => node.id === 'app-service')?.status).toBe('failing');
  });

  it('marks health below the application thresholds correctly', () => {
    expect(statusForHealth(1)).toBe('healthy');
    expect(statusForHealth(0.89)).toBe('degraded');
    expect(statusForHealth(0.59)).toBe('failing');
    expect(statusForHealth(0.29)).toBe('down');
  });
});

describe('Simulation', () => {
  it('drifts the root cause downward deterministically', () => {
    const simulation = new Simulation(42);
    const rootHealth = simulation.rootHealth;
    simulation.tickOnce();
    expect(simulation.rootHealth).toBeLessThanOrEqual(rootHealth);
    expect(simulation.tick).toBe(1);
  });

  it('restores the checkpoint after a failed remediation', () => {
    const simulation = new Simulation(42);
    simulation.checkpoint();
    const rootHealth = simulation.rootHealth;
    simulation.applyRemediation('database', 0.8);
    expect(simulation.rootHealth).toBeGreaterThan(rootHealth);
    expect(simulation.restore().ok).toBe(true);
    expect(simulation.rootHealth).toBe(rootHealth);
  });

  it('rejects remediation for an unknown service', () => {
    const simulation = new Simulation(42);
    const result = simulation.applyRemediation('missing', 0.5);
    expect(result.ok).toBe(false);
    if (!result.ok) expect(result.code).toBe('FL-RM-0080');
  });
});

describe('IncidentLifecycle', () => {
  it('walks the full CASCADE happy path', () => {
    const lifecycle = IncidentLifecycle.create('database', 'cascade-1');
    const path: Array<'triaged' | 'investigating' | 'awaitingApproval' | 'remediating' | 'verifying' | 'resolved'> = [
      'triaged',
      'investigating',
      'awaitingApproval',
      'remediating',
      'verifying',
      'resolved',
    ];
    for (const next of path) expect(lifecycle.transition(next).ok).toBe(true);
    expect(lifecycle.incident.state).toBe('resolved');
  });

  it('refuses an illegal direct transition', () => {
    const lifecycle = IncidentLifecycle.create('database', 'cascade-1');
    const result = lifecycle.transition('resolved');
    expect(result.ok).toBe(false);
    if (!result.ok) expect(result.code).toBe('FL-IC-0050');
    expect(lifecycle.incident.state).toBe('detected');
  });
});

describe('AgentOrchestrator', () => {
  it('produces deterministic findings for two agents', () => {
    const orchestrator = new AgentOrchestrator();
    const oracle = orchestrator.dispatch('ORACLE', 'incident-1');
    const trace = orchestrator.dispatch('TRACE', 'incident-1');
    expect(oracle.ok).toBe(true);
    expect(trace.ok).toBe(true);
    const oracleFinding = orchestrator.complete(oracle.ok ? oracle.value.id : '');
    const traceFinding = orchestrator.complete(trace.ok ? trace.value.id : '');
    expect(oracleFinding.ok).toBe(true);
    expect(traceFinding.ok).toBe(true);
    expect(orchestrator.allFindings).toHaveLength(2);
    expect(orchestrator.averageConfidence()).toBeGreaterThan(0.8);
  });

  it('does not duplicate a completed finding', () => {
    const orchestrator = new AgentOrchestrator();
    const task = orchestrator.dispatch('FORGE', 'incident-1');
    if (!task.ok) throw new Error('Dispatch failed');
    const first = orchestrator.complete(task.value.id);
    const second = orchestrator.complete(task.value.id);
    expect(first.ok).toBe(true);
    expect(second.ok).toBe(true);
    if (first.ok && second.ok) expect(second.value.id).toBe(first.value.id);
    expect(orchestrator.allFindings).toHaveLength(1);
  });
});

describe('RemediationService', () => {
  it('moves proposed -> approved -> executed -> verified', () => {
    const simulation = new Simulation(42);
    const lifecycle = IncidentLifecycle.create('database', 'cascade-1');
    const service = new RemediationService();

    const proposal = service.propose(lifecycle.incident.id, 'database');
    expect(proposal.ok).toBe(true);
    if (!proposal.ok) return;

    expect(service.execute(proposal.value.id, simulation).ok).toBe(false);
    expect(service.approve(proposal.value.id, 'operator@faultline').ok).toBe(true);
    expect(service.execute(proposal.value.id, simulation).ok).toBe(true);
    expect(service.verify(proposal.value.id, simulation)).toEqual({ ok: true, value: true });
    expect(proposal.value.status).toBe('verified');
  });

  it('keeps the execution and verification gates explicit', () => {
    const simulation = new Simulation(42);
    const service = new RemediationService();
    const proposal = service.propose('incident-1', 'database');
    if (!proposal.ok) throw new Error('Proposal failed');
    const premature = service.execute(proposal.value.id, simulation);
    expect(premature.ok).toBe(false);
    if (!premature.ok) expect(premature.code).toBe('FL-RM-0080');
    expect(service.verify(proposal.value.id, simulation).ok).toBe(false);
  });

  it('keeps verification retryable after a failed check', () => {
    // Degrade the graph with an extended incident before remediating.
    const simulation = new Simulation(7);
    for (let i = 0; i < 120; i += 1) simulation.tickOnce();
    const service = new RemediationService();
    const proposal = service.propose('incident-1', 'database');
    if (!proposal.ok) throw new Error('Proposal failed');
    service.approve(proposal.value.id, 'operator@faultline');
    service.execute(proposal.value.id, simulation);

    const before = simulation.aggregateHealth;
    if (before < 0.8) {
      const failed = service.verify(proposal.value.id, simulation);
      expect(failed).toEqual({ ok: true, value: false });
      expect(proposal.value.status).toBe('executed');
    }

    // Recovery ticks heal the remaining services; verification must pass.
    for (let i = 0; i < 80; i += 1) simulation.tickOnce();
    const passed = service.verify(proposal.value.id, simulation);
    expect(passed).toEqual({ ok: true, value: true });
    expect(proposal.value.status).toBe('verified');
  });

  it('stops the root-cause decay after remediation so verification can pass', () => {
    const simulation = new Simulation(3);
    const service = new RemediationService();
    const proposal = service.propose('incident-1', 'database');
    if (!proposal.ok) throw new Error('Proposal failed');
    service.approve(proposal.value.id, 'operator@faultline');
    service.execute(proposal.value.id, simulation);

    const rootAfterExecute = simulation.rootHealth;
    for (let i = 0; i < 40; i += 1) simulation.tickOnce();
    // The fault is fixed: the root must not decay back below the threshold.
    expect(simulation.rootHealth).toBeGreaterThanOrEqual(rootAfterExecute);
    expect(simulation.rootHealth).toBeGreaterThanOrEqual(0.9);
    const verified = service.verify(proposal.value.id, simulation);
    expect(verified).toEqual({ ok: true, value: true });
  });
});

describe('SaveData', () => {
  const sampleSave = (): SaveData => ({
    version: 1,
    settings: {
      textScale: 1.15,
      highContrast: true,
      reducedMotion: true,
      audio: false,
      leftHanded: true,
    },
    runs: [
      {
        at: 123,
        scenarioId: 'cascade-42',
        resolved: true,
        durationMs: 120000,
        agentsDispatched: 2,
        averageConfidence: 0.9,
        healthRecovered: 0.4,
        incidentId: 'incident-1',
      },
    ],
  });

  const memoryStorage = (): Storage => {
    const map = new Map<string, string>();
    return {
      getItem: (key: string) => map.get(key) ?? null,
      setItem: (key: string, value: string) => {
        map.set(key, String(value));
      },
      removeItem: (key: string) => {
        map.delete(key);
      },
      clear: () => {
        map.clear();
      },
      key: (index: number) => [...map.keys()][index] ?? null,
      get length() {
        return map.size;
      },
    } as Storage;
  };

  const brokenStorage = (): Storage =>
    ({
      getItem: () => {
        throw new Error('storage denied');
      },
      setItem: () => {
        throw new Error('storage denied');
      },
    }) as unknown as Storage;

  it('round-trips settings and run history through injected storage', () => {
    const store = memoryStorage();
    const saved = sampleSave();
    const written = writeSave(saved, store);
    expect(written.ok).toBe(true);
    expect(loadSave(store)).toEqual(saved);
  });

  it('degrades gracefully with FL-PS-0100 when storage throws', () => {
    const store = brokenStorage();
    const result = writeSave(sampleSave(), store);
    expect(result.ok).toBe(false);
    if (!result.ok) expect(result.code).toBe('FL-PS-0100');
    expect(loadSave(store)).toEqual(defaultSave());
  });
});
