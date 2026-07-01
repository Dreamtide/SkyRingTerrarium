import type { InputState } from "@dream/shared";

const TOUCH_PULSE_MS = 140;

/**
 * Unifies keyboard/mouse, virtual touch joystick/buttons, and gamepad input into a
 * single InputState.
 *
 * Two control schemes:
 *  - Desktop (mouse+keyboard): facing (yaw) follows the mouse cursor raycast onto the
 *    ground (set continuously by scene/DesktopAimController), and WASD is aim-relative
 *    (strafing) - you can move and shoot independently, like a twin-stick shooter.
 *  - Touch: single-stick - facing follows the movement heading, since there's no
 *    second input channel to dedicate to aiming on a small screen. Combat auto-targets
 *    the nearest enemy in a forward cone server-side either way, so both schemes just
 *    need to get "roughly facing the target" right.
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
  private desktopAimYaw: number | null = null;

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

  /** True when the primary pointer is touch (phones/tablets) - false for mouse-driven devices, even ones that also have a touchscreen. */
  isMobileLike(): boolean {
    if (typeof window.matchMedia === "function") {
      return window.matchMedia("(pointer: coarse)").matches;
    }
    return "ontouchstart" in window || navigator.maxTouchPoints > 0;
  }

  /** Called every frame by DesktopAimController with the mouse's ground-raycast yaw. Pass null to fall back to movement-heading facing. */
  setDesktopAim(yaw: number | null) {
    this.desktopAimYaw = yaw;
  }

  /** Monotonic sequence number for outgoing network input messages only. */
  nextSeq(): number {
    return this.sendSeq++;
  }

  sample(): InputState {
    let kx = 0;
    let kz = 0;
    if (this.keys.has("KeyW") || this.keys.has("ArrowUp")) kz += 1;
    if (this.keys.has("KeyS") || this.keys.has("ArrowDown")) kz -= 1;
    if (this.keys.has("KeyD") || this.keys.has("ArrowRight")) kx += 1;
    if (this.keys.has("KeyA") || this.keys.has("ArrowLeft")) kx -= 1;

    let mx = this.touchMoveX + kx;
    let mz = this.touchMoveZ + kz;
    const len = Math.hypot(mx, mz);
    if (len > 1) {
      mx /= len;
      mz /= len;
    }

    let yaw: number;
    let worldMx = mx;
    let worldMz = mz;

    if (this.desktopAimYaw !== null) {
      // Mouse-aim: facing is independent of movement, WASD is aim-relative (forward = toward aim, strafe = perpendicular).
      yaw = this.desktopAimYaw;
      worldMx = mz * Math.sin(yaw) - mx * Math.cos(yaw);
      worldMz = mz * Math.cos(yaw) + mx * Math.sin(yaw);
    } else {
      // Touch: facing follows movement heading (no separate aim channel on a small screen).
      if (len > 0.12) this.lastYaw = Math.atan2(mx, mz);
      yaw = this.lastYaw;
    }

    const now = performance.now();
    const attack = this.touchAttack || this.keys.has("Space") || this.keys.has("KeyJ") || this.keys.has("Mouse0");
    const transform = now < this.touchTransformUntil || this.keys.has("KeyF") || this.keys.has("KeyE");
    const dash = now < this.touchDashUntil || this.keys.has("ShiftLeft") || this.keys.has("ShiftRight") || this.keys.has("KeyK");

    return {
      moveX: worldMx,
      moveZ: worldMz,
      yaw,
      attack,
      transform,
      dash,
      seq: 0,
    };
  }
}

export const inputManager = new InputManager();
