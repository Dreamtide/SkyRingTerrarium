import { PartDefinition, PartRarity, PartSlot } from "./types";

const RARITY_MULT: Record<PartRarity, number> = {
  [PartRarity.Common]: 1,
  [PartRarity.Uncommon]: 1.6,
  [PartRarity.Rare]: 2.4,
  [PartRarity.Epic]: 3.4,
  [PartRarity.Legendary]: 4.8,
};

export const RARITY_WEIGHTS: Record<PartRarity, number> = {
  [PartRarity.Common]: 100,
  [PartRarity.Uncommon]: 55,
  [PartRarity.Rare]: 24,
  [PartRarity.Epic]: 9,
  [PartRarity.Legendary]: 2,
};

export const RARITY_COLOR: Record<PartRarity, string> = {
  [PartRarity.Common]: "#9fb4c7",
  [PartRarity.Uncommon]: "#57d97b",
  [PartRarity.Rare]: "#4fa8ff",
  [PartRarity.Epic]: "#b563ff",
  [PartRarity.Legendary]: "#ffb62e",
};

interface PartArchetype {
  slot: PartSlot;
  idPrefix: string;
  name: string;
  description: string;
  baseStats: (rarity: PartRarity) => Record<string, number>;
  colorHue: number;
}

const ARCHETYPES: PartArchetype[] = [
  {
    slot: PartSlot.Chassis,
    idPrefix: "chassis-titan",
    name: "Titan Frame",
    description: "Reinforced chassis plating. Bulks up max health.",
    baseStats: (r) => ({ health: 18 * RARITY_MULT[r] }),
    colorHue: 0.58,
  },
  {
    slot: PartSlot.Chassis,
    idPrefix: "chassis-aero",
    name: "Aero Frame",
    description: "Lightweight chassis. Trades health for raw speed.",
    baseStats: (r) => ({ health: 8 * RARITY_MULT[r], speed: 0.35 * RARITY_MULT[r] }),
    colorHue: 0.52,
  },
  {
    slot: PartSlot.Weapon,
    idPrefix: "weapon-cannon",
    name: "Arc Cannon",
    description: "Heavy blaster. Big damage, slower fire rate.",
    baseStats: (r) => ({ damage: 6 * RARITY_MULT[r], fireRate: -0.15 * RARITY_MULT[r] }),
    colorHue: 0.02,
  },
  {
    slot: PartSlot.Weapon,
    idPrefix: "weapon-pulse",
    name: "Pulse Repeater",
    description: "Rapid-fire emitter. Lower damage, faster shots.",
    baseStats: (r) => ({ damage: 2.4 * RARITY_MULT[r], fireRate: 0.5 * RARITY_MULT[r] }),
    colorHue: 0.1,
  },
  {
    slot: PartSlot.Engine,
    idPrefix: "engine-turbo",
    name: "Turbo Core",
    description: "High-output engine core. Boosts speed and dash cadence.",
    baseStats: (r) => ({ speed: 0.45 * RARITY_MULT[r], dashCooldown: -0.18 * RARITY_MULT[r] }),
    colorHue: 0.14,
  },
  {
    slot: PartSlot.Engine,
    idPrefix: "engine-fusion",
    name: "Fusion Core",
    description: "Stable fusion core. Raises shield capacity.",
    baseStats: (r) => ({ shield: 10 * RARITY_MULT[r] }),
    colorHue: 0.48,
  },
  {
    slot: PartSlot.Plating,
    idPrefix: "plating-reactive",
    name: "Reactive Plating",
    description: "Adaptive armor plating. Raises armor damage reduction.",
    baseStats: (r) => ({ armor: 1.6 * RARITY_MULT[r] }),
    colorHue: 0.72,
  },
  {
    slot: PartSlot.Plating,
    idPrefix: "plating-barrier",
    name: "Barrier Plating",
    description: "Shield-weave plating. Raises shield and modest armor.",
    baseStats: (r) => ({ shield: 6 * RARITY_MULT[r], armor: 0.6 * RARITY_MULT[r] }),
    colorHue: 0.66,
  },
];

const RARITIES = [
  PartRarity.Common,
  PartRarity.Uncommon,
  PartRarity.Rare,
  PartRarity.Epic,
  PartRarity.Legendary,
];

export const ALL_PARTS: PartDefinition[] = ARCHETYPES.flatMap((arch) =>
  RARITIES.map((rarity) => ({
    id: `${arch.idPrefix}-${rarity}`,
    slot: arch.slot,
    rarity,
    name: `${rarity === PartRarity.Common ? "" : rarityLabel(rarity) + " "}${arch.name}`.trim(),
    description: arch.description,
    stats: arch.baseStats(rarity),
    colorHue: arch.colorHue,
  }))
);

function rarityLabel(r: PartRarity): string {
  switch (r) {
    case PartRarity.Uncommon:
      return "Reinforced";
    case PartRarity.Rare:
      return "Advanced";
    case PartRarity.Epic:
      return "Prototype";
    case PartRarity.Legendary:
      return "Mythic";
    default:
      return "";
  }
}

export const PARTS_BY_ID: Record<string, PartDefinition> = Object.fromEntries(
  ALL_PARTS.map((p) => [p.id, p])
);

/** Rolls a random part, weighted by rarity, optionally favouring a wave tier (deeper waves -> better odds). */
export function rollPart(rng: () => number, waveTier = 0): PartDefinition {
  const bonus = Math.min(waveTier * 6, 40);
  const weights = RARITIES.map((r) => {
    const w = RARITY_WEIGHTS[r];
    if (r === PartRarity.Common) return Math.max(10, w - bonus);
    return w + bonus / RARITIES.length;
  });
  const total = weights.reduce((a, b) => a + b, 0);
  let roll = rng() * total;
  let rarity = RARITIES[0];
  for (let i = 0; i < RARITIES.length; i++) {
    if (roll < weights[i]) {
      rarity = RARITIES[i];
      break;
    }
    roll -= weights[i];
  }
  const candidates = ALL_PARTS.filter((p) => p.rarity === rarity);
  return candidates[Math.floor(rng() * candidates.length)];
}
