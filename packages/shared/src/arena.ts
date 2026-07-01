import { ARENA_RADIUS } from "./balance";
import { mulberry32 } from "./rng";

export type BiomeId = "nebula" | "ember" | "verdant" | "glacier";

export interface BiomePalette {
  /** ground base colors (near center / toward edge) as hex */
  groundInner: string;
  groundOuter: string;
  /** the glowing grid/edge accent */
  grid: string;
  /** sky gradient stops */
  skyTop: string;
  skyHorizon: string;
  skyBand: string;
  fog: string;
  /** obstacle materials */
  rockColor: string;
  rockEmissive: string;
  crateColor: string;
  crateEmissive: string;
  ringColor: string;
}

export const BIOMES: Record<BiomeId, { name: string; palette: BiomePalette }> = {
  nebula: {
    name: "Nebula Verge",
    palette: {
      groundInner: "#060a14",
      groundOuter: "#0d1526",
      grid: "#4d8cff",
      skyTop: "#04050d",
      skyHorizon: "#171129",
      skyBand: "#59408c",
      fog: "#060912",
      rockColor: "#2b3a5c",
      rockEmissive: "#3d5ba8",
      crateColor: "#4a5a3a",
      crateEmissive: "#8fae3e",
      ringColor: "#8a5cff",
    },
  },
  ember: {
    name: "Ember Wastes",
    palette: {
      groundInner: "#140705",
      groundOuter: "#26100a",
      grid: "#ff7a3d",
      skyTop: "#0d0405",
      skyHorizon: "#2e0f0a",
      skyBand: "#8c3520",
      fog: "#160806",
      rockColor: "#3a2018",
      rockEmissive: "#ff5a2a",
      crateColor: "#4f3a22",
      crateEmissive: "#ffb62e",
      ringColor: "#ff4d4d",
    },
  },
  verdant: {
    name: "Verdant Ruin",
    palette: {
      groundInner: "#06120b",
      groundOuter: "#0e2415",
      grid: "#4dffa0",
      skyTop: "#040b07",
      skyHorizon: "#10291c",
      skyBand: "#2f7a52",
      fog: "#07130c",
      rockColor: "#2c473a",
      rockEmissive: "#3fae7a",
      crateColor: "#5a5a2e",
      crateEmissive: "#c8e04a",
      ringColor: "#40e0b0",
    },
  },
  glacier: {
    name: "Glacier Rift",
    palette: {
      groundInner: "#08101c",
      groundOuter: "#14263a",
      grid: "#6ee7ff",
      skyTop: "#050a12",
      skyHorizon: "#122233",
      skyBand: "#3d6f96",
      fog: "#0a1420",
      rockColor: "#38546e",
      rockEmissive: "#6ee7ff",
      crateColor: "#4a5a6a",
      crateEmissive: "#b0f0ff",
      ringColor: "#8ab8ff",
    },
  },
};

const BIOME_IDS: BiomeId[] = ["nebula", "ember", "verdant", "glacier"];

export interface ArenaObstacle {
  x: number;
  z: number;
  radius: number;
  height: number;
  kind: "pillar" | "crate" | "ring" | "crystal" | "tree" | "monolith";
}

/** Purely visual scatter (no collision): rendered client-side only. */
export interface ArenaDecor {
  x: number;
  z: number;
  scale: number;
  rot: number;
  kind: "shard" | "tuft" | "pebble";
}

export interface ArenaShapeParams {
  /** harmonic amplitudes/frequencies/phases defining the lobed boundary radius */
  a1: number;
  k1: number;
  p1: number;
  a2: number;
  k2: number;
  p2: number;
  base: number;
}

export interface ArenaDef {
  seed: number;
  biome: BiomeId;
  shape: ArenaShapeParams;
  obstacles: ArenaObstacle[];
  decor: ArenaDecor[];
}

/** Boundary radius at polar angle theta - a seeded lobed closed curve instead of a plain circle. */
export function boundaryRadius(shape: ArenaShapeParams, theta: number): number {
  return ARENA_RADIUS * (shape.base + shape.a1 * Math.sin(shape.k1 * theta + shape.p1) + shape.a2 * Math.sin(shape.k2 * theta + shape.p2));
}

