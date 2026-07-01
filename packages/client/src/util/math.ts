export function damp(current: number, target: number, lambda: number, dt: number): number {
  return current + (target - current) * (1 - Math.exp(-lambda * dt));
}

export function lerpAngle(current: number, target: number, lambda: number, dt: number): number {
  let diff = target - current;
  while (diff > Math.PI) diff -= Math.PI * 2;
  while (diff < -Math.PI) diff += Math.PI * 2;
  return current + diff * (1 - Math.exp(-lambda * dt));
}

export function clamp01(v: number): number {
  return Math.max(0, Math.min(1, v));
}

export function hueToHex(hue: number, sat = 0.55, light = 0.55): string {
  const h = ((hue % 1) + 1) % 1;
  const a = sat * Math.min(light, 1 - light);
  const f = (n: number) => {
    const k = (n + h * 12) % 12;
    const color = light - a * Math.max(-1, Math.min(k - 3, 9 - k, 1));
    return Math.round(255 * color);
  };
  const r = f(0);
  const g = f(8);
  const b = f(4);
  return `#${[r, g, b].map((v) => v.toString(16).padStart(2, "0")).join("")}`;
}
