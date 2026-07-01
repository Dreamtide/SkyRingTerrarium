import { useEffect } from "react";
import { SAND_COLOR, SAND_LABEL, SandWallet } from "@dream/shared";
import { useGameStore } from "../state/store";
import { audioEngine } from "../audio/audio";

export default function ResultOverlay() {
  const hud = useGameStore((s) => s.hud);
  const room = useGameStore((s) => s.room);
  const mySessionId = useGameStore((s) => s.mySessionId);
  const me = useGameStore((s) => s.players[mySessionId]);
  const isHost = hud.hostSessionId === mySessionId;
  const setScreen = useGameStore((s) => s.setScreen);
  const setRoom = useGameStore((s) => s.setRoom);
  const reset = useGameStore((s) => s.reset);

  const win = hud.phase === "victory";

  useEffect(() => {
    if (win) audioEngine.victory();
    else audioEngine.defeat();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  function returnToLobby() {
    audioEngine.click();
    room?.send("returnToLobby");
    setScreen("lobby");
  }

  function returnToBase() {
    audioEngine.click();
    room?.leave();
    setRoom(null);
    reset();
    setScreen("hub");
  }

  const earned = me?.sandEarned;

  return (
    <div
      style={{
        position: "absolute",
        inset: 0,
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        background: "rgba(4,6,12,0.72)",
        backdropFilter: "blur(4px)",
      }}
    >
      <div className="dream-panel dream-fade-in" style={{ padding: "36px 40px", textAlign: "center", minWidth: 340 }}>
        <div className="dream-title" style={{ fontSize: 30, marginBottom: 8 }}>
          {win ? "Deployment Complete" : "Squad Down"}
        </div>
        <div style={{ color: "var(--text-dim)", marginBottom: 20 }}>
          {win ? "Every hostile wave neutralized. Great work, pilots." : "The Decepticon swarm overwhelmed the squad."}
        </div>

        {earned && (
          <div style={{ marginBottom: 24 }}>
            <div style={{ fontSize: 11, color: "var(--text-dim)", fontWeight: 700, letterSpacing: "0.08em", marginBottom: 8 }}>CYBERSAND BANKED</div>
            <div style={{ display: "flex", gap: 14, justifyContent: "center", flexWrap: "wrap" }}>
              {(Object.keys(SAND_LABEL) as (keyof SandWallet)[]).map((t) => (
                <div key={t} style={{ color: SAND_COLOR[t], fontWeight: 800, fontSize: 16 }}>
                  ◆ {earned[t]}
                  <div style={{ fontSize: 9, color: "var(--text-dim)", fontWeight: 600 }}>{SAND_LABEL[t].split(" ")[0]}</div>
                </div>
              ))}
            </div>
          </div>
        )}

        <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
          {isHost && (
            <button className="dream-btn" style={{ width: "100%" }} onClick={returnToLobby}>
              Run It Back
            </button>
          )}
          <button className={isHost ? "dream-btn secondary" : "dream-btn"} style={{ width: "100%" }} onClick={returnToBase}>
            Return to Base
          </button>
        </div>
      </div>
    </div>
  );
}
