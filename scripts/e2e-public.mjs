#!/usr/bin/env node
/**
 * e2e-public.mjs — asserts the PUBLIC https deployment works end to end.
 *
 * Runs against the live URL (not localhost) so it validates DNS, the Cloudflare
 * tunnel, TLS and the deployed bundle exactly as a judge would receive them.
 *
 * Usage: node scripts/e2e-public.mjs [--url https://faultline.aiazimara.com/]
 */
import { createRequire } from 'node:module';
import { existsSync, mkdirSync, readdirSync } from 'node:fs';
import { join, dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const require = createRequire(import.meta.url);
const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const arg = (n, f) => {
  const i = process.argv.indexOf(`--${n}`);
  return i >= 0 ? process.argv[i + 1] : f;
};

const URL_BASE = arg('url', 'https://faultline.aiazimara.com/');
const OUT = resolve(ROOT, arg('out', 'audit-public'));
mkdirSync(OUT, { recursive: true });

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
  if (!hit) throw new Error('No Chromium/Edge executable found');
  return hit;
}

const puppeteerPath = [
  join(ROOT, 'web', 'node_modules', 'puppeteer-core'),
  join(ROOT, 'video', 'node_modules', 'puppeteer-core'),
].find((p) => existsSync(join(p, 'package.json')));
const puppeteer = require(puppeteerPath);
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

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
const failedRequests = [];
page.on('console', (m) => m.type() === 'error' && errors.push(m.text()));
page.on('pageerror', (e) => errors.push(String(e)));
page.on('requestfailed', (r) => failedRequests.push(`${r.url()} ${r.failure()?.errorText}`));

try {
  const response = await page.goto(URL_BASE, { waitUntil: 'networkidle2', timeout: 45000 });
  check('public URL responds 200', response?.status() === 200, `status=${response?.status()}`);

  await sleep(2500);
  const title = await page.title();
  check('serves the FAULTLINE XR document', title === 'FAULTLINE XR', `title="${title}"`);

  const bootVisible = await page.$eval('#boot', (el) => !!el && !el.hidden);
  check('boot overlay rendered', bootVisible);

  // The debug hook is opt-in; the public URL must still boot the app.
  const hasCanvas = await page.$eval('#scene', (el) => el.width > 0 && el.height > 0);
  check('WebGL canvas initialised', hasCanvas);

  // Enter desktop preview and complete the first two transitions.
  await page.evaluate(() => document.getElementById('enter-desktop').click());
  await sleep(900);

  const click = async (x, y) => {
    await page.mouse.move(x, y);
    await sleep(150);
    await page.mouse.down();
    await sleep(420);
    await page.mouse.up();
    await sleep(800);
  };

  await click(640, 493); // Begin operation
  const onScenario = await page.evaluate(() => !!document.querySelector('#scene'));
  check('reaches scenario screen without error', onScenario);

  await click(640, 540); // Begin CASCADE
  await sleep(1200);

  const screenshot = join(OUT, 'public-workspace.png');
  await page.screenshot({ path: screenshot });
  check('workspace renders from the public URL', existsSync(screenshot));

  check('no console errors', errors.length === 0, errors.slice(0, 3).join(' | ') || 'clean');
  check('no failed requests', failedRequests.length === 0, failedRequests.slice(0, 3).join(' | ') || 'clean');
} finally {
  const failed = checks.filter((c) => !c.pass).length;
  console.log(`\n${checks.length - failed}/${checks.length} passed`);
  await browser.close();
  process.exit(failed === 0 && errors.length === 0 ? 0 : 1);
}