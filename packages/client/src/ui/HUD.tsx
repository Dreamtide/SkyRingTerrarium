import { useEffect, useMemo } from "react";
import { DOG_TASK_LIST } from "@dream/shared";
import { useGameStore } from "../state/store";
import { audioEngine } from "../audio/audio";
import { inputManager } from "../input/InputManager";
import { useNarrowViewport } from "./useNarrowViewport";

const TASK_LABEL: Record<string, string> = {
  guard: "Guard",
  hunt: "Hunt",
  scavenge: "Scavenge",
  scout: "Scout",
  mend: "Mend",
};
const TASK_ICON: Record<string, string> = {
  guard: "⛨",
  hunt: "⚔",
  scavenge: "✦",
  scout: "◈",
  mend: "✚",
};

function PhaseBanner({ narrow }: { narrow: boolean }) {
  const hud = useGameStore((s) => s.hud);
  const label = useMemo(() => {
    if (hud.phase === "wave") return `WAVE ${hud.waveIndex + 1} — ${hud.enemiesRemaining} / ${hud.enemiesTotal} HOSTILES`;
    if (hud.phase === "boss") return `BOSS — ${hud.enemiesRemaining > 0 ? "ENGAGED" : "DEFEATED"}`;
    if (hud.phase === "upgrade") return `SUPPLY CACHE — ${Math.max(0, Math.ceil(hud.waveTimer))}s`;
    return hud.announcement;
  }, [hud]);

  if (!label) return null;
  return (
    <div
      style={{
        position: "absolute",
        top: narrow ? 66 : 14,
        left: "50%",
        transform: "translateX(-50%)",
        padding: narrow ? "5px 12px" : "8px 22px",
        borderRadius: 10,
        pointerEvents: "none",
        maxWidth: "94vw",
      }}
      className="dream-panel"
    >
      <div style={{ fontWeight: 800, letterSpacing: "0.05em", fontSize: narrow ? 11 : 14, color: "var(--text)", whiteSpace: "nowrap" }}>{label}</div>
    </div>
  );
}

function SquadPanel({ narrow }: { narrow: boolean }) {
  const playerIds = useGameStore((s) => s.playerIds);
  const players = useGameStore((s) => s.players);
  const dogs = useGameStore((s) => s.dogs);
  const mySessionId = useGameStore((s) => s.mySessionId);

  return (
    <div
      style={{
        position: "absolute",
        top: 14,
        left: 14,
        display: "flex",
        flexDirection: narrow ? "row" : "column",
        flexWrap: "wrap",
        gap: 6,
        pointerEvents: "none",
        maxWidth: narrow ? "72vw" : undefined,
      }}
    >
      {playerIds.map((id) => {
        const p = players[id];
        const d = dogs[id];
        if (!p) return null;
        const hpPct = Math.max(0, p.health / Math.max(1, p.maxHealth));
        const spPct = Math.max(0, p.shield / Math.max(1, p.maxShield));
        return (
          <div
            key={id}
            className="dream-panel"
            style={{
              width: narrow ? 110 : 190,
              padding: narrow ? "5px 8px" : "7px 10px",
              opacity: p.downed ? 0.55 : 1,
              border: id === mySessionId ? `1px solid ${p.color}` : undefined,
            }}
          >
            <div style={{ display: "flex", justifyContent: "space-between", fontSize: narrow ? 9 : 11, fontWeight: 700, marginBottom: 3, gap: 4 }}>
              <span style={{ color: p.color, overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>
                {p.name}
                {p.downed ? " (DOWN)" : ""}
              </span>
              <span style={{ color: "var(--text-dim)" }}>{d ? TASK_ICON[d.task] ?? "" : ""}</span>
            </div>
            <div style={{ height: 4, background: "rgba(255,255,255,0.08)", borderRadius: 4, overflow: "hidden", marginBottom: 3 }}>
              <div style={{ width: `${spPct * 100}%`, height: "100%", background: "#4fa8ff" }} />
            </div>
            <div style={{ height: 5, background: "rgba(255,255,255,0.08)", borderRadius: 4, overflow: "hidden" }}>
              <div style={{ width: `${hpPct * 100}%`, height: "100%", background: hpPct > 0.5 ? "#57d97b" : hpPct > 0.25 ? "#ffb62e" : "#ff5d5d" }} />
            </div>
          </div>
        );
      })}
    </div>
  );
}

function DogPanel({ bottomOffset }: { bottomOffset: number }) {
  const mySessionId = useGameStore((s) => s.mySessionId);
  const dog = useGameStore((s) => s.dogs[mySessionId]);
  const room = useGameStore((s) => s.room);

  if (!dog) return null;
  const total = dog.affGuard + dog.affHunt + dog.affScavenge + dog.affMend + dog.affScout;

  function assign(task: string) {
    audioEngine.click();
    room?.send("assignTask", { task });
  }

  return (
    <div
      style={{
        position: "absolute",
        bottom: bottomOffset,
        left: "50%",
        transform: "translateX(-50%)",
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        gap: 6,
      }}
    >
      <div className="dream-panel" style={{ padding: "5px 12px", fontSize: 10, color: "var(--text-dim)", fontWeight: 600 }}>
        {dog.stage.toUpperCase()} &middot; XP {Math.round(total)}
      </div>
      <div style={{ display: "flex", gap: 6 }}>
        {DOG_TASK_LIST.map((task, i) => (
          <button
            key={task}
            onClick={() => assign(task)}
            className="dream-btn"
            style={{
              width: 44,
              height: 44,
              padding: 0,
              borderRadius: 10,
              fontSize: 16,
              display: "flex",
              flexDirection: "column",
              alignItems: "center",
              justifyContent: "center",
              gap: 1,
              background: dog.task === task ? "linear-gradient(180deg, rgba(87,217,123,0.4), rgba(87,217,123,0.15))" : undefined,
              borderColor: dog.task === task ? "#57d97b" : undefined,
            }}
            title={`${TASK_LABEL[task]} (${i + 1})`}
          >
            <span>{TASK_ICON[task]}</span>
            <span style={{ fontSize: 7 }}>{i + 1}</span>
          </button>
        ))}
      </div>
    </div>
  );
}

function ShardCounter({ narrow }: { narrow: boolean }) {
  const shards = useGameStore((s) => s.hud.coreShardsEarned);
  return (
    <div
      className="dream-panel"
      style={{ position: "absolute", top: 14, right: 14, padding: narrow ? "5px 9px" : "8px 14px", fontWeight: 700, fontSize: narrow ? 11 : 14, color: "var(--warn)" }}
    >
      ◆ {shards}
    </div>
  );
}

export default function HUD() {
  const room = useGameStore((s) => s.room);
  const narrow = useNarrowViewport();
  const isTouch = useMemo(() => inputManager.isMobileLike(), []);
  const dogPanelBottom = isTouch ? 150 : 14;

  useEffect(() => {
    function onKey(e: KeyboardEvent) {
      const idx = Number(e.key) - 1;
      if (idx >= 0 && idx < DOG_TASK_LIST.length) {
        room?.send("assignTask", { task: DOG_TASK_LIST[idx] });
      }
    }
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [room]);

  return (
    <div style={{ position: "absolute", inset: 0, pointerEvents: "none" }}>
      <PhaseBanner narrow={narrow} />
      <SquadPanel narrow={narrow} />
      <ShardCounter narrow={narrow} />
      <div style={{ pointerEvents: "auto" }}>
        <DogPanel bottomOffset={dogPanelBottom} />
      </div>
    </div>
  );
}
