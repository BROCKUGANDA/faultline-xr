import type {
  AgentRole,
  Finding,
  Incident,
  RemediationProposal,
  RunRecord,
  ServiceNode,
  Settings,
  TimelineEvent,
} from '../domain';
import { AGENT_PROFILES } from '../domain';
import type { Painter } from './panel';
import { COLORS, statusColor } from './theme';

export type ScreenName = 'welcome' | 'tutorial' | 'scenario' | 'workspace' | 'results' | 'settings';

export type Phase =
  | 'detected'
  | 'investigating'
  | 'findings'
  | 'proposed'
  | 'approved'
  | 'executed'
  | 'verified'
  | 'resolved'
  | 'rejected';

export interface Toast {
  text: string;
  kind: 'info' | 'success' | 'warning' | 'error';
  until: number;
}

export interface AppState {
  screen: ScreenName;
  phase: Phase;
  scenarioId: string;
  incident: Incident | null;
  selectedNodeId: string;
  findings: Finding[];
  proposal: RemediationProposal | null;
  runStartedAt: number;
  tickCount: number;
  settings: Settings;
  runs: RunRecord[];
  toast: Toast | null;
  resultsAt: number;
  dispatchCount: number;
  lastPhaseChangeAt: number;
}

export interface ScreenModel {
  state: AppState;
  nodes: ServiceNode[];
  events: TimelineEvent[];
  aggregateHealth: number;
  rootHealth: number;
  elapsedMs: number;
  now: number;
}

const MARGIN = 0.018;

function secondary(p: Painter): string {
  return p.theme.highContrast ? '#D6E6F8' : COLORS.mist;
}

function fmtDuration(ms: number): string {
  const total = Math.max(0, Math.floor(ms / 1000));
  const minutes = Math.floor(total / 60);
  const seconds = total % 60;
  return `${minutes}:${String(seconds).padStart(2, '0')}`;
}

function fmtTime(at: number): string {
  const date = new Date(at);
  return `${String(date.getHours()).padStart(2, '0')}:${String(date.getMinutes()).padStart(2, '0')}:${String(date.getSeconds()).padStart(2, '0')}`;
}

function pct(value: number): string {
  return `${Math.round(value * 100)}%`;
}

function phaseLabel(phase: Phase): string {
  switch (phase) {
    case 'detected':
      return 'DETECTED';
    case 'investigating':
      return 'INVESTIGATING';
    case 'findings':
      return 'FINDINGS READY';
    case 'proposed':
      return 'AWAITING APPROVAL';
    case 'approved':
      return 'APPROVED';
    case 'executed':
      return 'EXECUTING';
    case 'verified':
      return 'VERIFYING';
    case 'resolved':
      return 'RESOLVED';
    case 'rejected':
      return 'REJECTED';
    default:
      return 'ACTIVE';
  }
}

function phaseColor(phase: Phase): string {
  switch (phase) {
    case 'resolved':
      return COLORS.recoveryGreen;
    case 'rejected':
      return COLORS.criticalRed;
    case 'proposed':
    case 'approved':
      return COLORS.amber;
    default:
      return COLORS.signalCyan;
  }
}

function button(
  p: Painter,
  id: string,
  label: string,
  x: number,
  y: number,
  w: number,
  h: number,
  variant: 'primary' | 'secondary' | 'danger' | 'ghost' = 'secondary',
  enabled = true,
  meta?: string,
): void {
  p.button({ id, label, x, y, w, h, variant, enabled, meta });
}

/* ------------------------------------------------------------------ */
/* Centre panel: welcome, tutorial, scenario, results, settings         */
/* ------------------------------------------------------------------ */

export function drawCenterPanel(p: Painter, model: ScreenModel): void {
  const { state } = model;
  switch (state.screen) {
    case 'tutorial':
      drawTutorial(p);
      return;
    case 'scenario':
      drawScenario(p, model);
      return;
    case 'results':
      drawResults(p, model);
      return;
    case 'settings':
      drawSettings(p, state.settings);
      return;
    case 'workspace':
      return;
    default:
      drawWelcome(p, model);
  }
}

