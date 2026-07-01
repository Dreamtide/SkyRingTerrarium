import { useEffect } from "react";
import { useGameStore } from "./state/store";
import HubScreen from "./ui/hub/HubScreen";
import Lobby from "./ui/Lobby";
import GameScreen from "./ui/GameScreen";
import { useFxAudioBridge } from "./audio/useFxAudioBridge";
import { initAudioFromStorage } from "./ui/SettingsPanel";
import "./ui/theme.css";

export default function App() {
  const screen = useGameStore((s) => s.screen);
  const hudPhase = useGameStore((s) => s.hud.phase);
  const setScreen = useGameStore((s) => s.setScreen);

  useFxAudioBridge();

  useEffect(() => {
    initAudioFromStorage();
  }, []);

  useEffect(() => {
    if (screen === "lobby" && hudPhase !== "lobby") {
      setScreen("game");
    }
  }, [screen, hudPhase, setScreen]);

  return (
    <div style={{ width: "100vw", height: "100dvh", position: "relative", overflow: "hidden", background: "#05070d" }}>
      <div key={screen} className="dream-fade-in" style={{ width: "100%", height: "100%" }}>
        {screen === "hub" && <HubScreen />}
        {screen === "lobby" && <Lobby />}
        {screen === "game" && <GameScreen />}
      </div>
    </div>
  );
}
