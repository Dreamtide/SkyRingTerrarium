import { useEffect } from "react";
import { useGameStore } from "../state/store";
import { audioEngine } from "../audio/audio";

export default function ResultOverlay() {
  const hud = useGameStore((s) => s.hud);
  const room = useGameStore((s) => s.room);
  const mySessionId = useGameStore((s) => s.mySessionId);
  const isHost = hud.hostSessionId === mySessionId;
  const setScreen = useGameStore((s) => s.setScreen);

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
      <div className="dream-panel dream-fade-in" style={{ padding: "36px 40px", textAlign: "center", minWidth: 320 }}>
        <div className="dream-title" style={{ fontSize: 30, marginBottom: 8 }}>
          {win ? "Deployment Complete" : "Squad Down"}
        </div>
        <div style={{ color: "var(--text-dim)", marginBottom: 22 }}>
          {win ? "Every hostile wave neutralized. Great work, pilots." : "The Decepticon swarm overwhelmed the squad."}
        </div>
        <div style={{ fontSize: 22, fontWeight: 800, color: "var(--warn)", marginBottom: 24 }}>◆ {hud.coreShardsEarned} core shards earned</div>
        {isHost ? (
          <button className="dream-btn" style={{ width: "100%" }} onClick={returnToLobby}>
            Return to Lobby
          </button>
        ) : (
          <div style={{ color: "var(--text-dim)", fontSize: 13 }}>Waiting for host...</div>
        )}
      </div>
    </div>
  );
}
