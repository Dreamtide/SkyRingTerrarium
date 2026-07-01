// Mech classes: distinct chassis players unlock and choose between for a deployment.
// Each modifies the PLAYER_BASE stats and picks a distinct procedural silhouette client-side.

export type MechSilhouette = "vanguard" | "juggernaut" | "interceptor" | "artillery";

export interface MechStatMods {
  health: number; // flat add
  shield: number; // flat add
  speedMult: number; // multiplies both robot & vehicle speed
  damage: number; // flat add
  armor: number; // flat add
  fireRateMult: number;
  dashCooldownMult: number;
}

export interface MechClassDef {
  id: string;
  name: string;
  description: string;
  silhouette: MechSilhouette;
  /** Hue (0-1) used for the mech's signature accent trim, layered on top of the player's chosen color. */
  accentHue: number;
  /** null = unlocked for everyone from the start. */
  unlockCost: { ferrite?: number; volt?: number; pyros?: number; chroma?: number } | null;
  statMods: MechStatMods;
}

const NEUTRAL_MODS: MechStatMods = { health: 0, shield: 0, speedMult: 1, damage: 0, armor: 0, fireRateMult: 1, dashCooldownMult: 1 };

export const MECH_CLASSES: MechClassDef[] = [
  {
    id: "vanguard",
    name: "Vanguard",
    description: "Balanced all-rounder. No weaknesses, no specialties - a dependable starting frame.",
    silhouette: "vanguard",
    accentHue: 0.58,
    unlockCost: null,
    statMods: { ...NEUTRAL_MODS },
  },
  {
    id: "juggernaut",
    name: "Juggernaut",
    description: "Heavy chassis built to soak hits and shrug off fire. Slow, but hard to put down.",
    silhouette: "juggernaut",
    accentHue: 0.08,
    unlockCost: { ferrite: 180 },
    statMods: { health: 45, shield: 15, speedMult: 0.8, damage: 4, armor: 6, fireRateMult: 0.92, dashCooldownMult: 1.15 },
  },
  {
    id: "interceptor",
    name: "Interceptor",
    description: "Lightweight speed frame. Outruns everything on the field and dashes almost twice as often.",
    silhouette: "interceptor",
    accentHue: 0.5,
    unlockCost: { volt: 180 },
    statMods: { health: -15, shield: -5, speedMult: 1.32, damage: -2, armor: -1, fireRateMult: 1.1, dashCooldownMult: 0.62 },
  },
  {
    id: "artillery",
    name: "Artillery",
    description: "Long-range heavy weapons platform. Devastating damage per shot, slow to bring back to bear.",
    silhouette: "artillery",
    accentHue: 0.03,
    unlockCost: { pyros: 180 },
    statMods: { health: 5, shield: 0, speedMult: 0.88, damage: 11, armor: 1, fireRateMult: 0.65, dashCooldownMult: 1.05 },
  },
];

export const MECH_BY_ID: Record<string, MechClassDef> = Object.fromEntries(MECH_CLASSES.map((m) => [m.id, m]));

export function getMechClass(id: string): MechClassDef {
  return MECH_BY_ID[id] ?? MECH_CLASSES[0];
}

export const DEFAULT_MECH_ID = "vanguard";
