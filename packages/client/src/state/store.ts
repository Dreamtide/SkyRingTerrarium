import { create } from "zustand";
import type { Room } from "colyseus.js";
import { ArenaObstacle, generateArenaLayout } from "@dream/shared";
import { DogSnapshot, HudState, PlayerSnapshot } from "../net/types";

export type Screen = "menu" | "lobby" | "game";
export type Quality = "low" | "medium" | "high";

interface GameStore {
  screen: Screen;
  room: Room | null;
  mySessionId: string;
  playerName: string;
  lastError: string | null;
  quality: Quality;
  playerIds: string[];
  enemyIds: string[];
  pickupIds: string[];
  players: Record<string, PlayerSnapshot>;
  dogs: Record<string, DogSnapshot>;
  hud: HudState;
  /** Derived from hud.seed - kept in sync so client-side movement prediction and
   *  rendering (Arena) always agree on the exact same obstacle layout as the server. */
  obstacles: ArenaObstacle[];

  setScreen: (s: Screen) => void;
  setRoom: (r: Room | null) => void;
  setMySessionId: (id: string) => void;
  setPlayerName: (n: string) => void;
  setError: (e: string | null) => void;
  setQuality: (q: Quality) => void;

  setPlayerIds: (ids: string[]) => void;
  setEnemyIds: (ids: string[]) => void;
  setPickupIds: (ids: string[]) => void;
  updatePlayer: (id: string, p: PlayerSnapshot) => void;
  removePlayer: (id: string) => void;
  updateDog: (id: string, d: DogSnapshot) => void;
  removeDog: (id: string) => void;
  setHud: (h: Partial<HudState>) => void;
  reset: () => void;
}

const defaultHud: HudState = {
  phase: "lobby",
  waveIndex: 0,
  waveTimer: 0,
  seed: 0,
  hostSessionId: "",
  coreShardsEarned: 0,
  announcement: "",
  enemiesRemaining: 0,
  enemiesTotal: 0,
};

function detectDefaultQuality(): Quality {
  const isMobile = /Mobi|Android|iPhone|iPad/i.test(navigator.userAgent);
  const cores = navigator.hardwareConcurrency ?? 4;
  if (isMobile || cores <= 4) return "medium";
  return "high";
}

export const useGameStore = create<GameStore>((set) => ({
  screen: "menu",
  room: null,
  mySessionId: "",
  playerName: typeof localStorage !== "undefined" ? localStorage.getItem("dream_name") || "" : "",
  lastError: null,
  quality: detectDefaultQuality(),
  playerIds: [],
  enemyIds: [],
  pickupIds: [],
  players: {},
  dogs: {},
  hud: defaultHud,
  obstacles: [],

  setScreen: (s) => set({ screen: s }),
  setRoom: (r) => set({ room: r }),
  setMySessionId: (id) => set({ mySessionId: id }),
  setPlayerName: (n) => {
    if (typeof localStorage !== "undefined") localStorage.setItem("dream_name", n);
    set({ playerName: n });
  },
  setError: (e) => set({ lastError: e }),
  setQuality: (q) => set({ quality: q }),

  setPlayerIds: (ids) => set({ playerIds: ids }),
  setEnemyIds: (ids) => set({ enemyIds: ids }),
  setPickupIds: (ids) => set({ pickupIds: ids }),
  updatePlayer: (id, p) => set((s) => ({ players: { ...s.players, [id]: p } })),
  removePlayer: (id) =>
    set((s) => {
      const players = { ...s.players };
      delete players[id];
      return { players, playerIds: s.playerIds.filter((x) => x !== id) };
    }),
  updateDog: (id, d) => set((s) => ({ dogs: { ...s.dogs, [id]: d } })),
  removeDog: (id) =>
    set((s) => {
      const dogs = { ...s.dogs };
      delete dogs[id];
      return { dogs };
    }),
  setHud: (h) =>
    set((s) => {
      const nextSeed = h.seed ?? s.hud.seed;
      const obstacles = nextSeed !== s.hud.seed || s.obstacles.length === 0 ? generateArenaLayout(nextSeed || 1) : s.obstacles;
      return { hud: { ...s.hud, ...h }, obstacles };
    }),
  reset: () =>
    set({
      playerIds: [],
      enemyIds: [],
      pickupIds: [],
      players: {},
      dogs: {},
      hud: defaultHud,
      obstacles: [],
    }),
}));
