import * as THREE from 'three';
import { COLORS } from '../ui/theme';

export type PointerId = 'left' | 'right' | 'desktop';

export interface Pointer {
  id: PointerId;
  kind: 'hand' | 'mouse';
  active: boolean;
  pinching: boolean;
  origin: THREE.Vector3;
  direction: THREE.Vector3;
  point: THREE.Vector3;
}

interface HandVisual {
  group: THREE.Group;
  bead: THREE.Mesh<THREE.SphereGeometry, THREE.MeshBasicMaterial>;
  ring: THREE.Mesh<THREE.TorusGeometry, THREE.MeshBasicMaterial>;
  ray: THREE.Line<THREE.BufferGeometry, THREE.LineBasicMaterial>;
  joints: Map<string, THREE.Mesh<THREE.SphereGeometry, THREE.MeshBasicMaterial>>;
  color: string;
}

interface HandState {
  pinching: boolean;
  active: boolean;
  pinchPoint: THREE.Vector3;
  direction: THREE.Vector3;
  origin: THREE.Vector3;
}

const JOINT_NAMES = [
  'wrist',
  'thumb-tip',
  'index-finger-phalanx-proximal',
  'index-finger-phalanx-distal',
  'index-finger-tip',
  'middle-finger-tip',
  'ring-finger-tip',
  'pinky-finger-tip',
] as const;

const PINCH_ON = 0.024;
const PINCH_OFF = 0.032;

/**
 * Reads real WebXR hand joints (Meta Quest Browser) and produces pointer rays.
 * The same interface is fed by a mouse pointer in desktop preview mode.
 */
export class HandRig {
  readonly pointers: Pointer[] = [];
  private handSpaces: (THREE.Group & { joints?: Record<string, THREE.Object3D> })[] = [];
  private visual: HandVisual[] = [];
  private states: HandState[] = [];
  private desktopNdc = new THREE.Vector2(0, 0);
  private desktopDown = false;
  private camera: THREE.Camera;
  private raycaster = new THREE.Raycaster();
  private lastTrackedAt = 0;

  constructor(scene: THREE.Scene, camera: THREE.Camera, renderer: THREE.WebGLRenderer, canvas: HTMLCanvasElement) {
    this.camera = camera;

    for (let i = 0; i < 2; i += 1) {
      const hand = renderer.xr.getHand(i) as THREE.Group & { joints?: Record<string, THREE.Object3D> };
      hand.name = i === 0 ? 'hand-left' : 'hand-right';
      scene.add(hand);
      this.handSpaces.push(hand);

      const color = i === 0 ? COLORS.violet : COLORS.signalCyan;
      const visual = this.createVisual(color);
      scene.add(visual.group);
      this.visual.push(visual);

      this.states.push({
        pinching: false,
        active: false,
        pinchPoint: new THREE.Vector3(),
        direction: new THREE.Vector3(0, 0, -1),
        origin: new THREE.Vector3(),
      });
    }

    this.pointers.push(
      { id: 'left', kind: 'hand', active: false, pinching: false, origin: new THREE.Vector3(), direction: new THREE.Vector3(), point: new THREE.Vector3() },
      { id: 'right', kind: 'hand', active: false, pinching: false, origin: new THREE.Vector3(), direction: new THREE.Vector3(), point: new THREE.Vector3() },
      { id: 'desktop', kind: 'mouse', active: true, pinching: false, origin: new THREE.Vector3(), direction: new THREE.Vector3(), point: new THREE.Vector3() },
    );

    canvas.addEventListener('pointermove', (event) => {
      const rect = canvas.getBoundingClientRect();
      this.desktopNdc.set(((event.clientX - rect.left) / rect.width) * 2 - 1, -((event.clientY - rect.top) / rect.height) * 2 + 1);
    });
    canvas.addEventListener('pointerdown', (event) => {
      if (event.button === 0) this.desktopDown = true;
    });
    canvas.addEventListener('pointerup', (event) => {
      if (event.button === 0) this.desktopDown = false;
    });
    canvas.addEventListener('pointercancel', () => {
      this.desktopDown = false;
    });
  }

  get handTrackingActive(): boolean {
    return this.states.some((state) => state.active);
  }

  get trackingRecentlyLost(): boolean {
    return performance.now() - this.lastTrackedAt < 2500;
  }

