import * as THREE from 'three';
import {
  AGENT_PROFILES,
  AgentOrchestrator,
  IncidentLifecycle,
  RemediationService,
  Simulation,
  loadSave,
  writeSave,
  type AgentRole,
  type Finding,
  type RunRecord,
  type Settings,
} from './domain';
import type { Panel, PanelButton } from './ui/panel';
import {
  drawActionPanel,
  drawCenterPanel,
  drawTimelinePanel,
  type AppState,
  type Phase,
  type ScreenModel,
  type ScreenName,
  type Toast,
} from './ui/screens';
import type { InfrastructureScene } from './scene';
import type { Sfx } from './audio';
import { themeFromSettings } from './ui/theme';

interface AppPanels {
  center: Panel;
  action: Panel;
  timeline: Panel;
}

const START_AGGREGATE_SEED = 20261109;

export class FaultlineApp {
  readonly state: AppState;

  private simulation: Simulation;
  private orchestrator = new AgentOrchestrator();
  private remediation = new RemediationService();
  private lifecycle: IncidentLifecycle | null = null;
  private lastStatuses: Record<string, string> = {};
  private initialAggregate = 1;
  private tickAccumulator = 0;
  private refreshAccumulator = 0;
  private sessionToken = 0;
  private returnScreen: ScreenName = 'welcome';
  private theme;
  private actionHome = new THREE.Vector3(0.4, 1.14, -0.36);
  private timelineHome = new THREE.Vector3(-0.4, 1.14, -0.36);

  constructor(
    private readonly infra: InfrastructureScene,
    private readonly panels: AppPanels,
    private readonly sfx: Sfx,
    private readonly onResetView: () => void,
  ) {
    const save = loadSave();
    this.theme = themeFromSettings(save.settings);
    this.simulation = new Simulation(START_AGGREGATE_SEED);
    this.state = {
      screen: 'welcome',
      phase: 'detected',
      scenarioId: this.simulation.scenarioId,
      incident: null,
      selectedNodeId: 'database',
      findings: [],
      proposal: null,
      runStartedAt: 0,
      tickCount: 0,
      settings: save.settings,
      runs: save.runs,
      toast: null,
      resultsAt: 0,
      dispatchCount: 0,
      lastPhaseChangeAt: Date.now(),
    };

    this.applyLayout();
    this.applyTheme();
    this.setScreen('welcome');
  }

  /* ------------------------------------------------------------------ */
  /* Public surface                                                       */
  /* ------------------------------------------------------------------ */

  handleButton(_panelId: string, button: PanelButton): void {
    const id = button.id;
    if (id.startsWith('dispatch:')) {
      this.dispatchAgent(id.slice('dispatch:'.length) as AgentRole);
      return;
    }

    switch (id) {
      case 'begin':
        this.sfx.select();
        this.setScreen('scenario');
        break;
      case 'tutorial':
        this.sfx.select();
        this.setScreen('tutorial');
        break;
      case 'tutorial-done':
        this.sfx.select();
        this.setScreen('scenario');
        break;
      case 'back-welcome':
        this.sfx.select();
        this.setScreen('welcome');
        break;
      case 'begin-cascade':
        this.beginWorkspace();
        break;
      case 'open-incident':
        this.openIncident();
        break;
      case 'synthesise':
        this.synthesiseFindings();
        break;
      case 'propose':
        this.proposeRemediation();
        break;
      case 'approve':
        this.approveRemediation();
        break;
      case 'reject':
        this.rejectRemediation();
        break;
      case 'execute':
        this.executeRemediation();
        break;
      case 'verify':
        this.verifyRecovery();
        break;
      case 'resolve':
        this.resolveIncident();
        break;
      case 'results':
        this.showResults();
        break;
      case 'replay':
        this.beginWorkspace();
        break;
      case 'pause':
      case 'settings':
        this.openSettings();
        break;
      case 'settings-close':
        this.sfx.select();
        this.setScreen(this.returnScreen);
        break;
      case 'reset-view':
        this.onResetView();
        this.toast('Spatial view reset.', 'info');
        this.sfx.select();
        this.refresh();
        break;
      case 'reset-layout':
        this.applyLayout();
        this.onResetView();
        this.toast('Panels returned to their seated layout.', 'success');
        this.sfx.select();
        this.refresh();
        break;
      case 'text-scale':
        this.setTextScale();
        break;
      case 'contrast':
        this.updateSettings({ highContrast: !this.state.settings.highContrast });
        break;
      case 'motion':
        this.updateSettings({ reducedMotion: !this.state.settings.reducedMotion });
        break;
      case 'audio':
        this.updateSettings({ audio: !this.state.settings.audio });
        break;
      case 'handed':
        this.updateSettings({ leftHanded: !this.state.settings.leftHanded });
        break;
      default:
        break;
    }
  }

