import * as THREE from 'three';
import { COLORS, FONTS, type Theme } from './theme';

export type ButtonVariant = 'primary' | 'secondary' | 'danger' | 'ghost';

export interface PanelButton {
  id: string;
  label: string;
  /** Metres from the panel's top-left corner. */
  x: number;
  y: number;
  w: number;
  h: number;
  variant: ButtonVariant;
  enabled: boolean;
  meta?: string;
  icon?: string;
}

export interface TextOptions {
  sizeMm?: number;
  color?: string;
  weight?: number;
  mono?: boolean;
  align?: CanvasTextAlign;
  baseline?: CanvasTextBaseline;
  opacity?: number;
}

const PX_PER_M = 2600;

/**
 * Small canvas drawing surface addressed in metres. Keeping all layout in
 * metres makes the same screen code work on panels of different physical sizes.
 */
export class Painter {
  readonly ctx: CanvasRenderingContext2D;
  readonly width: number;
  readonly height: number;
  readonly buttons: PanelButton[] = [];
  hovered: string | null = null;
  theme: Theme;

  constructor(canvas: HTMLCanvasElement, widthM: number, heightM: number, theme: Theme) {
    const context = canvas.getContext('2d');
    if (!context) throw new Error('2D canvas context unavailable');
    this.ctx = context;
    this.width = widthM;
    this.height = heightM;
    this.theme = theme;
  }

  private px(m: number): number {
    return m * PX_PER_M;
  }

  reset(): void {
    this.buttons.length = 0;
    this.ctx.setTransform(1, 0, 0, 1, 0, 0);
    this.ctx.clearRect(0, 0, this.ctx.canvas.width, this.ctx.canvas.height);
  }

  private scaled(sizeMm: number): number {
    return sizeMm * this.theme.textScale;
  }

  font(sizeMm: number, weight = 500, mono = false): string {
    const family = mono ? FONTS.mono : FONTS.body;
    const px = Math.max(9, Math.round((this.scaled(sizeMm) / 1000) * PX_PER_M));
    return `${weight} ${px}px ${family}`;
  }

  text(text: string, x: number, y: number, options: TextOptions = {}): void {
    const ctx = this.ctx;
    const sizeMm = options.sizeMm ?? 6;
    ctx.save();
    ctx.font = this.font(sizeMm, options.weight ?? 500, options.mono);
    ctx.fillStyle = options.color ?? COLORS.ice;
    ctx.globalAlpha = options.opacity ?? 1;
    ctx.textAlign = options.align ?? 'left';
    ctx.textBaseline = options.baseline ?? 'top';
    ctx.fillText(text, this.px(x), this.px(y));
    ctx.restore();
  }

  measure(text: string, sizeMm: number, weight = 500, mono = false): number {
    this.ctx.save();
    this.ctx.font = this.font(sizeMm, weight, mono);
    const width = this.ctx.measureText(text).width / PX_PER_M;
    this.ctx.restore();
    return width;
  }

  wrap(
    text: string,
    x: number,
    y: number,
    maxWidth: number,
    options: TextOptions & { lineHeightMm?: number } = {},
  ): number {
    const sizeMm = options.sizeMm ?? 5.4;
    const lineHeight = (options.lineHeightMm ?? sizeMm * 1.45) / 1000;
    const words = text.split(/\s+/);
    let line = '';
    let cursor = y;
    for (const word of words) {
      const candidate = line ? `${line} ${word}` : word;
      if (this.measure(candidate, sizeMm, options.weight ?? 400) > maxWidth && line) {
        this.text(line, x, cursor, options);
        cursor += lineHeight;
        line = word;
      } else {
        line = candidate;
      }
    }
    if (line) {
      this.text(line, x, cursor, options);
      cursor += lineHeight;
    }
    return cursor;
  }

  roundedRect(x: number, y: number, w: number, h: number, radius: number, fill?: string | CanvasGradient, stroke?: string, lineWidth = 0.0015): void {
    const ctx = this.ctx;
    const px = this.px.bind(this);
    const r = Math.min(radius, w / 2, h / 2);
    const X = px(x);
    const Y = px(y);
    const W = px(w);
    const H = px(h);
    const R = px(r);
    ctx.beginPath();
    ctx.moveTo(X + R, Y);
    ctx.arcTo(X + W, Y, X + W, Y + H, R);
    ctx.arcTo(X + W, Y + H, X, Y + H, R);
    ctx.arcTo(X, Y + H, X, Y, R);
    ctx.arcTo(X, Y, X + W, Y, R);
    ctx.closePath();
    if (fill) {
      ctx.fillStyle = fill;
      ctx.fill();
    }
    if (stroke) {
      ctx.lineWidth = Math.max(1, px(lineWidth));
      ctx.strokeStyle = stroke;
      ctx.stroke();
    }
  }

  divider(x: number, y: number, width: number, color = 'rgba(38,54,74,0.85)'): void {
    const ctx = this.ctx;
    ctx.save();
    ctx.strokeStyle = color;
    ctx.lineWidth = Math.max(1, this.px(0.001));
    ctx.beginPath();
    ctx.moveTo(this.px(x), this.px(y));
    ctx.lineTo(this.px(x + width), this.px(y));
    ctx.stroke();
    ctx.restore();
  }

