import * as THREE from "three";
const cam = new THREE.PerspectiveCamera(66, 1280/720, 0.02, 80);
cam.position.set(0, 1.66, 0.17);
cam.quaternion.setFromEuler(new THREE.Euler(-0.46, 0, 0, "XYZ"));
cam.updateMatrixWorld(true);
const pts = {
  buttonCenter: new THREE.Vector3(0.4, 1.031, -0.36),
  panelTopLeft: new THREE.Vector3(),
  panelBottomRight: new THREE.Vector3(),
};
const g = new THREE.Group(); g.position.set(0.4, 1.14, -0.36); g.rotation.y = -0.55; g.updateMatrixWorld(true);
const W=0.32, H=0.34;
const toWorld = (cx, cy) => new THREE.Vector3(cx - W/2, H/2 - cy, 0).applyMatrix4(g.matrixWorld);
pts.panelTopLeft = toWorld(0, 0);
pts.panelBottomRight = toWorld(W, H);
const btnY = H - 0.086 + 0.025, btnX = 0.018 + (W-0.036)/2;
pts.buttonCenter = toWorld(btnX, btnY);
for (const [k, p] of Object.entries(pts)) {
  const v = p.clone().project(cam);
  const x = (v.x + 1) / 2 * 1280, y = (1 - (v.y + 1) / 2) * 720;
  console.log(k.padEnd(18), "ndc", v.x.toFixed(3), v.y.toFixed(3), "-> px", x.toFixed(0), y.toFixed(0));
}
