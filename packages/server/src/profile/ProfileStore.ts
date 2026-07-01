import { mkdirSync, readFileSync, writeFileSync, existsSync, renameSync } from "fs";
import { join } from "path";
import { PlayerProfile, freshProfile, normalizeProfile } from "@dream/shared";

const SAVE_DIR = process.env.DREAM_DATA_DIR || join(process.cwd(), "data");
const SAVE_FILE = join(SAVE_DIR, "profiles.json");
const FLUSH_DELAY_MS = 1500;

/**
 * Persistent player profiles, keyed by a client-generated device id.
 *
 * Deliberately a single JSON file with debounced atomic writes rather than a
 * database: the game tops out at 5 concurrent players per room and profiles are
 * a few KB each, so a file keeps deployment to "any Node host with a writable
 * disk" - no extra infrastructure for someone self-hosting a co-op game.
 */
export class ProfileStore {
  private profiles = new Map<string, PlayerProfile>();
  private flushTimer: NodeJS.Timeout | null = null;
  private loaded = false;

  private load() {
    if (this.loaded) return;
    this.loaded = true;
    try {
      if (existsSync(SAVE_FILE)) {
        const raw = JSON.parse(readFileSync(SAVE_FILE, "utf8")) as Record<string, Partial<PlayerProfile>>;
        for (const [deviceId, profile] of Object.entries(raw)) {
          this.profiles.set(deviceId, normalizeProfile(profile, deviceId));
        }
      }
    } catch (err) {
      // A corrupt save file shouldn't take the server down; start fresh but keep the bad file for inspection.
      console.error("[ProfileStore] failed to load profiles, starting fresh:", err);
      try {
        renameSync(SAVE_FILE, `${SAVE_FILE}.corrupt-${Date.now()}`);
      } catch {
        /* ignore */
      }
    }
  }

  get(deviceId: string, name?: string): PlayerProfile {
    this.load();
    let profile = this.profiles.get(deviceId);
    if (!profile) {
      profile = freshProfile(deviceId, name ?? "Pilot");
      this.profiles.set(deviceId, profile);
      this.scheduleFlush();
    } else if (name && name !== profile.name) {
      profile.name = name;
      this.scheduleFlush();
    }
    return profile;
  }

  /** Mutate a profile through the callback, then persist. Returns the updated profile. */
  update(deviceId: string, mutate: (profile: PlayerProfile) => void): PlayerProfile {
    const profile = this.get(deviceId);
    mutate(profile);
    profile.updatedAt = Date.now();
    this.scheduleFlush();
    return profile;
  }

  private scheduleFlush() {
    if (this.flushTimer) return;
    this.flushTimer = setTimeout(() => {
      this.flushTimer = null;
      this.flushNow();
    }, FLUSH_DELAY_MS);
  }

  flushNow() {
    this.load();
    try {
      mkdirSync(SAVE_DIR, { recursive: true });
      const obj = Object.fromEntries(this.profiles.entries());
      const tmp = `${SAVE_FILE}.tmp`;
      writeFileSync(tmp, JSON.stringify(obj), "utf8");
      renameSync(tmp, SAVE_FILE);
    } catch (err) {
      console.error("[ProfileStore] flush failed:", err);
    }
  }
}

export const profileStore = new ProfileStore();
