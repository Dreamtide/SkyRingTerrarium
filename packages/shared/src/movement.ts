import { ArenaObstacle, clampToArena } from "./arena";
import { PLAYER_BASE } from "./balance";
import { InputState } from "./types";

/**
 * Resolves a movement delta against circular obstacles + the arena boundary.
 * Used identically by the authoritative server simulation and the client's
 * local prediction so the two can never disagree about where a collision happens.
 */
export function resolveMove(x: number, z: number, dx: number, dz: number, obstacles: ArenaObstacle[]): { x: number; z: number } {
  let nx = x + dx;
  let nz = z + dz;
  for (const o of obstacles) {
    const ddx = nx - o.x;
    const ddz = nz - o.z;
    const d = Math.hypot(ddx, ddz);
    const minD = o.radius + 0.55;
    if (d < minD && d > 0.0001) {
      nx = o.x + (ddx / d) * minD;
      nz = o.z + (ddz / d) * minD;
    }
  }
  return clampToArena(nx, nz);
}

export interface MovementState {
  x: number;
  z: number;
  yaw: number;
  mode: string;
  transforming: boolean;
}

export interface MovementRuntime {
  transformTimer: number;
  dashCooldown: number;
  dashTimer: number;
  dashDirX: number;
  dashDirZ: number;
  overdriveTimer: number;
  prevTransformBtn: boolean;
  prevDashBtn: boolean;
}

export interface MovementStats {
  robotSpeed: number;
  vehicleSpeed: number;
  dashCooldown: number;
}

export interface MovementEvents {
  transformStart?: boolean;
  transformEnd?: boolean;
  dashStart?: boolean;
  /** true if this tick's input was fully consumed by a transform lock (server also skips attack processing) */
  blocked?: boolean;
}

/**
 * A single fixed-timestep movement update: transform lock, dash, then free movement.
 * This is the ONE authoritative definition of "how a mech moves" - shared by the
 * server (ground truth) and the client (local prediction) so predicted motion always
 * matches what the server will eventually confirm.
 */
export function stepPlayerMovement(
  state: MovementState,
  rt: MovementRuntime,
  input: InputState,
  stats: MovementStats,
  obstacles: ArenaObstacle[],
  dt: number
): MovementEvents {
  const events: MovementEvents = {};

  if (rt.transformTimer > 0) {
    rt.transformTimer -= dt;
    if (rt.transformTimer <= 0) {
      state.mode = state.mode === "robot" ? "vehicle" : "robot";
      state.transforming = false;
      events.transformEnd = true;
    }
    rt.prevTransformBtn = input.transform;
    events.blocked = true;
    return events;
  }

  const transformPressed = input.transform && !rt.prevTransformBtn;
  rt.prevTransformBtn = input.transform;
  if (transformPressed) {
    rt.transformTimer = PLAYER_BASE.transformLockSeconds;
    state.transforming = true;
    events.transformStart = true;
    events.blocked = true;
    return events;
  }

  if (rt.dashCooldown > 0) rt.dashCooldown -= dt;
  const dashPressed = input.dash && !rt.prevDashBtn;
  rt.prevDashBtn = input.dash;

  if (rt.dashTimer > 0) {
    rt.dashTimer -= dt;
    const speed = PLAYER_BASE.dashDistance / 0.22;
    const moved = resolveMove(state.x, state.z, rt.dashDirX * speed * dt, rt.dashDirZ * speed * dt, obstacles);
    state.x = moved.x;
    state.z = moved.z;
  } else if (dashPressed && rt.dashCooldown <= 0) {
    let dx = input.moveX;
    let dz = input.moveZ;
    const len = Math.hypot(dx, dz);
    if (len < 0.1) {
      dx = Math.sin(input.yaw);
      dz = Math.cos(input.yaw);
    } else {
      dx /= len;
      dz /= len;
    }
    rt.dashDirX = dx;
    rt.dashDirZ = dz;
    rt.dashTimer = 0.22;
    rt.dashCooldown = stats.dashCooldown;
    events.dashStart = true;
  } else {
    const speedBase = state.mode === "vehicle" ? stats.vehicleSpeed : stats.robotSpeed;
    const speed = speedBase * (rt.overdriveTimer > 0 ? 1.5 : 1);
    let dx = input.moveX;
    let dz = input.moveZ;
    const len = Math.hypot(dx, dz);
    if (len > 1) {
      dx /= len;
      dz /= len;
    }
    const moved = resolveMove(state.x, state.z, dx * speed * dt, dz * speed * dt, obstacles);
    state.x = moved.x;
    state.z = moved.z;
  }

  state.yaw = input.yaw;
  if (rt.overdriveTimer > 0) rt.overdriveTimer -= dt;

  return events;
}

export function freshMovementRuntime(): MovementRuntime {
  return {
    transformTimer: 0,
    dashCooldown: 0,
    dashTimer: 0,
    dashDirX: 0,
    dashDirZ: 0,
    overdriveTimer: 0,
    prevTransformBtn: false,
    prevDashBtn: false,
  };
}
