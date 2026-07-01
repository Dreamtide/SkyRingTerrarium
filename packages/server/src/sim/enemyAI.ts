import { ENEMY_BASE, EnemyType, TICK_DT } from "@dream/shared";
import { DogSchema, EnemySchema, PlayerSchema } from "../state/schema";
import { EnemyRuntime, PlayerRuntime } from "./types";
import { applyDamage } from "./combat";

export interface EnemyTickCtx {
  enemy: EnemySchema;
  rt: EnemyRuntime;
  players: Map<string, PlayerSchema>;
  playerRt: Map<string, PlayerRuntime>;
  dogs: Map<string, DogSchema>;
  obstacles: { x: number; z: number; radius: number }[];
  onDamagePlayer: (sessionId: string, amount: number) => void;
  onFx: (type: string, data: Record<string, unknown>) => void;
}

function statsFor(type: EnemyType) {
  return ENEMY_BASE[type as keyof typeof ENEMY_BASE];
}

/** Finds the nearest living, non-downed player to (x,z). Guard-tasked dogs can redirect aggro. */
function pickTarget(enemy: EnemySchema, ctx: EnemyTickCtx): { id: string; x: number; z: number } | null {
  let best: { id: string; x: number; z: number; d: number } | null = null;
  for (const [sid, p] of ctx.players) {
    if (!p.alive || p.downed) continue;
    const d = Math.hypot(p.x - enemy.x, p.z - enemy.z);
    if (!best || d < best.d) best = { id: sid, x: p.x, z: p.z, d };
  }
  // Bastion-form guard dogs project a taunt aura that can out-compete a slightly-closer player target.
  for (const [ownerId, dog] of ctx.dogs) {
    if (dog.task !== "guard") continue;
    const d = Math.hypot(dog.x - enemy.x, dog.z - enemy.z);
    if (d < 9 && (!best || d < best.d + 4)) {
      best = { id: `dog:${ownerId}`, x: dog.x, z: dog.z, d };
    }
  }
  return best ? { id: best.id, x: best.x, z: best.z } : null;
}

export function tickEnemy(ctx: EnemyTickCtx) {
  const { enemy, rt } = ctx;
  if (!enemy.alive) return;
  if (rt.spawnTelegraph > 0) {
    rt.spawnTelegraph -= TICK_DT;
    enemy.telegraph = true;
    return;
  }
  enemy.telegraph = false;
  const stats = statsFor(rt.type);

  // knockback decay
  if (Math.abs(rt.knockX) > 0.01 || Math.abs(rt.knockZ) > 0.01) {
    enemy.x += rt.knockX * TICK_DT;
    enemy.z += rt.knockZ * TICK_DT;
    rt.knockX *= 0.85;
    rt.knockZ *= 0.85;
  }

  const target = pickTarget(enemy, ctx);
  if (rt.attackCooldown > 0) rt.attackCooldown -= TICK_DT;

  if (!target) return;

  const dx = target.x - enemy.x;
  const dz = target.z - enemy.z;
  const dist = Math.hypot(dx, dz) || 0.0001;
  enemy.yaw = Math.atan2(dx, dz);

  const isRanged = rt.type === EnemyType.Strafer || rt.type === EnemyType.Sniper || rt.type === EnemyType.Boss;
  const desiredRange = isRanged ? stats.range * 0.7 : stats.range;

  if (dist > desiredRange) {
    const nx = enemy.x + (dx / dist) * stats.speed * TICK_DT;
    const nz = enemy.z + (dz / dist) * stats.speed * TICK_DT;
    if (!collides(nx, nz, ctx.obstacles)) {
      enemy.x = nx;
      enemy.z = nz;
    }
  } else if (rt.attackCooldown <= 0) {
    const fireRate = stats.fireRate || 1.4;
    rt.attackCooldown = 1 / fireRate;
    if (!target.id.startsWith("dog:")) {
      const accuracy = isRanged ? Math.max(0.55, 1 - dist / (stats.range * 1.6)) : 0.95;
      if (Math.random() < accuracy) {
        ctx.onDamagePlayer(target.id, stats.damage);
        ctx.onFx("hit", { targetType: "player", targetId: target.id, x: target.x, z: target.z });
      } else {
        ctx.onFx("miss", { targetType: "player", targetId: target.id });
      }
    } else {
      // "Hits" the guard dog: magic construct shrugs it off, but the attack is spent (tanked).
      ctx.onFx("hit", { targetType: "dog", targetId: target.id.slice(4), x: target.x, z: target.z });
    }
    ctx.onFx("shot", { from: { x: enemy.x, y: 1, z: enemy.z }, to: { x: target.x, y: 1, z: target.z }, ranged: isRanged, enemyType: rt.type });
  }
}

function collides(x: number, z: number, obstacles: { x: number; z: number; radius: number }[]): boolean {
  for (const o of obstacles) {
    if (Math.hypot(o.x - x, o.z - z) < o.radius + 0.6) return true;
  }
  return false;
}

export function damageEnemy(enemy: EnemySchema, amount: number): boolean {
  const dmg = { health: enemy.health, maxHealth: enemy.maxHealth, shield: 0, maxShield: 0 };
  applyDamage(dmg, amount);
  enemy.health = dmg.health;
  if (enemy.health <= 0 && enemy.alive) {
    enemy.alive = false;
    return true;
  }
  return false;
}
