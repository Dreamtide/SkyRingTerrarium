import { Room, getStateCallbacks } from "colyseus.js";
import { useGameStore } from "../state/store";
import { fxBus } from "./fx";
import { DogSnapshot, HudState, PlayerSnapshot } from "./types";

/* eslint-disable @typescript-eslint/no-explicit-any */

function snapshotPlayer(p: any): PlayerSnapshot {
  return {
    sessionId: p.sessionId,
    name: p.name,
    color: p.color,
    mode: p.mode,
    transforming: p.transforming,
    health: p.health,
    maxHealth: p.maxHealth,
    shield: p.shield,
    maxShield: p.maxShield,
    alive: p.alive,
    downed: p.downed,
    ready: p.ready,
    kills: p.kills,
    partsCollected: p.partsCollected,
    reviveProgress: p.reviveProgress,
    loadout: {
      chassis: p.loadout?.chassis ?? "",
      weapon: p.loadout?.weapon ?? "",
      engine: p.loadout?.engine ?? "",
      plating: p.loadout?.plating ?? "",
    },
  };
}

function snapshotDog(d: any): DogSnapshot {
  return {
    ownerSessionId: d.ownerSessionId,
    task: d.task,
    formId: d.formId,
    stage: d.stage,
    affGuard: d.affGuard,
    affHunt: d.affHunt,
    affScavenge: d.affScavenge,
    affMend: d.affMend,
    affScout: d.affScout,
  };
}

export function attachRoomSync(room: Room) {
  const $ = getStateCallbacks(room);
  const state = room.state as any;
  if (import.meta.env.DEV) (window as unknown as { __dreamRoom: unknown }).__dreamRoom = room;

  $(state).players.onAdd((player: any, sessionId: string) => {
    useGameStore.getState().updatePlayer(sessionId, snapshotPlayer(player));
    useGameStore.setState((s) => ({ playerIds: Array.from(new Set([...s.playerIds, sessionId])) }));
    $(player).onChange(() => {
      useGameStore.getState().updatePlayer(sessionId, snapshotPlayer(player));
    });
  });
  $(state).players.onRemove((_player: any, sessionId: string) => {
    useGameStore.getState().removePlayer(sessionId);
  });

  $(state).dogs.onAdd((dog: any, ownerId: string) => {
    useGameStore.getState().updateDog(ownerId, snapshotDog(dog));
    $(dog).onChange(() => {
      useGameStore.getState().updateDog(ownerId, snapshotDog(dog));
    });
  });
  $(state).dogs.onRemove((_dog: any, ownerId: string) => {
    useGameStore.getState().removeDog(ownerId);
  });

  $(state).enemies.onAdd((_e: any, id: string) => {
    useGameStore.setState((s) => ({ enemyIds: Array.from(new Set([...s.enemyIds, id])) }));
  });
  $(state).enemies.onRemove((_e: any, id: string) => {
    useGameStore.setState((s) => ({ enemyIds: s.enemyIds.filter((x) => x !== id) }));
  });

  $(state).pickups.onAdd((_p: any, id: string) => {
    useGameStore.setState((s) => ({ pickupIds: Array.from(new Set([...s.pickupIds, id])) }));
  });
  $(state).pickups.onRemove((_p: any, id: string) => {
    useGameStore.setState((s) => ({ pickupIds: s.pickupIds.filter((x) => x !== id) }));
  });

  const syncHud = () => {
    if (state.phase === undefined) return; // initial schema snapshot not decoded yet
    const h: HudState = {
      phase: state.phase,
      waveIndex: state.waveIndex,
      waveTimer: state.waveTimer,
      seed: state.seed,
      hostSessionId: state.hostSessionId,
      coreShardsEarned: state.coreShardsEarned,
      announcement: state.announcement,
      enemiesRemaining: state.enemiesRemaining,
      enemiesTotal: state.enemiesTotal,
    };
    useGameStore.getState().setHud(h);
  };
  $(state).onChange(() => syncHud());
  syncHud();

  room.onMessage("fx", (data: Record<string, unknown> & { type: string }) => {
    fxBus.emit(data.type, data);
  });

  room.onLeave(() => {
    useGameStore.getState().reset();
  });
}
