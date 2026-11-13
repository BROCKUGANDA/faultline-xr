#!/usr/bin/env node
/**
 * e2e-settings.mjs — asserts the settings / accessibility flow and persistence.
 *
 * Button coordinates are derived from project-buttons.mjs (exact three.js
 * projection of the screens.ts layout through the main.ts desktop camera pose),
 * so they stay correct if the layout changes.
 *
 * Usage: node scripts/e2e-settings.mjs --url "http://127.0.0.1:4173/?faultline-debug"
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

const puppeteerPath = [
  join(ROOT, 'web', 'node_modules', 'puppeteer-core'),
  join(ROOT, 'video', 'node_modules', 'puppeteer-core'),
].find((p) => existsSync(join(p, 'package.json')));
if (!puppeteerPath) {
  console.error('puppeteer-core not found');
  process.exit(2);
}
const puppeteer = require(puppeteerPath);
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

// Projected centres — see scripts/project-buttons.mjs
const PT = {
  welcome: { begin: [640, 493], tutorial: [588, 526], settings: [728, 526] },
  settings: {
    'text-scale': [640, 397],
    contrast: [640, 432],
    motion: [640, 466],
    audio: [640, 497],
    handed: [640, 526],
    'reset-layout': [640, 554],
    'settings-close': [640, 539],
  },
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
    return { screen: s.screen, settings: s.settings, runs: s.runs.length };
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

  let s = await state();
  check('boots on welcome', s.screen === 'welcome', `screen=${s.screen}`);

  await click(PT.welcome.settings);
  s = await state();
  check('welcome settings button opens settings', s.screen === 'settings', `screen=${s.screen}`);

  const base = s.settings;

  await click(PT.settings['text-scale']);
  s = await state();
  check('text scale 100% → 115%', s.settings.textScale === 1.15, `${base.textScale} → ${s.settings.textScale}`);

  await click(PT.settings.contrast);
  s = await state();
  check('high contrast on', s.settings.highContrast === true, `highContrast=${s.settings.highContrast}`);

  await click(PT.settings.motion);
  s = await state();
  check('reduced motion on', s.settings.reducedMotion === true, `reducedMotion=${s.settings.reducedMotion}`);

  await click(PT.settings.audio);
  s = await state();
  check('audio off', s.settings.audio === false, `audio=${s.settings.audio}`);

  await click(PT.settings.handed);
  s = await state();
  check('left-handed layout on', s.settings.leftHanded === true, `leftHanded=${s.settings.leftHanded}`);

  const raw = await page.evaluate(() => localStorage.getItem('faultline-xr.save.v1'));
  const parsed = raw ? JSON.parse(raw) : null;
  check(
    'settings persisted to localStorage',
    !!parsed && parsed.settings.leftHanded === true && parsed.settings.audio === false && parsed.settings.highContrast === true,
    raw ? `${raw.length} bytes` : 'missing',
  );

  await click(PT.settings['settings-close']);
  s = await state();
  check('Done returns to welcome', s.screen === 'welcome', `screen=${s.screen}`);

  await page.reload({ waitUntil: 'networkidle2' });
  await sleep(1800);
  await page.click('#enter-desktop');
  await sleep(800);
  s = await state();
  check(
    'settings survive reload',
    s.settings.leftHanded === true && s.settings.audio === false && s.settings.textScale === 1.15,
    JSON.stringify(s.settings),
  );

  check('no console/page errors', errors.length === 0, errors.join(' | ') || 'clean');
} finally {
  const failed = checks.filter((c) => !c.pass).length;
  console.log('\n' + JSON.stringify({ failed, passed: checks.length - failed, errors }, null, 2));
  await browser.close();
  process.exit(failed === 0 && errors.length === 0 ? 0 : 1);
}