import { EnemyType } from "./types";

export interface WaveSpawnEntry {
  type: EnemyType;
  count: number;
}

/** Builds a wave's spawn list, scaling with wave index (0-based) and player count. */
export function buildWave(waveIndex: number, playerCount: number): WaveSpawnEntry[] {
  const scale = 1 + waveIndex * 0.55 + Math.max(0, playerCount - 1) * 0.35;
  const entries: WaveSpawnEntry[] = [
    { type: EnemyType.Scrapling, count: Math.round((3 + waveIndex * 2) * (0.6 + playerCount * 0.25)) },
  ];
  if (waveIndex >= 1) entries.push({ type: EnemyType.Strafer, count: Math.round(2 * scale) });
  if (waveIndex >= 2) entries.push({ type: EnemyType.Brute, count: Math.round(1 * scale) });
  if (waveIndex >= 1) entries.push({ type: EnemyType.Sniper, count: Math.round(1 + waveIndex * 0.5) });
  return entries.filter((e) => e.count > 0);
}

export function bossForDeployment(): WaveSpawnEntry[] {
  return [{ type: EnemyType.Boss, count: 1 }];
}
