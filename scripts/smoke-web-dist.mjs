#!/usr/bin/env node
/**
 * smoke-web-dist.mjs — headless smoke/e2e driver for the FAULTLINE XR production build.
 *
 * Launches a local Chromium/Edge against an ALREADY-SERVING URL of web/dist/
 * (start one with `npx vite preview --port 4173 --host 127.0.0.1` from web/),
 * collects console errors and page errors, walks the desktop-preview journey
 * using canvas click coordinates from a steps JSON file, and screenshots after
 * each step. This verifies the built artifact the way a reviewer sees it; it is
 * NOT a substitute for Meta Quest Browser device verification.
 *
 * Usage:
 *   node scripts/smoke-web-dist.mjs --url http://127.0.0.1:4173/
 *                                    [--steps scripts/smoke-steps.json]
 *                                    [--out audit-2026-11-10]
 *
 * Steps JSON: { "steps": [ { "label": "enter-desktop" }, { "label": "click", "x": 640, "y": 500 }, ... ] }
 * A step with only "label" waits 800 ms and screenshots without clicking.
 * The script exits non-zero if any console error or page error was recorded.
 *
 * Chromium is resolved from (first hit wins):
 *   1. FAULTLINE_CHROME env var
 *   2. ms-playwright cache (chromium-N folders, chrome-win64/chrome.exe)
 *   3. Microsoft Edge at the default Windows install path
 */
import { createRequire } from 'node:module';
import { existsSync, readFileSync, mkdirSync, readdirSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const require = createRequire(import.meta.url);
const ROOT = join(dirname(fileURLToPath(import.meta.url)), '..');

function resolveBrowser() {
  if (process.env.FAULTLINE_CHROME && existsSync(process.env.FAULTLINE_CHROME)) {
    return process.env.FAULTLINE_CHROME;
  }
  const cache = join(process.env.USERPROFILE ?? process.env.HOME ?? '', 'AppData', 'Local', 'ms-playwright');
  if (existsSync(cache)) {
    const candidates = readdirSync(cache)
      .filter((d) => /^chromium-\d+$/.test(d))
      .sort()
      .reverse();
    for (const dir of candidates) {
      const exe = join(cache, dir, 'chrome-win64', 'chrome.exe');
      if (existsSync(exe)) return exe;
    }
  }
  const edge = 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';
  if (existsSync(edge)) return edge;
  throw new Error('No Chromium/Edge executable found; set FAULTLINE_CHROME.');
}

function arg(name, fallback) {
  const i = process.argv.indexOf(`--${name}`);
  return i >= 0 ? process.argv[i + 1] : fallback;
}

const url = arg('url', 'http://127.0.0.1:4173/');
const stepsPath = arg('steps', join(ROOT, 'scripts', 'smoke-steps.json'));
const outDir = arg('out', join(ROOT, 'audit-2026-11-10'));
const evalExpr = arg('eval', null);
mkdirSync(outDir, { recursive: true });

const steps = existsSync(stepsPath)
  ? JSON.parse(readFileSync(stepsPath, 'utf8')).steps ?? []
  : [{ label: 'enter-desktop' }];

const puppeteerCandidates = [
  join(ROOT, 'web', 'node_modules', 'puppeteer-core'),
  join(ROOT, 'video', 'node_modules', 'puppeteer-core'),
];
const puppeteerPath = puppeteerCandidates.find((p) => existsSync(join(p, 'package.json')));
if (!puppeteerPath) {
  console.error('puppeteer-core not found in web/node_modules or video/node_modules');
  process.exit(2);
}
const puppeteer = require(puppeteerPath);

const consoleErrors = [];
const pageErrors = [];

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

await page.goto(url, { waitUntil: 'networkidle2', timeout: 30000 });
await new Promise((r) => setTimeout(r, 1500));
await page.screenshot({ path: join(outDir, 'smoke-00-boot.png') });

let stepIndex = 0;
for (const step of steps) {
  stepIndex += 1;
  const name = String(step.label ?? `step-${stepIndex}`).replace(/[^a-z0-9-_]/gi, '-').toLowerCase();
  if (step.label === 'enter-desktop') {
    await page.click('#enter-desktop');
    await new Promise((r) => setTimeout(r, 1200));
  } else if (typeof step.x === 'number' && typeof step.y === 'number') {
    await page.mouse.move(step.x, step.y);
    await new Promise((r) => setTimeout(r, 120));
    // The app samples pointer state in its render loop, so the button must stay
    // held across at least one frame before release registers a pinch.
    await page.mouse.down();
    // Hold across several render frames: headless software WebGL can drop below
    // 10 fps, and the app only detects the pinch edge in a frame callback.
    await new Promise((r) => setTimeout(r, 420));
    await page.mouse.up();
    await new Promise((r) => setTimeout(r, step.wait ?? 900));
  } else {
    await new Promise((r) => setTimeout(r, step.wait ?? 800));
  }
  await page.screenshot({ path: join(outDir, `smoke-${String(stepIndex).padStart(2, '0')}-${name}.png`) });
}

if (evalExpr) {
  const result = await page.evaluate(evalExpr);
  console.log('eval:', JSON.stringify(result));
}

await browser.close();
console.log(JSON.stringify({ url, steps: stepIndex, consoleErrors, pageErrors, outDir }, null, 2));
if (consoleErrors.length > 0 || pageErrors.length > 0) process.exit(1);
