#!/usr/bin/env node
/**
 * build-twa-apk.mjs — builds a signed FAULTLINE XR Android APK from the TWA
 * project in android-twa/, without Gradle.
 *
 * Why no Gradle: the TWA here is a thin launcher (one activity, no AndroidX
 * dependencies), so the canonical aapt2 -> javac -> d8 -> zipalign -> apksigner
 * chain is both faster and hermetic. It needs no network at build time.
 *
 * Prerequisites (resolved from env, with sensible Windows defaults):
 *   JAVA_HOME       JDK 17+ (javac, keytool)
 *   ANDROID_HOME    Android SDK (build-tools, platforms)
 *
 * Usage:
 *   node scripts/build-twa-apk.mjs --host faultline.aiazimara.com
 *   node scripts/build-twa-apk.mjs --host faultline.aiazimara.com --out submission/build
 *
 * The script fails loudly and exits non-zero if any stage fails; it never
 * reports success without verifying the artifact exists and is signed.
 */
import { execFileSync } from 'node:child_process';
import { existsSync, mkdirSync, rmSync, copyFileSync, readFileSync, writeFileSync, readdirSync } from 'node:fs';
import { join, dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { createHash } from 'node:crypto';

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const arg = (name, fallback) => {
  const i = process.argv.indexOf(`--${name}`);
  return i >= 0 ? process.argv[i + 1] : fallback;
};

const HOST = arg('host', '');
const OUT = resolve(ROOT, arg('out', 'submission/build'));
const APP_NAME = arg('name', 'FAULTLINE XR');
const KEYSTORE = arg('keystore', join(OUT, 'faultline-xr.keystore'));
const KEY_ALIAS = arg('alias', 'faultline-xr');
const DIALOG = arg('keypass', process.env.TWA_KEYSTORE_PASSWORD || '');

if (!HOST) {
  console.error('FAIL  --host is required (e.g. --host faultline.aiazimara.com)');
  process.exit(2);
}
if (!DIALOG) {
  console.error('FAIL  no keystore password. Pass --keypass or set TWA_KEYSTORE_PASSWORD.');
  process.exit(2);
}

/* ------------------------------------------------------------------ */
/* Toolchain resolution                                                */
/* ------------------------------------------------------------------ */

function resolveJavaHome() {
  const candidates = [
    process.env.JAVA_HOME,
    'D:\\Tools\\jdk\\jdk-21.0.13.0.2+1',
    'C:\\Program Files\\Android\\Android Studio\\jbr',
    'C:\\Program Files\\Java\\jdk-21',
    'C:\\Program Files\\Eclipse Adoptium\\jdk-21',
  ].filter(Boolean);
  for (const c of candidates) {
    const javac = join(c, 'bin', 'javac');
    const exe = process.platform === 'win32' ? `${javac}.exe` : javac;
    if (existsSync(exe)) return c;
  }
  throw new Error('No JDK 17+ found (javac missing). Set JAVA_HOME.');
}

function resolveAndroidHome() {
  const candidates = [
    process.env.ANDROID_HOME,
    process.env.ANDROID_SDK_ROOT,
    join(process.env.LOCALAPPDATA ?? '', 'Android', 'Sdk'),
  ].filter(Boolean);
  for (const c of candidates) {
    if (existsSync(join(c, 'platforms')) && existsSync(join(c, 'build-tools'))) return c;
  }
  throw new Error('No Android SDK found (platforms/ + build-tools/ missing). Set ANDROID_HOME.');
}

const JAVA_HOME = resolveJavaHome();
const ANDROID_HOME = resolveAndroidHome();

function buildTools() {
  const dir = join(ANDROID_HOME, 'build-tools');
  const versions = readdirSync(dir)
    .filter((v) => /^\d+\.\d+\.\d+$/.test(v))
    .sort((a, b) => b.localeCompare(a, undefined, { numeric: true }));
  if (!versions.length) throw new Error(`No build-tools installed under ${dir}`);
  return join(dir, versions[0]);
}
const BT = buildTools();

function platform() {
  const dir = join(ANDROID_HOME, 'platforms');
  const versions = readdirSync(dir)
    .filter((v) => /^android-\d+$/.test(v))
    .map((v) => Number(v.replace('android-', '')))
    .sort((a, b) => b - a);
  if (!versions.length) throw new Error(`No platforms installed under ${dir}`);
  return { androidJar: join(dir, `android-${versions[0]}`, 'android.jar'), api: versions[0] };
}
const PLATFORM = platform();

const exe = (name) => (process.platform === 'win32' ? `${name}.exe` : name);
const AAPT2 = join(BT, exe('aapt2'));
const ZIPALIGN = join(BT, exe('zipalign'));
const APKSIGNER = join(BT, process.platform === 'win32' ? 'apksigner.bat' : 'apksigner');
const JAVAC = join(JAVA_HOME, 'bin', exe('javac'));
const KEYTOOL = join(JAVA_HOME, 'bin', exe('keytool'));
const JAR = join(JAVA_HOME, 'bin', exe('jar'));
const JAVA = join(JAVA_HOME, 'bin', exe('java'));
// d8.bat cannot be launched via execFileSync on Windows without a shell, and
// it also depends on JAVA_HOME being set in the child environment. Invoking
// com.android.tools.r8.D8 directly with the bundled d8.jar is the reliable
// no-shell path and gives us full control of the heap settings.
const D8_JAR = [join(BT, 'd8.jar'), join(BT, 'lib', 'd8.jar')].find((p) => existsSync(p)) ?? '';
const D8_ARGS = [
  '-Xmx1024M',
  '-cp', D8_JAR,
  'com.android.tools.r8.D8',
];

for (const [label, path] of Object.entries({ AAPT2, ZIPALIGN, APKSIGNER, JAVAC, KEYTOOL, JAR, JAVA })) {
  if (!existsSync(path)) throw new Error(`${label} not found at ${path}`);
}
if (!D8_JAR) throw new Error(`d8.jar not found under ${BT}`);
if (!existsSync(PLATFORM.androidJar)) throw new Error(`android.jar not found at ${PLATFORM.androidJar}`);

/* ------------------------------------------------------------------ */
/* Stage runner                                                        */
/* ------------------------------------------------------------------ */

const SRC = join(ROOT, 'android-twa', 'app', 'src', 'main');
const WORK = join(OUT, 'apk-build');
const STAGE = join(WORK, 'stage');
const COMPILED = join(WORK, 'compiled');
const DEX = join(WORK, 'dex');
const UNSIGNED = join(OUT, 'faultline-xr-unsigned.apk');
const ALIGNED = join(OUT, 'faultline-xr-aligned.apk');
const FINAL = join(OUT, 'faultline-xr.apk');

function run(label, command, args, options = {}) {
  console.log(`\n--- ${label} ---`);
  try {
    const out = execFileSync(command, args, { encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'], ...options });
    if (out && out.trim()) console.log(out.trim().split('\n').slice(-8).join('\n'));
    return out;
  } catch (error) {
    const detail = `${error.stdout ?? ''}${error.stderr ?? ''}`.trim();
    console.error(`FAIL  ${label}\n${detail.split('\n').slice(-30).join('\n')}`);
    throw new Error(`${label} failed (exit ${error.status})`);
  }
}

/* ------------------------------------------------------------------ */
/* Prepare                                                             */
/* ------------------------------------------------------------------ */

mkdirSync(OUT, { recursive: true });
rmSync(WORK, { recursive: true, force: true });
mkdirSync(STAGE, { recursive: true });
mkdirSync(COMPILED, { recursive: true });
mkdirSync(DEX, { recursive: true });

// Inject the resolved public URL into LaunchUrl.java (single source of truth).
const launchUrlPath = join(SRC, 'java', 'com', 'faultline', 'xr', 'LaunchUrl.java');
const original = readFileSync(launchUrlPath, 'utf8');
const injected = original.replace(
  /return "[^"]*";/,
  `return "https://${HOST}/";`,
);
if (injected === original && !original.includes(`https://${HOST}/`)) {
  throw new Error('Could not inject launch URL into LaunchUrl.java');
}
writeFileSync(launchUrlPath, injected, 'utf8');
console.log(`Launch URL: https://${HOST}/`);

try {
  /* 1. Compile + link resources */
  run('aapt2 compile', AAPT2, [
    'compile',
    '--dir', join(SRC, 'res'),
    '-o', join(WORK, 'res.zip'),
  ]);

  run('aapt2 link', AAPT2, [
    'link',
    '-o', UNSIGNED,
    '-I', PLATFORM.androidJar,
    '--manifest', join(SRC, 'AndroidManifest.xml'),
    '--java', COMPILED,
    '--min-sdk-version', '24',
    '--target-sdk-version', String(PLATFORM.api),
    '--version-code', '1',
    '--version-name', '1.0.0',
    '--no-version-vectors',
    join(WORK, 'res.zip'),
  ]);

  /* 2. Compile Java */
  const sources = [];
  const walk = (dir) => {
    for (const entry of readdirSync(dir, { withFileTypes: true })) {
      const full = join(dir, entry.name);
      if (entry.isDirectory()) walk(full);
      else if (entry.name.endsWith('.java')) sources.push(full);
    }
  };
  walk(join(SRC, 'java'));
  // aapt2 --java emits the generated R class into COMPILED. It must be compiled
  // alongside the sources, or any R.* reference fails with "package R does not
  // exist". It is generated, so it lives outside SRC and is picked up here.
  if (existsSync(COMPILED)) walk(COMPILED);
  if (!sources.some((s) => s.endsWith('R.java'))) {
    throw new Error('aapt2 did not generate R.java — resource references will not compile');
  }
  run('javac', JAVAC, [
    '-source', '8',
    '-target', '8',
    '-encoding', 'UTF-8',
    // Compile against android.jar on the classpath (not the bootclasspath,
    // which javac 12+ rejects for target 8+). This is the standard
    // no-Gradle Android compile path.
    '-classpath', PLATFORM.androidJar,
    '-nowarn',
    '-Xlint:-options',
    '-d', STAGE,
    ...sources,
  ]);

  /* 3. DEX */
  const classes = [];
  const walkClass = (dir) => {
    for (const entry of readdirSync(dir, { withFileTypes: true })) {
      const full = join(dir, entry.name);
      if (entry.isDirectory()) walkClass(full);
      else if (entry.name.endsWith('.class')) classes.push(full);
    }
  };
  walkClass(STAGE);
  run('d8', JAVA, [
    ...D8_ARGS,
    '--release',
    '--lib', PLATFORM.androidJar,
    '--min-api', '24',
    '--output', DEX,
    ...classes,
  ]);

  /* 4. Add classes.dex into the APK.
        aapt2 link emits a resource-only APK; the DEX must be inserted into the
        same archive before alignment. `jar` (JDK) updates the zip in place,
        which is deterministic and needs no extra tooling. */
  const dexFile = readdirSync(DEX).find((f) => f.endsWith('.dex'));
  if (!dexFile) throw new Error('d8 produced no classes.dex');
  run('jar add classes.dex', JAR, ['uf', UNSIGNED, '-C', DEX, dexFile]);

  /* 5. Zipalign (must run after DEX insertion so every entry is aligned) */
  run('zipalign', ZIPALIGN, ['-f', '4', UNSIGNED, ALIGNED]);

  /* 6. Keystore */
  if (!existsSync(KEYSTORE)) {
    run('keytool generate', KEYTOOL, [
      '-genkeypair',
      '-keystore', KEYSTORE,
      '-storepass', DIALOG,
      '-keypass', DIALOG,
      '-alias', KEY_ALIAS,
      '-keyalg', 'RSA',
      '-keysize', '2048',
      '-validity', '10000',
      '-dname', 'CN=FAULTLINE XR, OU=Engineering, O=FAULTLINE, L=Kampala, ST=UG, C=UG',
    ]);
  }

  /* 7. Sign. apksigner.bat cannot be exec'd directly on Windows without a
        shell, so invoke its wrapper jar with java for a hermetic path. */
  const APKSIGNER_JAR = join(BT, 'lib', 'apksigner.jar');
  const signerCommand = existsSync(APKSIGNER_JAR) ? JAVA : APKSIGNER;
  const signerArgs = existsSync(APKSIGNER_JAR)
    ? ['-cp', APKSIGNER_JAR, 'com.android.apksigner.ApkSignerTool']
    : [];
  run('apksigner sign', signerCommand, [
    ...signerArgs,
    'sign',
    '--ks', KEYSTORE,
    '--ks-pass', `pass:${DIALOG}`,
    '--key-pass', `pass:${DIALOG}`,
    '--ks-key-alias', KEY_ALIAS,
    '--out', FINAL,
    ALIGNED,
  ]);

  run('apksigner verify', signerCommand, [...signerArgs, 'verify', '--verbose', FINAL]);

  /* 8. Report */
  const sha = createHash('sha256').update(readFileSync(FINAL)).digest('hex').toUpperCase();
  const size = readFileSync(FINAL).length;
  console.log(`\nAPK:      ${FINAL}`);
  console.log(`Size:     ${size} bytes (${(size / 1024 / 1024).toFixed(2)} MB)`);
  console.log(`SHA-256:  ${sha}`);
  console.log(`Package:  com.faultline.xr`);
  console.log(`URL:      https://${HOST}/`);
} finally {
  // Restore the placeholder so the repo stays clean and re-runnable.
  const restored = original.replace(/return "[^"]*";/, 'return "https://PLACEHOLDER_HOST/";');
  writeFileSync(launchUrlPath, restored, 'utf8');
}

console.log('\nAPK build OK.');