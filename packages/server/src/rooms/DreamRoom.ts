import { Client, Room } from "colyseus";
import {
  ARENA_RADIUS,
  ArenaObstacle,
  DOG_TASK_LIST,
  ENEMY_BASE,
  InputState,
  MAX_PLAYERS,
  MIN_PLAYERS,
  PLAYER_BASE,
  RUN_BOND_GAIN,
  SAND_REWARD,
  SandWallet,
  TICK_DT,
  TICK_RATE,
  UPGRADE_CACHE_SECONDS,
  WAVES_PER_DEPLOYMENT,
  WaveSpawnEntry,
  buildWave,
  bossForDeployment,
  computeMechStats,
  generateArenaLayout,
  getMechClass,
  mulberry32,
  resolveDogForm,
  rollPart,
  stepPlayerMovement,
  MAX_BOND,
  MovementState,
} from "@dream/shared";
import { DogSchema, EnemySchema, PickupSchema, PlayerSchema, RoomState, SandDropSchema } from "../state/schema";
import { DogRuntime, EnemyRuntime, PlayerRuntime, freshInput, freshPlayerSandState } from "../sim/types";
import { applyDamage, mitigate, tryEquip } from "../sim/combat";
import { damageEnemy, tickEnemy } from "../sim/enemyAI";
import { tickDog } from "../sim/dogAI";
import { profileStore } from "../profile/ProfileStore";

const COLOR_PALETTE = ["#4fa8ff", "#ff5d5d", "#57d97b", "#ffb62e", "#b563ff"];
const SAND_VACUUM_RADIUS = 2.4;
const SAND_TYPES: (keyof SandWallet)[] = ["ferrite", "volt", "pyros", "chroma"];

function clamp(v: number, lo: number, hi: number): number {
  return Math.max(lo, Math.min(hi, v));
}

type Stats = ReturnType<typeof computeMechStats>;

export class DreamRoom extends Room<RoomState> {
  maxClients = MAX_PLAYERS;

  private obstacles: ArenaObstacle[] = [];
  private playerRt = new Map<string, PlayerRuntime>();
  private dogRt = new Map<string, DogRuntime>();
  private enemyRt = new Map<string, EnemyRuntime>();
  private enemyIdCounter = 0;
  private pickupIdCounter = 0;
  private sandIdCounter = 0;
  private colorIndex = 0;
  private allDownedTimer = 0;
  private pendingNext: number | "boss" = 0;
  private rng: () => number = mulberry32(1);

  onCreate() {
    this.setState(new RoomState());
    this.obstacles = generateArenaLayout(1);

    this.onMessage("ready", (client) => {
      const p = this.state.players.get(client.sessionId);
      if (p && this.state.phase === "lobby") p.ready = !p.ready;
    });

    this.onMessage("start", (client) => {
      if (client.sessionId !== this.state.hostSessionId) return;
      if (this.state.phase !== "lobby") return;
      const players = [...this.state.players.values()];
      if (players.length < MIN_PLAYERS) return;
      if (players.length > 1 && !players.every((p) => p.ready)) return;
      this.beginDeployment();
    });

    this.onMessage("returnToLobby", (client) => {
      if (client.sessionId !== this.state.hostSessionId) return;
      if (this.state.phase !== "victory" && this.state.phase !== "defeat") return;
      this.resetToLobby();
    });

    this.onMessage("assignTask", (client, msg: { task?: string }) => {
      const task = msg?.task;
      if (!task || (!DOG_TASK_LIST.includes(task as never) && task !== "idle")) return;
      const dog = this.state.dogs.get(client.sessionId);
      const player = this.state.players.get(client.sessionId);
      if (!dog || !player || !player.alive || player.downed) return;
      dog.task = task;
      if (task === "guard") {
        dog.guardX = player.x;
        dog.guardZ = player.z;
      }
    });

    this.onMessage("input", (client, msg: Partial<InputState>) => {
      const rt = this.playerRt.get(client.sessionId);
      const player = this.state.players.get(client.sessionId);
      if (!rt || !player || typeof msg?.moveX !== "number" || !isFinite(msg.moveX)) return;
      const seq = (msg.seq as number) | 0;
      rt.input = {
        moveX: clamp(msg.moveX, -1, 1),
        moveZ: isFinite(msg.moveZ as number) ? clamp(msg.moveZ as number, -1, 1) : 0,
        yaw: isFinite(msg.yaw as number) ? (msg.yaw as number) : rt.input.yaw,
        attack: !!msg.attack,
        transform: !!msg.transform,
        dash: !!msg.dash,
        seq,
      };
      // Lets the client discard acknowledged inputs from its prediction replay buffer.
      player.ackSeq = seq;
    });

    this.setSimulationInterval(() => this.update(), 1000 / TICK_RATE);
  }

