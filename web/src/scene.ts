import * as THREE from 'three';
import type { ServiceGraph, ServiceNode } from './domain';
import { COLORS, statusColor } from './ui/theme';

interface NodeVisual {
  id: string;
  name: string;
  group: THREE.Group;
  core: THREE.Mesh<THREE.IcosahedronGeometry, THREE.MeshStandardMaterial>;
  halo: THREE.Mesh<THREE.SphereGeometry, THREE.MeshBasicMaterial>;
  ring: THREE.Mesh<THREE.RingGeometry, THREE.MeshBasicMaterial>;
  label: THREE.Sprite;
  labelCanvas: HTMLCanvasElement;
  labelTexture: THREE.CanvasTexture;
  status: string;
  health: number;
  phase: number;
}

interface EdgeVisual {
  from: string;
  to: string;
  line: THREE.Line<THREE.BufferGeometry, THREE.LineBasicMaterial>;
  flow: THREE.Mesh<THREE.SphereGeometry, THREE.MeshBasicMaterial>;
  offset: number;
  speed: number;
  points: [THREE.Vector3, THREE.Vector3];
}

function makeLabelSprite(): {
  sprite: THREE.Sprite;
  canvas: HTMLCanvasElement;
  texture: THREE.CanvasTexture;
  context: CanvasRenderingContext2D;
} {
  const canvas = document.createElement('canvas');
  canvas.width = 512;
  canvas.height = 128;
  const context = canvas.getContext('2d');
  if (!context) throw new Error('2D canvas context unavailable');
  const texture = new THREE.CanvasTexture(canvas);
  texture.colorSpace = THREE.SRGBColorSpace;
  texture.minFilter = THREE.LinearFilter;
  const material = new THREE.SpriteMaterial({ map: texture, transparent: true, depthWrite: false });
  const sprite = new THREE.Sprite(material);
  sprite.scale.set(0.14, 0.035, 1);
  return { sprite, canvas, texture, context };
}

function drawLabel(
  context: CanvasRenderingContext2D,
  canvas: HTMLCanvasElement,
  name: string,
  status: string,
  health: number,
  selected: boolean,
): void {
  context.clearRect(0, 0, canvas.width, canvas.height);
  const color = statusColor(status);
  context.textAlign = 'center';
  context.textBaseline = 'middle';
  context.font = '600 40px "Inter", "Segoe UI", system-ui, sans-serif';
  context.fillStyle = '#EAF3FF';
  context.fillText(name, canvas.width / 2, 40, canvas.width - 24);
  context.font = '500 26px "JetBrains Mono", Consolas, monospace';
  context.fillStyle = color;
  context.fillText(`${status.toUpperCase()} · ${Math.round(health * 100)}%`, canvas.width / 2, 86);
  if (selected) {
    context.strokeStyle = '#41D9F5';
    context.lineWidth = 4;
    context.beginPath();
    context.roundRect(6, 6, canvas.width - 12, canvas.height - 12, 18);
    context.stroke();
  }
}

export class InfrastructureScene {
  readonly root = new THREE.Group();
  readonly graphGroup = new THREE.Group();
  readonly nodeTargets: THREE.Object3D[] = [];
  readonly reticle: THREE.Mesh<THREE.RingGeometry, THREE.MeshBasicMaterial>;
  reducedMotion = false;
  private nodes = new Map<string, NodeVisual>();
  private edges: EdgeVisual[] = [];
  private selectedId: string | null = null;
  private hoveredId: string | null = null;
  private pulse = 0;

  constructor() {
    this.root.add(this.buildEnvironment());
    this.graphGroup.position.set(0, 1.08, -0.42);
    this.graphGroup.scale.setScalar(0.8);
    this.root.add(this.graphGroup);

    this.reticle = new THREE.Mesh(
      new THREE.RingGeometry(0.006, 0.009, 32),
      new THREE.MeshBasicMaterial({ color: COLORS.signalCyan, transparent: true, opacity: 0.9, side: THREE.DoubleSide, depthTest: false }),
    );
    this.reticle.visible = false;
    this.reticle.renderOrder = 30;
    this.root.add(this.reticle);
  }

