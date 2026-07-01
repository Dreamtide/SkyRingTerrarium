import { DEFAULT_MECH_ID, MECH_CLASSES } from "./mechs";
import { MechUpgradeLevels, SandWallet, UpgradeCategory, freshUpgradeLevels, freshWallet } from "./sand";

export interface DogProfile {
  id: string;
  name: string;
  formId: string;
  stage: string;
  affGuard: number;
  affHunt: number;
  affScavenge: number;
  affMend: number;
  affScout: number;
  /** 0-100. Grows from being brought on deployments and from treats; boosts the dog's passive effectiveness. */
  bond: number;
  runsCompleted: number;
  createdAt: number;
}

export const MAX_BOND = 100;
export const BASE_KENNEL_SLOTS = 2;
export const KENNEL_SLOT_COST_CHROMA = 120;
export const TREAT_COST_CHROMA = 25;
export const TREAT_BOND_GAIN = 6;
export const RUN_BOND_GAIN = 4;
/** Bond level -> flat multiplier applied to the dog's task effectiveness (heal/damage/pickup radius/etc). */
export function bondMultiplier(bond: number): number {
  return 1 + (bond / MAX_BOND) * 0.5; // up to +50% at max bond
}

export interface PlayerProfile {
  deviceId: string;
  name: string;
  sand: SandWallet;
  unlockedMechs: string[];
  mechUpgrades: Record<string, MechUpgradeLevels>;
  dogs: DogProfile[];
  kennelSlots: number;
  selectedMechId: string;
  selectedDogId: string | null;
  totalRuns: number;
  totalVictories: number;
  createdAt: number;
  updatedAt: number;
}

function makeStarterDog(): DogProfile {
  return {
    id: `dog_${Math.random().toString(36).slice(2, 10)}`,
    name: "Pup",
    formId: "pup",
    stage: "pup",
    affGuard: 0,
    affHunt: 0,
    affScavenge: 0,
    affMend: 0,
    affScout: 0,
    bond: 0,
    runsCompleted: 0,
    createdAt: Date.now(),
  };
}

export function freshProfile(deviceId: string, name: string): PlayerProfile {
  const starterDog = makeStarterDog();
  return {
    deviceId,
    name,
    sand: freshWallet(),
    unlockedMechs: [DEFAULT_MECH_ID],
    mechUpgrades: Object.fromEntries(MECH_CLASSES.map((m) => [m.id, freshUpgradeLevels()])),
    dogs: [starterDog],
    kennelSlots: BASE_KENNEL_SLOTS,
    selectedMechId: DEFAULT_MECH_ID,
    selectedDogId: starterDog.id,
    totalRuns: 0,
    totalVictories: 0,
    createdAt: Date.now(),
    updatedAt: Date.now(),
  };
}

/** Fills in any fields missing from an older/partial save (schema migrations) without discarding progress. */
export function normalizeProfile(raw: Partial<PlayerProfile>, deviceId: string): PlayerProfile {
  const fresh = freshProfile(deviceId, raw.name ?? "Pilot");
  const dogs = Array.isArray(raw.dogs) && raw.dogs.length > 0 ? raw.dogs : fresh.dogs;
  const mechUpgrades = { ...fresh.mechUpgrades, ...(raw.mechUpgrades ?? {}) };
  for (const key of Object.keys(mechUpgrades)) {
    mechUpgrades[key] = { ...freshUpgradeLevels(), ...mechUpgrades[key] };
  }
  return {
    deviceId,
    name: raw.name ?? fresh.name,
    sand: { ...fresh.sand, ...(raw.sand ?? {}) },
    unlockedMechs: raw.unlockedMechs && raw.unlockedMechs.length > 0 ? raw.unlockedMechs : fresh.unlockedMechs,
    mechUpgrades,
    dogs,
    kennelSlots: raw.kennelSlots ?? fresh.kennelSlots,
    selectedMechId: raw.selectedMechId && mechUpgrades[raw.selectedMechId] ? raw.selectedMechId : fresh.selectedMechId,
    selectedDogId: raw.selectedDogId && dogs.some((d) => d.id === raw.selectedDogId) ? raw.selectedDogId : dogs[0].id,
    totalRuns: raw.totalRuns ?? 0,
    totalVictories: raw.totalVictories ?? 0,
    createdAt: raw.createdAt ?? Date.now(),
    updatedAt: Date.now(),
  };
}

export function makeNewDog(): DogProfile {
  return makeStarterDog();
}

export const UPGRADE_CATEGORY_LIST: UpgradeCategory[] = ["armor", "engine", "weapon", "systems"];
