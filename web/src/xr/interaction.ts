import * as THREE from 'three';
import type { Panel, PanelButton } from '../ui/panel';
import type { Pointer } from './hands';

export interface PanelRegistration {
  id: string;
  panel: Panel;
  draggable: boolean;
}

export interface InteractionHost {
  panels: PanelRegistration[];
  nodeTargets: THREE.Object3D[];
  graphGroup: THREE.Group;
  onButton(panelId: string, button: PanelButton): void;
  onNode(nodeId: string): void;
  showReticle(point: THREE.Vector3 | null): void;
  setHoveredNode(nodeId: string | null): void;
  onHover(): void;
}

type Hit =
  | { kind: 'panel'; panelId: string; panel: Panel; button: PanelButton | null; distance: number; point: THREE.Vector3 }
  | { kind: 'node'; nodeId: string; distance: number; point: THREE.Vector3 }
  | null;

interface DragState {
  kind: 'panel' | 'graph';
  panelId?: string;
  last: THREE.Vector3;
}

interface PointerRuntime {
  pinching: boolean;
  startedAt: number;
  pressed: Hit;
  committed: boolean;
  drag: DragState | null;
  hover: Hit;
  point: THREE.Vector3;
  lastHoverKey: string | null;
}

const CLICK_DEBOUNCE_MS = 70;

export class InteractionManager {
  enabled = false;
  private raycaster = new THREE.Raycaster();
  private runtimes = new Map<string, PointerRuntime>();
  private lastTwoHandDistance = 0;
  private lastHoverKey: string | null = null;

  constructor(
    private readonly scene: THREE.Scene,
    private readonly host: InteractionHost,
  ) {
    this.raycaster.far = 5;
  }

  update(pointers: Pointer[]): void {
    if (!this.enabled) {
      this.host.showReticle(null);
      for (const pointer of pointers) {
        const runtime = this.runtimeFor(pointer.id);
        if (runtime.pinching) this.onRelease(runtime);
      }
      return;
    }

    this.scene.updateMatrixWorld(true);

    const hits = new Map<string, Hit>();
    const interactive = pointers.filter((pointer) => pointer.active);

    for (const pointer of interactive) {
      const runtime = this.runtimeFor(pointer.id);
      const hit = this.raycast(pointer);
      hits.set(pointer.id, hit);
      runtime.hover = hit;
      runtime.point.copy(hit ? hit.point : pointer.point);

      if (pointer.pinching && !runtime.pinching) {
        this.onPress(runtime, hit, pointer);
      } else if (!pointer.pinching && runtime.pinching) {
        this.onRelease(runtime);
      }

      if (pointer.pinching) this.updatePress(runtime, pointer);
    }

    this.updateHover(pointers, hits);
    this.updateTwoHandScale(pointers, hits);
  }

  private runtimeFor(id: string): PointerRuntime {
    let runtime = this.runtimes.get(id);
    if (!runtime) {
      runtime = {
        pinching: false,
        startedAt: 0,
        pressed: null,
        committed: false,
        drag: null,
        hover: null,
        point: new THREE.Vector3(),
        lastHoverKey: null,
      };
      this.runtimes.set(id, runtime);
    }
    return runtime;
  }

  private raycast(pointer: Pointer): Hit {
    this.raycaster.set(pointer.origin, pointer.direction.clone().normalize());

    const panelMeshes = this.host.panels
      .filter((entry) => entry.panel.group.visible)
      .map((entry) => entry.panel.mesh);
    const panelHits = this.raycaster.intersectObjects(panelMeshes, false);
    const nodeHits = this.raycaster.intersectObjects(this.host.nodeTargets, false);

    let best: Hit = null;
    const panelHit = panelHits[0];
    if (panelHit) {
      const entry = this.host.panels.find((candidate) => candidate.panel.mesh === panelHit.object);
      if (entry && panelHit.uv) {
        best = {
          kind: 'panel',
          panelId: entry.id,
          panel: entry.panel,
          button: entry.panel.hitTest(panelHit.uv),
          distance: panelHit.distance,
          point: panelHit.point.clone(),
        };
      }
    }

    const nodeHit = nodeHits[0];
    if (nodeHit && (!best || nodeHit.distance < best.distance)) {
      const nodeId = (nodeHit.object.userData.nodeId as string | undefined) ?? null;
      if (nodeId) {
        best = {
          kind: 'node',
          nodeId,
          distance: nodeHit.distance,
          point: nodeHit.point.clone(),
        };
      }
    }

    return best;
  }

  private onPress(runtime: PointerRuntime, hit: Hit, pointer: Pointer): void {
    runtime.pinching = true;
    runtime.startedAt = performance.now();
    runtime.pressed = hit;
    runtime.committed = false;
    runtime.drag = null;
    runtime.lastHoverKey = null;
    this.host.onHover();
    void pointer;
  }

