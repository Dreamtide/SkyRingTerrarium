import { ARENA_RADIUS } from "./balance";
import { mulberry32 } from "./rng";

export interface ArenaObstacle {
  x: number;
  z: number;
  radius: number;
  height: number;
  kind: "pillar" | "crate" | "ring";
}

/** Deterministically derives an obstacle layout from a numeric seed, used by both client & server. */
export function generateArenaLayout(seed: number): ArenaObstacle[] {
  const rng = mulberry32(seed);
  const count = 10 + Math.floor(rng() * 6);
  const obstacles: ArenaObstacle[] = [];
  let attempts = 0;
  while (obstacles.length < count && attempts < count * 20) {
    attempts++;
    const angle = rng() * Math.PI * 2;
    const dist = 6 + rng() * (ARENA_RADIUS - 12);
    const x = Math.cos(angle) * dist;
    const z = Math.sin(angle) * dist;
    const radius = 1.2 + rng() * 2.2;
    const tooClose = obstacles.some((o) => Math.hypot(o.x - x, o.z - z) < o.radius + radius + 2);
    if (tooClose) continue;
    const roll = rng();
    const kind: ArenaObstacle["kind"] = roll < 0.45 ? "pillar" : roll < 0.8 ? "crate" : "ring";
    const height = kind === "pillar" ? 4 + rng() * 3 : kind === "ring" ? 2.4 : 1.4 + rng() * 0.8;
    obstacles.push({ x, z, radius, height, kind });
  }
  return obstacles;
}

export function isInsideArena(x: number, z: number): boolean {
  return Math.hypot(x, z) <= ARENA_RADIUS;
}

export function clampToArena(x: number, z: number): { x: number; z: number } {
  const d = Math.hypot(x, z);
  if (d <= ARENA_RADIUS) return { x, z };
  const scale = ARENA_RADIUS / d;
  return { x: x * scale, z: z * scale };
}