export function shapeFromSeed(seed: number): ArenaShapeParams {
  const rng = mulberry32(seed ^ 0x5eed);
  return {
    base: 0.88,
    a1: 0.07 + rng() * 0.07,
    k1: 2 + Math.floor(rng() * 3), // 2-4 lobes
    p1: rng() * Math.PI * 2,
    a2: 0.03 + rng() * 0.04,
    k2: 5 + Math.floor(rng() * 3),
    p2: rng() * Math.PI * 2,
  };
}

export function biomeFromSeed(seed: number): BiomeId {
  return BIOME_IDS[Math.abs(seed >> 3) % BIOME_IDS.length];
}

function obstacleKindsFor(biome: BiomeId): { kinds: ArenaObstacle["kind"][]; weights: number[] } {
  switch (biome) {
    case "ember":
      return { kinds: ["pillar", "crate", "monolith"], weights: [0.45, 0.3, 0.25] };
    case "verdant":
      return { kinds: ["tree", "crate", "monolith"], weights: [0.45, 0.25, 0.3] };
    case "glacier":
      return { kinds: ["crystal", "crate", "pillar"], weights: [0.5, 0.25, 0.25] };
    default:
      return { kinds: ["pillar", "crate", "ring"], weights: [0.45, 0.35, 0.2] };
  }
}

/** Deterministically derives a full arena (biome, boundary shape, obstacles, decor) from a numeric seed. */
export function generateArena(seed: number): ArenaDef {
  const rng = mulberry32(seed);
  const biome = biomeFromSeed(seed);
  const shape = shapeFromSeed(seed);
  const { kinds, weights } = obstacleKindsFor(biome);

  const count = 11 + Math.floor(rng() * 6);
  const obstacles: ArenaObstacle[] = [];
  let attempts = 0;
  while (obstacles.length < count && attempts < count * 25) {
    attempts++;
    const angle = rng() * Math.PI * 2;
    const maxR = boundaryRadius(shape, angle) - 5;
    const dist = 6 + rng() * Math.max(4, maxR - 6);
    const x = Math.cos(angle) * dist;
    const z = Math.sin(angle) * dist;
    const radius = 1.2 + rng() * 2.2;
    const tooClose = obstacles.some((o) => Math.hypot(o.x - x, o.z - z) < o.radius + radius + 2);
    if (tooClose) continue;
    let roll = rng();
    let kind = kinds[kinds.length - 1];
    for (let i = 0; i < kinds.length; i++) {
      if (roll < weights[i]) {
        kind = kinds[i];
        break;
      }
      roll -= weights[i];
    }
    const height =
      kind === "pillar" || kind === "monolith"
        ? 4 + rng() * 3
        : kind === "crystal"
          ? 2.6 + rng() * 2.4
          : kind === "tree"
            ? 3.4 + rng() * 2
            : kind === "ring"
              ? 2.4
              : 1.4 + rng() * 0.8;
    obstacles.push({ x, z, radius, height, kind });
  }

  const decor: ArenaDecor[] = [];
  const decorCount = 60;
  for (let i = 0; i < decorCount; i++) {
    const angle = rng() * Math.PI * 2;
    const maxR = boundaryRadius(shape, angle) - 1.5;
    const dist = 2 + rng() * (maxR - 2);
    const roll = rng();
    decor.push({
      x: Math.cos(angle) * dist,
      z: Math.sin(angle) * dist,
      scale: 0.3 + rng() * 0.8,
      rot: rng() * Math.PI * 2,
      kind: roll < 0.35 ? "shard" : roll < 0.7 ? "tuft" : "pebble",
    });
  }

  return { seed, biome, shape, obstacles, decor };
}

/** Back-compat helper: obstacle list only. */
export function generateArenaLayout(seed: number): ArenaObstacle[] {
  return generateArena(seed).obstacles;
}

export function isInsideArena(shape: ArenaShapeParams, x: number, z: number): boolean {
  const theta = Math.atan2(z, x);
  return Math.hypot(x, z) <= boundaryRadius(shape, theta);
}

/** Clamps a point to the seeded lobed boundary (radial projection). */
export function clampToShape(shape: ArenaShapeParams, x: number, z: number): { x: number; z: number } {
  const d = Math.hypot(x, z);
  if (d < 0.0001) return { x, z };
  const theta = Math.atan2(z, x);
  const maxR = boundaryRadius(shape, theta);
  if (d <= maxR) return { x, z };
  const scale = maxR / d;
  return { x: x * scale, z: z * scale };
}
