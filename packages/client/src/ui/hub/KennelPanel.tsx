import { useState } from "react";
import {
  DogProfile,
  KENNEL_SLOT_COST_CHROMA,
  MAX_BOND,
  SAND_COLOR,
  TREAT_COST_CHROMA,
  affinityProgress,
  canAfford,
  resolveDogForm,
} from "@dream/shared";
import { useGameStore } from "../../state/store";
import { profileApi } from "../../net/profileApi";
import { audioEngine } from "../../audio/audio";

const AFF_LABELS: { key: keyof Pick<DogProfile, "affGuard" | "affHunt" | "affScavenge" | "affMend" | "affScout">; label: string; color: string }[] = [
  { key: "affGuard", label: "Guard", color: "#ff9d4d" },
  { key: "affHunt", label: "Hunt", color: "#ff5d5d" },
  { key: "affScavenge", label: "Scavenge", color: "#57d97b" },
  { key: "affScout", label: "Scout", color: "#4fa8ff" },
  { key: "affMend", label: "Mend", color: "#b563ff" },
];

function DogCard({ dog, isSelected, busy, onAction }: { dog: DogProfile; isSelected: boolean; busy: boolean; onAction: (fn: () => Promise<unknown>) => void }) {
  const profile = useGameStore((s) => s.profile);
  const [renaming, setRenaming] = useState(false);
  const [newName, setNewName] = useState(dog.name);
  const form = resolveDogForm({ guard: dog.affGuard, hunt: dog.affHunt, scavenge: dog.affScavenge, mend: dog.affMend, scout: dog.affScout });
  const progress = affinityProgress({ guard: dog.affGuard, hunt: dog.affHunt, scavenge: dog.affScavenge, mend: dog.affMend, scout: dog.affScout });
  const maxAff = Math.max(8, dog.affGuard, dog.affHunt, dog.affScavenge, dog.affMend, dog.affScout);
  const canTreat = profile ? canAfford(profile.sand, { chroma: TREAT_COST_CHROMA }) : false;

  return (
    <div
      className="dream-panel"
      style={{ padding: "12px 14px", border: isSelected ? "1px solid #57d97b" : undefined }}
    >
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 6 }}>
        {renaming ? (
          <form
            onSubmit={(e) => {
              e.preventDefault();
              setRenaming(false);
              if (newName.trim() && newName.trim() !== dog.name) onAction(() => profileApi.renameDog(dog.id, newName.trim()));
            }}
            style={{ display: "flex", gap: 6 }}
          >
            <input className="dream-input" style={{ padding: "4px 8px", fontSize: 13, width: 120 }} value={newName} maxLength={14} autoFocus onChange={(e) => setNewName(e.target.value)} />
            <button className="dream-btn secondary" style={{ padding: "4px 10px", fontSize: 11 }} type="submit">OK</button>
          </form>
        ) : (
          <div style={{ fontWeight: 800, fontSize: 15, cursor: "pointer" }} title="Click to rename" onClick={() => setRenaming(true)}>
            {dog.name} <span style={{ fontSize: 10, color: "var(--text-dim)" }}>✎</span>
          </div>
        )}
        <div style={{ fontSize: 11, color: "var(--warn)", fontWeight: 700 }}>{form.name}</div>
      </div>

      <div style={{ fontSize: 11, color: "var(--text-dim)", marginBottom: 8 }}>
        {form.passive} &middot; {dog.runsCompleted} runs
      </div>

      {/* bond hearts */}
      <div style={{ display: "flex", alignItems: "center", gap: 6, marginBottom: 8 }}>
        <span style={{ fontSize: 10, color: "var(--text-dim)", fontWeight: 700, width: 38 }}>BOND</span>
        <div style={{ flex: 1, height: 7, borderRadius: 4, background: "rgba(255,255,255,0.08)", overflow: "hidden" }}>
          <div style={{ width: `${(dog.bond / MAX_BOND) * 100}%`, height: "100%", background: "linear-gradient(90deg, #ff8dd8, #ff5d8d)" }} />
        </div>
        <span style={{ fontSize: 11, fontWeight: 700, color: "#ff8dd8" }}>{Math.round(dog.bond)}</span>
      </div>

      {/* affinity bars */}
      <div style={{ display: "flex", flexDirection: "column", gap: 3, marginBottom: 10 }}>
        {AFF_LABELS.map(({ key, label, color }) => (
          <div key={key} style={{ display: "flex", alignItems: "center", gap: 6 }}>
            <span style={{ fontSize: 9, color: "var(--text-dim)", width: 52, fontWeight: 600 }}>{label.toUpperCase()}</span>
            <div style={{ flex: 1, height: 4, borderRadius: 3, background: "rgba(255,255,255,0.07)", overflow: "hidden" }}>
              <div style={{ width: `${(dog[key] / maxAff) * 100}%`, height: "100%", background: color }} />
            </div>
          </div>
        ))}
      </div>
      <div style={{ fontSize: 10, color: "var(--text-dim)", marginBottom: 10 }}>
        Evolution: {Math.round(progress.total)} / {progress.nextThreshold} XP {progress.stage !== "stage3" ? "to next stage" : "(fully evolved)"}
      </div>

      <div style={{ display: "flex", gap: 8 }}>
        {!isSelected && (
          <button className="dream-btn" style={{ flex: 1, padding: "8px 0", fontSize: 11 }} disabled={busy} onClick={() => onAction(() => profileApi.selectLoadout(undefined, dog.id))}>
            Take on runs
          </button>
        )}
        <button
          className="dream-btn secondary"
          style={{ flex: 1, padding: "8px 0", fontSize: 11 }}
          disabled={busy || dog.bond >= MAX_BOND || !canTreat}
          title={`Feed a chroma treat (+bond). Costs ${TREAT_COST_CHROMA} chroma sand.`}
          onClick={() => onAction(() => profileApi.treatDog(dog.id))}
        >
          Treat <span style={{ color: SAND_COLOR.chroma }}>{TREAT_COST_CHROMA}◆</span>
        </button>
      </div>
    </div>
  );
}

