import { Client, Room } from "colyseus.js";

function resolveServerUrl(): string {
  const fromEnv = import.meta.env.VITE_SERVER_URL as string | undefined;
  if (fromEnv) return fromEnv;
  const protocol = window.location.protocol === "https:" ? "wss" : "ws";
  return `${protocol}://${window.location.hostname}:2567`;
}

let client: Client | null = null;

export function getClient(): Client {
  if (!client) client = new Client(resolveServerUrl());
  return client;
}

export async function createLobby(name: string): Promise<Room> {
  return getClient().create("dream_room", { name });
}

export async function joinLobby(code: string, name: string): Promise<Room> {
  return getClient().joinById(code.trim(), { name });
}

export async function joinAnyLobby(name: string): Promise<Room> {
  return getClient().joinOrCreate("dream_room", { name });
}
