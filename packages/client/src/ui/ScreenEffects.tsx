import { useEffect, useState } from "react";
import { useGameStore } from "../state/store";
import { fxBus } from "../net/fx";

/** Full-viewport damage flash + low-health vignette, driven by CSS keyframe animations (cheap, no per-frame React work). */
export default function ScreenEffects() {
  const mySessionId = useGameStore((s) => s.mySessionId);
  const player = useGameStore((s) => s.players[mySessionId]);
  const [flashKey, setFlashKey] = useState(0);

  useEffect(() => {
    const off = fxBus.on("damage", (d) => {
      if (d.target === "player" && d.sessionId === mySessionId) setFlashKey((k) => k + 1);
    });
    return () => off();
  }, [mySessionId]);

  const hpPct = player ? player.health / Math.max(1, player.maxHealth) : 1;
  const lowHealth = !!player && player.alive && !player.downed && hpPct < 0.35;

  return (
    <div style={{ position: "absolute", inset: 0, pointerEvents: "none" }}>
      <div key={flashKey} className="dream-damage-flash" />
      {lowHealth && <div className="dream-low-health" />}
      {player?.downed && <div className="dream-downed-overlay" />}
    </div>
  );
}