  private updatePress(runtime: PointerRuntime, pointer: Pointer): void {
    if (runtime.committed) {
      if (runtime.drag) {
        const delta = runtime.point.clone().sub(runtime.drag.last);
        runtime.drag.last.copy(runtime.point);
        if (runtime.drag.kind === 'panel' && runtime.drag.panelId) {
          const entry = this.host.panels.find((candidate) => candidate.id === runtime.drag?.panelId);
          if (entry) {
            entry.panel.group.position.add(delta);
            this.clampPanel(entry.panel);
            entry.panel.group.updateMatrixWorld(true);
          }
        } else if (runtime.drag.kind === 'graph') {
          const graph = this.host.graphGroup;
          graph.rotation.y = THREE.MathUtils.clamp(graph.rotation.y + delta.x * 5, -1.1, 1.1);
          graph.rotation.x = THREE.MathUtils.clamp(graph.rotation.x + delta.y * 4, -0.55, 0.55);
        }
      }
      return;
    }

    const delay = pointer.kind === 'mouse' ? 0 : CLICK_DEBOUNCE_MS;
    if (performance.now() - runtime.startedAt < delay) return;

    // Commit the press. Before this point the pointer is still ambiguous, so
    // nothing has fired and no drag has started.
    const pressed = runtime.pressed;

    // A press that began on empty space (no panel, no node) is a graph drag
    // and must NOT be reclassified as a panel drag if the hand later sweeps
    // across a panel. Re-resolving the hit at commit time was the bug: it let
    // a rotate gesture end by "clicking" whatever button happened to sit under
    // the hand on release. The hit captured at press time is authoritative.
    if (!pressed) {
      runtime.committed = true;
      runtime.drag = { kind: 'graph', last: runtime.point.clone() };
      return;
    }

    if (pressed.kind === 'panel' && pressed.button) {
      runtime.committed = true;
      if (pressed.button.enabled) this.host.onButton(pressed.panelId, pressed.button);
      return;
    }
    if (pressed.kind === 'node') {
      runtime.committed = true;
      this.host.onNode(pressed.nodeId);
      return;
    }
    if (pressed.kind === 'panel') {
      runtime.committed = true;
      runtime.drag = { kind: 'panel', panelId: pressed.panelId, last: runtime.point.clone() };
      return;
    }
  }

  private onRelease(runtime: PointerRuntime): void {
    runtime.pinching = false;
    runtime.pressed = null;
    runtime.committed = false;
    runtime.drag = null;
    runtime.startedAt = 0;
  }

  private updateHover(pointers: Pointer[], hits: Map<string, Hit>): void {
    const priority: Pointer['id'][] = ['right', 'left', 'desktop'];
    let chosen: Hit = null;
    let chosenId: Pointer['id'] | null = null;
    for (const id of priority) {
      const pointer = pointers.find((candidate) => candidate.id === id);
      if (!pointer?.active) continue;
      const hit = hits.get(id) ?? null;
      if (hit) {
        chosen = hit;
        chosenId = id;
        break;
      }
    }

    const hoverKey = chosen
      ? chosen.kind === 'node'
        ? `node:${chosen.nodeId}`
        : `panel:${chosen.panelId}:${chosen.button?.id ?? 'surface'}`
      : null;

    if (hoverKey !== this.lastHoverKey) {
      this.lastHoverKey = hoverKey;
      this.host.onHover();
      for (const entry of this.host.panels) {
        const hoveredButtonId = chosen?.kind === 'panel' && chosen.panelId === entry.id ? chosen.button?.id ?? null : null;
        entry.panel.setHover(hoveredButtonId);
      }
      this.host.setHoveredNode(chosen?.kind === 'node' ? chosen.nodeId : null);
    }

    if (chosen) {
      this.host.showReticle(chosen.point);
      for (const pointer of pointers) {
        if (!pointer.active) continue;
        const runtime = this.runtimeFor(pointer.id);
        runtime.lastHoverKey = chosenId === pointer.id ? hoverKey : null;
      }
    } else {
      this.host.showReticle(null);
    }
  }

  private updateTwoHandScale(pointers: Pointer[], hits: Map<string, Hit>): void {
    const hands = pointers.filter((pointer) => pointer.kind === 'hand' && pointer.active);
    const bothPinching = hands.length === 2 && hands.every((hand) => hand.pinching);
    if (!bothPinching) {
      this.lastTwoHandDistance = 0;
      return;
    }

    const leftRuntime = this.runtimeFor(hands[0].id);
    const rightRuntime = this.runtimeFor(hands[1].id);
    const isInteractive = (hit: Hit, runtime: PointerRuntime) =>
      hit?.kind === 'node' || (hit?.kind === 'panel' && hit.button !== null) || !!runtime.drag && runtime.drag.kind === 'panel';
    if (isInteractive(hits.get(hands[0].id) ?? null, leftRuntime) || isInteractive(hits.get(hands[1].id) ?? null, rightRuntime)) {
      this.lastTwoHandDistance = 0;
      return;
    }

    const distance = hands[0].point.distanceTo(hands[1].point);
    if (this.lastTwoHandDistance > 0.01 && distance > 0.01) {
      const factor = distance / this.lastTwoHandDistance;
      const graph = this.host.graphGroup;
      const next = THREE.MathUtils.clamp(graph.scale.x * (1 + (factor - 1) * 0.85), 0.55, 1.45);
      graph.scale.setScalar(next);
      leftRuntime.drag = null;
      rightRuntime.drag = null;
    }
    this.lastTwoHandDistance = distance;
  }

  private clampPanel(panel: Panel): void {
    const position = panel.group.position;
    position.y = THREE.MathUtils.clamp(position.y, 0.78, 1.6);
    position.z = THREE.MathUtils.clamp(position.z, -0.85, -0.18);
    const head = new THREE.Vector3(0, 1.2, 0);
    const offset = position.clone().sub(head);
    const distance = offset.length();
    if (distance > 0.82) position.copy(head).addScaledVector(offset.normalize(), 0.82);
  }

  resetGraph(): void {
    this.host.graphGroup.rotation.set(0, 0, 0);
    this.host.graphGroup.scale.setScalar(0.8);
  }

  cancelAll(): void {
    for (const runtime of this.runtimes.values()) this.onRelease(runtime);
  }
}