function drawWelcome(p: Painter, model: ScreenModel): void {
  const { width: W, height: H } = p;
  p.panelBackground();
  const x = MARGIN;
  const w = W - MARGIN * 2;

  p.text('META VR START · DEVELOPER COMPETITION 2026', x, 0.016, {
    sizeMm: 3.1,
    color: COLORS.signalCyan,
    weight: 700,
    mono: true,
  });

  p.text('FAULTLINE', x, 0.034, { sizeMm: 13.5, color: COLORS.ice, weight: 700 });
  const titleWidth = p.measure('FAULTLINE', 13.5, 700);
  p.text('XR', x + titleWidth + 0.006, 0.04, { sizeMm: 9, color: COLORS.signalCyan, weight: 700 });

  p.wrap(
    'A cascading failure is tearing through the infrastructure. Diagnose it, dispatch analyst agents, and bring the system back — with your hands.',
    x,
    0.072,
    w,
    { sizeMm: 4.6, color: secondary(p), lineHeightMm: 6.4 },
  );

  let chipX = x;
  for (const label of ['HANDS-FIRST', 'SEATED', 'DETERMINISTIC', '~5 MIN']) {
    chipX += p.chip(label, chipX, 0.108, label === 'HANDS-FIRST' ? COLORS.signalCyan : COLORS.slate, {
      sizeMm: 3.1,
      filled: false,
    }) + 0.006;
  }

  const buttonY = H - 0.145;
  button(p, 'begin', 'Begin operation', x, buttonY, w, 0.052, 'primary', true, 'PINCH');
  button(p, 'tutorial', 'How pinch works', x, buttonY + 0.062, w * 0.62, 0.046, 'secondary');
  button(p, 'settings', 'Settings', x + w * 0.64, buttonY + 0.062, w * 0.36, 0.046, 'ghost');

  const lastRun = model.state.runs[0];
  p.text(
    lastRun
      ? `LAST RUN · ${fmtDuration(lastRun.durationMs)} · ${lastRun.resolved ? 'RESOLVED' : 'INCOMPLETE'} · ${lastRun.agentsDispatched} AGENTS`
      : 'NO API KEY · NO CONTROLLERS · WORKS OFFLINE',
    x,
    H - 0.026,
    { sizeMm: 2.9, color: COLORS.mist, mono: true },
  );
}

function drawTutorial(p: Painter): void {
  const { width: W, height: H } = p;
  p.panelBackground('HANDS, NOT CONTROLLERS', 'OPERATOR BRIEF');
  const x = MARGIN;
  const w = W - MARGIN * 2;
  const rows: [string, string][] = [
    ['01', 'Pinch thumb and index together to select a button, node, or card.'],
    ['02', 'Keep pinching and move your hand to grab and reposition a panel.'],
    ['03', 'Pinch with both hands and pull apart to scale the infrastructure graph.'],
    ['04', 'Open your hand to release. Lost tracking never triggers an action.'],
  ];
  let y = 0.05;
  for (const [index, text] of rows) {
    p.roundedRect(x, y, 0.032, 0.032, 0.016, 'rgba(65,217,245,0.14)', COLORS.signalCyan, 0.0011);
    p.text(index, x + 0.016, y + 0.016, {
      sizeMm: 3.4,
      color: COLORS.signalCyan,
      weight: 700,
      mono: true,
      align: 'center',
      baseline: 'middle',
    });
    p.wrap(text, x + 0.044, y + 0.001, w - 0.044, { sizeMm: 4.3, color: COLORS.ice, lineHeightMm: 5.6 });
    y += 0.062;
  }
  p.divider(x, 0.316, w);
  p.wrap(
    'Everything is within a seated two-foot reach. Every critical action has a visible hand target. Controllers are never required.',
    x,
    0.33,
    w,
    { sizeMm: 4.1, color: secondary(p), lineHeightMm: 5.4 },
  );
  button(p, 'tutorial-done', 'Got it', x, H - 0.07, w * 0.62, 0.05, 'primary', true, 'GO');
  button(p, 'back-welcome', 'Back', x + w * 0.64, H - 0.07, w * 0.36, 0.05, 'ghost');
}

