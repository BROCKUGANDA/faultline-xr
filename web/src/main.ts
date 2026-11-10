import * as THREE from 'three';
import './styles.css';
import { Sfx } from './audio';
import { FaultlineApp } from './app';
import { InfrastructureScene } from './scene';
import { Panel, type PanelButton } from './ui/panel';
import { drawCenterPanel } from './ui/screens';
import { themeFromSettings } from './ui/theme';
import { loadSave } from './domain';
import { HandRig } from './xr/hands';
import { InteractionManager, type PanelRegistration } from './xr/interaction';

const canvas: HTMLCanvasElement | null = document.querySelector('#scene');
const boot: HTMLElement | null = document.querySelector('#boot');
const bootStatus: HTMLElement | null = document.querySelector('#boot-status');
const enterXr: HTMLButtonElement | null = document.querySelector('#enter-xr');
const enterDesktop: HTMLButtonElement | null = document.querySelector('#enter-desktop');
const desktopHint: HTMLElement | null = document.querySelector('#desktop-hint');

if (!canvas || !boot || !bootStatus || !enterXr || !enterDesktop || !desktopHint) {
  throw new Error('FAULTLINE XR UI must be present in index.html');
}

function status(message: string): void {
  if (bootStatus) bootStatus.textContent = message;
}

status('Preparing renderer…');

let renderer: THREE.WebGLRenderer;
try {
  renderer = new THREE.WebGLRenderer({
    canvas,
    antialias: true,
    alpha: false,
    powerPreference: 'high-performance',
  });
} catch (error) {
  // WebGL unavailable (blocked/older browser). Surface a plain-language message
  // on the boot overlay instead of an uncaught exception and a blank screen.
  status('This browser could not start WebGL. Open the deployed experience in Meta Quest Browser — that is the supported runtime.');
  throw error;
}
renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 1.75));
renderer.setSize(window.innerWidth, window.innerHeight);
renderer.outputColorSpace = THREE.SRGBColorSpace;
renderer.toneMapping = THREE.ACESFilmicToneMapping;
renderer.toneMappingExposure = 1.05;
renderer.xr.enabled = true;
renderer.xr.setReferenceSpaceType('local-floor');

const scene = new THREE.Scene();
scene.background = new THREE.Color('#070B12');
scene.fog = new THREE.FogExp2('#070B12', 0.045);

const cameraRig = new THREE.Group();
const camera = new THREE.PerspectiveCamera(66, window.innerWidth / window.innerHeight, 0.02, 80);
camera.position.set(0, 1.6, 0.05);
cameraRig.add(camera);
scene.add(cameraRig);

const infra = new InfrastructureScene();
scene.add(infra.root);

const initialTheme = themeFromSettings(loadSave().settings);
const center = new Panel(0.42, 0.34, initialTheme);
center.group.position.set(0, 1.22, -0.42);
scene.add(center.group);

const action = new Panel(0.32, 0.34, initialTheme);
action.group.position.set(0.4, 1.14, -0.36);
action.group.rotation.set(0, -0.55, 0);
action.group.visible = false;
scene.add(action.group);

const timeline = new Panel(0.32, 0.34, initialTheme);
timeline.group.position.set(-0.4, 1.14, -0.36);
timeline.group.rotation.set(0, 0.55, 0);
timeline.group.visible = false;
scene.add(timeline.group);

const panels: PanelRegistration[] = [
  { id: 'center', panel: center, draggable: true },
  { id: 'action', panel: action, draggable: true },
  { id: 'timeline', panel: timeline, draggable: true },
];

const sfx = new Sfx();
const cameraHomePosition = cameraRig.position.clone();
const cameraHomeQuaternion = cameraRig.quaternion.clone();
const cameraHomeRotation = camera.quaternion.clone();
// Desktop preview moves the rig slightly back and up (camera sits at 1.6/0.05 inside
// the rig, so world eye becomes ~1.72/0.18) and pitches the camera down, keeping the
// seated-height panels (including their primary buttons) inside the frustum at common
// review window sizes. XR sessions override the camera pose.
const DESKTOP_PITCH_X = -0.46;
const DESKTOP_RIG_POSITION = new THREE.Vector3(0, 0.06, 0.12);

function applyCameraPitch(): void {
  camera.quaternion.copy(cameraHomeRotation);
  if (!renderer.xr.isPresenting) {
    camera.rotateX(DESKTOP_PITCH_X);
    cameraRig.position.copy(DESKTOP_RIG_POSITION);
  }
}
function applyXrPose(): void {
  camera.quaternion.copy(cameraHomeRotation);
  cameraRig.position.copy(cameraHomePosition);
}
const actionHomePosition = action.group.position.clone();
const actionHomeQuaternion = action.group.quaternion.clone();
const timelineHomePosition = timeline.group.position.clone();
const timelineHomeQuaternion = timeline.group.quaternion.clone();

function resetView(): void {
  cameraRig.position.copy(cameraHomePosition);
  cameraRig.quaternion.copy(cameraHomeQuaternion);
  applyCameraPitch();
  action.group.position.copy(actionHomePosition);
  action.group.quaternion.copy(actionHomeQuaternion);
  timeline.group.position.copy(timelineHomePosition);
  timeline.group.quaternion.copy(timelineHomeQuaternion);
  interaction.resetGraph();
}