  private buildEnvironment(): THREE.Group {
    const group = new THREE.Group();

    const starCount = 900;
    const positions = new Float32Array(starCount * 3);
    for (let i = 0; i < starCount; i += 1) {
      const radius = 5 + Math.random() * 14;
      const theta = Math.random() * Math.PI * 2;
      const phi = Math.acos(2 * Math.random() - 1);
      positions[i * 3] = radius * Math.sin(phi) * Math.cos(theta);
      positions[i * 3 + 1] = Math.abs(radius * Math.cos(phi)) * 0.5 + 0.2;
      positions[i * 3 + 2] = radius * Math.sin(phi) * Math.sin(theta);
    }
    const starGeometry = new THREE.BufferGeometry();
    starGeometry.setAttribute('position', new THREE.BufferAttribute(positions, 3));
    const stars = new THREE.Points(
      starGeometry,
      new THREE.PointsMaterial({ color: '#6C86A6', size: 0.018, transparent: true, opacity: 0.55, sizeAttenuation: true }),
    );
    group.add(stars);

    const grid = new THREE.GridHelper(8, 80, 0x1d334b, 0x101a28);
    const gridMaterial = grid.material as THREE.Material;
    gridMaterial.transparent = true;
    gridMaterial.opacity = 0.5;
    grid.position.y = -0.02;
    group.add(grid);

    const stage = new THREE.Mesh(
      new THREE.RingGeometry(0.5, 0.52, 96),
      new THREE.MeshBasicMaterial({ color: COLORS.signalCyan, transparent: true, opacity: 0.18, side: THREE.DoubleSide }),
    );
    stage.rotation.x = -Math.PI / 2;
    stage.position.y = 0.001;
    group.add(stage);

    const ambient = new THREE.HemisphereLight(0x9fd9ff, 0x05070c, 1.05);
    group.add(ambient);

    const cyan = new THREE.PointLight(0x41d9f5, 9, 5, 2);
    cyan.position.set(-1.2, 1.7, 0.4);
    group.add(cyan);

    const violet = new THREE.PointLight(0x9c83ff, 7, 5, 2);
    violet.position.set(1.4, 1.3, -0.8);
    group.add(violet);

    const warm = new THREE.PointLight(0xffbe55, 3, 4, 2);
    warm.position.set(0.2, 0.4, 1.2);
    group.add(warm);

    return group;
  }

  setGraph(graph: ServiceGraph): void {
    for (const child of [...this.graphGroup.children]) this.graphGroup.remove(child);
    this.nodes.clear();
    this.edges.length = 0;
    this.nodeTargets.length = 0;

    for (const node of graph.nodes) this.addNode(node);
    for (const edge of graph.edges) this.addEdge(edge.from, edge.to, edge.latencyMs);
    this.updateNodes(graph.nodes);
  }

  private addNode(node: ServiceNode): void {
    const group = new THREE.Group();
    group.position.set(node.x, node.y, node.z);

    const color = statusColor(node.status);
    const core = new THREE.Mesh(
      new THREE.IcosahedronGeometry(0.026, 2),
      new THREE.MeshStandardMaterial({
        color,
        emissive: new THREE.Color(color),
        emissiveIntensity: 1.1,
        roughness: 0.28,
        metalness: 0.35,
      }),
    );
    core.userData.nodeId = node.id;
    group.add(core);

    const halo = new THREE.Mesh(
      new THREE.SphereGeometry(0.05, 18, 14),
      new THREE.MeshBasicMaterial({ color, transparent: true, opacity: 0.1, depthWrite: false, blending: THREE.AdditiveBlending }),
    );
    halo.userData.nodeId = node.id;
    group.add(halo);

    const ring = new THREE.Mesh(
      new THREE.RingGeometry(0.037, 0.041, 48),
      new THREE.MeshBasicMaterial({ color: COLORS.signalCyan, transparent: true, opacity: 0.95, side: THREE.DoubleSide }),
    );
    ring.visible = false;
    group.add(ring);

    const { sprite, canvas, texture, context } = makeLabelSprite();
    sprite.position.set(0, 0.048, 0);
    drawLabel(context, canvas, node.name, node.status, node.health, false);
    texture.needsUpdate = true;
    group.add(sprite);

    this.graphGroup.add(group);
    this.nodeTargets.push(core, halo);
    this.nodes.set(node.id, {
      id: node.id,
      name: node.name,
      group,
      core,
      halo,
      ring,
      label: sprite,
      labelCanvas: canvas,
      labelTexture: texture,
      status: node.status,
      health: node.health,
      phase: Math.random() * Math.PI * 2,
    });
  }