  onHover(): void {
    this.sfx.hover();
  }

  selectNode(nodeId: string): void {
    this.state.selectedNodeId = nodeId;
    this.infra.setSelected(nodeId);
    this.sfx.select();
    this.refresh();
  }

  update(dt: number): void {
    const now = Date.now();
    if (this.state.toast && now > this.state.toast.until) {
      this.state.toast = null;
      this.refresh();
    }
    if (this.state.screen === 'workspace') {
      this.tickAccumulator += dt;
      if (this.tickAccumulator >= 1) {
        this.tickAccumulator = 0;
        this.tickSimulation();
      }
      this.refreshAccumulator += dt;
      if (this.refreshAccumulator >= 1) {
        this.refreshAccumulator = 0;
        this.refresh();
      }
    }
  }

  refresh(): void {
    const model = this.model();
    if (this.panels.center.group.visible) this.panels.center.draw((painter) => drawCenterPanel(painter, model));
    if (this.panels.action.group.visible) this.panels.action.draw((painter) => drawActionPanel(painter, model));
    if (this.panels.timeline.group.visible) this.panels.timeline.draw((painter) => drawTimelinePanel(painter, model));
  }

  /* ------------------------------------------------------------------ */
  /* Scenario flow                                                        */
  /* ------------------------------------------------------------------ */

  private beginWorkspace(): void {
    this.sessionToken += 1;
    this.simulation = new Simulation(START_AGGREGATE_SEED);
    this.orchestrator = new AgentOrchestrator();
    this.remediation = new RemediationService();
    this.lifecycle = IncidentLifecycle.create('database', this.simulation.graph.scenarioId, 'critical');
    this.state.phase = 'detected';
    this.state.incident = this.lifecycle.incident;
    this.state.selectedNodeId = 'database';
    this.state.findings = [];
    this.state.proposal = null;
    this.state.runStartedAt = Date.now();
    this.state.tickCount = 0;
    this.state.dispatchCount = 0;
    this.state.toast = null;
    this.state.resultsAt = 0;
    this.state.lastPhaseChangeAt = Date.now();
    this.initialAggregate = this.simulation.aggregateHealth;
    this.tickAccumulator = 0;
    this.refreshAccumulator = 0;

    this.infra.setGraph(this.simulation.graph);
    this.infra.reducedMotion = this.state.settings.reducedMotion;
    this.infra.setSelected('database');
    this.lastStatuses = Object.fromEntries(this.simulation.nodes.map((node) => [node.id, node.status]));

    this.simulation.log('FL-SM-0001', 'CASCADE scenario started', '7 services loaded. Database is below its availability SLO.', 'info');
    this.simulation.log(
      'FL-IC-0050',
      'Incident detected',
      'Database breached its availability SLO. App Service is following.',
      'critical',
    );
    this.simulation.log('FL-AG-0001', 'Analyst agents on standby', 'Dispatch ORACLE, TRACE, SENTRY or FORGE to gather evidence.', 'info');

    this.onResetView();
    this.setScreen('workspace');
    this.sfx.dispatch();
  }