  onJoin(client: Client, options: { name?: string; deviceId?: string }) {
    const player = new PlayerSchema();
    player.sessionId = client.sessionId;
    player.name = (options?.name || "Pilot").slice(0, 16);
    player.color = COLOR_PALETTE[this.colorIndex % COLOR_PALETTE.length];
    this.colorIndex++;
    const idx = this.state.players.size;
    const ang = ((Math.PI * 2) / MAX_PLAYERS) * idx;
    player.x = Math.cos(ang) * 3;
    player.z = Math.sin(ang) * 3;

    const dog = new DogSchema();
    dog.ownerSessionId = client.sessionId;
    dog.x = player.x - 1.2;
    dog.z = player.z - 1.2;

    const rt: PlayerRuntime = {
      sessionId: client.sessionId,
      deviceId: null,
      dogProfileId: null,
      input: freshInput(),
      attackCooldown: 0,
      dashCooldown: 0,
      dashTimer: 0,
      dashDirX: 0,
      dashDirZ: 0,
      transformTimer: 0,
      overdriveTimer: 0,
      downedTimer: 0,
      reviveProgress: 0,
      invulnTimer: 0,
      hitEnemyCooldowns: new Map(),
      prevTransformBtn: false,
      prevDashBtn: false,
      ...freshPlayerSandState(),
    };

    // Restore the player's chosen mech + garage upgrades + kennel dog from their profile.
    const deviceId = typeof options?.deviceId === "string" && /^[a-zA-Z0-9_-]{8,64}$/.test(options.deviceId) ? options.deviceId : null;
    if (deviceId) {
      const profile = profileStore.get(deviceId, player.name);
      rt.deviceId = deviceId;
      player.mechId = getMechClass(profile.selectedMechId).id;
      rt.upgrades = { ...profile.mechUpgrades[player.mechId] };
      const dogProfile = profile.dogs.find((d) => d.id === profile.selectedDogId) ?? profile.dogs[0];
      if (dogProfile) {
        rt.dogProfileId = dogProfile.id;
        dog.name = dogProfile.name;
        dog.bond = dogProfile.bond;
        dog.affGuard = dogProfile.affGuard;
        dog.affHunt = dogProfile.affHunt;
        dog.affScavenge = dogProfile.affScavenge;
        dog.affMend = dogProfile.affMend;
        dog.affScout = dogProfile.affScout;
        const form = resolveDogForm({
          guard: dog.affGuard,
          hunt: dog.affHunt,
          scavenge: dog.affScavenge,
          mend: dog.affMend,
          scout: dog.affScout,
        });
        dog.formId = form.id;
        dog.stage = form.stage;
      }
    }

    const stats = computeMechStats(player.mechId, rt.upgrades, player.loadout);
    player.maxHealth = stats.maxHealth;
    player.health = stats.maxHealth;
    player.maxShield = stats.maxShield;
    player.shield = stats.maxShield;

    this.state.players.set(client.sessionId, player);
    this.state.dogs.set(client.sessionId, dog);
    this.playerRt.set(client.sessionId, rt);
    this.dogRt.set(client.sessionId, { ownerSessionId: client.sessionId, attackCooldown: 0, guardSet: false, scoutPulseTimer: 0 });

    if (!this.state.hostSessionId) this.state.hostSessionId = client.sessionId;
  }

