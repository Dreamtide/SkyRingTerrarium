import { Schema, type, MapSchema } from "@colyseus/schema";

export class PartLoadoutSchema extends Schema {
  @type("string") chassis: string = "";
  @type("string") weapon: string = "";
  @type("string") engine: string = "";
  @type("string") plating: string = "";
}

export class PlayerSchema extends Schema {
  @type("string") sessionId: string = "";
  @type("string") name: string = "Pilot";
  @type("string") color: string = "#4fa8ff";
  @type("number") x: number = 0;
  @type("number") y: number = 0;
  @type("number") z: number = 0;
  @type("number") yaw: number = 0;
  @type("string") mode: string = "robot";
  @type("boolean") transforming: boolean = false;
  @type("number") ackSeq: number = 0;
  @type("number") health: number = 100;
  @type("number") maxHealth: number = 100;
  @type("number") shield: number = 40;
  @type("number") maxShield: number = 40;
  @type("boolean") alive: boolean = true;
  @type("boolean") downed: boolean = false;
  @type("number") reviveProgress: number = 0;
  @type("boolean") ready: boolean = false;
  @type("number") kills: number = 0;
  @type("number") partsCollected: number = 0;
  @type(PartLoadoutSchema) loadout: PartLoadoutSchema = new PartLoadoutSchema();
}

export class DogSchema extends Schema {
  @type("string") ownerSessionId: string = "";
  @type("number") x: number = 0;
  @type("number") y: number = 0;
  @type("number") z: number = 0;
  @type("number") yaw: number = 0;
  @type("string") task: string = "idle";
  @type("string") formId: string = "pup";
  @type("string") stage: string = "pup";
  @type("number") guardX: number = 0;
  @type("number") guardZ: number = 0;
  @type("number") affGuard: number = 0;
  @type("number") affHunt: number = 0;
  @type("number") affScavenge: number = 0;
  @type("number") affMend: number = 0;
  @type("number") affScout: number = 0;
}

export class EnemySchema extends Schema {
  @type("string") id: string = "";
  @type("string") enemyType: string = "scrapling";
  @type("number") x: number = 0;
  @type("number") y: number = 0;
  @type("number") z: number = 0;
  @type("number") yaw: number = 0;
  @type("number") health: number = 10;
  @type("number") maxHealth: number = 10;
  @type("boolean") alive: boolean = true;
  @type("boolean") telegraph: boolean = false;
}

export class PickupSchema extends Schema {
  @type("string") id: string = "";
  @type("string") partId: string = "";
  @type("number") x: number = 0;
  @type("number") z: number = 0;
}

export class RoomState extends Schema {
  @type("string") phase: string = "lobby";
  @type("number") waveIndex: number = 0;
  @type("number") waveTimer: number = 0;
  @type("number") seed: number = 0;
  @type("string") hostSessionId: string = "";
  @type("number") coreShardsEarned: number = 0;
  @type("string") announcement: string = "";
  @type("number") enemiesRemaining: number = 0;
  @type("number") enemiesTotal: number = 0;
  @type({ map: PlayerSchema }) players = new MapSchema<PlayerSchema>();
  @type({ map: DogSchema }) dogs = new MapSchema<DogSchema>();
  @type({ map: EnemySchema }) enemies = new MapSchema<EnemySchema>();
  @type({ map: PickupSchema }) pickups = new MapSchema<PickupSchema>();
}
