import { useState } from "react";
import { MECH_BY_ID } from "@dream/shared";
import { useGameStore } from "../../state/store";
import { createLobby, joinLobby } from "../../net/room";
import { attachRoomSync } from "../../net/sync";
import { audioEngine } from "../../audio/audio";

export default function DeployPanel({ onClose }: { onClose: () => void }) {
  const playerName = useGameStore((s) => s.playerName);
  const setPlayerName = useGameStore((s) => s.setPlayerName);
  const profile = useGameStore((s) => s.profile);
  const setRoom = useGameStore((s) => s.setRoom);
  const setMySessionId = useGameStore((s) => s.setMySessionId);
  const setScreen = useGameStore((s) => s.setScreen);
  const setError = useGameStore((s) => s.setError);
  const lastError = useGameStore((s) => s.lastError);

  const [code, setCode] = useState("");
  const [busy, setBusy] = useState(false);

  const nameOrDefault = () => (playerName.trim() ? playerName.trim() : `Pilot-${Math.floor(Math.random() * 900 + 100)}`);
  const mechName = profile ? MECH_BY_ID[profile.selectedMechId]?.name ?? "Vanguard" : "Vanguard";
  const dogName = profile?.dogs.find((d) => d.id === profile.selectedDogId)?.name ?? "Pup";

  async function connect(fn: (name: string) => Promise<import("colyseus.js").Room>) {
    audioEngine.unlock();
    audioEngine.click();
    setBusy(true);
    setError(null);
    try {
      const name = nameOrDefault();
      setPlayerName(name);
      const room = await fn(name);
      setRoom(room);
      setMySessionId(room.sessionId);
      attachRoomSync(room);
      setScreen("lobby");
    } catch (e) {
      setError(e instanceof Error ? e.message : "Couldn't connect. Try again.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="dream-hub-modal" onClick={onClose}>
      <div className="dream-panel dream-fade-in dream-hub-panel" style={{ maxWidth: 420 }} onClick={(e) => e.stopPropagation()}>
        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 14 }}>
          <div className="dream-title" style={{ fontSize: 22 }}>Deploy</div>
          <button className="dream-btn secondary" style={{ width: 32, height: 32, padding: 0, borderRadius: 8 }} onClick={onClose}>✕</button>
        </div>

        <div style={{ fontSize: 12, color: "var(--text-dim)", marginBottom: 16, lineHeight: 1.6 }}>
          Deploying with <b style={{ color: "var(--text)" }}>{mechName}</b> and <b style={{ color: "var(--text)" }}>{dogName}</b>.
          <br />
          Change your loadout in the Garage and Kennel.
        </div>

        <input
          className="dream-input"
          style={{ width: "100%", marginBottom: 12, textAlign: "center" }}
          placeholder="Pilot callsign"
          maxLength={16}
          value={playerName}
          onChange={(e) => setPlayerName(e.target.value)}
        />

        <button className="dream-btn" style={{ width: "100%" }} disabled={busy} onClick={() => connect((n) => createLobby(n))}>
          Start New Deployment
        </button>

        <div style={{ display: "flex", alignItems: "center", gap: 10, margin: "14px 0" }}>
          <div style={{ flex: 1, height: 1, background: "var(--border)" }} />
          <span style={{ color: "var(--text-dim)", fontSize: 11 }}>OR JOIN A SQUAD</span>
          <div style={{ flex: 1, height: 1, background: "var(--border)" }} />
        </div>

        <div style={{ display: "flex", gap: 8 }}>
          <input
            className="dream-input"
            style={{ flex: 1 }}
            placeholder="Squad code (case-sensitive)"
            maxLength={12}
            value={code}
            onChange={(e) => setCode(e.target.value)}
          />
          <button
            className="dream-btn secondary"
            disabled={busy}
            onClick={() => {
              if (!code.trim()) {
                setError("Enter a squad code first.");
                return;
              }
              connect((n) => joinLobby(code, n));
            }}
          >
            Join
          </button>
        </div>

        {lastError && <div style={{ marginTop: 12, color: "var(--danger)", fontSize: 12 }}>{lastError}</div>}
      </div>
    </div>
  );
}
