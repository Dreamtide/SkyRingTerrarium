import { BIOMES } from "@dream/shared";
import { useGameStore } from "../state/store";

export default function Lighting() {
  const quality = useGameStore((s) => s.quality);
  const arena = useGameStore((s) => s.arena);
  const palette = BIOMES[arena.biome].palette;
  const shadows = quality !== "low";

  return (
    <>
      <hemisphereLight args={["#5578c9", "#0a0e18", 0.65]} />
      <ambientLight intensity={0.18} color="#3a5aff" />
      <directionalLight
        position={[24, 34, 14]}
        intensity={1.85}
        color="#fff3da"
        castShadow={shadows}
        shadow-mapSize={quality === "high" ? [2048, 2048] : [1024, 1024]}
        shadow-camera-near={5}
        shadow-camera-far={90}
        shadow-camera-left={-45}
        shadow-camera-right={45}
        shadow-camera-top={45}
        shadow-camera-bottom={-45}
        shadow-bias={-0.0015}
      />
      <directionalLight position={[-20, 10, -18]} intensity={0.35} color="#6f8dff" />
      <fog attach="fog" args={[palette.fog, 34, 105]} />
    </>
  );
}
