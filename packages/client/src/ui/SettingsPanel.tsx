import { useEffect, useState } from "react";
import { useGameStore, HudMode, Quality } from "../state/store";
import { audioEngine } from "../audio/audio";

const QUALITY_OPTIONS: { value: Quality; label: string }[] = [
  { value: "low", label: "Low" },
  { value: "medium", label: "Medium" },
  { value: "high", label: "High" },
];

function readStoredVolume(): number {
  const raw = typeof localStorage !== "undefined" ? localStorage.getItem("dream_volume") : null;
  const parsed = raw !== null ? parseFloat(raw) : NaN;
  return isNaN(parsed) ? 0.55 : parsed;
}

export function initAudioFromStorage() {
  audioEngine.setVolume(readStoredVolume());
}

export default function SettingsButton() {
  const [open, setOpen] = useState(false);
  return (
    <>
      <button
        className="dream-btn secondary"
        onClick={() => setOpen(true)}
        title="Settings"
        style={{ position: "absolute", top: 14, right: 14, width: 40, height: 40, padding: 0, fontSize: 17, borderRadius: 10 }}
      >
        ⚙
      </button>
      {open && <SettingsModal onClose={() => setOpen(false)} />}
    </>
  );
}

const HUD_OPTIONS: { value: HudMode; label: string; hint: string }[] = [
  { value: "full", label: "Full", hint: "everything visible" },
  { value: "minimal", label: "Minimal", hint: "appears only when needed" },
  { value: "hidden", label: "Off", hint: "pure gameplay (hotkeys still work)" },
];

function SettingsModal({ onClose }: { onClose: () => void }) {
  const quality = useGameStore((s) => s.quality);
  const setQuality = useGameStore((s) => s.setQuality);
  const hudMode = useGameStore((s) => s.hudMode);
  const setHudMode = useGameStore((s) => s.setHudMode);
  const [volume, setVolume] = useState(() => readStoredVolume());

  useEffect(() => {
    audioEngine.setVolume(volume);
    if (typeof localStorage !== "undefined") localStorage.setItem("dream_volume", String(volume));
  }, [volume]);

  return (
    <div
      style={{
        position: "fixed",
        inset: 0,
        background: "rgba(4,6,12,0.6)",
        backdropFilter: "blur(3px)",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        zIndex: 50,
      }}
      onClick={onClose}
    >
      <div className="dream-panel dream-fade-in" style={{ width: "min(360px, 90vw)", padding: "24px 22px" }} onClick={(e) => e.stopPropagation()}>
        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 18 }}>
          <div className="dream-title" style={{ fontSize: 18 }}>Settings</div>
          <button className="dream-btn secondary" style={{ width: 32, height: 32, padding: 0, fontSize: 13, borderRadius: 8 }} onClick={onClose}>
            ✕
          </button>
        </div>

        <div style={{ marginBottom: 20 }}>
          <div style={{ fontSize: 12, color: "var(--text-dim)", marginBottom: 8, fontWeight: 700, letterSpacing: "0.04em" }}>
            VOLUME &middot; {Math.round(volume * 100)}%
          </div>
          <input
            type="range"
            min={0}
            max={1}
            step={0.01}
            value={volume}
            onChange={(e) => setVolume(parseFloat(e.target.value))}
            style={{ width: "100%", accentColor: "#4fa8ff" }}
          />
        </div>

        <div style={{ marginBottom: 20 }}>
          <div style={{ fontSize: 12, color: "var(--text-dim)", marginBottom: 8, fontWeight: 700, letterSpacing: "0.04em" }}>VISUAL QUALITY</div>
          <div style={{ display: "flex", gap: 8 }}>
            {QUALITY_OPTIONS.map((opt) => (
              <button
                key={opt.value}
                className="dream-btn secondary"
                style={{
                  flex: 1,
                  padding: "8px 0",
                  fontSize: 12,
                  background: quality === opt.value ? "linear-gradient(180deg, rgba(79,168,255,0.35), rgba(79,168,255,0.12))" : undefined,
                  borderColor: quality === opt.value ? "var(--accent)" : undefined,
                }}
                onClick={() => setQuality(opt.value)}
              >
                {opt.label}
              </button>
            ))}
          </div>
        </div>

        <div>
          <div style={{ fontSize: 12, color: "var(--text-dim)", marginBottom: 8, fontWeight: 700, letterSpacing: "0.04em" }}>COMBAT HUD</div>
          <div style={{ display: "flex", gap: 8 }}>
            {HUD_OPTIONS.map((opt) => (
              <button
                key={opt.value}
                className="dream-btn secondary"
                title={opt.hint}
                style={{
                  flex: 1,
                  padding: "8px 0",
                  fontSize: 12,
                  background: hudMode === opt.value ? "linear-gradient(180deg, rgba(79,168,255,0.35), rgba(79,168,255,0.12))" : undefined,
                  borderColor: hudMode === opt.value ? "var(--accent)" : undefined,
                }}
                onClick={() => setHudMode(opt.value)}
              >
                {opt.label}
              </button>
            ))}
          </div>
          <div style={{ fontSize: 10, color: "var(--text-dim)", marginTop: 6 }}>
            Minimal fades the HUD in only when something needs attention.
          </div>
        </div>
      </div>
    </div>
  );
}