function drawScenario(p: Painter, model: ScreenModel): void {
  const { width: W, height: H } = p;
  p.panelBackground('SELECT SCENARIO', 'OPERATIONS');
  const x = MARGIN;
  const w = W - MARGIN * 2;

  p.roundedRect(x, 0.048, w, 0.225, 0.012, 'rgba(21,33,49,0.6)', COLORS.slate, 0.0012);
  p.text('CASCADE', x + 0.014, 0.058, { sizeMm: 8.4, color: COLORS.ice, weight: 700 });
  p.chip('MANDATORY', x + 0.014, 0.082, COLORS.criticalRed, { sizeMm: 3.2 });
  p.chip('7 SERVICES · 6 DEPENDENCIES', x + 0.014 + 0.056, 0.082, COLORS.slate, { sizeMm: 3.2 });
  p.wrap(
    'Application Service is degraded because Database is failing. Two dependents are being dragged down. Contain the cascade before the gateway follows.',
    x + 0.014,
    0.104,
    w - 0.028,
    { sizeMm: 4.2, color: COLORS.ice, lineHeightMm: 5.4 },
  );
  p.divider(x + 0.014, 0.16, w - 0.028);
  p.text('OBJECTIVE', x + 0.014, 0.17, { sizeMm: 3.1, color: COLORS.mist, mono: true, weight: 700 });
  p.text('Detect → dispatch agents → approve remediation → verify recovery', x + 0.014, 0.183, {
    sizeMm: 4,
    color: COLORS.signalCyan,
    weight: 500,
  });
  p.text('ROOT CAUSE IS HIDDEN UNTIL INVESTIGATED', x + 0.014, 0.207, {
    sizeMm: 3,
    color: COLORS.mist,
    mono: true,
  });

  p.text(
    `RUNS COMPLETED · ${model.state.runs.length}`,
    x,
    0.29,
    { sizeMm: 3.1, color: COLORS.mist, mono: true },
  );

  button(p, 'begin-cascade', 'Begin CASCADE', x, H - 0.075, w, 0.054, 'primary', true, 'ENTER');
  button(p, 'back-welcome', 'Back', x, H - 0.017 - 0.044, w, 0.042, 'ghost');
}

function drawResults(p: Painter, model: ScreenModel): void {
  const { width: W, height: H } = p;
  const { state, nodes } = model;
  p.panelBackground('OPERATION COMPLETE', 'AFTER ACTION REPORT');
  const x = MARGIN;
  const w = W - MARGIN * 2;

  p.statusDot(x + 0.008, 0.056, COLORS.recoveryGreen, 0.006);
  p.text('CASCADE CONTAINED', x + 0.02, 0.05, { sizeMm: 7.2, color: COLORS.recoveryGreen, weight: 700 });
  p.text(`INCIDENT ${state.incident?.id?.slice(-6).toUpperCase() ?? '—'} · RESOLVED`, x + 0.02, 0.068, {
    sizeMm: 3.1,
    color: COLORS.mist,
    mono: true,
  });

  const stats: [string, string][] = [
    ['TIME', fmtDuration(model.elapsedMs)],
    ['AGENTS', String(state.findings.length)],
    ['CONFIDENCE', pct(state.findings.length ? state.findings.reduce((sum, f) => sum + f.confidence, 0) / state.findings.length : 0)],
    ['HEALTH', pct(model.aggregateHealth)],
  ];
  const cell = (w - 0.012) / 2;
  stats.forEach(([label, value], index) => {
    const cx = x + (index % 2) * (cell + 0.012);
    const cy = 0.092 + Math.floor(index / 2) * 0.058;
    p.roundedRect(cx, cy, cell, 0.05, 0.009, 'rgba(21,33,49,0.55)', 'rgba(38,54,74,0.8)', 0.001);
    p.text(label, cx + 0.012, cy + 0.009, { sizeMm: 2.9, color: COLORS.mist, mono: true, weight: 700 });
    p.text(value, cx + 0.012, cy + 0.022, { sizeMm: 7, color: COLORS.ice, weight: 700 });
  });

  p.text('SERVICE HEALTH', x, 0.218, { sizeMm: 3.1, color: COLORS.mist, mono: true, weight: 700 });
  nodes.slice(0, 4).forEach((node, index) => {
    const y = 0.235 + index * 0.026;
    p.text(node.name, x, y, { sizeMm: 3.7, color: COLORS.ice });
    p.progress(x + 0.2, y + 0.002, w - 0.27, 0.006, node.health, statusColor(node.status));
    p.text(pct(node.health), x + w - 0.01, y, { sizeMm: 3.3, color: statusColor(node.status), mono: true, align: 'right' });
  });

  button(p, 'replay', 'Run again', x, H - 0.075, w * 0.62, 0.05, 'primary', true, 'RETRY');
  button(p, 'settings', 'Settings', x + w * 0.64, H - 0.075, w * 0.36, 0.05, 'secondary');
  button(p, 'back-welcome', 'Back to start', x, H - 0.017 - 0.026, w, 0.026, 'ghost');
}