  async onLeave(client: Client, consented: boolean) {
    try {
      if (consented) throw new Error("consented");
      await this.allowReconnection(client, 20);
    } catch {
      this.persistPlayer(client.sessionId, false);
      this.removePlayer(client.sessionId);
    }
  }

  private removePlayer(sessionId: string) {
    this.state.players.delete(sessionId);
    this.state.dogs.delete(sessionId);
    this.playerRt.delete(sessionId);
    this.dogRt.delete(sessionId);
    if (this.state.hostSessionId === sessionId) {
      const next = [...this.state.players.keys()][0];
      this.state.hostSessionId = next ?? "";
    }
  }

  private statsFor(player: PlayerSchema, rt: PlayerRuntime): Stats {
    return computeMechStats(player.mechId, rt.upgrades, player.loadout);
  }

  // ---------- sand economy ----------

  private awardSand(sessionId: string, gain: Partial<SandWallet>) {
    const player = this.state.players.get(sessionId);
    const rt = this.playerRt.get(sessionId);
    if (!player || !rt) return;
    for (const key of SAND_TYPES) {
      const amt = gain[key];
      if (!amt) continue;
      rt.sandFrac[key] += amt;
    }
    player.sandEarned.ferrite = Math.floor(rt.sandFrac.ferrite);
    player.sandEarned.volt = Math.floor(rt.sandFrac.volt);
    player.sandEarned.pyros = Math.floor(rt.sandFrac.pyros);
    player.sandEarned.chroma = Math.floor(rt.sandFrac.chroma);
  }

  private awardSandToAll(gain: Partial<SandWallet>) {
    this.state.players.forEach((_, sessionId) => this.awardSand(sessionId, gain));
  }

  private spawnSandBurst(x: number, z: number, enemyType: string) {
    // Bigger enemies spill more grains; grain type is weighted so kills lean pyros but
    // everything shows up over time, keeping all four garage tracks progressing.
    const grains = enemyType === "boss" ? 12 : enemyType === "brute" ? 4 : 2;
    for (let i = 0; i < grains; i++) {
      const roll = this.rng();
      const sandType: keyof SandWallet = roll < 0.4 ? "pyros" : roll < 0.62 ? "ferrite" : roll < 0.84 ? "volt" : "chroma";
      const drop = new SandDropSchema();
      drop.id = `s${this.sandIdCounter++}`;
      drop.sandType = sandType;
      drop.amount = enemyType === "boss" ? 4 + Math.floor(this.rng() * 4) : 1 + Math.floor(this.rng() * 3);
      const ang = this.rng() * Math.PI * 2;
      const dist = 0.4 + this.rng() * 1.6;
      drop.x = x + Math.cos(ang) * dist;
      drop.z = z + Math.sin(ang) * dist;
      this.state.sandDrops.set(drop.id, drop);
    }
  }

  private tickSandVacuum(player: PlayerSchema) {
    if (!player.alive || player.downed) return;
    const collected: string[] = [];
    this.state.sandDrops.forEach((s, id) => {
      if (Math.hypot(s.x - player.x, s.z - player.z) < SAND_VACUUM_RADIUS) collected.push(id);
    });
    for (const id of collected) {
      const s = this.state.sandDrops.get(id);
      if (!s) continue;
      this.awardSand(player.sessionId, { [s.sandType]: s.amount });
      this.broadcast("fx", { type: "sand", sessionId: player.sessionId, sandType: s.sandType, amount: s.amount, x: s.x, z: s.z });
      this.state.sandDrops.delete(id);
    }
  }

  private clearSandDrops() {
    [...this.state.sandDrops.keys()].forEach((id) => this.state.sandDrops.delete(id));
  }