const interaction = new InteractionManager(scene, {
  panels,
  nodeTargets: infra.nodeTargets,
  graphGroup: infra.graphGroup,
  onButton: (panelId: string, button: PanelButton) => {
    unlockAndMaybePresent();
    app.handleButton(panelId, button);
  },
  onNode: (nodeId: string) => {
    unlockAndMaybePresent();
    app.selectNode(nodeId);
  },
  showReticle: (point: THREE.Vector3 | null) => {
    infra.showReticle(point);
  },
  setHoveredNode: (nodeId: string | null) => {
    infra.setHovered(nodeId);
  },
  onHover: () => {
    if (entered) sfx.hover();
  },
});

const app = new FaultlineApp(infra, { center, action, timeline }, sfx, resetView);
sfx.setEnabled(app.state.settings.audio);

// Read-only debug hook for automated verification (smoke driver). Exposes app
// state only when the URL carries ?faultline-debug; stripping this block after
// verification is safe. Never used in normal play.
if (new URLSearchParams(window.location.search).has('faultline-debug')) {
  (window as unknown as Record<string, unknown>).__faultlineApp = { get state() { return app.state; } };
}
infra.reducedMotion = app.state.settings.reducedMotion;

const hands = new HandRig(scene, camera, renderer, canvas);
interaction.enabled = true;

let entered = false;
let xrSession: XRSession | null = null;
let vrSupported = false;
let touchGuard = 0;

function unlockAndMaybePresent(): void {
  sfx.unlock();
}

function enterDesktopMode(): void {
  entered = true;
  if (boot) boot.hidden = true;
  if (desktopHint) desktopHint.hidden = false;
  interaction.enabled = true;
  applyCameraPitch();
  status('Desktop preview ready.');
  app.refresh();
}

function enterXrMode(): void {
  entered = true;
  if (boot) boot.hidden = true;
  if (desktopHint) desktopHint.hidden = true;
  interaction.enabled = true;
  applyXrPose();
  app.refresh();
}

if (enterDesktop) {
  enterDesktop.addEventListener('click', () => {
    unlockAndMaybePresent();
    enterDesktopMode();
  });
}

if (enterXr) {
  enterXr.addEventListener('click', async () => {
    if (!vrSupported || !xrSession) return;
    unlockAndMaybePresent();
    try {
      await renderer.xr.setSession(xrSession);
      enterXrMode();
    } catch (error) {
      status(error instanceof Error ? `Could not start VR: ${error.message}` : 'Could not start VR.');
    }
  });
}

renderer.xr.addEventListener('sessionstart', () => {
  status('Hands active.');
  enterXrMode();
  if (app.state.screen === 'welcome') {
    center.draw((inner) => drawCenterPanel(inner, {
      state: app.state,
      nodes: [],
      events: [],
      aggregateHealth: 0,
      rootHealth: 0,
      elapsedMs: 0,
      now: Date.now(),
    }));
  }
});

renderer.xr.addEventListener('sessionend', () => {
  status('Returned to desktop.');
  if (desktopHint) desktopHint.hidden = false;
  interaction.enabled = entered;
  applyCameraPitch();
  app.refresh();
});

if (canvas) {
  canvas.addEventListener('pointerdown', () => {
    sfx.unlock();
  }, { passive: true });

  canvas.addEventListener('touchstart', () => {
    const now = Date.now();
    if (now - touchGuard < 250) return;
    touchGuard = now;
    sfx.unlock();
    if (!entered) enterDesktopMode();
  }, { passive: true });
}

window.addEventListener('resize', () => {
  camera.aspect = window.innerWidth / window.innerHeight;
  camera.updateProjectionMatrix();
  renderer.setSize(window.innerWidth, window.innerHeight);
});

(async function init(): Promise<void> {
  status('Checking immersive-vr support…');
  const navigatorWithXr = navigator as Navigator & {
    xr?: {
      isSessionSupported(mode: XRSessionMode): Promise<boolean>;
      requestSession(mode: XRSessionMode, options?: XRSessionInit): Promise<XRSession>;
    };
  };
  if (navigatorWithXr.xr) {
    try {
      vrSupported = await navigatorWithXr.xr.isSessionSupported('immersive-vr');
      if (vrSupported) {
        try {
          xrSession = await navigatorWithXr.xr.requestSession('immersive-vr', {
            optionalFeatures: ['local-floor', 'hand-tracking', 'bounded-floor'],
          });
          await renderer.xr.setSession(xrSession);
          if (enterXr) {
            enterXr.disabled = false;
            enterXr.hidden = false;
          }
          status('VR ready. Pinch “Enter VR” or put on the headset.');
        } catch {
          status('VR supported, but the session could not be prepared.');
        }
      } else {
        status('Immersive VR is not available in this browser.');
      }
    } catch {
      status('Immersive VR is not available in this browser.');
    }
  } else {
    status('WebXR is not available in this browser.');
  }
  if (!vrSupported && enterDesktop) {
    enterDesktop.textContent = 'Continue in desktop preview';
  }
  app.refresh();
})();

const clock = new THREE.Clock();

renderer.setAnimationLoop(() => {
  const delta = Math.min(clock.getDelta(), 0.05);
  const elapsed = clock.elapsedTime;

  hands.update(renderer);
  interaction.update(hands.pointers);
  app.update(delta);
  infra.update(delta, elapsed);

  renderer.render(scene, camera);
});
