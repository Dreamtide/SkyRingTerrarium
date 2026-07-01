// Tuning constants. Centralised so client prediction and server simulation agree.

export const TICK_RATE = 20; // simulation ticks per second
export const TICK_DT = 1 / TICK_RATE;

export const MAX_PLAYERS = 5;
export const MIN_PLAYERS = 1;

export const ARENA_RADIUS = 42;

export const PLAYER_BASE = {
  health: 100,
  shield: 40,
  robotSpeed: 6.2,
  vehicleSpeed: 13.5,
  damage: 12,
  armor: 0,
  fireRate: 3.2, // shots per second
  dashCooldown: 2.5,
  dashDistance: 6,
  attackRange: 16,
  transformLockSeconds: 0.55,
  reviveSeconds: 3,
  reviveRadius: 3.2,
};

export const DOG_BASE = {
  followDistance: 2.4,
  speed: 7.5,
  guardRadius: 6,
  huntDamage: 6,
  huntRange: 10,
  mendPerSecond: 4,
  mendRadius: 5,
  scavengeRadius: 14,
  scavengeSpeed: 10,
  scoutSpeed: 9.5,
  scoutRevealRadius: 26,
};

// XP required (cumulative, per-task affinity) to reach each dog stage.
export const DOG_EVOLUTION_THRESHOLDS = {
  stage2: 60,
  stage3: 160,
};

export const DOG_TASK_XP = {
  guardTick: 0.6, // per second spent guarding with an enemy nearby
  huntKillAssist: 8,
  scavengePickup: 10,
  scoutRevealTick: 0.5, // per second spent scouting near unexplored/enemy area
  mendTick: 1, // per HP actually healed
};

export const WAVES_PER_DEPLOYMENT = 4;
export const INTERMISSION_SECONDS = 20;
export const UPGRADE_CACHE_SECONDS = 18;

export const ENEMY_BASE = {
  scrapling: { health: 26, damage: 8, speed: 4.6, range: 1.8, fireRate: 0, xp: 1 },
  strafer: { health: 34, damage: 7, speed: 3.8, range: 13, fireRate: 1.1, xp: 1.4 },
  brute: { health: 90, damage: 16, speed: 3.1, range: 2.2, fireRate: 0, xp: 2.2 },
  sniper: { health: 30, damage: 22, speed: 3.0, range: 24, fireRate: 0.45, xp: 1.8 },
  boss: { health: 900, damage: 24, speed: 3.6, range: 15, fireRate: 0.8, xp: 20 },
};

