import { Canvas } from "@react-three/fiber";
import { EffectComposer, Bloom, Vignette, SMAA, BrightnessContrast, HueSaturation } from "@react-three/postprocessing";
import { useGameStore } from "../state/store";
import Lighting from "./Lighting";
import Arena from "./Arena";
import Sky from "./Sky";
import CameraRig from "./CameraRig";
import DesktopAimController from "./DesktopAimController";
import { Players, Dogs, Enemies, Pickups } from "./EntityGroups";
import FxLayer from "../entities/FxLayer";
import DamageNumbers from "../entities/DamageNumbers";
import { inputManager } from "../input/InputManager";

export default function GameCanvas() {
  const quality = useGameStore((s) => s.quality);
  const dpr: [number, number] = quality === "high" ? [1, 2] : quality === "medium" ? [1, 1.5] : [1, 1];
  const desktop = !inputManager.isMobileLike();

  return (
    <Canvas
      shadows={quality !== "low"}
      dpr={dpr}
      gl={{ antialias: false, powerPreference: "high-performance" }}
      camera={{ fov: 52, near: 0.1, far: 220, position: [0, 6, 10] }}
      style={{ position: "absolute", inset: 0, cursor: desktop ? "none" : "auto" }}
    >
      <Sky />
      <Lighting />
      <Arena />
      <Players />
      <Dogs />
      <Enemies />
      <Pickups />
      <FxLayer />
      <DamageNumbers />
      <CameraRig />
      {desktop && <DesktopAimController />}
      {quality !== "low" && (
        <EffectComposer multisampling={0}>
          <Bloom intensity={0.6} luminanceThreshold={0.32} luminanceSmoothing={0.25} mipmapBlur />
          <HueSaturation saturation={0.08} />
          <BrightnessContrast brightness={0.0} contrast={0.08} />
          <Vignette eskil={false} offset={0.16} darkness={0.8} />
          <SMAA />
        </EffectComposer>
      )}
    </Canvas>
  );
}