  /** Writes a player's run earnings + dog growth back to their persistent profile. Idempotent per run. */
  private persistPlayer(sessionId: string, victory: boolean) {
    const rt = this.playerRt.get(sessionId);
    const dog = this.state.dogs.get(sessionId);
    if (!rt || !rt.deviceId || rt.runPersisted) return;
    const hasEarnings = SAND_TYPES.some((k) => rt.sandFrac[k] >= 1);
    const ranAtAll = this.state.phase !== "lobby" || hasEarnings;
    if (!ranAtAll) return;
    rt.runPersisted = true;
    profileStore.update(rt.deviceId, (profile) => {
      for (const key of SAND_TYPES) {
        profile.sand[key] += Math.floor(rt.sandFrac[key]);
      }
      profile.totalRuns += 1;
      if (victory) profile.totalVictories += 1;
      if (rt.dogProfileId && dog) {
        const dogProfile = profile.dogs.find((d) => d.id === rt.dogProfileId);
        if (dogProfile) {
          dogProfile.affGuard = dog.affGuard;
          dogProfile.affHunt = dog.affHunt;
          dogProfile.affScavenge = dog.affScavenge;
          dogProfile.affMend = dog.affMend;
          dogProfile.affScout = dog.affScout;
          dogProfile.formId = dog.formId;
          dogProfile.stage = dog.stage;
          dogProfile.bond = Math.min(MAX_BOND, dogProfile.bond + RUN_BOND_GAIN);
          dogProfile.runsCompleted += 1;
        }
      }
    });
    // Reset accumulators so a subsequent run in the same room starts fresh.
    for (const key of SAND_TYPES) rt.sandFrac[key] = 0;
  }

  private persistAll(victory: boolean) {
    this.state.players.forEach((_, sessionId) => this.persistPlayer(sessionId, victory));
  }

  // ---------- deployment lifecycle ----------

  private beginDeployment() {
    this.state.seed = Math.floor(Math.random() * 1_000_000_000);
    this.obstacles = generateArenaLayout(this.state.seed);
    this.rng = mulberry32(this.state.seed);
    this.allDownedTimer = 0;
    let i = 0;
    const step = (Math.PI * 2) / Math.max(1, this.state.players.size);
    this.state.players.forEach((p) => {
      const ang = step * i;
      i++;
      p.x = Math.cos(ang) * 3;
      p.z = Math.sin(ang) * 3;
      p.y = 0;
      p.yaw = 0;
      const rt = this.playerRt.get(p.sessionId);
      if (rt) {
        const stats = this.statsFor(p, rt);
        p.maxHealth = stats.maxHealth;
        p.maxShield = stats.maxShield;
        rt.downedTimer = 0;
        rt.reviveProgress = 0;
        rt.dashCooldown = 0;
        rt.attackCooldown = 0;
        rt.overdriveTimer = 0;
        rt.transformTimer = 0;
        rt.runPersisted = false;
        rt.lastX = p.x;
        rt.lastZ = p.z;
        for (const key of SAND_TYPES) rt.sandFrac[key] = 0;
      }
      p.health = p.maxHealth;
      p.shield = p.maxShield;
      p.alive = true;
      p.downed = false;
      p.mode = "robot";
      p.transforming = false;
      p.reviveProgress = 0;
      p.sandEarned.ferrite = 0;
      p.sandEarned.volt = 0;
      p.sandEarned.pyros = 0;
      p.sandEarned.chroma = 0;
      const dog = this.state.dogs.get(p.sessionId);
      if (dog) {
        dog.x = p.x - 1.2;
        dog.z = p.z - 1.2;
        dog.task = "idle";
      }
    });
    [...this.state.pickups.keys()].forEach((id) => this.state.pickups.delete(id));
    this.clearSandDrops();
    this.startWave(0);
  }

  private resetToLobby() {
    this.state.phase = "lobby";
    this.state.players.forEach((p) => (p.ready = false));
    this.clearEnemies();
    [...this.state.pickups.keys()].forEach((id) => this.state.pickups.delete(id));
    this.clearSandDrops();
    this.state.announcement = "";
  }

  private arenaCenter() {
    let x = 0;
    let z = 0;
    let n = 0;
    this.state.players.forEach((p) => {
      x += p.x;
      z += p.z;
      n++;
    });
    return n > 0 ? { x: x / n, z: z / n } : { x: 0, z: 0 };
  }

  private clearEnemies() {
    [...this.state.enemies.keys()].forEach((id) => this.state.enemies.delete(id));
    this.enemyRt.clear();
    this.state.enemiesRemaining = 0;
    this.state.enemiesTotal = 0;
  }

