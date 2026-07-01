import { Canvas } from "@react-three/fiber";
import { EffectComposer, Bloom, Vignette, SMAA } from "@react-three/postprocessing";
import { useGameStore } from "../state/store";
import Lighting from "./Lighting";
import Arena from "./Arena";
import CameraRig from "./CameraRig";
import { Players, Dogs, Enemies, Pickups } from "./EntityGroups";
import FxLayer from "../entities/FxLayer";

export default function GameCanvas() {
  const quality = useGameStore((s) => s.quality);
  const dpr: [number, number] = quality === "high" ? [1, 2] : quality === "medium" ? [1, 1.5] : [1, 1];

  return (
    <Canvas
      shadows={quality !== "low"}
      dpr={dpr}
      gl={{ antialias: false, powerPreference: "high-performance" }}
      camera={{ fov: 52, near: 0.1, far: 220, position: [0, 6, 10] }}
      style={{ position: "absolute", inset: 0 }}
    >
      <color attach="background" args={["#05070d"]} />
      <Lighting />
      <Arena />
      <Players />
      <Dogs />
      <Enemies />
      <Pickups />
      <FxLayer />
      <CameraRig />
      {quality !== "low" && (
        <EffectComposer multisampling={0}>
          <Bloom intensity={0.55} luminanceThreshold={0.35} luminanceSmoothing={0.25} mipmapBlur />
          <Vignette eskil={false} offset={0.18} darkness={0.75} />
          <SMAA />
        </EffectComposer>
      )}
    </Canvas>
  );
}
