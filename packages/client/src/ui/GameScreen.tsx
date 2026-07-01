import { useEffect, useMemo } from "react";
import { TICK_RATE } from "@dream/shared";
import { useGameStore } from "../state/store";
import GameCanvas from "../scene/GameCanvas";
import HUD from "./HUD";
import TouchControls from "./TouchControls";
import ResultOverlay from "./ResultOverlay";
import ScreenEffects from "./ScreenEffects";
import { inputManager } from "../input/InputManager";
import { audioEngine } from "../audio/audio";

export default function GameScreen() {
  const room = useGameStore((s) => s.room);
  const phase = useGameStore((s) => s.hud.phase);
  const isMobile = useMemo(() => inputManager.isMobileLike(), []);

  useEffect(() => {
    inputManager.start();
    audioEngine.unlock();
    audioEngine.startAmbientMusic();
    const iv = setInterval(() => {
      if (!room) return;
      const input = inputManager.sample();
      input.seq = inputManager.nextSeq();
      room.send("input", input);
    }, 1000 / TICK_RATE);
    return () => {
      inputManager.stop();
      clearInterval(iv);
    };
  }, [room]);

  const showResult = phase === "victory" || phase === "defeat";

  return (
    <div style={{ width: "100%", height: "100%", position: "relative" }}>
      <GameCanvas />
      <ScreenEffects />
      <HUD />
      {isMobile && !showResult && <TouchControls />}
      {showResult && <ResultOverlay />}
    </div>
  );
}
