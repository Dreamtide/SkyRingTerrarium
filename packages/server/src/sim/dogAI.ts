import { MapSchema } from "@colyseus/schema";
import { DOG_BASE, DOG_TASK_XP, DogTask, affinityProgress, resolveDogForm } from "@dream/shared";
import { DogSchema, EnemySchema, PickupSchema, PlayerSchema } from "../state/schema";
import { DogRuntime } from "./types";
import { damageEnemy } from "./enemyAI";
import { tryEquip } from "./combat";

export interface DogTickCtx {
  dog: DogSchema;
  rt: DogRuntime;
  owner: PlayerSchema;
  enemies: MapSchema<EnemySchema>;
  pickups: MapSchema<PickupSchema>;
  dt: number;
  onEnemyKilled: (enemyId: string, enemy: EnemySchema, killerSessionId: string) => void;
  onFx: (type: string, data: Record<string, unknown>) => void;
}

function stageMult(stage: string): number {
  if (stage === "stage3") return 2.2;
  if (stage === "stage2") return 1.5;
  return 1;
}

function moveToward(dog: DogSchema, tx: number, tz: number, speed: number, dt: number, stopDist = 0.4) {
  const dx = tx - dog.x;
  const dz = tz - dog.z;
  const d = Math.hypot(dx, dz);
  if (d < stopDist) return;
  dog.yaw = Math.atan2(dx, dz);
  const step = Math.min(d, speed * dt);
  dog.x += (dx / d) * step;
  dog.z += (dz / d) * step;
}

function addAffinity(dog: DogSchema, task: DogTask, amount: number) {
  if (amount <= 0) return;
  const key = ({
    [DogTask.Guard]: "affGuard",
    [DogTask.Hunt]: "affHunt",
    [DogTask.Scavenge]: "affScavenge",
    [DogTask.Mend]: "affMend",
    [DogTask.Scout]: "affScout",
  } as const)[task as Exclude<DogTask, DogTask.Idle>];
  if (!key) return;
  (dog as unknown as Record<string, number>)[key] += amount;
}

function refreshEvolution(dog: DogSchema, onFx: DogTickCtx["onFx"], ownerSessionId: string) {
  const aff = {
    guard: dog.affGuard,
    hunt: dog.affHunt,
    scavenge: dog.affScavenge,
    mend: dog.affMend,
    scout: dog.affScout,
  };
  const form = resolveDogForm(aff);
  if (form.id !== dog.formId) {
    dog.formId = form.id;
    dog.stage = form.stage;
    onFx("evolve", { ownerSessionId, formId: form.id, formName: form.name, stage: form.stage });
  }
}