  private openIncident(): void {
    if (!this.lifecycle || this.state.phase !== 'detected') return;
    this.lifecycle.transition('triaged');
    this.lifecycle.transition('investigating');
    this.state.phase = 'investigating';
    this.state.incident = this.lifecycle.incident;
    this.state.lastPhaseChangeAt = Date.now();
    this.simulation.log('FL-IC-0051', 'Incident triaged', 'Severity critical. Root cause suspected in Database.', 'warning');
    this.simulation.log('FL-IC-0052', 'Investigation opened', 'Dispatch analyst agents to collect corroborating evidence.', 'info');
    this.sfx.select();
    this.refresh();
  }

  private dispatchAgent(role: AgentRole): void {
    if (!this.state.incident) return;
    if (this.state.phase !== 'investigating' && this.state.phase !== 'findings') {
      this.toast('Investigation is closed. Finish the current step.', 'warning');
      this.sfx.error();
      this.refresh();
      return;
    }
    if (this.orchestrator.hasDispatched(role)) {
      this.toast(`${role} is already on the incident.`, 'warning');
      this.sfx.error();
      this.refresh();
      return;
    }
    const task = this.orchestrator.dispatch(role, this.state.incident.id);
    if (!task.ok) {
      this.toast(task.message, 'error');
      this.sfx.error();
      // Do not leak a stale task id: dispatch failed, nothing to complete.
      this.refresh();
      return;
    }
    this.state.dispatchCount += 1;
    this.simulation.log('FL-AG-0060', `${role} dispatched`, AGENT_PROFILES[role].blurb, 'info');
    this.sfx.dispatch();
    this.refresh();

    const token = this.sessionToken;
    window.setTimeout(() => {
      if (token !== this.sessionToken) return;
      const result = this.orchestrator.complete(task.value.id);
      if (!result.ok) return;
      this.state.findings = this.orchestrator.allFindings;
      this.simulation.log(
        'FL-AG-0061',
        `${role} finding ready`,
        `${result.value.title} · confidence ${Math.round(result.value.confidence * 100)}%`,
        'success',
      );
      if (this.state.findings.length < 2) {
        this.toast('One more independent finding will unlock remediation.', 'info');
      } else {
        this.toast('Evidence complete. Synthesise the findings.', 'success');
      }
      this.sfx.approve();
      this.refresh();
    }, 1400);
  }

  private synthesiseFindings(): void {
    if (this.state.findings.length < 2) {
      this.toast('Dispatch at least two agents first.', 'warning');
      this.sfx.error();
      this.refresh();
      return;
    }
    this.state.phase = 'findings';
    this.state.lastPhaseChangeAt = Date.now();
    this.simulation.log(
      'FL-AG-0062',
      'Findings synthesised',
      `${this.state.findings.length} findings · confidence ${Math.round(this.orchestrator.averageConfidence() * 100)}%.`,
      'success',
    );
    this.sfx.approve();
    this.refresh();
  }

  private proposeRemediation(): void {
    if (!this.lifecycle || !this.state.incident) return;
    const transition = this.lifecycle.transition('awaitingApproval');
    if (!transition.ok) {
      if (this.state.phase !== 'proposed') return;
      // Already in awaitingApproval (re-propose after rejection).
    }
    const result = this.remediation.propose(this.state.incident.id, 'database', 'RestartService');
    if (!result.ok) {
      this.toast(result.message, 'error');
      this.sfx.error();
      this.refresh();
      return;
    }
    this.state.proposal = result.value;
    this.state.phase = 'proposed';
    this.state.incident = this.lifecycle.incident;
    this.state.lastPhaseChangeAt = Date.now();
    this.simulation.log('FL-RM-0080', 'Remediation proposed', 'RestartService on Database. Awaiting explicit approval.', 'warning');
    this.sfx.select();
    this.refresh();
  }

