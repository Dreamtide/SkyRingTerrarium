import { PARTS_BY_ID } from "./parts";
import { PLAYER_BASE } from "./balance";
import { getMechClass } from "./mechs";
import { MechUpgradeLevels, freshUpgradeLevels } from "./sand";

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
  attackRange: number;
}

/** Per-garage-upgrade-level flat stat bonus, applied UPGRADE level times. */
const UPGRADE_STEP = {
  armor: { health: 9, armor: 0.7 },
  engine: { speed: 0.16, dashCooldown: -0.035 },
  weapon: { damage: 1.4, fireRate: 0.045 },
  systems: { shield: 4.5, attackRange: 0.9 },
};

function baseStatsForMech(mechId: string): AggregatedStats {
  const mech = getMechClass(mechId);
  const m = mech.statMods;
  return {
    maxHealth: PLAYER_BASE.health + m.health,
    maxShield: PLAYER_BASE.shield + m.shield,
    robotSpeed: PLAYER_BASE.robotSpeed * m.speedMult,
    vehicleSpeed: PLAYER_BASE.vehicleSpeed * m.speedMult,
    damage: PLAYER_BASE.damage + m.damage,
    armor: PLAYER_BASE.armor + m.armor,
    fireRate: PLAYER_BASE.fireRate * m.fireRateMult,
    dashCooldown: PLAYER_BASE.dashCooldown * m.dashCooldownMult,
    attackRange: PLAYER_BASE.attackRange,
  };
}

/**
 * Full stat aggregation: mech class base -> persistent garage upgrades -> equipped
 * field parts. Shared by the server (authoritative combat/movement) and the client
 * (local movement prediction) so both always agree on how a given loadout performs.
 */
export function computeMechStats(mechId: string, upgrades: MechUpgradeLevels, loadout: PartLoadout): AggregatedStats {
  const stats = baseStatsForMech(mechId);

  stats.maxHealth += upgrades.armor * UPGRADE_STEP.armor.health;
  stats.armor += upgrades.armor * UPGRADE_STEP.armor.armor;
  stats.robotSpeed += upgrades.engine * UPGRADE_STEP.engine.speed;
  stats.vehicleSpeed += upgrades.engine * UPGRADE_STEP.engine.speed * 1.6;
  stats.dashCooldown = Math.max(0.5, stats.dashCooldown + upgrades.engine * UPGRADE_STEP.engine.dashCooldown);
  stats.damage += upgrades.weapon * UPGRADE_STEP.weapon.damage;
  stats.fireRate += upgrades.weapon * UPGRADE_STEP.weapon.fireRate;
  stats.maxShield += upgrades.systems * UPGRADE_STEP.systems.shield;
  stats.attackRange += upgrades.systems * UPGRADE_STEP.systems.attackRange;

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
    if (def.stats.dashCooldown) stats.dashCooldown = Math.max(0.5, stats.dashCooldown + def.stats.dashCooldown);
  }
  return stats;
}

/** Convenience wrapper for callers that don't yet have a mech/upgrade context (e.g. garage previews). */
export function computeStats(loadout: PartLoadout, mechId = "vanguard", upgrades: MechUpgradeLevels = freshUpgradeLevels()): AggregatedStats {
  return computeMechStats(mechId, upgrades, loadout);
}
