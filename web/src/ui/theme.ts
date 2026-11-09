export const COLORS = {
  void: '#070B12',
  obsidian: '#0D1420',
  graphite: '#152131',
  slate: '#26364A',
  ice: '#EAF3FF',
  mist: '#91A4BA',
  signalCyan: '#41D9F5',
  electricBlue: '#537EFF',
  violet: '#9C83FF',
  amber: '#FFBE55',
  criticalRed: '#FF526A',
  recoveryGreen: '#54D99A',
} as const;

export const FONTS = {
  display: '"Space Grotesk", "Segoe UI Variable Display", "Segoe UI", system-ui, sans-serif',
  body: '"Inter", "Segoe UI Variable Text", "Segoe UI", system-ui, sans-serif',
  mono: '"JetBrains Mono", "Cascadia Mono", Consolas, ui-monospace, monospace',
} as const;

export type Palette = typeof COLORS;

export interface Theme {
  colors: Palette;
  highContrast: boolean;
  textScale: number;
  reducedMotion: boolean;
}

export function themeFromSettings(settings: {
  highContrast: boolean;
  textScale: number;
  reducedMotion: boolean;
}): Theme {
  return {
    colors: COLORS,
    highContrast: settings.highContrast,
    textScale: settings.textScale,
    reducedMotion: settings.reducedMotion,
  };
}

export function statusColor(status: string): string {
  switch (status) {
    case 'healthy':
      return COLORS.signalCyan;
    case 'degraded':
      return COLORS.amber;
    case 'failing':
      return COLORS.criticalRed;
    case 'down':
      return '#B3243A';
    default:
      return COLORS.mist;
  }
}

export function severityColor(severity: string): string {
  switch (severity) {
    case 'success':
      return COLORS.recoveryGreen;
    case 'warning':
      return COLORS.amber;
    case 'critical':
      return COLORS.criticalRed;
    default:
      return COLORS.signalCyan;
  }
}
