import { useEffect, useState } from "react";
import { useGameStore } from "../state/store";
import { fxBus } from "../net/fx";

const ENEMY_LABEL: Record<string, string> = {
  scrapling: "Scrapling",
  strafer: "Strafer",
  brute: "Brute",
  sniper: "Sniper",
  boss: "Warforge Sentinel",
};

interface Toast {
  id: number;
  text: string;
  color: string;
}

let nextToastId = 0;

export default function KillFeed() {
  const players = useGameStore((s) => s.players);
  const [toasts, setToasts] = useState<Toast[]>([]);

  useEffect(() => {
    function push(text: string, color: string) {
      const id = nextToastId++;
      setToasts((t) => [...t.slice(-3), { id, text, color }]);
      setTimeout(() => setToasts((t) => t.filter((x) => x.id !== id)), 3400);
    }
    const offs = [
      fxBus.on("death", (d) => {
        const killer = players[d.killerSessionId as string];
        const label = ENEMY_LABEL[d.enemyType as string] ?? "hostile";
        if (killer) push(`${killer.name} destroyed a ${label}`, killer.color);
      }),
      fxBus.on("evolve", (d) => {
        const owner = players[d.ownerSessionId as string];
        if (owner) push(`${owner.name}'s hound evolved into ${d.formName}!`, "#b563ff");
      }),
    ];
    return () => offs.forEach((o) => o());
  }, [players]);

  return (
    <div
      style={{
        position: "absolute",
        top: 96,
        right: 14,
        display: "flex",
        flexDirection: "column",
        gap: 6,
        alignItems: "flex-end",
        pointerEvents: "none",
        maxWidth: "62vw",
      }}
    >
      {toasts.map((t) => (
        <div
          key={t.id}
          className="dream-panel dream-toast"
          style={{ padding: "6px 12px", fontSize: 12, fontWeight: 600, borderLeft: `3px solid ${t.color}`, whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" }}
        >
          {t.text}
        </div>
      ))}
    </div>
  );
}
