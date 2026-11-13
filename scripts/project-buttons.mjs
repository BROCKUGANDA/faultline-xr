#!/usr/bin/env node
/**
 * project-buttons.mjs — computes canvas-projected screen coordinates of panel buttons.
 *
 * Reproduces the exact three.js camera/rig/panel geometry used at runtime
 * (main.ts desktop pose + screens.ts button layout) and prints pixel centres.
 * No browser needed; used to author deterministic click scripts.
 */
import * as THREE from 'three';

const W = 1280;
const H = 720;
const camera = new THREE.PerspectiveCamera(66, W / H, 0.02, 80);
camera.position.set(0, 1.6, 0.05);
const rig = new THREE.Group();
rig.position.set(0, 0.06, 0.12);
rig.add(camera);

// Desktop pose (main.ts applyCameraPitch)
camera.rotation.set(0, 0, 0);
camera.rotateX(-0.46);
rig.updateMatrixWorld(true);
camera.updateMatrixWorld(true);
const vp = new THREE.Matrix4().multiplyMatrices(camera.projectionMatrix, camera.matrixWorldInverse);

function project(x, y, z) {
  const v = new THREE.Vector3(x, y, z).applyMatrix4(vp);
  return { x: ((v.x + 1) / 2) * W, y: ((1 - v.y) / 2) * H, ndcZ: v.z };
}

// Panel plane: centre group, PlaneGeometry(w, h) faces +Z by default.
function buttonToWorld(panel, btn) {
  const local = new THREE.Vector3(
    -panel.w / 2 + btn.x + btn.w / 2,
    panel.h / 2 - btn.y - btn.h / 2,
    0.001,
  );
  return local.applyMatrix4(panel.group.matrixWorld);
}

function makePanel(id, w, h, pos, rotY = 0) {
  const group = new THREE.Group();
  group.position.set(...pos);
  group.rotation.set(0, rotY, 0);
  group.updateMatrixWorld(true);
  return { id, w, h, group };
}

const center = makePanel('center', 0.42, 0.34, [0, 1.22, -0.42]);
const action = makePanel('action', 0.32, 0.34, [0.4, 1.14, -0.36], -0.55);
const timeline = makePanel('timeline', 0.32, 0.34, [-0.4, 1.14, -0.36], 0.55);
// centre panel sits in the scene root (not the rig) — three.js: rig offset applies to camera only
center.group.updateMatrixWorld(true);

const MARGIN = 0.018;

function settingsButtons() {
  const w = center.w - MARGIN * 2;
  const H2 = center.h;
  const row = 0.052;
  const out = [];
  let y = 0.05;
  const ids = ['text-scale', 'contrast', 'motion', 'audio', 'handed'];
  for (const id of ids) {
    out.push({ id, x: MARGIN, y, w, h: row - 0.008 });
    y += row;
  }
  out.push({ id: 'reset-layout', x: MARGIN, y, w, h: row - 0.008 });
  y += row + 0.004;
  out.push({ id: 'settings-close', x: MARGIN, y: H2 - 0.062, w, h: 0.05 });
  return out;
}

function welcomeButtons() {
  const w = center.w - MARGIN * 2;
  const H2 = center.h;
  const buttonY = H2 - 0.145;
  return [
    { id: 'begin', x: MARGIN, y: buttonY, w, h: 0.052 },
    { id: 'tutorial', x: MARGIN, y: buttonY + 0.062, w: w * 0.62, h: 0.046 },
    { id: 'settings', x: MARGIN + w * 0.64, y: buttonY + 0.062, w: w * 0.36, h: 0.046 },
  ];
}

for (const [label, list] of [
  ['welcome', welcomeButtons()],
  ['settings', settingsButtons()],
]) {
  console.log(`\n# ${label}`);
  for (const b of list) {
    const world = buttonToWorld(center, b);
    const p = project(world.x, world.y, world.z);
    console.log(`${b.id.padEnd(16)} x=${p.x.toFixed(0).padStart(6)} y=${p.y.toFixed(0).padStart(6)}`);
  }
}

// Action panel buttons (workspace)
function actionButtons(phase) {
  const w = action.w - MARGIN * 2;
  const H2 = action.h;
  const y0 = phase === 'investigating' ? 0.145 : 0.225;
  return [
    { id: 'primary-action', x: MARGIN, y: H2 - 0.086, w, h: 0.05 },
    { id: 'pause', x: MARGIN, y: H2 - 0.026, w: w * 0.62, h: 0.02 },
    { id: 'reset-view', x: MARGIN + w * 0.64, y: H2 - 0.026, w: w * 0.36, h: 0.02 },
    { id: 'body-y', x: MARGIN, y: y0, w, h: 0.05 },
  ];
}
console.log('\n# action');
for (const b of actionButtons('detected')) {
  const world = buttonToWorld(action, b);
  const p = project(world.x, world.y, world.z);
  console.log(`${b.id.padEnd(16)} x=${p.x.toFixed(0).padStart(6)} y=${p.y.toFixed(0).padStart(6)}`);
}