  private startWave(index: number) {
    this.state.waveIndex = index;
    this.state.phase = "wave";
    this.state.waveTimer = 0;
    this.state.announcement = `Wave ${index + 1} of ${WAVES_PER_DEPLOYMENT}`;
    this.clearEnemies();
    this.spawnEnemyEntries(buildWave(index, this.state.players.size), 1 + index * 0.18, false);
    this.obstacles
      .filter((o) => o.kind === "crate")
      .forEach((o) => {
        if (this.rng() < 0.45) this.spawnPickup(o.x, o.z + 1.4);
      });
  }

  private startBoss() {
    this.state.phase = "boss";
    this.state.waveTimer = 0;
    this.state.announcement = "BOSS: Warforge Sentinel";
    this.clearEnemies();
    this.spawnEnemyEntries(bossForDeployment(), 1, true);
  }

  private startUpgrade(next: number | "boss") {
    this.pendingNext = next;
    this.state.phase = "upgrade";
    this.state.waveTimer = UPGRADE_CACHE_SECONDS;
    this.state.announcement = "Supply Cache - regroup and gear up!";
    this.clearEnemies();
    this.awardSandToAll(SAND_REWARD.perWaveClear);
    const count = 2 + this.state.players.size;
    const center = this.arenaCenter();
    for (let i = 0; i < count; i++) {
      const ang = this.rng() * Math.PI * 2;
      const dist = 3 + this.rng() * (ARENA_RADIUS - 8);
      this.spawnPickup(center.x + Math.cos(ang) * dist, center.z + Math.sin(ang) * dist);
    }
  }

  private beginNextAfterUpgrade() {
    if (this.pendingNext === "boss") this.startBoss();
    else this.startWave(this.pendingNext);
  }

  private victory() {
    this.state.phase = "victory";
    this.state.announcement = "Deployment Complete!";
    this.awardSandToAll(SAND_REWARD.perWaveClear);
    this.awardSandToAll(SAND_REWARD.victoryBonus);
    this.clearEnemies();
    this.persistAll(true);
  }

  private defeat() {
    this.state.phase = "defeat";
    this.state.announcement = "Squad Down...";
    this.awardSandToAll(SAND_REWARD.defeatConsolation);
    this.clearEnemies();
    this.persistAll(false);
  }

  private spawnEnemyEntries(entries: WaveSpawnEntry[], healthMult: number, isBoss: boolean) {
    let total = 0;
    const center = this.arenaCenter();
    entries.forEach((entry) => {
      for (let i = 0; i < entry.count; i++) {
        const id = `e${this.enemyIdCounter++}`;
        const angle = this.rng() * Math.PI * 2;
        const dist = isBoss ? 15 : ARENA_RADIUS - 5 - this.rng() * 8;
        const base = ENEMY_BASE[entry.type as keyof typeof ENEMY_BASE];
        const enemy = new EnemySchema();
        enemy.id = id;
        enemy.enemyType = entry.type;
        enemy.x = center.x + Math.cos(angle) * dist;
        enemy.z = center.z + Math.sin(angle) * dist;
        enemy.y = 0;
        enemy.maxHealth = Math.round(base.health * healthMult);
        enemy.health = enemy.maxHealth;
        enemy.alive = true;
        enemy.telegraph = true;
        this.state.enemies.set(id, enemy);
        this.enemyRt.set(id, {
          id,
          type: entry.type,
          attackCooldown: 0.6,
          targetId: null,
          spawnTelegraph: isBoss ? 2.4 : 1.1,
          knockX: 0,
          knockZ: 0,
        });
        total++;
      }
    });
    this.state.enemiesTotal = total;
    this.state.enemiesRemaining = total;
  }

  private spawnPickup(x: number, z: number, partId?: string) {
    const id = `p${this.pickupIdCounter++}`;
    const pickup = new PickupSchema();
    pickup.id = id;
    pickup.x = x;
    pickup.z = z;
    pickup.partId = partId ?? rollPart(this.rng, this.state.waveIndex).id;
    this.state.pickups.set(id, pickup);
  }

