import { DogTask, EnemyType, InputState } from "@dream/shared";

export interface PlayerRuntime {
  sessionId: string;
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