  private createVisual(color: string): HandVisual {
    const group = new THREE.Group();
    group.visible = false;

    const bead = new THREE.Mesh(
      new THREE.SphereGeometry(0.008, 16, 12),
      new THREE.MeshBasicMaterial({ color, transparent: true, opacity: 0.92, depthTest: false }),
    );
    bead.renderOrder = 40;
    group.add(bead);

    const ring = new THREE.Mesh(
      new THREE.TorusGeometry(0.015, 0.0018, 8, 32),
      new THREE.MeshBasicMaterial({ color, transparent: true, opacity: 0.8, depthTest: false }),
    );
    ring.renderOrder = 40;
    group.add(ring);

    const rayGeometry = new THREE.BufferGeometry().setFromPoints([new THREE.Vector3(), new THREE.Vector3()]);
    const ray = new THREE.Line(
      rayGeometry,
      new THREE.LineBasicMaterial({ color, transparent: true, opacity: 0.5, depthTest: false }),
    );
    ray.renderOrder = 39;
    group.add(ray);

    const joints = new Map<string, THREE.Mesh<THREE.SphereGeometry, THREE.MeshBasicMaterial>>();
    const jointMaterial = new THREE.MeshBasicMaterial({ color, transparent: true, opacity: 0.32, depthTest: false });
    for (const name of JOINT_NAMES) {
      const dot = new THREE.Mesh(new THREE.SphereGeometry(name === 'wrist' ? 0.006 : 0.0036, 8, 6), jointMaterial.clone());
      dot.renderOrder = 38;
      jointMaterial.opacity = 0.32;
      group.add(dot);
      joints.set(name, dot);
    }

    return { group, bead, ring, ray, joints, color };
  }

  update(renderer: THREE.WebGLRenderer): Pointer[] {
    const xrPresenting = renderer.xr.isPresenting;

    for (let i = 0; i < 2; i += 1) {
      const hand = this.handSpaces[i];
      const state = this.states[i];
      const visual = this.visual[i];
      const joints = hand?.joints;

      const thumbTip = joints?.['thumb-tip'];
      const indexTip = joints?.['index-finger-tip'];
      const indexProx = joints?.['index-finger-phalanx-proximal'];
      const wrist = joints?.wrist;

      const active = xrPresenting && !!thumbTip && !!indexTip && !!indexProx;
      state.active = active;

      if (active && thumbTip && indexTip && indexProx) {
        const thumbPos = thumbTip.getWorldPosition(new THREE.Vector3());
        const indexPos = indexTip.getWorldPosition(new THREE.Vector3());
        const proxPos = indexProx.getWorldPosition(new THREE.Vector3());
        const wristPos = wrist ? wrist.getWorldPosition(new THREE.Vector3()) : proxPos.clone();

        const distance = thumbPos.distanceTo(indexPos);
        if (!state.pinching && distance < PINCH_ON) state.pinching = true;
        if (state.pinching && distance > PINCH_OFF) state.pinching = false;

        state.origin.copy(indexPos);
        state.pinchPoint.copy(thumbPos).add(indexPos).multiplyScalar(0.5);
        state.direction.copy(indexPos).sub(wristPos);
        if (state.direction.lengthSq() < 1e-8) state.direction.copy(indexPos).sub(proxPos);
        if (state.direction.lengthSq() < 1e-8) state.direction.set(0, 0, -1);
        state.direction.normalize();

        this.lastTrackedAt = performance.now();

        visual.group.visible = true;
        visual.bead.position.copy(state.pinchPoint);
        visual.ring.position.copy(state.pinchPoint);
        visual.ring.quaternion.copy(this.camera.quaternion);
        const beadScale = state.pinching ? 1.7 : 1;
        visual.bead.scale.setScalar(beadScale);
        visual.ring.scale.setScalar(state.pinching ? 1.25 : 1);
        visual.ray.visible = true;
        const start = state.pinchPoint.clone();
        const end = start.clone().addScaledVector(state.direction, 0.55);
        const position = visual.ray.geometry.getAttribute('position') as THREE.BufferAttribute;
        position.setXYZ(0, start.x, start.y, start.z);
        position.setXYZ(1, end.x, end.y, end.z);
        position.needsUpdate = true;

        for (const name of JOINT_NAMES) {
          const dot = visual.joints.get(name);
          const joint = joints?.[name];
          if (!dot || !joint) {
            if (dot) dot.visible = false;
            continue;
          }
          dot.visible = true;
          dot.position.copy(joint.getWorldPosition(new THREE.Vector3()));
          dot.scale.setScalar(state.pinching && (name === 'thumb-tip' || name === 'index-finger-tip') ? 1.35 : 1);
        }
      } else {
        state.pinching = false;
        visual.group.visible = false;
      }
    }

    const left = this.pointers[0];
    left.active = this.states[0].active;
    left.pinching = this.states[0].pinching;
    left.origin.copy(this.states[0].origin);
    left.direction.copy(this.states[0].direction);
    left.point.copy(this.states[0].pinchPoint);

    const right = this.pointers[1];
    right.active = this.states[1].active;
    right.pinching = this.states[1].pinching;
    right.origin.copy(this.states[1].origin);
    right.direction.copy(this.states[1].direction);
    right.point.copy(this.states[1].pinchPoint);

    const desktop = this.pointers[2];
    const canUseDesktop = !xrPresenting;
    desktop.active = canUseDesktop;
    desktop.pinching = canUseDesktop && this.desktopDown;
    if (canUseDesktop) {
      this.raycaster.setFromCamera(this.desktopNdc, this.camera);
      desktop.origin.copy(this.raycaster.ray.origin);
      desktop.direction.copy(this.raycaster.ray.direction);
      desktop.point.copy(this.raycaster.ray.origin).addScaledVector(this.raycaster.ray.direction, 1.2);
    }

    return this.pointers;
  }
}