  private onEnemyDeath(id: string, enemy: EnemySchema, killerSessionId: string) {
    this.state.enemiesRemaining = Math.max(0, this.state.enemiesRemaining - 1);
    this.awardSand(killerSessionId, SAND_REWARD.perKill);
    this.spawnSandBurst(enemy.x, enemy.z, enemy.enemyType);
    if (enemy.enemyType === "boss") {
      for (let i = 0; i < 4; i++) this.spawnPickup(enemy.x + (this.rng() - 0.5) * 3, enemy.z + (this.rng() - 0.5) * 3);
    } else {
      const dropChance = enemy.enemyType === "scrapling" ? 0.3 : 0.55;
      if (this.rng() < dropChance) this.spawnPickup(enemy.x, enemy.z);
    }
    this.broadcast("fx", { type: "death", enemyType: enemy.enemyType, x: enemy.x, z: enemy.z, killerSessionId });
  }

  private damagePlayer(targetId: string, amount: number) {
    const player = this.state.players.get(targetId);
    const rt = this.playerRt.get(targetId);
    if (!player || !rt || !player.alive || player.downed) return;
    if (rt.invulnTimer > 0) return;
    const stats = this.statsFor(player, rt);
    const mitigated = mitigate(amount, stats.armor);
    applyDamage(player, mitigated);
    this.broadcast("fx", { type: "damage", target: "player", sessionId: targetId, x: player.x, y: 1.6, z: player.z, amount: Math.round(mitigated) });
    if (player.health <= 0) {
      player.health = 0;
      player.downed = true;
      this.broadcast("fx", { type: "downed", sessionId: targetId });
    } else {
      // Ferrite is earned by weathering hits and staying up - tanky play feeds armor upgrades.
      this.awardSand(targetId, { ferrite: mitigated * SAND_REWARD.perDamageTaken.ferrite });
    }
  }

  // ---------- per-tick simulation ----------

  private update() {
    const phase = this.state.phase;
    const active = phase === "wave" || phase === "boss";

    this.state.players.forEach((player, sessionId) => {
      const rt = this.playerRt.get(sessionId);
      if (!rt) return;
      const stats = this.statsFor(player, rt);
      if (stats.maxHealth !== player.maxHealth) {
        const delta = stats.maxHealth - player.maxHealth;
        player.maxHealth = stats.maxHealth;
        if (delta > 0) player.health = Math.min(player.maxHealth, player.health + delta);
      }
      if (stats.maxShield !== player.maxShield) {
        const delta = stats.maxShield - player.maxShield;
        player.maxShield = stats.maxShield;
        if (delta > 0) player.shield = Math.min(player.maxShield, player.shield + delta);
      }
      this.tickPlayerMovement(sessionId, player, rt, stats);
      if (active) {
        this.tickVehicleRam(sessionId, player, rt, stats);
        // Volt is earned by covering ground - fast, mobile play feeds engine upgrades.
        const moved = Math.hypot(player.x - rt.lastX, player.z - rt.lastZ);
        if (moved > 0.001 && moved < 3) this.awardSand(sessionId, { volt: moved * SAND_REWARD.perDistanceUnit.volt });
      }
      rt.lastX = player.x;
      rt.lastZ = player.z;
      this.tickFieldPickups(player);
      this.tickSandVacuum(player);
    });

    this.tickRevivesAndDefeat();

    this.state.dogs.forEach((dog, ownerId) => {
      const owner = this.state.players.get(ownerId);
      const rt = this.dogRt.get(ownerId);
      if (!owner || !rt) return;
      tickDog({
        dog,
        rt,
        owner,
        enemies: this.state.enemies,
        pickups: this.state.pickups,
        dt: TICK_DT,
        onEnemyKilled: (id, e, killer) => this.onEnemyDeath(id, e, killer),
        onFx: (t, d) => this.broadcast("fx", { type: t, ...d }),
      });
      // Chroma is earned by directing your hound - support/pet play feeds systems upgrades.
      if (active && dog.task !== "idle" && owner.alive && !owner.downed) {
        this.awardSand(ownerId, { chroma: SAND_REWARD.perDogTaskTick.chroma * TICK_DT });
      }
    });

    if (active) {
      this.state.enemies.forEach((enemy, id) => {
        const rt = this.enemyRt.get(id);
        if (!rt) return;
        tickEnemy({
          enemy,
          rt,
          players: this.state.players,
          playerRt: this.playerRt,
          dogs: this.state.dogs,
          obstacles: this.obstacles,
          onDamagePlayer: (sid, amt) => this.damagePlayer(sid, amt),
          onFx: (t, d) => this.broadcast("fx", { type: t, ...d }),
        });
      });
      this.state.waveTimer += TICK_DT;
      if (this.state.enemiesTotal > 0 && this.state.enemiesRemaining <= 0) {
        if (phase === "boss") this.victory();
        else {
          const next = this.state.waveIndex + 1;
          this.startUpgrade(next >= WAVES_PER_DEPLOYMENT ? "boss" : next);
        }
      }
      this.checkDefeat();
    } else if (phase === "upgrade") {
      this.state.waveTimer -= TICK_DT;
      this.state.players.forEach((p) => {
        if (p.alive && !p.downed) {
          p.shield = Math.min(p.maxShield, p.shield + p.maxShield * 0.18 * TICK_DT);
          p.health = Math.min(p.maxHealth, p.health + p.maxHealth * 0.035 * TICK_DT);
        }
      });
      if (this.state.waveTimer <= 0) this.beginNextAfterUpgrade();
    }
  }