function drawSettings(p: Painter, settings: Settings): void {
  const { width: W, height: H } = p;
  p.panelBackground('SETTINGS & ACCESSIBILITY', 'COMFORT');
  const x = MARGIN;
  const w = W - MARGIN * 2;

  let y = 0.05;
  const row = 0.052;
  const options: [string, string, string][] = [
    ['text-scale', `Text size · ${Math.round(settings.textScale * 100)}%`, 'SIZE'],
    ['contrast', `High contrast · ${settings.highContrast ? 'On' : 'Off'}`, settings.highContrast ? 'ON' : 'OFF'],
    ['motion', `Reduced motion · ${settings.reducedMotion ? 'On' : 'Off'}`, settings.reducedMotion ? 'ON' : 'OFF'],
    ['audio', `Audio cues · ${settings.audio ? 'On' : 'Off'}`, settings.audio ? 'ON' : 'OFF'],
    ['handed', `Left-handed layout · ${settings.leftHanded ? 'On' : 'Off'}`, settings.leftHanded ? 'ON' : 'OFF'],
  ];
  for (const [id, label, meta] of options) {
    button(p, id, label, x, y, w, row - 0.008, 'secondary', true, meta);
    y += row;
  }
  button(p, 'reset-layout', 'Reset panel layout', x, y, w, row - 0.008, 'ghost');
  y += row + 0.004;
  p.wrap(
    'Comfort mode removes pulsing motion and adds a slower, higher-contrast palette. All settings persist locally.',
    x,
    y,
    w,
    { sizeMm: 3.6, color: secondary(p), lineHeightMm: 4.8 },
  );
  button(p, 'settings-close', 'Done', x, H - 0.062, w, 0.05, 'primary', true, 'BACK');
}

/* ------------------------------------------------------------------ */
/* Action panel: the live operation                                     */
/* ------------------------------------------------------------------ */

