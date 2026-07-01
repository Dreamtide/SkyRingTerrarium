import { useRef } from "react";
import { inputManager } from "../input/InputManager";
import { audioEngine } from "../audio/audio";

const STICK_RADIUS = 52;

export default function TouchControls() {
  const baseRef = useRef<HTMLDivElement>(null);
  const knobRef = useRef<HTMLDivElement>(null);
  const activePointer = useRef<number | null>(null);
  const origin = useRef({ x: 0, y: 0 });

  function onStickDown(e: React.PointerEvent) {
    activePointer.current = e.pointerId;
    const rect = baseRef.current!.getBoundingClientRect();
    origin.current = { x: rect.left + rect.width / 2, y: rect.top + rect.height / 2 };
    updateStick(e.clientX, e.clientY);
    (e.target as Element).setPointerCapture(e.pointerId);
  }

  function onStickMove(e: React.PointerEvent) {
    if (activePointer.current !== e.pointerId) return;
    updateStick(e.clientX, e.clientY);
  }

  function onStickUp(e: React.PointerEvent) {
    if (activePointer.current !== e.pointerId) return;
    activePointer.current = null;
    inputManager.setTouchMove(0, 0);
    if (knobRef.current) knobRef.current.style.transform = `translate(-50%, -50%)`;
  }

  function updateStick(clientX: number, clientY: number) {
    const dx = clientX - origin.current.x;
    const dy = clientY - origin.current.y;
    const dist = Math.min(STICK_RADIUS, Math.hypot(dx, dy));
    const angle = Math.atan2(dy, dx);
    const kx = Math.cos(angle) * dist;
    const ky = Math.sin(angle) * dist;
    if (knobRef.current) knobRef.current.style.transform = `translate(calc(-50% + ${kx}px), calc(-50% + ${ky}px))`;
    // screen X -> world moveX (right), screen Y (down) -> world -moveZ (forward is up on screen)
    inputManager.setTouchMove(dx / STICK_RADIUS, -dy / STICK_RADIUS);
  }

  return (
    <div style={{ position: "absolute", inset: 0, pointerEvents: "none" }}>
      <div
        ref={baseRef}
        onPointerDown={onStickDown}
        onPointerMove={onStickMove}
        onPointerUp={onStickUp}
        onPointerCancel={onStickUp}
        style={{
          position: "absolute",
          left: 24,
          bottom: 24,
          width: 116,
          height: 116,
          borderRadius: "50%",
          background: "rgba(255,255,255,0.06)",
          border: "1px solid var(--border)",
          pointerEvents: "auto",
          touchAction: "none",
        }}
      >
        <div
          ref={knobRef}
          style={{
            position: "absolute",
            left: "50%",
            top: "50%",
            width: 52,
            height: 52,
            borderRadius: "50%",
            background: "rgba(79,168,255,0.5)",
            border: "1px solid rgba(79,168,255,0.9)",
            transform: "translate(-50%, -50%)",
          }}
        />
      </div>

      <div style={{ position: "absolute", right: 20, bottom: 24, display: "flex", gap: 14, pointerEvents: "auto" }}>
        <button
          onPointerDown={() => {
            inputManager.pulseTouchDash();
            audioEngine.unlock();
          }}
          className="dream-btn secondary"
          style={{ width: 62, height: 62, borderRadius: "50%", fontSize: 20, padding: 0, touchAction: "none" }}
        >
          ⚡
        </button>
        <button
          onPointerDown={(e) => {
            e.preventDefault();
            inputManager.setTouchAttack(true);
          }}
          onPointerUp={() => inputManager.setTouchAttack(false)}
          onPointerLeave={() => inputManager.setTouchAttack(false)}
          className="dream-btn"
          style={{ width: 78, height: 78, borderRadius: "50%", fontSize: 24, padding: 0, touchAction: "none" }}
        >
          ⊙
        </button>
        <button
          onPointerDown={() => {
            inputManager.pulseTouchTransform();
            audioEngine.unlock();
          }}
          className="dream-btn secondary"
          style={{ width: 62, height: 62, borderRadius: "50%", fontSize: 18, padding: 0, touchAction: "none" }}
        >
          ⇄
        </button>
      </div>
    </div>
  );
}
