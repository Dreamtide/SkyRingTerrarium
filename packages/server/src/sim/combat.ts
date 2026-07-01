import { PARTS_BY_ID, PLAYER_BASE } from "@dream/shared";
import { PartLoadoutSchema } from "../state/schema";

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

export function computeStats(loadout: PartLoadoutSchema): AggregatedStats {
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

/** Simple flat armor mitigation curve: each armor point cuts ~2% damage, capped at 70%. */
export function mitigate(rawDamage: number, armor: number): number {
  const reduction = Math.min(0.7, armor * 0.02);
  return rawDamage * (1 - reduction);
}

export interface DamageableRef {
  health: number;
  maxHealth: number;
  shield: number;
  maxShield: number;
}

/** Applies damage to shield first, then health. Returns actual total damage absorbed. */
export function applyDamage(target: DamageableRef, amount: number): number {
  let remaining = amount;
  if (target.shield > 0) {
    const absorbed = Math.min(target.shield, remaining);
    target.shield -= absorbed;
    remaining -= absorbed;
  }
  if (remaining > 0) {
    target.health = Math.max(0, target.health - remaining);
  }
  return amount;
}

/** Equips a part if the slot is empty or the new part scores higher. Returns true if equipped. */
export function tryEquip(loadout: PartLoadoutSchema, partId: string): boolean {
  const def = PARTS_BY_ID[partId];
  if (!def) return false;
  const slot = def.slot as keyof PartLoadoutSchema as "chassis" | "weapon" | "engine" | "plating";
  const current = loadout[slot];
  if (!current || partScore(partId) > partScore(current)) {
    loadout[slot] = partId;
    return true;
  }
  return false;
}

export function partScore(partId: string): number {
  const def = PARTS_BY_ID[partId];
  if (!def) return 0;
  const s = def.stats;
  return (s.health ?? 0) * 1 + (s.shield ?? 0) * 1.2 + (s.speed ?? 0) * 12 + (s.damage ?? 0) * 3 + (s.armor ?? 0) * 8 + (s.fireRate ?? 0) * 20 + (s.dashCooldown ?? 0) * -20;
}