export function tickDog(ctx: DogTickCtx) {
  const { dog, rt, owner, dt } = ctx;
  if (!owner.alive || owner.downed) {
    // owner is down: dog waits protectively nearby instead of acting
    moveToward(dog, owner.x, owner.z, DOG_BASE.speed, dt, 1.5);
    return;
  }

  switch (dog.task) {
    case DogTask.Guard: {
      moveToward(dog, dog.guardX, dog.guardZ, DOG_BASE.speed, dt, 0.6);
      let threatened = false;
      ctx.enemies.forEach((e) => {
        if (e.alive && Math.hypot(e.x - dog.guardX, e.z - dog.guardZ) < DOG_BASE.guardRadius) threatened = true;
      });
      if (threatened) addAffinity(dog, DogTask.Guard, DOG_TASK_XP.guardTick * dt);
      break;
    }
    case DogTask.Hunt: {
      let nearest: { id: string; e: EnemySchema; d: number } | null = null;
      ctx.enemies.forEach((e, id) => {
        if (!e.alive) return;
        const d = Math.hypot(e.x - dog.x, e.z - dog.z);
        if (d < DOG_BASE.huntRange && (!nearest || d < nearest.d)) nearest = { id, e, d };
      });
      if (nearest) {
        const n = nearest as { id: string; e: EnemySchema; d: number };
        if (n.d > 1.6) {
          moveToward(dog, n.e.x, n.e.z, DOG_BASE.speed * 1.1, dt, 1.4);
        } else if (rt.attackCooldown <= 0) {
          rt.attackCooldown = 0.75;
          const dmg = DOG_BASE.huntDamage * stageMult(dog.stage);
          const killed = damageEnemy(n.e, dmg);
          addAffinity(dog, DogTask.Hunt, DOG_TASK_XP.huntKillAssist * 0.5);
          ctx.onFx("bite", { x: n.e.x, z: n.e.z, ownerSessionId: dog.ownerSessionId });
          if (killed) {
            owner.kills += 1;
            addAffinity(dog, DogTask.Hunt, DOG_TASK_XP.huntKillAssist);
            ctx.onEnemyKilled(n.id, n.e, owner.sessionId);
          }
        }
      } else {
        moveToward(dog, owner.x, owner.z, DOG_BASE.speed, dt, DOG_BASE.followDistance);
      }
      if (rt.attackCooldown > 0) rt.attackCooldown -= dt;
      break;
    }
    case DogTask.Scavenge: {
      let nearest: { id: string; p: PickupSchema; d: number } | null = null;
      const radius = DOG_BASE.scavengeRadius * stageMult(dog.stage);
      ctx.pickups.forEach((p, id) => {
        const d = Math.hypot(p.x - dog.x, p.z - dog.z);
        if (d < radius && (!nearest || d < nearest.d)) nearest = { id, p, d };
      });
      if (nearest) {
        const n = nearest as { id: string; p: PickupSchema; d: number };
        if (n.d > 0.9) {
          moveToward(dog, n.p.x, n.p.z, DOG_BASE.scavengeSpeed, dt, 0.7);
        } else {
          tryEquip(owner.loadout, n.p.partId);
          owner.partsCollected += 1;
          addAffinity(dog, DogTask.Scavenge, DOG_TASK_XP.scavengePickup);
          ctx.onFx("collect", { sessionId: owner.sessionId, x: n.p.x, z: n.p.z });
          ctx.pickups.delete(n.id);
        }
      } else {
        moveToward(dog, owner.x, owner.z, DOG_BASE.speed, dt, DOG_BASE.followDistance);
      }
      break;
    }
    case DogTask.Scout: {
      const aheadX = owner.x + Math.sin(owner.yaw) * DOG_BASE.scoutRevealRadius * 0.5;
      const aheadZ = owner.z + Math.cos(owner.yaw) * DOG_BASE.scoutRevealRadius * 0.5;
      moveToward(dog, aheadX, aheadZ, DOG_BASE.scoutSpeed, dt, 1.5);
      let enemyNear = false;
      ctx.enemies.forEach((e) => {
        if (e.alive && Math.hypot(e.x - dog.x, e.z - dog.z) < DOG_BASE.scoutRevealRadius) enemyNear = true;
      });
      if (enemyNear) addAffinity(dog, DogTask.Scout, DOG_TASK_XP.scoutRevealTick * dt);
      break;
    }
    case DogTask.Mend: {
      const radius = DOG_BASE.mendRadius * stageMult(dog.stage);
      // find lowest health% ally within radius of the dog (falls back to owner)
      let target: PlayerSchema | null = null;
      let lowestPct = 1.01;
      const consider = (p: PlayerSchema) => {
        if (!p.alive || p.downed) return;
        if (Math.hypot(p.x - dog.x, p.z - dog.z) > radius + 6) return;
        const pct = p.health / p.maxHealth;
        if (pct < lowestPct) {
          lowestPct = pct;
          target = p;
        }
      };
      consider(owner);
      if (target && lowestPct < 0.98) {
        const t = target as PlayerSchema;
        moveToward(dog, t.x, t.z, DOG_BASE.speed * 1.2, dt, 1.3);
        const healPerSec = DOG_BASE.mendPerSecond * stageMult(dog.stage);
        const before = t.health;
        t.health = Math.min(t.maxHealth, t.health + healPerSec * dt);
        const healed = t.health - before;
        if (healed > 0) addAffinity(dog, DogTask.Mend, DOG_TASK_XP.mendTick * healed);
      } else {
        moveToward(dog, owner.x, owner.z, DOG_BASE.speed, dt, DOG_BASE.followDistance);
      }
      break;
    }
    default: {
      const behindX = owner.x - Math.sin(owner.yaw) * DOG_BASE.followDistance;
      const behindZ = owner.z - Math.cos(owner.yaw) * DOG_BASE.followDistance;
      moveToward(dog, behindX, behindZ, DOG_BASE.speed, dt, 0.5);
    }
  }

  refreshEvolution(dog, ctx.onFx, owner.sessionId);
}

export function progressFor(dog: DogSchema) {
  return affinityProgress({
    guard: dog.affGuard,
    hunt: dog.affHunt,
    scavenge: dog.affScavenge,
    mend: dog.affMend,
    scout: dog.affScout,
  });
}
