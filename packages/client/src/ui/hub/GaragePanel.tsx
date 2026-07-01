import { useState } from "react";
import {
  MECH_CLASSES,
  SAND_COLOR,
  SAND_LABEL,
  SandWallet,
  UPGRADE_CATEGORIES,
  UPGRADE_LABEL,
  UPGRADE_MAX_LEVEL,
  UPGRADE_SAND_TYPE,
  canAfford,
  upgradeCost,
} from "@dream/shared";
import { useGameStore } from "../../state/store";
import { profileApi } from "../../net/profileApi";
import { audioEngine } from "../../audio/audio";
import SandFall from "./SandFall";

export default function GaragePanel({ onClose }: { onClose: () => void }) {
  const profile = useGameStore((s) => s.profile);
  const setProfile = useGameStore((s) => s.setProfile);
  const [selectedMech, setSelectedMech] = useState(profile?.selectedMechId ?? "vanguard");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (!profile) return null;
  const mech = MECH_CLASSES.find((m) => m.id === selectedMech) ?? MECH_CLASSES[0];
  const unlocked = profile.unlockedMechs.includes(mech.id);
  const levels = profile.mechUpgrades[mech.id];

  async function run(fn: () => Promise<unknown>) {
    setBusy(true);
    setError(null);
    try {
      const updated = await fn();
      setProfile(updated as typeof profile);
      audioEngine.collect();
    } catch (e) {
      setError(e instanceof Error ? e.message : "action failed");
      audioEngine.click();
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="dream-hub-modal" onClick={onClose}>
      <div className="dream-panel dream-fade-in dream-hub-panel" onClick={(e) => e.stopPropagation()}>
        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 14 }}>
          <div className="dream-title" style={{ fontSize: 22 }}>Garage</div>
          <button className="dream-btn secondary" style={{ width: 32, height: 32, padding: 0, borderRadius: 8 }} onClick={onClose}>✕</button>
        </div>

        {/* sand reserves as a live falling-sand pile */}
        <div style={{ marginBottom: 6 }}>
          <SandFall wallet={profile.sand} />
        </div>
        <div style={{ display: "flex", gap: 10, flexWrap: "wrap", marginBottom: 16, fontSize: 12, fontWeight: 700 }}>
          {(Object.keys(SAND_LABEL) as (keyof SandWallet)[]).map((t) => (
            <span key={t} style={{ color: SAND_COLOR[t] }}>
              {SAND_LABEL[t]} {Math.floor(profile.sand[t])}
            </span>
          ))}
        </div>

        {/* mech tabs */}
        <div style={{ display: "flex", gap: 6, marginBottom: 14, flexWrap: "wrap" }}>
          {MECH_CLASSES.map((m) => {
            const isUnlocked = profile.unlockedMechs.includes(m.id);
            const isSelectedLoadout = profile.selectedMechId === m.id;
            return (
              <button
                key={m.id}
                className="dream-btn secondary"
                style={{
                  padding: "8px 12px",
                  fontSize: 12,
                  opacity: isUnlocked ? 1 : 0.55,
                  borderColor: selectedMech === m.id ? "var(--accent)" : undefined,
                  background: isSelectedLoadout ? "linear-gradient(180deg, rgba(87,217,123,0.3), rgba(87,217,123,0.1))" : undefined,
                }}
                onClick={() => setSelectedMech(m.id)}
              >
                {m.name} {!isUnlocked && "🔒"}{isSelectedLoadout && " ✓"}
              </button>
            );
          })}
        </div>

        <div style={{ color: "var(--text-dim)", fontSize: 13, marginBottom: 14, lineHeight: 1.5 }}>{mech.description}</div>

        {!unlocked ? (
          <button
            className="dream-btn"
            style={{ width: "100%" }}
            disabled={busy || !canAfford(profile.sand, mech.unlockCost ?? {})}
            onClick={() => run(() => profileApi.unlockMech(mech.id))}
          >
            Unlock —{" "}
            {Object.entries(mech.unlockCost ?? {})
              .map(([t, amt]) => `${amt} ${SAND_LABEL[t as keyof SandWallet]}`)
              .join(", ")}
          </button>
        ) : (
          <>
            {profile.selectedMechId !== mech.id && (
              <button className="dream-btn" style={{ width: "100%", marginBottom: 12 }} disabled={busy} onClick={() => run(() => profileApi.selectLoadout(mech.id))}>
                Deploy with {mech.name}
              </button>
            )}
            <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
              {UPGRADE_CATEGORIES.map((cat) => {
                const level = levels[cat];
                const sandType = UPGRADE_SAND_TYPE[cat];
                const cost = upgradeCost(level);
                const maxed = level >= UPGRADE_MAX_LEVEL;
                const affordable = canAfford(profile.sand, { [sandType]: cost });
                return (
                  <div key={cat} style={{ display: "flex", alignItems: "center", gap: 10 }}>
                    <div style={{ flex: 1 }}>
                      <div style={{ fontSize: 12, fontWeight: 700 }}>{UPGRADE_LABEL[cat].name}</div>
                      <div style={{ fontSize: 10, color: "var(--text-dim)" }}>{UPGRADE_LABEL[cat].effect}</div>
                      <div style={{ display: "flex", gap: 3, marginTop: 4 }}>
                        {Array.from({ length: UPGRADE_MAX_LEVEL }, (_, i) => (
                          <div
                            key={i}
                            style={{
                              width: 14,
                              height: 6,
                              borderRadius: 2,
                              background: i < level ? SAND_COLOR[sandType] : "rgba(255,255,255,0.1)",
                            }}
                          />
                        ))}
                      </div>
                    </div>
                    <button
                      className="dream-btn secondary"
                      style={{ minWidth: 92, fontSize: 11, padding: "8px 10px", opacity: maxed ? 0.4 : 1 }}
                      disabled={busy || maxed || !affordable}
                      onClick={() => run(() => profileApi.upgrade(mech.id, cat))}
                    >
                      {maxed ? "MAX" : <span style={{ color: affordable ? SAND_COLOR[sandType] : "var(--danger)" }}>{cost} ◆</span>}
                    </button>
                  </div>
                );
              })}
            </div>
          </>
        )}

        {error && <div style={{ marginTop: 12, color: "var(--danger)", fontSize: 12 }}>{error}</div>}
      </div>
    </div>
  );
}
