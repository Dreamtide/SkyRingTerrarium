import { useEffect } from "react";
import { SAND_COLOR, SandWallet } from "@dream/shared";
import { useGameStore } from "../../state/store";
import { fetchProfile } from "../../net/profileApi";
import HubScene from "../../scene/hub/HubScene";
import GaragePanel from "./GaragePanel";
import KennelPanel from "./KennelPanel";
import DeployPanel from "./DeployPanel";
import SettingsButton from "../SettingsPanel";

/**
 * The home base: a peaceful tropical oceanside island rendered full-screen, with a
 * deliberately sparse UI - one action row along the bottom and the sand wallet as a
 * quiet readout. Everything else (garage, kennel, deploy) opens on demand.
 */
export default function HubScreen() {
  const profile = useGameStore((s) => s.profile);
  const setProfile = useGameStore((s) => s.setProfile);
  const playerName = useGameStore((s) => s.playerName);
  const hubPanel = useGameStore((s) => s.hubPanel);
  const setHubPanel = useGameStore((s) => s.setHubPanel);

  useEffect(() => {
    let cancelled = false;
    fetchProfile(playerName || undefined)
      .then((p) => {
        if (!cancelled) setProfile(p);
      })
      .catch(() => {
        /* server offline - hub still renders; deploy will surface the error */
      });
    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return (
    <div style={{ width: "100%", height: "100%", position: "relative" }}>
      <HubScene />

      {/* title, small and out of the way */}
      <div style={{ position: "absolute", top: 18, left: 22, pointerEvents: "none" }}>
        <div className="dream-title" style={{ fontSize: 26, lineHeight: 1 }}>D.R.E.A.M.</div>
        <div style={{ color: "rgba(255,255,255,0.75)", fontSize: 10, letterSpacing: "0.12em", textShadow: "0 1px 4px rgba(0,0,0,0.4)" }}>
          DOGS RIDE EVOLVING ATTACK MECHS
        </div>
      </div>

      {/* sand wallet - quiet readout */}
      {profile && (
        <div style={{ position: "absolute", top: 18, right: 66, display: "flex", gap: 10, alignItems: "center" }} className="dream-panel dream-wallet">
          {(Object.keys(SAND_COLOR) as (keyof SandWallet)[]).map((t) => (
            <span key={t} style={{ color: SAND_COLOR[t], fontSize: 12, fontWeight: 800 }} title={t}>
              ◆ {Math.floor(profile.sand[t])}
            </span>
          ))}
        </div>
      )}

      <SettingsButton />

      {/* bottom action row */}
      <div
        style={{
          position: "absolute",
          bottom: 26,
          left: "50%",
          transform: "translateX(-50%)",
          display: "flex",
          gap: 12,
        }}
      >
        <button className="dream-btn" style={{ minWidth: 150, fontSize: 15 }} onClick={() => setHubPanel("deploy")}>
          Deploy
        </button>
        <button className="dream-btn secondary" onClick={() => setHubPanel("garage")}>
          Garage
        </button>
        <button className="dream-btn secondary" onClick={() => setHubPanel("kennel")}>
          Kennel
        </button>
      </div>

      {hubPanel === "garage" && <GaragePanel onClose={() => setHubPanel("none")} />}
      {hubPanel === "kennel" && <KennelPanel onClose={() => setHubPanel("none")} />}
      {hubPanel === "deploy" && <DeployPanel onClose={() => setHubPanel("none")} />}
    </div>
  );
}
