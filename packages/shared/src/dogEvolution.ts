import { DOG_EVOLUTION_THRESHOLDS } from "./balance";
import { DogStage, DogTask } from "./types";

export interface DogAffinities {
  guard: number;
  hunt: number;
  scavenge: number;
  mend: number;
  scout: number;
}

export function freshAffinities(): DogAffinities {
  return { guard: 0, hunt: 0, scavenge: 0, mend: 0, scout: 0 };
}

export interface DogForm {
  id: string;
  stage: DogStage;
  name: string;
  description: string;
  /** procedural visual params consumed by the client renderer */
  visual: {
    scale: number;
    bodyHue: number;
    glowHue: number;
    hasWings: boolean;
    hasArmor: boolean;
    hasHorns: boolean;
    maneStyle: "none" | "spikes" | "flame" | "aura";
  };
  passive: string;
}

export const PUP_FORM: DogForm = {
  id: "pup",
  stage: DogStage.Pup,
  name: "Spark Pup",
  description: "A newly-bonded magic hound. Still deciding what it wants to become.",
  visual: { scale: 0.62, bodyHue: 0.58, glowHue: 0.58, hasWings: false, hasArmor: false, hasHorns: false, maneStyle: "none" },
  passive: "None yet - assign it tasks to shape its evolution.",
};

type Stage2Key = DogTask.Guard | DogTask.Hunt | DogTask.Scavenge | DogTask.Scout | DogTask.Mend;

export const STAGE2_FORMS: Record<Stage2Key, DogForm> = {
  [DogTask.Guard]: {
    id: "bastion",
    stage: DogStage.Stage2,
    name: "Bastion Hound",
    description: "Armored plating and a defiant bark that taunts nearby enemies.",
    visual: { scale: 0.85, bodyHue: 0.08, glowHue: 0.1, hasWings: false, hasArmor: true, hasHorns: false, maneStyle: "spikes" },
    passive: "Taunts enemies near its guard point, soaking hits for the team.",
  },
  [DogTask.Hunt]: {
    id: "fang",
    stage: DogStage.Stage2,
    name: "Fang Hound",
    description: "Lean and vicious, it strikes alongside its bonded pilot.",
    visual: { scale: 0.78, bodyHue: 0.0, glowHue: 0.02, hasWings: false, hasArmor: false, hasHorns: true, maneStyle: "flame" },
    passive: "Pack Attack: bonus damage on enemies you've already hit.",
  },
  [DogTask.Scavenge]: {
    id: "magpie",
    stage: DogStage.Stage2,
    name: "Magpie Hound",
    description: "Quick paws and a nose for salvage. Loot practically jumps to it.",
    visual: { scale: 0.7, bodyHue: 0.12, glowHue: 0.13, hasWings: false, hasArmor: false, hasHorns: false, maneStyle: "none" },
    passive: "Auto-collects nearby parts and boosts rare-part drop odds.",
  },
  [DogTask.Scout]: {
    id: "wisp",
    stage: DogStage.Stage2,
    name: "Wisp Hound",
    description: "Half-phased into light. Outruns everything on the field.",
    visual: { scale: 0.68, bodyHue: 0.52, glowHue: 0.55, hasWings: true, hasArmor: false, hasHorns: false, maneStyle: "aura" },
    passive: "Reveals enemies on the minimap and grants a team speed aura.",
  },
  [DogTask.Mend]: {
    id: "aegis",
    stage: DogStage.Stage2,
    name: "Aegis Hound",
    description: "A warm, steady glow follows it - allies heal just by staying close.",
    visual: { scale: 0.76, bodyHue: 0.36, glowHue: 0.38, hasWings: true, hasArmor: false, hasHorns: false, maneStyle: "aura" },
    passive: "Passive team regeneration and faster ally revives nearby.",
  },
};

interface HybridDef {
  a: Stage2Key;
  b: Stage2Key;
  form: DogForm;
}

