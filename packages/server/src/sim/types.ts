import { DogTask, EnemyType, InputState, MechUpgradeLevels, SandWallet, freshUpgradeLevels, freshWallet } from "@dream/shared";

export interface PlayerRuntime {
  sessionId: string;
  deviceId: string | null; // null = guest with no persistence
  dogProfileId: string | null;
  upgrades: MechUpgradeLevels;
  input: InputState;
  attackCooldown: number;
  dashCooldown: number;
  dashTimer: number; // >0 while dashing (i-frames + speed burst)
  dashDirX: number;
  dashDirZ: number;
  transformTimer: number; // >0 while locked mid-transform
  overdriveTimer: number; // >0 while vehicle overdrive boost active
  downedTimer: number;
  reviveProgress: number;
  invulnTimer: number;
  hitEnemyCooldowns: Map<string, number>; // enemyId -> seconds remaining before this player can be ram-hit again
  prevTransformBtn: boolean;
  prevDashBtn: boolean;
  // sand accounting (fractional accumulators; the schema mirrors the floored totals)
  sandFrac: SandWallet;
  lastX: number;
  lastZ: number;
  runPersisted: boolean; // guards against double-writing profile deltas (run end + leave)
}

export function freshPlayerSandState(): Pick<PlayerRuntime, "sandFrac" | "lastX" | "lastZ" | "runPersisted" | "upgrades"> {
  return { sandFrac: freshWallet(), lastX: 0, lastZ: 0, runPersisted: false, upgrades: freshUpgradeLevels() };
}

export interface DogRuntime {
  ownerSessionId: string;
  attackCooldown: number;
  guardSet: boolean;
  scoutPulseTimer: number;
}

export interface EnemyRuntime {
  id: string;
  type: EnemyType;
  attackCooldown: number;
  targetId: string | null; // sessionId or "dog:<sessionId>"
  spawnTelegraph: number; // seconds remaining before it can act after spawning
  knockX: number;
  knockZ: number;
}

export function freshInput(): InputState {
  return { moveX: 0, moveZ: 0, yaw: 0, attack: false, transform: false, dash: false, seq: 0 };
}
