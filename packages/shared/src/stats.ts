import { PARTS_BY_ID } from "./parts";
import { PLAYER_BASE } from "./balance";

export interface PartLoadout {
  chassis: string;
  weapon: string;
  engine: string;
  plating: string;
}

export interface AggregatedStats {
  maxHealth: number;
  maxShield: number;
  robotSpeed: number;
  vehicleSpeed: number;
  damage: number;
  armor: number;
  fireRate: number;
  dashCooldown: number;
}

/**
 * Aggregates a mech's stats from its base values + equipped parts.
 * Shared by the server (authoritative combat/movement) and the client
 * (local movement prediction) so both always agree on how fast a given
 * loadout should move.
 */
export function computeStats(loadout: PartLoadout): AggregatedStats {
  const stats: AggregatedStats = {
    maxHealth: PLAYER_BASE.health,
    maxShield: PLAYER_BASE.shield,
    robotSpeed: PLAYER_BASE.robotSpeed,
    vehicleSpeed: PLAYER_BASE.vehicleSpeed,
    damage: PLAYER_BASE.damage,
    armor: PLAYER_BASE.armor,
    fireRate: PLAYER_BASE.fireRate,
    dashCooldown: PLAYER_BASE.dashCooldown,
  };
  for (const partId of [loadout.chassis, loadout.weapon, loadout.engine, loadout.plating]) {
    if (!partId) continue;
    const def = PARTS_BY_ID[partId];
    if (!def) continue;
    if (def.stats.health) stats.maxHealth += def.stats.health;
    if (def.stats.shield) stats.maxShield += def.stats.shield;
    if (def.stats.speed) {
      stats.robotSpeed += def.stats.speed;
      stats.vehicleSpeed += def.stats.speed * 1.6;
    }
    if (def.stats.damage) stats.damage += def.stats.damage;
    if (def.stats.armor) stats.armor += def.stats.armor;
    if (def.stats.fireRate) stats.fireRate = Math.max(0.6, stats.fireRate + def.stats.fireRate);
    if (def.stats.dashCooldown) stats.dashCooldown = Math.max(0.6, stats.dashCooldown + def.stats.dashCooldown);
  }
  return stats;
}