  panelBackground(title?: string, kicker?: string, accent: string = COLORS.signalCyan): void {
    const ctx = this.ctx;
    this.roundedRect(0.0012, 0.0012, this.width - 0.0024, this.height - 0.0024, 0.014, 'rgba(7,11,18,0.955)', this.theme.highContrast ? '#5C769B' : COLORS.slate, 0.0018);

    const headerH = title ? 0.035 : 0;
    if (title && headerH > 0) {
      const grad = ctx.createLinearGradient(0, 0, this.px(this.width), 0);
      grad.addColorStop(0, 'rgba(21,33,49,0.95)');
      grad.addColorStop(1, 'rgba(13,20,32,0.65)');
      ctx.save();
      ctx.beginPath();
      ctx.rect(this.px(0.002), this.px(0.002), this.px(this.width - 0.004), this.px(headerH));
      ctx.clip();
      ctx.fillStyle = grad;
      ctx.fillRect(this.px(0.002), this.px(0.002), this.px(this.width - 0.004), this.px(headerH));
      ctx.restore();
      ctx.save();
      ctx.fillStyle = accent;
      ctx.globalAlpha = 0.95;
      ctx.fillRect(this.px(0.014), this.px(headerH - 0.0025), this.px(this.width - 0.028), this.px(0.0022));
      ctx.restore();
    }

    if (kicker) {
      this.text(kicker, 0.018, 0.009, { sizeMm: 3.3, color: COLORS.mist, weight: 500, mono: true });
    }
    if (title) {
      this.text(title, 0.018, 0.0145, { sizeMm: 6.4, color: COLORS.ice, weight: 700 });
    }
  }

  chip(text: string, x: number, y: number, color: string, options: { sizeMm?: number; filled?: boolean } = {}): number {
    const sizeMm = options.sizeMm ?? 3.6;
    const padding = 0.006;
    const width = this.measure(text, sizeMm, 600, true) + padding * 2;
    const height = (sizeMm / 1000) * 1.9;
    this.roundedRect(
      x,
      y,
      width,
      height,
      0.006,
      options.filled ? color : 'rgba(21,33,49,0.6)',
      options.filled ? undefined : color,
      0.0012,
    );
    this.text(text, x + padding, y + (height - (sizeMm / 1000)) * 0.5 - 0.0008, {
      sizeMm,
      color: options.filled ? COLORS.void : color,
      weight: 700,
      mono: true,
    });
    return width;
  }

  progress(x: number, y: number, w: number, h: number, value: number, color: string): void {
    this.roundedRect(x, y, w, h, h / 2, 'rgba(38,54,74,0.55)');
    const fill = Math.max(0, Math.min(1, value));
    if (fill > 0.005) this.roundedRect(x, y, w * fill, h, h / 2, color);
  }

  button(spec: Omit<PanelButton, 'variant' | 'enabled'> & { variant?: ButtonVariant; enabled?: boolean }): void {
    const button: PanelButton = {
      variant: spec.variant ?? 'secondary',
      enabled: spec.enabled ?? true,
      ...spec,
    };
    this.buttons.push(button);
    this.drawButton(button);
  }

  private drawButton(button: PanelButton): void {
    const ctx = this.ctx;
    const hovered = button.enabled && this.hovered === button.id;
    const radius = 0.009;
    let fill: string | CanvasGradient = 'rgba(21,33,49,0.78)';
    let stroke: string = COLORS.slate;
    let textColor: string = COLORS.ice;
    let borderWidth = 0.0013;

    switch (button.variant) {
      case 'primary': {
        const grad = ctx.createLinearGradient(this.px(button.x), this.px(button.y), this.px(button.x + button.w), this.px(button.y + button.h));
        grad.addColorStop(0, hovered ? '#67E6FF' : COLORS.signalCyan);
        grad.addColorStop(1, hovered ? '#7A9CFF' : COLORS.electricBlue);
        fill = grad;
        stroke = hovered ? '#BFEFFF' : 'rgba(65,217,245,0.5)';
        textColor = COLORS.void;
        borderWidth = 0.0016;
        break;
      }
      case 'danger':
        fill = hovered ? 'rgba(255,82,106,0.28)' : 'rgba(255,82,106,0.12)';
        stroke = hovered ? '#FF8CA0' : COLORS.criticalRed;
        textColor = COLORS.criticalRed;
        break;
      case 'ghost':
        fill = hovered ? 'rgba(38,54,74,0.55)' : 'rgba(7,11,18,0.2)';
        stroke = hovered ? COLORS.mist : 'rgba(38,54,74,0.7)';
        textColor = hovered ? COLORS.ice : COLORS.mist;
        break;
      default:
        if (hovered) {
          fill = 'rgba(38,54,74,0.95)';
          stroke = COLORS.signalCyan;
          textColor = '#FFFFFF';
        }
        break;
    }

    if (!button.enabled) {
      ctx.save();
      ctx.globalAlpha = 0.34;
    }

    if (button.variant === 'primary') {
      ctx.save();
      ctx.shadowColor = 'rgba(65,217,245,0.45)';
      ctx.shadowBlur = this.px(0.012);
      this.roundedRect(button.x, button.y, button.w, button.h, radius, fill as unknown as string, stroke, borderWidth);
      ctx.restore();
    } else {
      this.roundedRect(button.x, button.y, button.w, button.h, radius, fill as unknown as string, stroke, borderWidth);
    }

    if (button.icon) {
      this.text(button.icon, button.x + 0.014, button.y + button.h / 2 - 0.003, {
        sizeMm: 6,
        color: textColor,
        weight: 700,
        baseline: 'middle',
      });
    }

    const labelX = button.x + (button.icon ? 0.032 : 0.014);
    this.text(button.label, labelX, button.y + button.h / 2 - 0.0022, {
      sizeMm: 5,
      color: textColor,
      weight: 600,
      baseline: 'middle',
    });

    if (button.meta) {
      this.text(button.meta, button.x + button.w - 0.012, button.y + button.h / 2 - 0.002, {
        sizeMm: 3.4,
        color: button.variant === 'primary' ? 'rgba(7,11,18,0.72)' : COLORS.mist,
        weight: 500,
        mono: true,
        align: 'right',
        baseline: 'middle',
      });
    }

    if (!button.enabled) ctx.restore();
  }

