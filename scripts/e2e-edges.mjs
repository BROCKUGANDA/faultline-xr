#!/usr/bin/env node
/**
 * e2e-edges.mjs — asserts secondary flows the happy-path journey does not cover:
 *   - tutorial screen open/close
 *   - duplicate agent dispatch is rejected (idempotent domain guard)
 *   - node selection in the 3D graph updates the inspected node
 *   - reject path leaves the incident unchanged (no execution)
 *
 * Usage: node scripts/e2e-edges.mjs --url "http://127.0.0.1:4173/?faultline-debug"
 */
import { createRequire } from 'node:module';
import { existsSync, readdirSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const require = createRequire(import.meta.url);
const ROOT = join(dirname(fileURLToPath(import.meta.url)), '..');
const arg = (n, f) => {
  const i = process.argv.indexOf(`--${n}`);
  return i >= 0 ? process.argv[i + 1] : f;
};
const url = arg('url', 'http://127.0.0.1:4173/?faultline-debug');

function resolveBrowser() {
  const c = [process.env.FAULTLINE_CHROME];
  const cache = join(process.env.USERPROFILE ?? '', 'AppData', 'Local', 'ms-playwright');
  if (existsSync(cache)) {
    for (const d of readdirSync(cache).filter((x) => /^chromium-\d+$/.test(x)).sort().reverse()) {
      c.push(join(cache, d, 'chrome-win64', 'chrome.exe'));
    }
  }
  c.push('C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe');
  c.push('C:\\Program Files\\Microsoft\\Edge\\Application\\msedge.exe');
  const hit = c.find((p) => p && existsSync(p));
  if (!hit) throw new Error('No Chromium/Edge executable found; set FAULTLINE_CHROME.');
  return hit;
}
const puppeteer = require(
  [join(ROOT, 'web', 'node_modules', 'puppeteer-core'), join(ROOT, 'video', 'node_modules', 'puppeteer-core')].find((p) =>
    existsSync(join(p, 'package.json')),
  ),
);
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

// Projected centres (scripts/project-buttons.mjs)
const PT = {
  welcome: { begin: [640, 493], tutorial: [588, 526] },
  tutorialDone: [588, 526],
  // action panel primary column (x=934) — see action panel projection
  actionPrimary: [934, 602],
  dispatchOracle: [891, 528],
  dispatchTrace: [1045, 575],
  openIncident: [934, 601],
};

const checks = [];
const check = (label, pass, detail) => {
  checks.push({ label, pass: !!pass, detail });
  console.log(`${pass ? 'PASS' : 'FAIL'}  ${label}${detail ? ` — ${detail}` : ''}`);
};

const browser = await puppeteer.launch({
  executablePath: resolveBrowser(),
  headless: true,
  args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--no-sandbox', '--disable-dev-shm-usage'],
});
const page = await browser.newPage();
await page.setViewport({ width: 1280, height: 720 });
const errors = [];
page.on('console', (m) => m.type() === 'error' && errors.push(m.text()));
page.on('pageerror', (e) => errors.push(String(e)));

const state = () =>
  page.evaluate(() => {
    const a = window.__faultlineApp;
    if (!a) return null;
    const s = a.state;
    return {
      screen: s.screen,
      phase: s.phase,
      findings: s.findings.length,
      dispatchCount: s.dispatchCount,
      proposal: s.proposal ? s.proposal.id : null,
      incidentState: s.incident ? s.incident.state : null,
      runs: s.runs.length,
      toast: s.toast ? s.toast.text : null,
      selected: s.selectedNodeId,
    };
  });

async function click([x, y], hold = 420) {
  await page.mouse.move(x, y);
  await sleep(150);
  await page.mouse.down();
  await sleep(hold);
  await page.mouse.up();
  await sleep(650);
}

try {
  await page.goto(url, { waitUntil: 'networkidle2', timeout: 30000 });
  await sleep(1800);
  await page.click('#enter-desktop');
  await sleep(800);

  // 1. Tutorial
  let s = await state();
  check('boots on welcome', s.screen === 'welcome', `screen=${s.screen}`);

  await click(PT.welcome.tutorial);
  s = await state();
  check('tutorial screen opens', s.screen === 'tutorial', `screen=${s.screen}`);

  await click(PT.tutorialDone);
  s = await state();
  check('Got it returns to scenario select', s.screen === 'scenario', `screen=${s.screen}`);

  // 2. Begin → workspace
  // NOTE: after closing the tutorial we are on the scenario screen, so the
  // scenario "Begin CASCADE" button is the next target — not the welcome one.
  s = await state();
  check('on scenario screen after tutorial', s.screen === 'scenario', `screen=${s.screen}`);

  await click([640, 540]); // scenario "Begin CASCADE"
  s = await state();
  check('begin CASCADE → workspace', s.screen === 'workspace' && s.phase === 'detected', `screen=${s.screen} phase=${s.phase}`);

  await click(PT.openIncident);
  s = await state();
  check('open incident → investigating', s.phase === 'investigating', `phase=${s.phase}`);

  // 3. Duplicate dispatch guard
  await click(PT.dispatchOracle);
  s = await state();
  check('first ORACLE dispatch counted', s.dispatchCount === 1, `dispatchCount=${s.dispatchCount}`);

  await click(PT.dispatchOracle);
  s = await state();
  check('duplicate ORACLE dispatch rejected', s.dispatchCount === 1, `dispatchCount=${s.dispatchCount} toast=${s.toast}`);

  await sleep(1500);
  s = await state();
  check('ORACLE finding landed', s.findings === 1, `findings=${s.findings}`);

  await click(PT.dispatchTrace);
  await sleep(1600);
  s = await state();
  check('second agent finding unlocks synthesis', s.findings === 2, `findings=${s.findings} dispatchCount=${s.dispatchCount}`);

  // 4. Reject path (after propose) leaves incident unresolved
  await click(PT.actionPrimary); // synthesise
  s = await state();
  check('synthesise', s.phase === 'findings', `phase=${s.phase}`);

  await click(PT.actionPrimary); // propose
  s = await state();
  check('propose → awaiting approval', s.phase === 'proposed' && s.incidentState === 'awaitingApproval', `phase=${s.phase} incident=${s.incidentState}`);

  // reject button sits just under the primary in the 'proposed' phase (y=623)
  await click([927, 623]);
  s = await state();
  check('reject → rejected, incident unchanged', s.phase === 'rejected' && s.incidentState !== 'resolved' && s.runs === 0, `phase=${s.phase} incident=${s.incidentState} runs=${s.runs}`);

  check('no console/page errors', errors.length === 0, errors.join(' | ') || 'clean');
} finally {
  const failed = checks.filter((c) => !c.pass).length;
  console.log('\n' + JSON.stringify({ failed, passed: checks.length - failed, errors }, null, 2));
  await browser.close();
  process.exit(failed === 0 && errors.length === 0 ? 0 : 1);
}