export function drawActionPanel(p: Painter, model: ScreenModel): void {
  const { width: W, height: H } = p;
  const { state, nodes } = model;
  const x = MARGIN;
  const w = W - MARGIN * 2;
  const selected = nodes.find((node) => node.id === state.selectedNodeId) ?? nodes.find((node) => node.id === 'database');

  p.panelBackground(undefined, undefined, phaseColor(state.phase));
  p.text('LIVE OPERATION', x, 0.012, { sizeMm: 3, color: COLORS.mist, mono: true, weight: 700 });
  p.text('CASCADE', x, 0.024, { sizeMm: 7.4, color: COLORS.ice, weight: 700 });
  p.chip(phaseLabel(state.phase), W - x - 0.11, 0.025, phaseColor(state.phase), { sizeMm: 3.1, filled: true });
  p.text(fmtDuration(model.elapsedMs), W - x, 0.013, {
    sizeMm: 3.3,
    color: COLORS.mist,
    mono: true,
    align: 'right',
  });

  p.text('SYSTEM HEALTH', x, 0.049, { sizeMm: 2.9, color: COLORS.mist, mono: true, weight: 700 });
  p.text(pct(model.aggregateHealth), W - x, 0.049, {
    sizeMm: 3.3,
    color: model.aggregateHealth >= 0.8 ? COLORS.recoveryGreen : COLORS.amber,
    mono: true,
    align: 'right',
  });
  p.progress(x, 0.06, w, 0.006, model.aggregateHealth, model.aggregateHealth >= 0.8 ? COLORS.recoveryGreen : COLORS.amber);

  const incident = state.incident;
  p.roundedRect(x, 0.075, w, 0.062, 0.009, 'rgba(255,82,106,0.08)', 'rgba(255,82,106,0.55)', 0.0011);
  p.statusDot(x + 0.012, 0.088, COLORS.criticalRed, 0.004);
  p.text(incident ? `INCIDENT ${incident.id.slice(-6).toUpperCase()}` : 'INCIDENT —', x + 0.022, 0.081, {
    sizeMm: 3.1,
    color: COLORS.criticalRed,
    mono: true,
    weight: 700,
  });
  p.text(
    `${selected?.name ?? 'Database'} · ${selected?.status.toUpperCase() ?? 'FAILING'} · ${pct(selected?.health ?? 0)}`,
    x + 0.022,
    0.094,
    { sizeMm: 3.8, color: COLORS.ice, weight: 600 },
  );
  p.text(`STATE ${incident?.state.toUpperCase() ?? 'DETECTED'}`, W - x, 0.081, {
    sizeMm: 2.7,
    color: COLORS.mist,
    mono: true,
    align: 'right',
  });

  let y = 0.145;
  // The investigating phase needs the full panel height for the dispatch grid;
  // the selected-node metrics card is collapsed there to avoid overlapping the
  // primary button (hitTest resolves first-drawn buttons, so overlap made
  // 'Synthesise findings' unreachable behind the dispatch grid).
  if (selected && state.phase !== 'investigating') {
    p.roundedRect(x, y, w, 0.07, 0.009, 'rgba(21,33,49,0.5)', 'rgba(38,54,74,0.85)', 0.001);
    p.text(selected.name.toUpperCase(), x + 0.01, y + 0.007, {
      sizeMm: 3.4,
      color: COLORS.ice,
      weight: 700,
      mono: true,
    });
    p.text(selected.type.toUpperCase(), W - x - 0.01, y + 0.007, {
      sizeMm: 2.7,
      color: statusColor(selected.status),
      mono: true,
      align: 'right',
    });
    const metrics: [string, string][] = [
      ['LATENCY', `${Math.round(selected.latencyMs * (1 + (1 - selected.health) * 3))} ms`],
      ['ERROR', pct(Math.min(1, selected.errorRate + (1 - selected.health) * 0.3))],
      ['RPS', String(Math.round(selected.throughput * selected.health))],
    ];
    metrics.forEach(([label, value], index) => {
      const mx = x + 0.01 + index * ((w - 0.02) / 3);
      p.text(label, mx, y + 0.022, { sizeMm: 2.4, color: COLORS.mist, mono: true });
      p.text(value, mx, y + 0.032, { sizeMm: 3.7, color: COLORS.ice, mono: true, weight: 600 });
    });
    p.text('PINCH A NODE TO INSPECT', x + 0.01, y + 0.054, { sizeMm: 2.4, color: COLORS.mist, mono: true });
    y += 0.08;
  }

  switch (state.phase) {
    case 'detected':
      p.wrap('Database is failing and App Service is following. Open the incident to triage.', x, y, w, {
        sizeMm: 3.9,
        color: COLORS.ice,
        lineHeightMm: 5,
      });
      button(p, 'open-incident', 'Open incident', x, H - 0.086, w, 0.05, 'primary', true, 'TRIAGE');
      break;

    case 'investigating': {
      const sy = 0.148;
      p.text('DISPATCH ANALYST AGENTS', x, sy, { sizeMm: 2.9, color: COLORS.mist, mono: true, weight: 700 });
      p.text(
        `${state.findings.length}/2 EVIDENCE REQUIRED`,
        W - x,
        sy,
        { sizeMm: 2.9, color: state.findings.length >= 2 ? COLORS.recoveryGreen : COLORS.amber, mono: true, align: 'right' },
      );
      const roles: AgentRole[] = ['ORACLE', 'TRACE', 'SENTRY', 'FORGE'];
      const bw = (w - 0.008) / 2;
      roles.forEach((role, index) => {
        const bx = x + (index % 2) * (bw + 0.008);
        const by = sy + 0.014 + Math.floor(index / 2) * 0.044;
        const profile = AGENT_PROFILES[role];
        const dispatched = state.findings.some((finding) => finding.role === role);
        button(
          p,
          `dispatch:${role}`,
          profile.name,
          bx,
          by,
          bw,
          0.036,
          dispatched ? 'ghost' : 'secondary',
          !dispatched && state.findings.length < 4,
          dispatched ? 'DONE' : 'DISPATCH',
        );
      });
      p.text(
        state.findings.length >= 2 ? 'Enough evidence. Synthesise findings.' : 'Two findings unlock remediation.',
        x,
        sy + 0.1,
        { sizeMm: 2.7, color: state.findings.length >= 2 ? COLORS.recoveryGreen : COLORS.mist },
      );
      button(p, 'synthesise', 'Synthesise findings', x, H - 0.086, w, 0.05, 'primary', state.findings.length >= 2, 'NEXT');
      break;
    }

    case 'findings': {
      p.text('AGENT FINDINGS', x, y, { sizeMm: 2.9, color: COLORS.mist, mono: true, weight: 700 });
      let fy = y + 0.012;
      state.findings.slice(0, 3).forEach((finding) => {
        const profile = AGENT_PROFILES[finding.role];
        p.roundedRect(x, fy, w, 0.052, 0.008, 'rgba(21,33,49,0.55)', 'rgba(38,54,74,0.8)', 0.001);
        p.text(profile.name, x + 0.009, fy + 0.007, { sizeMm: 3.1, color: profile.color, mono: true, weight: 700 });
        p.progress(x + 0.06, fy + 0.011, w - 0.1, 0.005, finding.confidence, profile.color);
        p.text(pct(finding.confidence), W - x - 0.009, fy + 0.006, {
          sizeMm: 2.9,
          color: profile.color,
          mono: true,
          align: 'right',
        });
        p.wrap(finding.recommendation, x + 0.009, fy + 0.021, w - 0.018, {
          sizeMm: 3.1,
          color: COLORS.ice,
          lineHeightMm: 3.9,
        });
        fy += 0.057;
      });
      button(p, 'propose', 'Propose RestartService', x, H - 0.086, w, 0.05, 'primary', true, 'SAFE');
      break;
    }

    case 'proposed': {
      const proposal = state.proposal;
      p.text('REMEDIATION PROPOSAL', x, y, { sizeMm: 2.9, color: COLORS.mist, mono: true, weight: 700 });
      p.roundedRect(x, y + 0.012, w, 0.085, 0.009, 'rgba(255,190,85,0.08)', 'rgba(255,190,85,0.6)', 0.0011);
      p.text('RestartService', x + 0.01, y + 0.019, { sizeMm: 5, color: COLORS.ice, weight: 700 });
      p.text('DATABASE', W - x - 0.01, y + 0.021, {
        sizeMm: 3,
        color: COLORS.amber,
        mono: true,
        align: 'right',
      });
      p.text('Reversible · no data migration · checkpoint taken before execution', x + 0.01, y + 0.034, {
        sizeMm: 3.1,
        color: secondary(p),
      });
      p.text(`PROPOSAL ${proposal?.id.slice(-6).toUpperCase() ?? '—'} · EXPIRES 10:00`, x + 0.01, y + 0.048, {
        sizeMm: 2.6,
        color: COLORS.mist,
        mono: true,
      });
      p.wrap('Approval is an explicit, recorded action. Execution is idempotent and rolls back on failure.', x, y + 0.106, w, {
        sizeMm: 3.3,
        color: COLORS.ice,
        lineHeightMm: 4.3,
      });
      button(p, 'approve', 'Approve remediation', x, H - 0.086, w, 0.05, 'primary', true, 'RECORD');
      button(p, 'reject', 'Reject', x, H - 0.032, w, 0.026, 'danger');
      break;
    }

    case 'approved':
      p.text('APPROVED', x, y, { sizeMm: 2.9, color: COLORS.recoveryGreen, mono: true, weight: 700 });
      p.wrap(
        `Signed off by the operator at ${fmtTime(state.proposal?.approvedAt ?? Date.now())}. Execution applies the restart, then verifies health before resolution.`,
        x,
        y + 0.014,
        w,
        { sizeMm: 3.6, color: COLORS.ice, lineHeightMm: 4.7 },
      );
      button(p, 'execute', 'Execute remediation', x, H - 0.086, w, 0.05, 'primary', true, 'RUN');
      break;

    case 'executed':
      p.text('EXECUTION COMPLETE', x, y, { sizeMm: 2.9, color: COLORS.signalCyan, mono: true, weight: 700 });
      p.wrap(
        `RestartService applied. Database health recovered by ${pct(state.proposal?.recovery?.healthDelta ?? 0)}. Verification checks the root node and aggregate health.`,
        x,
        y + 0.014,
        w,
        { sizeMm: 3.6, color: COLORS.ice, lineHeightMm: 4.7 },
      );
      button(p, 'verify', 'Verify recovery', x, H - 0.086, w, 0.05, 'primary', true, 'CHECK');
      break;

    case 'verified':
      p.text('VERIFICATION PASSED', x, y, { sizeMm: 2.9, color: COLORS.recoveryGreen, mono: true, weight: 700 });
      p.wrap(
        'Database is healthy and the aggregate service health is above target. The incident can be resolved safely.',
        x,
        y + 0.014,
        w,
        { sizeMm: 3.6, color: COLORS.ice, lineHeightMm: 4.7 },
      );
      button(p, 'resolve', 'Resolve incident', x, H - 0.086, w, 0.05, 'primary', true, 'CLOSE');
      break;

    case 'resolved':
      p.text('INCIDENT RESOLVED', x, y, { sizeMm: 2.9, color: COLORS.recoveryGreen, mono: true, weight: 700 });
      p.wrap('The cascade is contained. Open the after-action report for the full timeline.', x, y + 0.014, w, {
        sizeMm: 3.6,
        color: COLORS.ice,
        lineHeightMm: 4.7,
      });
      button(p, 'results', 'View after-action report', x, H - 0.086, w, 0.05, 'primary', true, 'REPORT');
      break;

    case 'rejected':
      p.text('PROPOSAL REJECTED', x, y, { sizeMm: 2.9, color: COLORS.criticalRed, mono: true, weight: 700 });
      p.wrap('No changes were executed. Re-propose a remediation or review the evidence again.', x, y + 0.014, w, {
        sizeMm: 3.6,
        color: COLORS.ice,
        lineHeightMm: 4.7,
      });
      button(p, 'propose', 'Propose again', x, H - 0.086, w, 0.05, 'primary', true, 'RETRY');
      break;
  }

  button(p, 'pause', 'Pause / settings', x, H - 0.026, w * 0.62, 0.02, 'ghost');
  button(p, 'reset-view', 'Reset view', x + w * 0.64, H - 0.026, w * 0.36, 0.02, 'ghost');
}

