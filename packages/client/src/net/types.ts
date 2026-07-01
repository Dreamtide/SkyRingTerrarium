export interface SandEarnedSnapshot {
  ferrite: number;
  volt: number;
  pyros: number;
  chroma: number;
}

export interface PlayerSnapshot {
  sessionId: string;
  name: string;
  color: string;
  mechId: string;
  mode: string;
  transforming: boolean;
  health: number;
  maxHealth: number;
  shield: number;
  maxShield: number;
  alive: boolean;
  downed: boolean;
  ready: boolean;
  kills: number;
  partsCollected: number;
  reviveProgress: number;
  loadout: { chassis: string; weapon: string; engine: string; plating: string };
  sandEarned: SandEarnedSnapshot;
}

export interface DogSnapshot {
  ownerSessionId: string;
  name: string;
  bond: number;
  task: string;
  formId: string;
  stage: string;
  affGuard: number;
  affHunt: number;
  affScavenge: number;
  affMend: number;
  affScout: number;
}

export interface HudState {
  phase: string;
  waveIndex: number;
  waveTimer: number;
  seed: number;
  hostSessionId: string;
  announcement: string;
  enemiesRemaining: number;
  enemiesTotal: number;
}

export interface FxEvent {
  type: string;
  [key: string]: unknown;
}
