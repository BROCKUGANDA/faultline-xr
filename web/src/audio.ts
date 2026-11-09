/**
 * Tiny WebAudio synthesiser for interaction cues. No audio files are shipped,
 * so the experience still works offline and inside the headset browser.
 */
export class Sfx {
  private context: AudioContext | null = null;
  private enabled = true;
  private lastHoverAt = 0;

  setEnabled(value: boolean): void {
    this.enabled = value;
  }

  unlock(): void {
    if (!this.enabled) return;
    try {
      if (!this.context) {
        const Ctor = window.AudioContext ?? (window as unknown as { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;
        if (!Ctor) return;
        this.context = new Ctor();
      }
      if (this.context.state === 'suspended') void this.context.resume();
    } catch {
      this.context = null;
    }
  }

  private tone(frequency: number, duration: number, gainValue: number, type: OscillatorType = 'sine', delay = 0): void {
    if (!this.enabled) return;
    this.unlock();
    const context = this.context;
    if (!context) return;
    try {
      const start = context.currentTime + delay;
      const oscillator = context.createOscillator();
      const gain = context.createGain();
      oscillator.type = type;
      oscillator.frequency.setValueAtTime(frequency, start);
      gain.gain.setValueAtTime(0.0001, start);
      gain.gain.exponentialRampToValueAtTime(Math.max(0.0002, gainValue), start + 0.012);
      gain.gain.exponentialRampToValueAtTime(0.0001, start + duration);
      oscillator.connect(gain);
      gain.connect(context.destination);
      oscillator.start(start);
      oscillator.stop(start + duration + 0.02);
    } catch {
      // Audio is non-critical; silently continue.
    }
  }

  hover(): void {
    const now = performance.now();
    if (now - this.lastHoverAt < 70) return;
    this.lastHoverAt = now;
    this.tone(1320, 0.035, 0.012, 'sine');
  }

  select(): void {
    this.tone(660, 0.08, 0.035, 'triangle');
    this.tone(990, 0.07, 0.02, 'sine', 0.035);
  }

  dispatch(): void {
    this.tone(420, 0.09, 0.03, 'sawtooth');
    this.tone(630, 0.12, 0.024, 'triangle', 0.07);
  }

  approve(): void {
    this.tone(523.25, 0.1, 0.032, 'triangle');
    this.tone(659.25, 0.12, 0.028, 'triangle', 0.06);
    this.tone(783.99, 0.16, 0.026, 'triangle', 0.12);
  }

  execute(): void {
    this.tone(220, 0.12, 0.04, 'sine');
    this.tone(330, 0.14, 0.03, 'sine', 0.08);
    this.tone(440, 0.18, 0.026, 'sine', 0.16);
  }

  resolve(): void {
    this.tone(523.25, 0.18, 0.03, 'sine');
    this.tone(659.25, 0.2, 0.028, 'sine', 0.1);
    this.tone(783.99, 0.24, 0.026, 'sine', 0.2);
    this.tone(1046.5, 0.34, 0.022, 'sine', 0.3);
  }

  error(): void {
    this.tone(180, 0.12, 0.03, 'square');
    this.tone(140, 0.16, 0.025, 'square', 0.08);
  }
}
