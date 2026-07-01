/** Lightweight procedural sound engine (Web Audio API oscillators/noise) - no audio asset files required. */
class AudioEngine {
  private ctx: AudioContext | null = null;
  private master: GainNode | null = null;
  private musicGain: GainNode | null = null;
  private musicStarted = false;
  private volume = 0.55;

  private ensure(): AudioContext {
    if (!this.ctx) {
      this.ctx = new (window.AudioContext || (window as unknown as { webkitAudioContext: typeof AudioContext }).webkitAudioContext)();
      this.master = this.ctx.createGain();
      this.master.gain.value = this.volume;
      this.master.connect(this.ctx.destination);
      this.musicGain = this.ctx.createGain();
      this.musicGain.gain.value = 0.16;
      this.musicGain.connect(this.master);
    }
    if (this.ctx.state === "suspended") this.ctx.resume();
    return this.ctx;
  }

  setVolume(v: number) {
    this.volume = Math.max(0, Math.min(1, v));
    if (this.master) this.master.gain.value = this.volume;
  }

  getVolume(): number {
    return this.volume;
  }

  /** Must be called from a user gesture handler to unlock audio on mobile browsers. */
  unlock() {
    this.ensure();
  }

  private tone(freq: number, dur: number, type: OscillatorType, gainAmt: number, glideTo?: number) {
    const ctx = this.ensure();
    const osc = ctx.createOscillator();
    const gain = ctx.createGain();
    osc.type = type;
    osc.frequency.setValueAtTime(freq, ctx.currentTime);
    if (glideTo) osc.frequency.exponentialRampToValueAtTime(Math.max(20, glideTo), ctx.currentTime + dur);
    gain.gain.setValueAtTime(0.0001, ctx.currentTime);
    gain.gain.exponentialRampToValueAtTime(gainAmt, ctx.currentTime + 0.012);
    gain.gain.exponentialRampToValueAtTime(0.0001, ctx.currentTime + dur);
    osc.connect(gain);
    gain.connect(this.master!);
    osc.start();
    osc.stop(ctx.currentTime + dur + 0.02);
  }

  private noiseBurst(dur: number, gainAmt: number, filterFreq = 2000) {
    const ctx = this.ensure();
    const bufferSize = Math.floor(ctx.sampleRate * dur);
    const buffer = ctx.createBuffer(1, bufferSize, ctx.sampleRate);
    const data = buffer.getChannelData(0);
    for (let i = 0; i < bufferSize; i++) data[i] = (Math.random() * 2 - 1) * (1 - i / bufferSize);
    const src = ctx.createBufferSource();
    src.buffer = buffer;
    const filter = ctx.createBiquadFilter();
    filter.type = "lowpass";
    filter.frequency.value = filterFreq;
    const gain = ctx.createGain();
    gain.gain.setValueAtTime(gainAmt, ctx.currentTime);
    gain.gain.exponentialRampToValueAtTime(0.0001, ctx.currentTime + dur);
    src.connect(filter);
    filter.connect(gain);
    gain.connect(this.master!);
    src.start();
  }

  shot() {
    this.tone(680, 0.09, "sawtooth", 0.12, 240);
  }
  hit() {
    this.noiseBurst(0.12, 0.25, 1400);
  }
  ram() {
    this.tone(120, 0.18, "square", 0.2, 60);
    this.noiseBurst(0.15, 0.2, 800);
  }
  dash() {
    this.tone(300, 0.16, "sine", 0.15, 900);
  }
  transform() {
    this.tone(180, 0.4, "sawtooth", 0.18, 620);
    this.noiseBurst(0.3, 0.12, 3000);
  }
  bite() {
    this.tone(220, 0.08, "square", 0.14, 90);
  }
  collect() {
    this.tone(700, 0.09, "sine", 0.16, 1100);
    this.tone(1400, 0.12, "sine", 0.1, 1600);
  }
  evolve() {
    [520, 660, 880, 1180].forEach((f, i) => {
      const ctx = this.ensure();
      const t = ctx.currentTime + i * 0.09;
      const osc = ctx.createOscillator();
      const gain = ctx.createGain();
      osc.type = "triangle";
      osc.frequency.value = f;
      gain.gain.setValueAtTime(0.0001, t);
      gain.gain.exponentialRampToValueAtTime(0.2, t + 0.02);
      gain.gain.exponentialRampToValueAtTime(0.0001, t + 0.3);
      osc.connect(gain);
      gain.connect(this.master!);
      osc.start(t);
      osc.stop(t + 0.35);
    });
  }
  downed() {
    this.tone(400, 0.5, "sawtooth", 0.18, 90);
  }
  revive() {
    this.tone(300, 0.35, "sine", 0.18, 780);
  }
  victory() {
    [523, 659, 784, 1046].forEach((f, i) => {
      const ctx = this.ensure();
      const t = ctx.currentTime + i * 0.14;
      const osc = ctx.createOscillator();
      const gain = ctx.createGain();
      osc.type = "triangle";
      osc.frequency.value = f;
      gain.gain.setValueAtTime(0.0001, t);
      gain.gain.exponentialRampToValueAtTime(0.22, t + 0.02);
      gain.gain.exponentialRampToValueAtTime(0.0001, t + 0.5);
      osc.connect(gain);
      gain.connect(this.master!);
      osc.start(t);
      osc.stop(t + 0.55);
    });
  }
  defeat() {
    this.tone(220, 1.1, "sawtooth", 0.18, 60);
  }
  click() {
    this.tone(500, 0.05, "square", 0.08, 500);
  }

  startAmbientMusic() {
    if (this.musicStarted) return;
    this.musicStarted = true;
    const ctx = this.ensure();
    const notes = [130.8, 155.6, 196, 220, 261.6];
    let step = 0;
    const playPad = () => {
      const freq = notes[step % notes.length];
      step++;
      const osc = ctx.createOscillator();
      const gain = ctx.createGain();
      osc.type = "sine";
      osc.frequency.value = freq;
      const t = ctx.currentTime;
      gain.gain.setValueAtTime(0.0001, t);
      gain.gain.exponentialRampToValueAtTime(0.5, t + 1.2);
      gain.gain.exponentialRampToValueAtTime(0.0001, t + 3.6);
      osc.connect(gain);
      gain.connect(this.musicGain!);
      osc.start(t);
      osc.stop(t + 3.7);
      setTimeout(playPad, 2600 + Math.random() * 1200);
    };
    playPad();
  }
}

export const audioEngine = new AudioEngine();
