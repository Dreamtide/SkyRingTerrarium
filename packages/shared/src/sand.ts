// Cybersand: the persistent meta-progression currency. Four types, each earned from a
// different kind of battlefield performance and spent on a matching garage upgrade
// category, so what you did in a run naturally points at what it improves.

export interface SandWallet {
  ferrite: number; // armor plating - earned from tanking/surviving hits
  volt: number; // engine cells - earned from movement/dashing/objective speed
  pyros: number; // weapon cores - earned from kills
  chroma: number; // systems crystal - earned from dog tasks / support play
}

export function freshWallet(): SandWallet {
  return { ferrite: 0, volt: 0, pyros: 0, chroma: 0 };
}

export function addWallets(a: SandWallet, b: Partial<SandWallet>): SandWallet {
  return {
    ferrite: a.ferrite + (b.ferrite ?? 0),
    volt: a.volt + (b.volt ?? 0),
    pyros: a.pyros + (b.pyros ?? 0),
    chroma: a.chroma + (b.chroma ?? 0),
  };
}

export function canAfford(wallet: SandWallet, cost: Partial<SandWallet>): boolean {
  return (
    wallet.ferrite >= (cost.ferrite ?? 0) &&
    wallet.volt >= (cost.volt ?? 0) &&
    wallet.pyros >= (cost.pyros ?? 0) &&
    wallet.chroma >= (cost.chroma ?? 0)
  );
}

export function spendWallet(wallet: SandWallet, cost: Partial<SandWallet>): SandWallet {
  return {
    ferrite: wallet.ferrite - (cost.ferrite ?? 0),
    volt: wallet.volt - (cost.volt ?? 0),
    pyros: wallet.pyros - (cost.pyros ?? 0),
    chroma: wallet.chroma - (cost.chroma ?? 0),
  };
}

export const SAND_LABEL: Record<keyof SandWallet, string> = {
  ferrite: "Ferrite Sand",
  volt: "Volt Sand",
  pyros: "Pyros Sand",
  chroma: "Chroma Sand",
};

export const SAND_COLOR: Record<keyof SandWallet, string> = {
  ferrite: "#9fb4c7",
  volt: "#57d97b",
  pyros: "#ff8a4d",
  chroma: "#b563ff",
};

// ---------- Garage upgrade categories ----------

export type UpgradeCategory = "armor" | "engine" | "weapon" | "systems";

export const UPGRADE_CATEGORIES: UpgradeCategory[] = ["armor", "engine", "weapon", "systems"];

export const UPGRADE_SAND_TYPE: Record<UpgradeCategory, keyof SandWallet> = {
  armor: "ferrite",
  engine: "volt",
  weapon: "pyros",
  systems: "chroma",
};

export const UPGRADE_LABEL: Record<UpgradeCategory, { name: string; effect: string }> = {
  armor: { name: "Armor Plating", effect: "+ Max Health, + Armor" },
  engine: { name: "Engine Core", effect: "+ Speed, - Dash Cooldown" },
  weapon: { name: "Weapon Core", effect: "+ Damage, + Fire Rate" },
  systems: { name: "Targeting Systems", effect: "+ Shield, + Attack Range" },
};

export const UPGRADE_MAX_LEVEL = 8;

/** Sand cost to advance a category from `level` to `level + 1`. */
export function upgradeCost(level: number): number {
  return 20 + level * 18;
}

export type MechUpgradeLevels = Record<UpgradeCategory, number>;

export function freshUpgradeLevels(): MechUpgradeLevels {
  return { armor: 0, engine: 0, weapon: 0, systems: 0 };
}

export const SAND_REWARD = {
  perWaveClear: { ferrite: 5, volt: 5, pyros: 5, chroma: 3 },
  perKill: { pyros: 1 },
  perDamageTaken: { ferrite: 0.15 }, // per point of damage survived (not fatal)
  perDistanceUnit: { volt: 0.06 },
  perDogTaskTick: { chroma: 0.4 }, // per second an active, useful dog task is running
  victoryBonus: { ferrite: 40, volt: 40, pyros: 40, chroma: 40 },
  defeatConsolation: { ferrite: 8, volt: 8, pyros: 8, chroma: 8 },
};
