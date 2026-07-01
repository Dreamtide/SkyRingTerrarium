import type { InputState } from "@dream/shared";

/**
 * Unifies keyboard, virtual touch joystick/buttons, and gamepad input into a single
 * per-frame InputState. Movement is single-stick: facing (yaw) always tracks the
 * movement heading, and combat auto-targets the nearest enemy in a forward cone
 * server-side, so the same simple control scheme works well on touch, keyboard and pad.
 */
class InputManager {
  private keys = new Set<string>();
  private touchMoveX = 0;
  private touchMoveZ = 0;
  private touchAttack = false;
  private touchTransformPulse = 0;
  private touchDashPulse = 0;

  private lastYaw = 0;
  private seq = 0;
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
    this.touchTransformPulse = 2; // held true for 2 sampled frames to guarantee edge detection
  }

  pulseTouchDash() {
    this.touchDashPulse = 2;
  }

  isMobileLike(): boolean {
    return "ontouchstart" in window || navigator.maxTouchPoints > 0;
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

    const attack = this.touchAttack || this.keys.has("Space") || this.keys.has("KeyJ") || this.keys.has("Mouse0");
    const transform = this.touchTransformPulse > 0 || this.keys.has("KeyF") || this.keys.has("KeyE");
    const dash = this.touchDashPulse > 0 || this.keys.has("ShiftLeft") || this.keys.has("ShiftRight") || this.keys.has("KeyK");

    if (this.touchTransformPulse > 0) this.touchTransformPulse--;
    if (this.touchDashPulse > 0) this.touchDashPulse--;

    return {
      moveX: mx,
      moveZ: mz,
      yaw: this.lastYaw,
      attack,
      transform,
      dash,
      seq: this.seq++,
    };
  }
}

export const inputManager = new InputManager();
