import { useMemo, useState } from "react";
import { useGameStore } from "../state/store";
import { audioEngine } from "../audio/audio";

export default function Lobby() {
  const room = useGameStore((s) => s.room);
  const mySessionId = useGameStore((s) => s.mySessionId);
  const players = useGameStore((s) => s.players);
  const playerIds = useGameStore((s) => s.playerIds);
  const hud = useGameStore((s) => s.hud);
  const setScreen = useGameStore((s) => s.setScreen);
  const reset = useGameStore((s) => s.reset);
  const setRoom = useGameStore((s) => s.setRoom);
  const [copied, setCopied] = useState(false);

  const isHost = hud.hostSessionId === mySessionId;
  const me = players[mySessionId];
  const list = useMemo(() => playerIds.map((id) => players[id]).filter(Boolean), [playerIds, players]);
  const allReady = list.length <= 1 || list.every((p) => p.ready);

  // Room codes from Colyseus are case-sensitive - never transform the case, or joining by code will fail.
  const code = room?.roomId ?? "";

  function copyCode() {
    navigator.clipboard?.writeText(code).catch(() => {});
    setCopied(true);
    setTimeout(() => setCopied(false), 1500);
  }

  function toggleReady() {
    audioEngine.click();
    room?.send("ready");
  }

  function start() {
    audioEngine.click();
    room?.send("start");
  }

  function leave() {
    room?.leave();
    setRoom(null);
    reset();
    setScreen("hub");
  }

  return (
    <div
      style={{
        width: "100%",
        height: "100%",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        padding: 20,
        background: "radial-gradient(ellipse at 50% -10%, rgba(120,170,255,0.16), transparent 60%), #05070d",
      }}
    >
      <div className="dream-panel" style={{ width: "min(520px, 100%)", padding: "28px 26px" }}>
        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 4 }}>
          <div className="dream-title" style={{ fontSize: 22 }}>Squad Lobby</div>
          <button className="dream-btn secondary" style={{ padding: "6px 12px", fontSize: 12 }} onClick={leave}>
            Leave
          </button>
        </div>
        <div style={{ color: "var(--text-dim)", fontSize: 12, marginBottom: 20 }}>Up to 5 pilots &middot; share the code to squad up</div>

        <div
          className="dream-btn secondary"
          style={{ textAlign: "center", fontSize: 22, letterSpacing: "0.15em", marginBottom: 20, cursor: "pointer", textTransform: "none" }}
          onClick={copyCode}
          title="Click to copy - code is case-sensitive"
        >
          {code || "..."} {copied ? "✓" : "⧉"}
        </div>

        <div style={{ display: "flex", flexDirection: "column", gap: 8, marginBottom: 22 }}>
          {list.map((p) => (
            <div
              key={p.sessionId}
              style={{
                display: "flex",
                alignItems: "center",
                gap: 12,
                padding: "10px 14px",
                borderRadius: 10,
                background: "rgba(255,255,255,0.04)",
                border: "1px solid var(--border)",
              }}
            >
              <div style={{ width: 12, height: 12, borderRadius: 4, background: p.color, boxShadow: `0 0 10px ${p.color}` }} />
              <div style={{ flex: 1, fontWeight: 600 }}>
                {p.name}
                {p.sessionId === hud.hostSessionId && <span style={{ color: "var(--warn)", fontSize: 11, marginLeft: 8 }}>HOST</span>}
                {p.sessionId === mySessionId && <span style={{ color: "var(--text-dim)", fontSize: 11, marginLeft: 8 }}>(you)</span>}
              </div>
              <div style={{ fontSize: 12, fontWeight: 700, color: p.ready ? "var(--good)" : "var(--text-dim)" }}>
                {p.ready ? "READY" : "WAITING"}
              </div>
            </div>
          ))}
        </div>

        {me && (
          <button className="dream-btn" style={{ width: "100%", marginBottom: 10 }} onClick={toggleReady}>
            {me.ready ? "Cancel Ready" : "Ready Up"}
          </button>
        )}

        {isHost ? (
          <button className="dream-btn" style={{ width: "100%", background: allReady ? undefined : "rgba(255,255,255,0.05)" }} disabled={!allReady} onClick={start}>
            Launch Deployment
          </button>
        ) : (
          <div style={{ textAlign: "center", color: "var(--text-dim)", fontSize: 13 }}>Waiting for host to launch...</div>
        )}
      </div>
    </div>
  );
}