  private approveRemediation(): void {
    const proposal = this.state.proposal;
    if (!proposal) return;
    const result = this.remediation.approve(proposal.id, 'operator@faultline');
    if (!result.ok) {
      this.toast(result.message, 'warning');
      this.sfx.error();
      this.refresh();
      return;
    }
    this.state.phase = 'approved';
    this.state.lastPhaseChangeAt = Date.now();
    this.simulation.log('FL-RM-0081', 'Approval recorded', 'Explicit approval captured for RestartService.', 'success');
    this.sfx.approve();
    this.refresh();
  }

  private rejectRemediation(): void {
    const proposal = this.state.proposal;
    if (!proposal) return;
    this.remediation.reject(proposal.id, 'operator@faultline', 'Rejected in review.');
    this.state.phase = 'rejected';
    this.state.lastPhaseChangeAt = Date.now();
    this.simulation.log('FL-RM-0082', 'Remediation rejected', 'No changes were executed.', 'warning');
    this.sfx.error();
    this.refresh();
  }

  private executeRemediation(): void {
    const proposal = this.state.proposal;
    if (!proposal) return;
    this.lifecycle?.transition('remediating');
    const result = this.remediation.execute(proposal.id, this.simulation);
    if (!result.ok) {
      this.toast(result.message, 'error');
      this.simulation.restore();
      this.sfx.error();
      this.refresh();
      return;
    }
    this.state.phase = 'executed';
    this.state.incident = this.lifecycle?.incident ?? this.state.incident;
    this.state.lastPhaseChangeAt = Date.now();
    this.simulation.log(
      'FL-RM-0083',
      'Remediation executed',
      `RestartService applied. Health delta +${Math.round(result.value.healthDelta * 100)}%.`,
      'success',
    );
    this.infra.updateNodes(this.simulation.nodes);
    this.sfx.execute();
    this.refresh();
  }

  private verifyRecovery(): void {
    const proposal = this.state.proposal;
    if (!proposal) return;
    this.lifecycle?.transition('verifying');
    const result = this.remediation.verify(proposal.id, this.simulation);
    if (!result.ok) {
      this.toast(result.message, 'error');
      this.sfx.error();
      this.refresh();
      return;
    }
    if (!result.value) {
      this.toast('Verification did not pass. Review the node health.', 'error');
      this.sfx.error();
      this.refresh();
      return;
    }
    this.state.phase = 'verified';
    this.state.incident = this.lifecycle?.incident ?? this.state.incident;
    this.state.lastPhaseChangeAt = Date.now();
    this.simulation.log('FL-VF-0090', 'Verification passed', 'Database and dependent services are healthy.', 'success');
    this.infra.updateNodes(this.simulation.nodes);
    this.sfx.approve();
    this.refresh();
  }

  private resolveIncident(): void {
    if (!this.lifecycle) return;
    const result = this.lifecycle.transition('resolved');
    if (!result.ok) {
      this.toast(result.message, 'error');
      this.sfx.error();
      this.refresh();
      return;
    }
    this.state.phase = 'resolved';
    this.state.incident = this.lifecycle.incident;
    this.state.lastPhaseChangeAt = Date.now();
    this.simulation.log('FL-IC-0059', 'Incident resolved', 'Cascade contained. All dependent services recovered.', 'success');
    this.recordRun();
    this.infra.updateNodes(this.simulation.nodes);
    this.sfx.resolve();
    this.refresh();
  }

  private showResults(): void {
    this.state.resultsAt = Date.now();
    this.setScreen('results');
    this.sfx.select();
  }