  private tickPlayerMovement(sessionId: string, player: PlayerSchema, rt: PlayerRuntime, stats: Stats) {
    if (!player.alive || player.downed) return;
    const input = rt.input;

    const state: MovementState = { x: player.x, z: player.z, yaw: player.yaw, mode: player.mode, transforming: player.transforming };
    const events = stepPlayerMovement(state, rt, input, stats, this.obstacles, TICK_DT);
    player.x = state.x;
    player.z = state.z;
    player.yaw = state.yaw;
    player.mode = state.mode;
    player.transforming = state.transforming;

    if (events.transformStart) this.broadcast("fx", { type: "transformStart", sessionId, x: player.x, z: player.z });
    if (events.transformEnd) this.broadcast("fx", { type: "transform", sessionId, mode: player.mode });
    if (events.dashStart) {
      rt.invulnTimer = 0.3;
      this.broadcast("fx", { type: "dash", sessionId, x: player.x, z: player.z, yaw: player.yaw });
    }
    if (events.blocked) return;

    if (rt.invulnTimer > 0) rt.invulnTimer -= TICK_DT;
    if (rt.attackCooldown > 0) rt.attackCooldown -= TICK_DT;
    if (input.attack && rt.attackCooldown <= 0) this.handlePlayerAttack(sessionId, player, rt, stats);
  }

  private handlePlayerAttack(sessionId: string, player: PlayerSchema, rt: PlayerRuntime, stats: Stats) {
    rt.attackCooldown = 1 / stats.fireRate;
    if (player.mode === "vehicle") {
      rt.overdriveTimer = 1.2;
      this.broadcast("fx", { type: "overdrive", sessionId });
      return;
    }
    let bestId: string | null = null;
    let best: EnemySchema | null = null;
    let bestD = Infinity;
    this.state.enemies.forEach((e, id) => {
      if (!e.alive) return;
      const dx = e.x - player.x;
      const dz = e.z - player.z;
      const d = Math.hypot(dx, dz);
      if (d > stats.attackRange) return;
      const ang = Math.atan2(dx, dz);
      let diff = Math.abs(ang - player.yaw);
      if (diff > Math.PI) diff = Math.PI * 2 - diff;
      if (diff > 0.55) return;
      if (d < bestD) {
        bestD = d;
        best = e;
        bestId = id;
      }
    });
    if (best && bestId) {
      const enemy = best as EnemySchema;
      const killed = damageEnemy(enemy, stats.damage);
      this.broadcast("fx", { type: "shot", from: { x: player.x, y: 1.1, z: player.z }, to: { x: enemy.x, y: 1, z: enemy.z }, sessionId });
      this.broadcast("fx", { type: "damage", target: "enemy", enemyId: bestId, x: enemy.x, y: 1.3, z: enemy.z, amount: Math.round(stats.damage) });
      if (killed) {
        player.kills += 1;
        this.onEnemyDeath(bestId, enemy, sessionId);
      }
    } else {
      this.broadcast("fx", { type: "shotmiss", from: { x: player.x, y: 1.1, z: player.z }, yaw: player.yaw, sessionId });
    }
  }

