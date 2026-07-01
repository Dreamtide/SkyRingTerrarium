import type { InputState } from "@dream/shared";

const TOUCH_PULSE_MS = 140;

/**
 * Unifies keyboard, virtual touch joystick/buttons, and gamepad input into a single
 * InputState. Movement is single-stick: facing (yaw) always tracks the movement
 * heading, and combat auto-targets the nearest enemy in a forward cone server-side,
 * so the same simple control scheme works well on touch, keyboard and pad.
 *
 * sample() is a pure read of "what does input look like right now" - it has no
 * side effects, so it's safe to call once per render frame (for local prediction)
 * AND again on the network-send timer without the two colliding. Edge-triggered
 * actions (transform/dash "pressed this tick") are detected independently by
 * each consumer (see stepPlayerMovement's prevTransformBtn/prevDashBtn), not here.
 */
class InputManager {
  private keys = new Set<string>();
  private touchMoveX = 0;
  private touchMoveZ = 0;
  private touchAttack = false;
  private touchTransformUntil = 0;
  private touchDashUntil = 0;

  private lastYaw = 0;
  private sendSeq = 0;
  private started = false;

  start() {
    if (this.started) return;
    this.started = true;
    window.addEventListener("keydown", this.onKeyDown);
    window.addEventListener("keyup", this.onKeyUp);
    window.addEventListener("blur", this.onBlur);
    window.addEventListener("mousedown", this.onMouseDown);
    window.addEventListener("mouseup", this.onMouseUp);
  }

  stop() {
    if (!this.started) return;
    this.started = false;
    window.removeEventListener("keydown", this.onKeyDown);
    window.removeEventListener("keyup", this.onKeyUp);
    window.removeEventListener("blur", this.onBlur);
    window.removeEventListener("mousedown", this.onMouseDown);
    window.removeEventListener("mouseup", this.onMouseUp);
    this.keys.clear();
  }

  private onMouseDown = (e: MouseEvent) => {
    if (e.button === 0) this.keys.add("Mouse0");
  };
  private onMouseUp = (e: MouseEvent) => {
    if (e.button === 0) this.keys.delete("Mouse0");
  };

  private onKeyDown = (e: KeyboardEvent) => {
    this.keys.add(e.code);
  };
  private onKeyUp = (e: KeyboardEvent) => {
    this.keys.delete(e.code);
  };
  private onBlur = () => {
    this.keys.clear();
  };

  setTouchMove(x: number, z: number) {
    this.touchMoveX = x;
    this.touchMoveZ = z;
  }

  setTouchAttack(v: boolean) {
    this.touchAttack = v;
  }

  pulseTouchTransform() {
    this.touchTransformUntil = performance.now() + TOUCH_PULSE_MS;
  }

  pulseTouchDash() {
    this.touchDashUntil = performance.now() + TOUCH_PULSE_MS;
  }

  isMobileLike(): boolean {
    return "ontouchstart" in window || navigator.maxTouchPoints > 0;
  }

  /** Monotonic sequence number for outgoing network input messages only. */
  nextSeq(): number {
    return this.sendSeq++;
  }

  sample(): InputState {
    let mx = this.touchMoveX;
    let mz = this.touchMoveZ;

    if (this.keys.has("KeyW") || this.keys.has("ArrowUp")) mz += 1;
    if (this.keys.has("KeyS") || this.keys.has("ArrowDown")) mz -= 1;
    if (this.keys.has("KeyD") || this.keys.has("ArrowRight")) mx += 1;
    if (this.keys.has("KeyA") || this.keys.has("ArrowLeft")) mx -= 1;

    const len = Math.hypot(mx, mz);
    if (len > 1) {
      mx /= len;
      mz /= len;
    }

    if (len > 0.12 || Math.hypot(this.touchMoveX, this.touchMoveZ) > 0.12) {
      this.lastYaw = Math.atan2(mx, mz);
    }

    const now = performance.now();
    const attack = this.touchAttack || this.keys.has("Space") || this.keys.has("KeyJ") || this.keys.has("Mouse0");
    const transform = now < this.touchTransformUntil || this.keys.has("KeyF") || this.keys.has("KeyE");
    const dash = now < this.touchDashUntil || this.keys.has("ShiftLeft") || this.keys.has("ShiftRight") || this.keys.has("KeyK");

    return {
      moveX: mx,
      moveZ: mz,
      yaw: this.lastYaw,
      attack,
      transform,
      dash,
      seq: 0,
    };
  }
}

export const inputManager = new InputManager();