export default function KennelPanel({ onClose }: { onClose: () => void }) {
  const profile = useGameStore((s) => s.profile);
  const setProfile = useGameStore((s) => s.setProfile);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (!profile) return null;

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

  const canBuySlot = canAfford(profile.sand, { chroma: KENNEL_SLOT_COST_CHROMA });

  return (
    <div className="dream-hub-modal" onClick={onClose}>
      <div className="dream-panel dream-fade-in dream-hub-panel" onClick={(e) => e.stopPropagation()}>
        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 6 }}>
          <div className="dream-title" style={{ fontSize: 22 }}>Kennel</div>
          <button className="dream-btn secondary" style={{ width: 32, height: 32, padding: 0, borderRadius: 8 }} onClick={onClose}>✕</button>
        </div>
        <div style={{ fontSize: 12, color: "var(--text-dim)", marginBottom: 14 }}>
          {profile.dogs.length} / {profile.kennelSlots} hounds &middot; chroma sand: <span style={{ color: SAND_COLOR.chroma, fontWeight: 700 }}>{Math.floor(profile.sand.chroma)}</span>
        </div>

        <div style={{ display: "flex", flexDirection: "column", gap: 10, marginBottom: 14, maxHeight: "46vh", overflowY: "auto" }}>
          {profile.dogs.map((dog) => (
            <DogCard key={dog.id} dog={dog} isSelected={profile.selectedDogId === dog.id} busy={busy} onAction={run} />
          ))}
        </div>

        <div style={{ display: "flex", gap: 8 }}>
          {profile.dogs.length < profile.kennelSlots ? (
            <button className="dream-btn" style={{ flex: 1 }} disabled={busy} onClick={() => run(() => profileApi.adoptDog())}>
              Adopt a new pup
            </button>
          ) : (
            <button className="dream-btn secondary" style={{ flex: 1, fontSize: 12 }} disabled={busy || !canBuySlot} onClick={() => run(() => profileApi.buyKennelSlot())}>
              Expand kennel — <span style={{ color: SAND_COLOR.chroma }}>{KENNEL_SLOT_COST_CHROMA}◆ chroma</span>
            </button>
          )}
        </div>

        {error && <div style={{ marginTop: 12, color: "var(--danger)", fontSize: 12 }}>{error}</div>}
      </div>
    </div>
  );
}
