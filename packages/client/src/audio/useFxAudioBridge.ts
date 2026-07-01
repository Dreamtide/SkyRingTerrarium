import { useEffect } from "react";
import { fxBus } from "../net/fx";
import { audioEngine } from "./audio";

const MAP: Record<string, keyof typeof audioEngine> = {
  shot: "shot",
  shotmiss: "shot",
  hit: "hit",
  ram: "ram",
  dash: "dash",
  transformStart: "transform",
  bite: "bite",
  collect: "collect",
  evolve: "evolve",
  downed: "downed",
  revive: "revive",
  death: "hit",
};

/** Bridges networked fx events (from the server) to procedural sound playback. Mount once. */
export function useFxAudioBridge() {
  useEffect(() => {
    const offs = Object.entries(MAP).map(([type, fn]) =>
      fxBus.on(type, () => {
        (audioEngine[fn] as () => void)();
      })
    );
    return () => offs.forEach((off) => off());
  }, []);
}
