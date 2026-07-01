import { AggregatedStats, PARTS_BY_ID, computeStats } from "@dream/shared";
import { PartLoadoutSchema } from "../state/schema";

export type { AggregatedStats };
// computeStats lives in @dream/shared so the client's local movement prediction
// and the server's authoritative simulation can never disagree about mech speed.
export { computeStats };

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