  private addEdge(from: string, to: string, latencyMs: number): void {
    const a = this.nodes.get(from)?.group.position;
    const b = this.nodes.get(to)?.group.position;
    if (!a || !b) return;
    const start = a.clone();
    const end = b.clone();
    const geometry = new THREE.BufferGeometry().setFromPoints([start, end]);
    const material = new THREE.LineBasicMaterial({
      color: COLORS.slate,
      transparent: true,
      opacity: 0.6,
      blending: THREE.AdditiveBlending,
    });
    const line = new THREE.Line(geometry, material);
    this.graphGroup.add(line);

    const flow = new THREE.Mesh(
      new THREE.SphereGeometry(0.0045, 8, 6),
      new THREE.MeshBasicMaterial({ color: COLORS.signalCyan, transparent: true, opacity: 0.9 }),
    );
    this.graphGroup.add(flow);

    this.edges.push({
      from,
      to,
      line,
      flow,
      offset: Math.random(),
      speed: 0.16 + 18 / Math.max(20, latencyMs),
      points: [start, end],
    });
  }

  updateNodes(nodes: ServiceNode[]): void {
    for (const node of nodes) {
      const visual = this.nodes.get(node.id);
      if (!visual) continue;
      const color = statusColor(node.status);
      visual.status = node.status;
      visual.health = node.health;
      visual.core.material.color.set(color);
      visual.core.material.emissive.set(color);
      visual.halo.material.color.set(color);
      drawLabel(visual.labelCanvas.getContext('2d')!, visual.labelCanvas, node.name, node.status, node.health, this.selectedId === node.id);
      visual.labelTexture.needsUpdate = true;
      visual.ring.visible = this.selectedId === node.id;
    }

    for (const edge of this.edges) {
      const from = this.nodes.get(edge.from);
      const to = this.nodes.get(edge.to);
      if (!from || !to) continue;
      const worst = Math.min(from.health, to.health);
      const color = worst >= 0.9 ? COLORS.signalCyan : worst >= 0.6 ? COLORS.amber : COLORS.criticalRed;
      edge.line.material.color.set(color);
      edge.line.material.opacity = 0.24 + Math.max(0, 1 - worst) * 0.7;
      edge.flow.material.color.set(color);
      edge.speed = 0.14 + 14 / Math.max(20, 240 * (1 - worst) + 20);
    }
  }

  setSelected(id: string | null): void {
    this.selectedId = id;
    for (const visual of this.nodes.values()) {
      visual.ring.visible = visual.id === id;
      drawLabel(
        visual.labelCanvas.getContext('2d')!,
        visual.labelCanvas,
        visual.name,
        visual.status,
        visual.health,
        visual.id === id,
      );
      visual.labelTexture.needsUpdate = true;
    }
  }

  setHovered(id: string | null): void {
    this.hoveredId = id;
  }

  nodeName(id: string): string {
    return this.nodes.get(id)?.name ?? id;
  }

  update(dt: number, elapsed: number): void {
    if (!this.reducedMotion) this.pulse += dt;
    for (const visual of this.nodes.values()) {
      const critical = visual.status === 'failing' || visual.status === 'down';
      const selected = visual.id === this.selectedId;
      const hovered = visual.id === this.hoveredId;
      const beat = critical && !this.reducedMotion ? 0.72 + Math.sin(this.pulse * 4 + visual.phase) * 0.28 : 1;
      const scale = selected ? 1.28 : hovered ? 1.16 : 1;
      visual.core.scale.setScalar(scale * (critical && !this.reducedMotion ? 0.96 + (beat - 0.72) * 0.12 : 1));
      visual.core.material.emissiveIntensity = critical ? 0.65 + beat * 1.5 : 1.1;
      visual.halo.material.opacity = (selected ? 0.24 : hovered ? 0.19 : 0.1) * (critical ? 0.7 + beat * 0.6 : 1);
      visual.halo.scale.setScalar(selected ? 1.2 : hovered ? 1.1 : 1);
      if (!this.reducedMotion) visual.ring.rotation.z = elapsed * 0.5;
      visual.label.material.opacity = selected || hovered ? 1 : 0.85;
    }

    for (const edge of this.edges) {
      edge.offset = (edge.offset + dt * edge.speed) % 1;
      const [a, b] = edge.points;
      edge.flow.position.lerpVectors(a, b, edge.offset);
      edge.flow.material.opacity = 0.35 + Math.sin(edge.offset * Math.PI) * 0.6;
    }
  }

  showReticle(point: THREE.Vector3 | null): void {
    if (!point) {
      this.reticle.visible = false;
      return;
    }
    this.reticle.visible = true;
    this.reticle.position.copy(point);
  }

  nodeIdFor(object: THREE.Object3D): string | null {
    return (object.userData.nodeId as string | undefined) ?? null;
  }
}
