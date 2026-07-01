import {
  ArenaObstacle,
  InputState,
  MovementEvents,
  MovementRuntime,
  MovementStats,
  freshMovementRuntime,
  stepPlayerMovement,
} from "@dream/shared";

export interface PredictedState {
  x: number;
  z: number;
  yaw: number;
  mode: string;
  transforming: boolean;
}

/**
 * Local movement prediction for the player you're controlling.
 *
 * The server is still fully authoritative (it re-simulates movement itself and
 * ignores anything the client claims), but predicting locally means input feels
 * instant instead of waiting a network round trip to see your mech respond.
 * Mode/transform state is trusted from prediction (deterministic given the same
 * input + constants as the server); position is continuously nudged toward the
 * authoritative server value and hard-snapped on a large mismatch (e.g. a wave
 * transition teleport or a revive).
 */
export class LocalPredictor {
  state: PredictedState = { x: 0, z: 0, yaw: 0, mode: "robot", transforming: false };
  private rt: MovementRuntime = freshMovementRuntime();
  private ready = false;

  isReady(): boolean {
    return this.ready;
  }

  snapTo(x: number, z: number, yaw: number, mode: string, transforming: boolean) {
    this.state.x = x;
    this.state.z = z;
    this.state.yaw = yaw;
    this.state.mode = mode;
    this.state.transforming = transforming;
    this.ready = true;
  }

  step(input: InputState, stats: MovementStats, obstacles: ArenaObstacle[], dt: number): MovementEvents {
    if (!this.ready) return {};
    return stepPlayerMovement(this.state, this.rt, input, stats, obstacles, dt);
  }

  /** Pulls prediction toward the authoritative server position; hard-snaps on a big desync. */
  reconcile(serverX: number, serverZ: number, serverYaw: number, serverMode: string, serverTransforming: boolean) {
    if (!this.ready) {
      this.snapTo(serverX, serverZ, serverYaw, serverMode, serverTransforming);
      return;
    }
    const dist = Math.hypot(serverX - this.state.x, serverZ - this.state.z);
    if (dist > 2.2) {
      this.snapTo(serverX, serverZ, serverYaw, serverMode, serverTransforming);
      return;
    }
    this.state.x += (serverX - this.state.x) * 0.08;
    this.state.z += (serverZ - this.state.z) * 0.08;
  }
}