  private tickVehicleRam(sessionId: string, player: PlayerSchema, rt: PlayerRuntime, stats: Stats) {
    if (player.mode !== "vehicle" || !player.alive || player.downed) return;
    this.state.enemies.forEach((e, id) => {
      if (!e.alive) return;
      const d = Math.hypot(e.x - player.x, e.z - player.z);
      const cd = rt.hitEnemyCooldowns.get(id) ?? 0;
      if (cd > 0) {
        rt.hitEnemyCooldowns.set(id, cd - TICK_DT);
        return;
      }
      if (d < 1.7) {
        const dmg = stats.damage * (rt.overdriveTimer > 0 ? 2.2 : 1.15);
        const killed = damageEnemy(e, dmg);
        rt.hitEnemyCooldowns.set(id, 0.5);
        const erRt = this.enemyRt.get(id);
        if (erRt) {
          const kx = (e.x - player.x) / (d || 1);
          const kz = (e.z - player.z) / (d || 1);
          erRt.knockX += kx * 9;
          erRt.knockZ += kz * 9;
        }
        this.broadcast("fx", { type: "ram", x: e.x, z: e.z });
        this.broadcast("fx", { type: "damage", target: "enemy", enemyId: id, x: e.x, y: 1.2, z: e.z, amount: Math.round(dmg) });
        if (killed) {
          player.kills += 1;
          this.onEnemyDeath(id, e, sessionId);
        }
      }
    });
  }

  private tickFieldPickups(player: PlayerSchema) {
    if (!player.alive || player.downed) return;
    const collected: string[] = [];
    this.state.pickups.forEach((p, id) => {
      if (Math.hypot(p.x - player.x, p.z - player.z) < 1.3) collected.push(id);
    });
    collected.forEach((id) => {
      const p = this.state.pickups.get(id);
      if (!p) return;
      tryEquip(player.loadout, p.partId);
      player.partsCollected += 1;
      this.broadcast("fx", { type: "collect", sessionId: player.sessionId, x: p.x, z: p.z });
      this.state.pickups.delete(id);
    });
  }

  private tickRevivesAndDefeat() {
    const allies = [...this.state.players.values()];
    this.state.players.forEach((player, sessionId) => {
      const rt = this.playerRt.get(sessionId);
      if (!rt) return;
      if (player.downed) {
        rt.downedTimer += TICK_DT;
        const helped = allies.some(
          (o) => o.sessionId !== sessionId && o.alive && !o.downed && Math.hypot(o.x - player.x, o.z - player.z) <= PLAYER_BASE.reviveRadius
        );
        rt.reviveProgress = helped
          ? Math.min(PLAYER_BASE.reviveSeconds, rt.reviveProgress + TICK_DT)
          : Math.max(0, rt.reviveProgress - TICK_DT * 0.5);
        player.reviveProgress = rt.reviveProgress;
        if (rt.reviveProgress >= PLAYER_BASE.reviveSeconds) {
          player.downed = false;
          player.health = player.maxHealth * 0.4;
          rt.reviveProgress = 0;
          rt.downedTimer = 0;
          player.reviveProgress = 0;
          this.broadcast("fx", { type: "revive", sessionId });
        }
      } else {
        rt.downedTimer = 0;
        rt.reviveProgress = 0;
        player.reviveProgress = 0;
      }
    });
  }

  private checkDefeat() {
    const players = [...this.state.players.values()];
    if (players.length === 0) return;
    const allDown = players.every((p) => p.downed);
    if (allDown) {
      this.allDownedTimer += TICK_DT;
      if (this.allDownedTimer > 2.5) this.defeat();
    } else {
      this.allDownedTimer = 0;
    }
  }
}
