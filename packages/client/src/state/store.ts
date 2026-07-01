import { create } from "zustand";
import type { Room } from "colyseus.js";
import { ArenaDef, PlayerProfile, generateArena } from "@dream/shared";
import { DogSnapshot, HudState, PlayerSnapshot } from "../net/types";

export type Screen = "hub" | "lobby" | "game";
export type Quality = "low" | "medium" | "high";
/** HUD visibility: full = everything; minimal = only combat-critical, fading when idle; hidden = nothing but the reticle. */
export type HudMode = "full" | "minimal" | "hidden";
export type HubPanel = "none" | "garage" | "kennel" | "deploy" | "settings";

interface GameStore {
  screen: Screen;
  hubPanel: HubPanel;
  room: Room | null;
  mySessionId: string;
  playerName: string;
  lastError: string | null;
  quality: Quality;
  hudMode: HudMode;
  profile: PlayerProfile | null;
  playerIds: string[];
  enemyIds: string[];
  pickupIds: string[];
  sandDropIds: string[];
  players: Record<string, PlayerSnapshot>;
  dogs: Record<string, DogSnapshot>;
  hud: HudState;
  /** Derived from hud.seed - kept in sync so client-side movement prediction and
   *  rendering (Arena) always agree on the exact same biome/layout as the server. */
  arena: ArenaDef;

  setScreen: (s: Screen) => void;
  setHubPanel: (p: HubPanel) => void;
  setRoom: (r: Room | null) => void;
  setMySessionId: (id: string) => void;
  setPlayerName: (n: string) => void;
  setError: (e: string | null) => void;
  setQuality: (q: Quality) => void;
  setHudMode: (m: HudMode) => void;
  setProfile: (p: PlayerProfile | null) => void;

  setPlayerIds: (ids: string[]) => void;
  setEnemyIds: (ids: string[]) => void;
  setPickupIds: (ids: string[]) => void;
  setSandDropIds: (ids: string[]) => void;
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

function storedHudMode(): HudMode {
  const raw = typeof localStorage !== "undefined" ? localStorage.getItem("dream_hud_mode") : null;
  return raw === "minimal" || raw === "hidden" ? raw : "full";
}

export const useGameStore = create<GameStore>((set) => ({
  screen: "hub",
  hubPanel: "none",
  room: null,
  mySessionId: "",
  playerName: typeof localStorage !== "undefined" ? localStorage.getItem("dream_name") || "" : "",
  lastError: null,
  quality: detectDefaultQuality(),
  hudMode: storedHudMode(),
  profile: null,
  playerIds: [],
  enemyIds: [],
  pickupIds: [],
  sandDropIds: [],
  players: {},
  dogs: {},
  hud: defaultHud,
  arena: generateArena(1),

  setScreen: (s) => set({ screen: s }),
  setHubPanel: (p) => set({ hubPanel: p }),
  setRoom: (r) => set({ room: r }),
  setMySessionId: (id) => set({ mySessionId: id }),
  setPlayerName: (n) => {
    if (typeof localStorage !== "undefined") localStorage.setItem("dream_name", n);
    set({ playerName: n });
  },
  setError: (e) => set({ lastError: e }),
  setQuality: (q) => set({ quality: q }),
  setHudMode: (m) => {
    if (typeof localStorage !== "undefined") localStorage.setItem("dream_hud_mode", m);
    set({ hudMode: m });
  },
  setProfile: (p) => set({ profile: p }),

  setPlayerIds: (ids) => set({ playerIds: ids }),
  setEnemyIds: (ids) => set({ enemyIds: ids }),
  setPickupIds: (ids) => set({ pickupIds: ids }),
  setSandDropIds: (ids) => set({ sandDropIds: ids }),
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
      const arena = nextSeed !== s.hud.seed ? generateArena(nextSeed || 1) : s.arena;
      return { hud: { ...s.hud, ...h }, arena };
    }),
  reset: () =>
    set({
      playerIds: [],
      enemyIds: [],
      pickupIds: [],
      sandDropIds: [],
      players: {},
      dogs: {},
      hud: defaultHud,
      arena: generateArena(1),
    }),
}));
