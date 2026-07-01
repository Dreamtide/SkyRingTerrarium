// Core shared type definitions for D.R.E.A.M.
// Kept framework-agnostic so both the Colyseus server and the R3F client can import them.

export type Vec2 = { x: number; z: number };
export type Vec3 = { x: number; y: number; z: number };

export const enum RobotMode {
  Robot = "robot",
  Vehicle = "vehicle",
}

export const enum PartSlot {
  Chassis = "chassis",
  Weapon = "weapon",
  Engine = "engine",
  Plating = "plating",
}

export const enum PartRarity {
  Common = "common",
  Uncommon = "uncommon",
  Rare = "rare",
  Epic = "epic",
  Legendary = "legendary",
}

export const enum DogTask {
  Idle = "idle",
  Guard = "guard",
  Hunt = "hunt",
  Scavenge = "scavenge",
  Scout = "scout",
  Mend = "mend",
}

export const enum DogStage {
  Pup = "pup",
  Stage2 = "stage2",
  Stage3 = "stage3",
}

export const enum EnemyType {
  Scrapling = "scrapling",
  Strafer = "strafer",
  Brute = "brute",
  Sniper = "sniper",
  Boss = "boss",
}

export const enum RunPhase {
  Lobby = "lobby",
  Intermission = "intermission",
  Wave = "wave",
  Upgrade = "upgrade",
  Boss = "boss",
  Victory = "victory",
  Defeat = "defeat",
}

export interface PartDefinition {
  id: string;
  slot: PartSlot;
  rarity: PartRarity;
  name: string;
  /** flavor description shown in UI */
  description: string;
  /** multiplicative/additive stat modifiers */
  stats: PartStats;
  /** hue used to tint the procedural mesh attachment, 0-1 */
  colorHue: number;
}

export interface PartStats {
  health?: number;
  shield?: number;
  speed?: number;
  damage?: number;
  armor?: number;
  fireRate?: number;
  dashCooldown?: number;
}

export interface InputState {
  moveX: number; // -1..1
  moveZ: number; // -1..1
  yaw: number; // radians, facing direction
  attack: boolean;
  transform: boolean; // edge-triggered on client, server treats as toggle request
  dash: boolean;
  assignTask?: DogTask; // sent only on the tick the player issues a command
  seq: number; // input sequence number for reconciliation
}

export const DOG_TASK_LIST: DogTask[] = [
  DogTask.Guard,
  DogTask.Hunt,
  DogTask.Scavenge,
  DogTask.Scout,
  DogTask.Mend,
];

export const PART_SLOT_LIST: PartSlot[] = [
  PartSlot.Chassis,
  PartSlot.Weapon,
  PartSlot.Engine,
  PartSlot.Plating,
];