const HYBRIDS: HybridDef[] = [
  {
    a: DogTask.Guard,
    b: DogTask.Mend,
    form: {
      id: "sentinel",
      stage: DogStage.Stage3,
      name: "Sentinel Hound",
      description: "A living bulwark. Shields the lowest-health ally the instant they need it.",
      visual: { scale: 0.98, bodyHue: 0.55, glowHue: 0.6, hasWings: true, hasArmor: true, hasHorns: false, maneStyle: "aura" },
      passive: "Auto-shields whichever ally drops critically low on health.",
    },
  },
  {
    a: DogTask.Hunt,
    b: DogTask.Scout,
    form: {
      id: "storm",
      stage: DogStage.Stage3,
      name: "Storm Hound",
      description: "A blur of claws and static. Blitzes priority targets before they can react.",
      visual: { scale: 0.9, bodyHue: 0.58, glowHue: 0.62, hasWings: true, hasArmor: false, hasHorns: true, maneStyle: "flame" },
      passive: "Opens fights with a lightning strike on the nearest ranged enemy.",
    },
  },
  {
    a: DogTask.Guard,
    b: DogTask.Hunt,
    form: {
      id: "warden",
      stage: DogStage.Stage3,
      name: "Warden Hound",
      description: "Holds the line and bites back twice as hard.",
      visual: { scale: 1.0, bodyHue: 0.03, glowHue: 0.08, hasWings: false, hasArmor: true, hasHorns: true, maneStyle: "spikes" },
      passive: "Counter-attacks enemies that strike it or its guard point.",
    },
  },
  {
    a: DogTask.Scavenge,
    b: DogTask.Scout,
    form: {
      id: "phantom",
      stage: DogStage.Stage3,
      name: "Phantom Hound",
      description: "Slips past danger to strip the battlefield of every scrap of loot.",
      visual: { scale: 0.82, bodyHue: 0.75, glowHue: 0.8, hasWings: true, hasArmor: false, hasHorns: false, maneStyle: "aura" },
      passive: "Massively boosted loot radius and reveals hidden caches.",
    },
  },
  {
    a: DogTask.Mend,
    b: DogTask.Scavenge,
    form: {
      id: "alchemist",
      stage: DogStage.Stage3,
      name: "Alchemist Hound",
      description: "Turns scavenged scrap into restorative field charges.",
      visual: { scale: 0.86, bodyHue: 0.28, glowHue: 0.32, hasWings: false, hasArmor: true, hasHorns: false, maneStyle: "aura" },
      passive: "Converts picked-up parts into bonus team healing bursts.",
    },
  },
];

const PRIME_FORM: DogForm = {
  id: "prime",
  stage: DogStage.Stage3,
  name: "Prime Hound",
  description: "A balanced, fully-realised evolution - no single instinct dominates.",
  visual: { scale: 0.92, bodyHue: 0.62, glowHue: 0.66, hasWings: true, hasArmor: true, hasHorns: true, maneStyle: "aura" },
  passive: "Small bonus to every task it performs.",
};

function topTwo(aff: DogAffinities): [Stage2Key, number][] {
  const entries = Object.entries(aff) as [Stage2Key, number][];
  entries.sort((a, b) => b[1] - a[1]);
  return [entries[0], entries[1]];
}

/** Determines the dog's current evolved form purely from its affinity XP. Deterministic & pure. */
export function resolveDogForm(aff: DogAffinities): DogForm {
  const total = aff.guard + aff.hunt + aff.scavenge + aff.mend + aff.scout;
  const [[topKey, topVal], [secondKey, secondVal]] = topTwo(aff);

  if (total >= DOG_EVOLUTION_THRESHOLDS.stage3) {
    // Require the top two affinities to be reasonably close to unlock a hybrid;
    // otherwise a single dominant instinct produces the "pure" prime form.
    if (secondVal >= topVal * 0.5 && secondVal > 10) {
      const hybrid = HYBRIDS.find(
        (h) => (h.a === topKey && h.b === secondKey) || (h.a === secondKey && h.b === topKey)
      );
      if (hybrid) return hybrid.form;
    }
    return PRIME_FORM;
  }

  if (total >= DOG_EVOLUTION_THRESHOLDS.stage2 && topVal > 0) {
    return STAGE2_FORMS[topKey];
  }

  return PUP_FORM;
}

export function affinityProgress(aff: DogAffinities): { total: number; nextThreshold: number; stage: DogStage } {
  const total = aff.guard + aff.hunt + aff.scavenge + aff.mend + aff.scout;
  if (total >= DOG_EVOLUTION_THRESHOLDS.stage3) return { total, nextThreshold: DOG_EVOLUTION_THRESHOLDS.stage3, stage: DogStage.Stage3 };
  if (total >= DOG_EVOLUTION_THRESHOLDS.stage2) return { total, nextThreshold: DOG_EVOLUTION_THRESHOLDS.stage3, stage: DogStage.Stage2 };
  return { total, nextThreshold: DOG_EVOLUTION_THRESHOLDS.stage2, stage: DogStage.Pup };
}