  private recordRun(): void {
    if (!this.state.incident) return;
    const run: RunRecord = {
      at: Date.now(),
      scenarioId: this.state.scenarioId,
      resolved: true,
      durationMs: Date.now() - this.state.runStartedAt,
      agentsDispatched: this.state.findings.length,
      averageConfidence: this.orchestrator.averageConfidence(),
      healthRecovered: Math.max(0, this.simulation.aggregateHealth - this.initialAggregate),
      incidentId: this.state.incident.id,
    };
    this.state.runs = [run, ...this.state.runs].slice(0, 25);
    this.persist();
  }

  /* ------------------------------------------------------------------ */
  /* Simulation + screen plumbing                                         */
  /* ------------------------------------------------------------------ */

  private tickSimulation(): void {
    this.simulation.tickOnce();
    this.state.tickCount = this.simulation.tick;
    const nodes = this.simulation.nodes;

    for (const node of nodes) {
      const previous = this.lastStatuses[node.id];
      if (previous && previous !== node.status) {
        this.simulation.log(
          'FL-SM-0020',
          `${node.name} ${node.status}`,
          `Health moved to ${Math.round(node.health * 100)}%.`,
          node.status === 'healthy' ? 'success' : node.status === 'degraded' ? 'warning' : 'critical',
        );
      }
      this.lastStatuses[node.id] = node.status;
    }

    this.infra.updateNodes(nodes);
  }

  private model(): ScreenModel {
    const now = Date.now();
    return {
      state: this.state,
      nodes: this.simulation.nodes,
      events: this.simulation.events,
      aggregateHealth: this.simulation.aggregateHealth,
      rootHealth: this.simulation.rootHealth,
      elapsedMs:
        this.state.screen === 'workspace'
          ? now - this.state.runStartedAt
          : this.state.resultsAt
            ? this.state.resultsAt - this.state.runStartedAt
            : 0,
      now,
    };
  }

  private setScreen(screen: ScreenName): void {
    this.state.screen = screen;
    const workspace = screen === 'workspace';
    this.panels.center.group.visible = !workspace;
    this.panels.action.group.visible = workspace;
    this.panels.timeline.group.visible = workspace;
    this.infra.graphGroup.visible = workspace;
    this.refresh();
  }

  private openSettings(): void {
    this.returnScreen = this.state.screen;
    this.sfx.select();
    this.setScreen('settings');
  }

  private setTextScale(): void {
    const values = [1, 1.15, 1.3];
    const current = this.state.settings.textScale;
    const index = values.findIndex((value) => Math.abs(value - current) < 0.01);
    const next = values[(index + 1 + values.length) % values.length];
    this.updateSettings({ textScale: next });
  }

  private updateSettings(partial: Partial<Settings>): void {
    this.state.settings = { ...this.state.settings, ...partial };
    this.theme = themeFromSettings(this.state.settings);
    this.applyTheme();
    this.infra.reducedMotion = this.state.settings.reducedMotion;
    this.sfx.setEnabled(this.state.settings.audio);
    if (partial.leftHanded !== undefined) this.applyLayout();
    this.persist();
    this.sfx.select();
    this.refresh();
  }

  private applyTheme(): void {
    for (const panel of [this.panels.center, this.panels.action, this.panels.timeline]) {
      panel.setTheme(this.theme);
    }
  }

  private applyLayout(): void {
    const sign = this.state.settings.leftHanded ? -1 : 1;
    this.actionHome.set(0.4 * sign, 1.14, -0.36);
    this.timelineHome.set(-0.4 * sign, 1.14, -0.36);
    this.panels.action.group.position.copy(this.actionHome);
    this.panels.action.group.rotation.set(0, -0.55 * sign, 0);
    this.panels.timeline.group.position.copy(this.timelineHome);
    this.panels.timeline.group.rotation.set(0, 0.55 * sign, 0);
  }

  private persist(): void {
    writeSave({ version: 1, settings: this.state.settings, runs: this.state.runs });
  }

  private toast(text: string, kind: Toast['kind'] = 'info'): void {
    this.state.toast = { text, kind, until: Date.now() + 2800 };
  }
}
