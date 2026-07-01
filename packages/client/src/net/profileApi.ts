import type { PlayerProfile, UpgradeCategory } from "@dream/shared";
import { getDeviceId } from "./device";

function apiBase(): string {
  const fromEnv = import.meta.env.VITE_SERVER_URL as string | undefined;
  if (fromEnv) return fromEnv.replace(/^ws/, "http");
  return `${window.location.protocol}//${window.location.hostname}:2567`;
}

async function post(path: string, body: Record<string, unknown> = {}): Promise<PlayerProfile> {
  const res = await fetch(`${apiBase()}/profile/${getDeviceId()}${path}`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  const data = await res.json();
  if (!res.ok) throw new Error(data.error ?? `request failed (${res.status})`);
  return data as PlayerProfile;
}

export async function fetchProfile(name?: string): Promise<PlayerProfile> {
  const query = name ? `?name=${encodeURIComponent(name)}` : "";
  const res = await fetch(`${apiBase()}/profile/${getDeviceId()}${query}`);
  if (!res.ok) throw new Error(`profile fetch failed (${res.status})`);
  return (await res.json()) as PlayerProfile;
}

export const profileApi = {
  upgrade: (mechId: string, category: UpgradeCategory) => post("/upgrade", { mechId, category }),
  unlockMech: (mechId: string) => post("/unlockMech", { mechId }),
  selectLoadout: (mechId?: string, dogId?: string) => post("/selectLoadout", { mechId, dogId }),
  adoptDog: () => post("/adoptDog"),
  buyKennelSlot: () => post("/buyKennelSlot"),
  treatDog: (dogId: string) => post("/treatDog", { dogId }),
  renameDog: (dogId: string, name: string) => post("/renameDog", { dogId, name }),
};