  statusDot(x: number, y: number, color: string, radius = 0.004): void {
    const ctx = this.ctx;
    ctx.save();
    ctx.beginPath();
    ctx.arc(this.px(x), this.px(y), this.px(radius), 0, Math.PI * 2);
    ctx.fillStyle = color;
    ctx.shadowColor = color;
    ctx.shadowBlur = this.px(radius * 2.4);
    ctx.fill();
    ctx.restore();
  }
}

/**
 * A physical, hand-interactable panel. The canvas is mapped onto a plane in
 * metres; ray/UV hits are converted back into panel-local metres for button
 * hit-testing.
 */
export class Panel {
  readonly group: THREE.Group;
  readonly mesh: THREE.Mesh;
  private canvas: HTMLCanvasElement;
  private context: CanvasRenderingContext2D;
  private texture: THREE.CanvasTexture;
  private painter: Painter;
  private drawFn: ((painter: Painter) => void) | null = null;
  private widthM: number;
  private heightM: number;
  private theme: Theme;
  hovered: string | null = null;

  constructor(widthM: number, heightM: number, theme: Theme) {
    this.widthM = widthM;
    this.heightM = heightM;
    this.theme = theme;
    this.canvas = document.createElement('canvas');
    this.canvas.width = Math.round(widthM * PX_PER_M);
    this.canvas.height = Math.round(heightM * PX_PER_M);
    const context = this.canvas.getContext('2d');
    if (!context) throw new Error('2D canvas context unavailable');
    this.context = context;
    this.painter = new Painter(this.canvas, widthM, heightM, theme);

    this.texture = new THREE.CanvasTexture(this.canvas);
    this.texture.colorSpace = THREE.SRGBColorSpace;
    this.texture.minFilter = THREE.LinearFilter;
    this.texture.magFilter = THREE.LinearFilter;
    this.texture.generateMipmaps = true;

    const material = new THREE.MeshBasicMaterial({
      map: this.texture,
      transparent: true,
      side: THREE.FrontSide,
      depthWrite: false,
      toneMapped: false,
    });
    this.mesh = new THREE.Mesh(new THREE.PlaneGeometry(widthM, heightM), material);
    this.mesh.renderOrder = 20;

    this.group = new THREE.Group();
    this.group.add(this.mesh);
  }

  get width(): number {
    return this.widthM;
  }

  get height(): number {
    return this.heightM;
  }

  setTheme(theme: Theme): void {
    this.theme = theme;
    this.painter.theme = theme;
    this.redraw();
  }

  setAnisotropy(value: number): void {
    this.texture.anisotropy = value;
    this.texture.needsUpdate = true;
  }

  draw(fn: (painter: Painter) => void): void {
    this.drawFn = fn;
    this.redraw();
  }

  redraw(): void {
    if (!this.drawFn) return;
    this.painter.hovered = this.hovered;
    this.painter.reset();
    this.drawFn(this.painter);
    this.texture.needsUpdate = true;
  }

  setHover(id: string | null): void {
    if (this.hovered === id) return;
    this.hovered = id;
    this.redraw();
  }

  /** Find the button under a UV coordinate from a raycast. */
  hitTest(uv: THREE.Vector2): PanelButton | null {
    const x = uv.x * this.widthM;
    const y = (1 - uv.y) * this.heightM;
    for (const button of this.painter.buttons) {
      if (x >= button.x && x <= button.x + button.w && y >= button.y && y <= button.y + button.h) {
        return button;
      }
    }
    return null;
  }

  setVisible(visible: boolean): void {
    this.group.visible = visible;
  }
}
