import { useState } from "react";
import { useGameStore } from "../state/store";
import { createLobby, joinLobby } from "../net/room";
import { attachRoomSync } from "../net/sync";
import { audioEngine } from "../audio/audio";

export default function MainMenu() {
  const playerName = useGameStore((s) => s.playerName);
  const setPlayerName = useGameStore((s) => s.setPlayerName);
  const setRoom = useGameStore((s) => s.setRoom);
  const setMySessionId = useGameStore((s) => s.setMySessionId);
  const setScreen = useGameStore((s) => s.setScreen);
  const setError = useGameStore((s) => s.setError);
  const lastError = useGameStore((s) => s.lastError);

  const [code, setCode] = useState("");
  const [busy, setBusy] = useState(false);

  const nameOrDefault = () => (playerName.trim() ? playerName.trim() : `Pilot-${Math.floor(Math.random() * 900 + 100)}`);

  async function handleCreate() {
    audioEngine.unlock();
    audioEngine.click();
    setBusy(true);
    setError(null);
    try {
      const name = nameOrDefault();
      setPlayerName(name);
      const room = await createLobby(name);
      setRoom(room);
      setMySessionId(room.sessionId);
      attachRoomSync(room);
      setScreen("lobby");
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to create deployment.");
    } finally {
      setBusy(false);
    }
  }

  async function handleJoin() {
    if (!code.trim()) {
      setError("Enter a squad code first.");
      return;
    }
    audioEngine.unlock();
    audioEngine.click();
    setBusy(true);
    setError(null);
    try {
      const name = nameOrDefault();
      setPlayerName(name);
      const room = await joinLobby(code, name);
      setRoom(room);
      setMySessionId(room.sessionId);
      attachRoomSync(room);
      setScreen("lobby");
    } catch (e) {
      setError(e instanceof Error ? e.message : "Couldn't find that squad. Check the code and try again.");
    } finally {
      setBusy(false);
    }
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
        background:
          "radial-gradient(ellipse at 50% -10%, rgba(120,170,255,0.18), transparent 60%), radial-gradient(ellipse at 90% 110%, rgba(181,99,255,0.15), transparent 55%), #05070d",
      }}
    >
      <div className="dream-panel" style={{ width: "min(460px, 100%)", padding: "32px 28px", textAlign: "center" }}>
        <div className="dream-title" style={{ fontSize: "clamp(28px, 6vw, 40px)", lineHeight: 1.05 }}>D.R.E.A.M.</div>
        <div style={{ color: "var(--text-dim)", fontSize: 13, letterSpacing: "0.08em", marginTop: 6, marginBottom: 26 }}>
          DOGS RIDE EVOLVING ATTACK MECHS
        </div>

        <input
          className="dream-input"
          style={{ width: "100%", marginBottom: 14, textAlign: "center" }}
          placeholder="Pilot callsign"
          maxLength={16}
          value={playerName}
          onChange={(e) => setPlayerName(e.target.value)}
        />

        <button className="dream-btn" style={{ width: "100%" }} disabled={busy} onClick={handleCreate}>
          Start New Deployment
        </button>

        <div style={{ display: "flex", alignItems: "center", gap: 10, margin: "18px 0" }}>
          <div style={{ flex: 1, height: 1, background: "var(--border)" }} />
          <span style={{ color: "var(--text-dim)", fontSize: 12 }}>OR JOIN A SQUAD</span>
          <div style={{ flex: 1, height: 1, background: "var(--border)" }} />
        </div>

        <div style={{ display: "flex", gap: 10 }}>
          <input
            className="dream-input"
            style={{ flex: 1 }}
            placeholder="Squad code (case-sensitive)"
            maxLength={12}
            value={code}
            onChange={(e) => setCode(e.target.value)}
          />
          <button className="dream-btn secondary" disabled={busy} onClick={handleJoin}>
            Join
          </button>
        </div>

        {lastError && (
          <div style={{ marginTop: 16, color: "var(--danger)", fontSize: 13 }}>{lastError}</div>
        )}

        <div style={{ marginTop: 26, color: "var(--text-dim)", fontSize: 12, lineHeight: 1.6 }}>
          1-5 players co-op &middot; transform robot &harr; vehicle &middot; salvage parts &middot; raise a magic hound
        </div>
      </div>
    </div>
  );
}
