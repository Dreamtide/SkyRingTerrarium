import { Router } from "express";
import {
  KENNEL_SLOT_COST_CHROMA,
  MAX_BOND,
  MECH_BY_ID,
  TREAT_BOND_GAIN,
  TREAT_COST_CHROMA,
  UPGRADE_MAX_LEVEL,
  UPGRADE_SAND_TYPE,
  UpgradeCategory,
  canAfford,
  makeNewDog,
  spendWallet,
  upgradeCost,
} from "@dream/shared";
import { profileStore } from "./ProfileStore";

const DEVICE_ID_RE = /^[a-zA-Z0-9_-]{8,64}$/;

/**
 * Hub-screen API (garage/kennel/loadout). All spending and unlock validation lives
 * here on the server - the client renders whatever the profile says, so a modified
 * client can't grant itself sand or unlocks that other players' servers would honor.
 */
export function profileRoutes(): Router {
  const router = Router();

  router.use("/profile/:deviceId", (req, res, next) => {
    if (!DEVICE_ID_RE.test(req.params.deviceId)) {
      res.status(400).json({ error: "invalid device id" });
      return;
    }
    next();
  });

  router.get("/profile/:deviceId", (req, res) => {
    const name = typeof req.query.name === "string" ? req.query.name.slice(0, 16) : undefined;
    res.json(profileStore.get(req.params.deviceId, name));
  });

  router.post("/profile/:deviceId/upgrade", (req, res) => {
    const { mechId, category } = req.body as { mechId?: string; category?: UpgradeCategory };
    if (!mechId || !MECH_BY_ID[mechId] || !category || !(category in UPGRADE_SAND_TYPE)) {
      res.status(400).json({ error: "invalid mech or category" });
      return;
    }
    let error: string | null = null;
    const profile = profileStore.update(req.params.deviceId, (p) => {
      if (!p.unlockedMechs.includes(mechId)) {
        error = "mech not unlocked";
        return;
      }
      const levels = p.mechUpgrades[mechId];
      const level = levels[category];
      if (level >= UPGRADE_MAX_LEVEL) {
        error = "already at max level";
        return;
      }
      const sandType = UPGRADE_SAND_TYPE[category];
      const cost = { [sandType]: upgradeCost(level) };
      if (!canAfford(p.sand, cost)) {
        error = "not enough sand";
        return;
      }
      p.sand = spendWallet(p.sand, cost);
      levels[category] = level + 1;
    });
    if (error) res.status(409).json({ error });
    else res.json(profile);
  });

  router.post("/profile/:deviceId/unlockMech", (req, res) => {
    const { mechId } = req.body as { mechId?: string };
    const def = mechId ? MECH_BY_ID[mechId] : undefined;
    if (!def) {
      res.status(400).json({ error: "invalid mech" });
      return;
    }
    let error: string | null = null;
    const profile = profileStore.update(req.params.deviceId, (p) => {
      if (p.unlockedMechs.includes(def.id)) {
        error = "already unlocked";
        return;
      }
      const cost = def.unlockCost ?? {};
      if (!canAfford(p.sand, cost)) {
        error = "not enough sand";
        return;
      }
      p.sand = spendWallet(p.sand, cost);
      p.unlockedMechs.push(def.id);
    });
    if (error) res.status(409).json({ error });
    else res.json(profile);
  });

  router.post("/profile/:deviceId/selectLoadout", (req, res) => {
    const { mechId, dogId } = req.body as { mechId?: string; dogId?: string };
    let error: string | null = null;
    const profile = profileStore.update(req.params.deviceId, (p) => {
      if (mechId) {
        if (!p.unlockedMechs.includes(mechId)) {
          error = "mech not unlocked";
          return;
        }
        p.selectedMechId = mechId;
      }
      if (dogId) {
        if (!p.dogs.some((d) => d.id === dogId)) {
          error = "no such dog";
          return;
        }
        p.selectedDogId = dogId;
      }
    });
    if (error) res.status(409).json({ error });
    else res.json(profile);
  });

  router.post("/profile/:deviceId/adoptDog", (req, res) => {
    let error: string | null = null;
    const profile = profileStore.update(req.params.deviceId, (p) => {
      if (p.dogs.length >= p.kennelSlots) {
        error = "kennel is full";
        return;
      }
      p.dogs.push(makeNewDog());
    });
    if (error) res.status(409).json({ error });
    else res.json(profile);
  });

  router.post("/profile/:deviceId/buyKennelSlot", (req, res) => {
    let error: string | null = null;
    const profile = profileStore.update(req.params.deviceId, (p) => {
      const cost = { chroma: KENNEL_SLOT_COST_CHROMA };
      if (!canAfford(p.sand, cost)) {
        error = "not enough chroma sand";
        return;
      }
      p.sand = spendWallet(p.sand, cost);
      p.kennelSlots += 1;
    });
    if (error) res.status(409).json({ error });
    else res.json(profile);
  });

  router.post("/profile/:deviceId/treatDog", (req, res) => {
    const { dogId } = req.body as { dogId?: string };
    let error: string | null = null;
    const profile = profileStore.update(req.params.deviceId, (p) => {
      const dog = p.dogs.find((d) => d.id === dogId);
      if (!dog) {
        error = "no such dog";
        return;
      }
      if (dog.bond >= MAX_BOND) {
        error = "bond is already at max";
        return;
      }
      const cost = { chroma: TREAT_COST_CHROMA };
      if (!canAfford(p.sand, cost)) {
        error = "not enough chroma sand";
        return;
      }
      p.sand = spendWallet(p.sand, cost);
      dog.bond = Math.min(MAX_BOND, dog.bond + TREAT_BOND_GAIN);
    });
    if (error) res.status(409).json({ error });
    else res.json(profile);
  });

  router.post("/profile/:deviceId/renameDog", (req, res) => {
    const { dogId, name } = req.body as { dogId?: string; name?: string };
    const clean = (name ?? "").trim().slice(0, 14);
    if (!clean) {
      res.status(400).json({ error: "invalid name" });
      return;
    }
    let error: string | null = null;
    const profile = profileStore.update(req.params.deviceId, (p) => {
      const dog = p.dogs.find((d) => d.id === dogId);
      if (!dog) {
        error = "no such dog";
        return;
      }
      dog.name = clean;
    });
    if (error) res.status(409).json({ error });
    else res.json(profile);
  });

  return router;
}
