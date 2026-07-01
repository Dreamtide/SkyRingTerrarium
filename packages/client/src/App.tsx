import { useEffect } from "react";
import { useGameStore } from "./state/store";
import MainMenu from "./ui/MainMenu";
import Lobby from "./ui/Lobby";
import GameScreen from "./ui/GameScreen";
import { useFxAudioBridge } from "./audio/useFxAudioBridge";
import "./ui/theme.css";

export default function App() {
  const screen = useGameStore((s) => s.screen);
  const hudPhase = useGameStore((s) => s.hud.phase);
  const setScreen = useGameStore((s) => s.setScreen);

  useFxAudioBridge();

  useEffect(() => {
    if (screen === "lobby" && hudPhase !== "lobby") {
      setScreen("game");
    }
  }, [screen, hudPhase, setScreen]);

  return (
    <div style={{ width: "100vw", height: "100dvh", position: "relative", overflow: "hidden", background: "#05070d" }}>
      {screen === "menu" && <MainMenu />}
      {screen === "lobby" && <Lobby />}
      {screen === "game" && <GameScreen />}
    </div>
  );
}
