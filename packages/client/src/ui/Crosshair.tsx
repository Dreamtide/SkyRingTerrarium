import { useEffect, useRef } from "react";

/** Custom reticle that follows the OS cursor - paired with CSS `cursor: none` on the canvas. Desktop-only. */
export default function Crosshair() {
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    function onMove(e: MouseEvent) {
      const el = ref.current;
      if (!el) return;
      el.style.left = `${e.clientX}px`;
      el.style.top = `${e.clientY}px`;
    }
    window.addEventListener("mousemove", onMove);
    return () => window.removeEventListener("mousemove", onMove);
  }, []);

  return (
    <div ref={ref} style={{ position: "fixed", left: -100, top: -100, pointerEvents: "none", zIndex: 30, transform: "translate(-50%, -50%)" }}>
      <div style={{ width: 24, height: 24, position: "relative" }}>
        <div style={{ position: "absolute", left: "50%", top: 1, width: 2, height: 7, background: "rgba(143,211,255,0.95)", transform: "translateX(-50%)", boxShadow: "0 0 4px rgba(143,211,255,0.8)" }} />
        <div style={{ position: "absolute", left: "50%", bottom: 1, width: 2, height: 7, background: "rgba(143,211,255,0.95)", transform: "translateX(-50%)", boxShadow: "0 0 4px rgba(143,211,255,0.8)" }} />
        <div style={{ position: "absolute", top: "50%", left: 1, height: 2, width: 7, background: "rgba(143,211,255,0.95)", transform: "translateY(-50%)", boxShadow: "0 0 4px rgba(143,211,255,0.8)" }} />
        <div style={{ position: "absolute", top: "50%", right: 1, height: 2, width: 7, background: "rgba(143,211,255,0.95)", transform: "translateY(-50%)", boxShadow: "0 0 4px rgba(143,211,255,0.8)" }} />
        <div style={{ position: "absolute", top: "50%", left: "50%", width: 4, height: 4, borderRadius: "50%", background: "#fff", transform: "translate(-50%,-50%)" }} />
      </div>
    </div>
  );
}