/* ------------------------------------------------------------------ */
/* Timeline panel                                                       */
/* ------------------------------------------------------------------ */

export function drawTimelinePanel(p: Painter, model: ScreenModel): void {
  const { width: W, height: H } = p;
  p.panelBackground('INVESTIGATION RAIL', 'EVENTS');
  const x = MARGIN;
  const w = W - MARGIN * 2;

  const events = [...model.events].reverse().slice(0, 7);
  let y = 0.045;
  if (!events.length) {
    p.text('Awaiting first signal…', x, y, { sizeMm: 3.6, color: COLORS.mist });
  }
  for (const event of events) {
    const color =
      event.severity === 'critical'
        ? COLORS.criticalRed
        : event.severity === 'warning'
          ? COLORS.amber
          : event.severity === 'success'
            ? COLORS.recoveryGreen
            : COLORS.signalCyan;
    p.statusDot(x + 0.004, y + 0.006, color, 0.003);
    p.text(fmtTime(event.at), x + 0.013, y, { sizeMm: 2.5, color: COLORS.mist, mono: true });
    p.text(event.code, W - x, y, { sizeMm: 2.4, color, mono: true, align: 'right' });
    p.text(event.title, x + 0.013, y + 0.012, { sizeMm: 3.4, color: COLORS.ice, weight: 600 });
    p.wrap(event.detail, x + 0.013, y + 0.023, w - 0.013, {
      sizeMm: 2.9,
      color: secondary(p),
      lineHeightMm: 3.6,
      opacity: 0.95,
    });
    y += 0.059;
  }

  p.divider(x, H - 0.044, w);
  p.text(`EVENTS ${model.events.length}`, x, H - 0.036, { sizeMm: 2.7, color: COLORS.mist, mono: true });
  p.text(`TICK ${model.state.tickCount}`, W - x, H - 0.036, {
    sizeMm: 2.7,
    color: COLORS.mist,
    mono: true,
    align: 'right',
  });
  p.text('PINCH A NODE TO INSPECT', x, H - 0.024, { sizeMm: 2.6, color: COLORS.slate, mono: true });
}

export function drawToast(p: Painter, toast: Toast): void {
  const { width: W } = p;
  const color =
    toast.kind === 'error'
      ? COLORS.criticalRed
      : toast.kind === 'warning'
        ? COLORS.amber
        : toast.kind === 'success'
          ? COLORS.recoveryGreen
          : COLORS.signalCyan;
  const textWidth = Math.min(W - 0.04, p.measure(toast.text, 3.4, 600) + 0.03);
  p.roundedRect(W / 2 - textWidth / 2, 0.004, textWidth, 0.024, 0.012, 'rgba(7,11,18,0.92)', color, 0.0012);
  p.text(toast.text, W / 2, 0.009, { sizeMm: 3.4, color, weight: 600, align: 'center' });
}
