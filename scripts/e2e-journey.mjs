#!/usr/bin/env node
/**
 * e2e-journey.mjs — asserted end-to-end journey for the FAULTLINE XR production build.
 *
 * Unlike scripts/smoke-web-dist.mjs (coordinate click-through + screenshots),
 * this driver asserts the domain state after every step using the read-only
 * ?faultline-debug hook, and fails loudly if the flow does not reach RESOLVED.
 *
 * Usage:
 *   node scripts/e2e-journey.mjs --url http://127.0.0.1:4173/?faultline-debug
 */
import { createRequire } from 'node:module';
import { existsSync, mkdirSync, readdirSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const require = createRequire(import.meta.url);
const ROOT = join(dirname(fileURLToPath(import.meta.url)), '..');

const arg = (name, fallback) => {
  const i = process.argv.indexOf(`--${name}`);
  return i >= 0 ? process.argv[i + 1] : fallback;
};

const url = arg('url', 'http://127.0.0.1:4173/?faultline-debug');
const outDir = arg('out', join(ROOT, 'audit-e2e-journey'));
mkdirSync(outDir, { recursive: true });

function resolveBrowser() {
  const candidates = [];
  if (process.env.FAULTLINE_CHROME) candidates.push(process.env.FAULTLINE_CHROME);
  const cache = join(process.env.USERPROFILE ?? process.env.HOME ?? '', 'AppData', 'Local', 'ms-playwright');
  if (existsSync(cache)) {
    for (const dir of readdirSync(cache).filter((d) => /^chromium-\d+$/.test(d)).sort().reverse()) {
      candidates.push(join(cache, dir, 'chrome-win64', 'chrome.exe'));
    }
  }
  candidates.push('C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe');
  candidates.push('C:\\Program Files\\Microsoft\\Edge\\Application\\msedge.exe');
  const hit = candidates.find((p) => p && existsSync(p));
  if (!hit) throw new Error('No Chromium/Edge executable found; set FAULTLINE_CHROME.');
  return hit;
}

const puppeteerPath = [
  join(ROOT, 'web', 'node_modules', 'puppeteer-core'),
  join(ROOT, 'video', 'node_modules', 'puppeteer-core'),
].find((p) => existsSync(join(p, 'package.json')));
if (!puppeteerPath) {
  console.error('puppeteer-core not found in web/node_modules or video/node_modules');
  process.exit(2);
}
const puppeteer = require(puppeteerPath);

const consoleErrors = [];
const pageErrors = [];
const checks = [];

function check(label, condition, detail) {
  checks.push({ label, pass: !!condition, detail });
  console.log(`${condition ? 'PASS' : 'FAIL'}  ${label}${detail ? ` — ${detail}` : ''}`);
  return !!condition;
}

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

const browser = await puppeteer.launch({
  executablePath: resolveBrowser(),
  headless: true,
  args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--no-sandbox', '--disable-dev-shm-usage'],
});
const page = await browser.newPage();
await page.setViewport({ width: 1280, height: 720 });
page.on('console', (msg) => {
  if (msg.type() === 'error') consoleErrors.push(msg.text());
});
page.on('pageerror', (err) => pageErrors.push(String(err)));

const state = () =>
  page.evaluate(() => {
    const app = window.__faultlineApp;
    if (!app) return null;
    const s = app.state;
    return {
      screen: s.screen,
      phase: s.phase,
      findings: s.findings.length,
      dispatchCount: s.dispatchCount,
      proposal: s.proposal ? { id: s.proposal.id, status: s.proposal.status } : null,
      incident: s.incident ? { id: s.incident.id, state: s.incident.state } : null,
      settings: s.settings,
      runs: s.runs.length,
      toast: s.toast ? s.toast.text : null,
      recovery: s.proposal && s.proposal.recovery ? s.proposal.recovery : null,
    };
  });

async function click(x, y, label, holdMs = 420) {
  await page.mouse.move(x, y);
  await sleep(140);
  await page.mouse.down();
  await sleep(holdMs);
  await page.mouse.up();
  await sleep(700);
}

try {
  await page.goto(url, { waitUntil: 'networkidle2', timeout: 30000 });
  await sleep(2000);

  check('debug hook exposed', !!(await state()), 'window.__faultlineApp present');

  let s = await state();
  check('boots on welcome screen', s && s.screen === 'welcome', `screen=${s?.screen}`);

  await page.click('#enter-desktop');
  await sleep(900);

  // Welcome: Begin operation
  await click(640, 500);
  s = await state();
  check('welcome → scenario', s.screen === 'scenario', `screen=${s.screen}`);

  await click(640, 540);
  s = await state();
  check('scenario → workspace (detected)', s.screen === 'workspace' && s.phase === 'detected', `screen=${s.screen} phase=${s.phase}`);

  await click(934, 601);
  s = await state();
  check('open incident → investigating', s.phase === 'investigating', `phase=${s.phase} incident=${s.incident?.state}`);

  await click(891, 528);
  s = await state();
  check('dispatch ORACLE counted', s.dispatchCount >= 1, `dispatchCount=${s.dispatchCount}`);
  await sleep(1400);
  s = await state();
  check('ORACLE finding ready', s.findings >= 1, `findings=${s.findings}`);

  await click(1045, 575);
  s = await state();
  check('dispatch TRACE counted', s.dispatchCount >= 2, `dispatchCount=${s.dispatchCount}`);
  await sleep(1400);
  s = await state();
  check('two findings unlocked', s.findings >= 2, `findings=${s.findings}`);

  await click(934, 601);
  s = await state();
  check('synthesise → findings', s.phase === 'findings', `phase=${s.phase}`);

  await click(934, 601);
  s = await state();
  check('propose → proposed + awaitingApproval', s.phase === 'proposed' && s.proposal !== null && s.incident?.state === 'awaitingApproval', `phase=${s.phase} incident=${s.incident?.state} proposal=${s.proposal?.id}`);

  await click(934, 601);
  s = await state();
  check('approve → approved', s.phase === 'approved', `phase=${s.phase}`);

  await click(934, 601);
  s = await state();
  check('execute → executed with recovery', s.phase === 'executed' && s.recovery !== null, `phase=${s.phase} delta=${s.recovery?.healthDelta}`);

  await click(934, 601);
  s = await state();
  check('verify → verified', s.phase === 'verified', `phase=${s.phase}`);

  await click(934, 601);
  s = await state();
  check('resolve → resolved', s.phase === 'resolved' && s.incident?.state === 'resolved', `phase=${s.phase} incident=${s.incident?.state}`);

  await click(934, 601);
  s = await state();
  check('after-action report opens', s.screen === 'results', `screen=${s.screen}`);
  await page.screenshot({ path: join(outDir, 'results.png') });

  check('run record persisted', s.runs >= 1, `runs=${s.runs}`);
  check('no toast error at end', !s.toast, `toast=${s.toast}`);

  // Settings toggles persist
  const persisted = await page.evaluate(() => localStorage.getItem('faultline-xr.save.v1'));
  check('save payload written to localStorage', !!persisted && persisted.includes('"version":1'), persisted ? `${persisted.length} bytes` : 'missing');
} finally {
  const summary = {
    url,
    checks,
    passed: checks.filter((c) => c.pass).length,
    failed: checks.filter((c) => !c.pass).length,
    consoleErrors,
    pageErrors,
  };
  console.log('\n' + JSON.stringify(summary, null, 2));
  await browser.close();
  const okRun = summary.failed === 0 && consoleErrors.length === 0 && pageErrors.length === 0;
  process.exit(okRun ? 0 : 1);
}