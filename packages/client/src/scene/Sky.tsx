import { useMemo, useRef } from "react";
import { useFrame } from "@react-three/fiber";
import * as THREE from "three";
import { Stars, Sparkles } from "@react-three/drei";
import { BIOMES } from "@dream/shared";
import { useGameStore } from "../state/store";

function Backdrop() {
  const arena = useGameStore((s) => s.arena);
  const palette = BIOMES[arena.biome].palette;
  const matRef = useRef<THREE.ShaderMaterial>(null);
  useFrame((state) => {
    if (matRef.current) matRef.current.uniforms.uTime.value = state.clock.elapsedTime;
  });
  const uniforms = useMemo(
    () => ({
      uTime: { value: 0 },
      uTop: { value: new THREE.Color(palette.skyTop) },
      uHorizon: { value: new THREE.Color(palette.skyHorizon) },
      uBand: { value: new THREE.Color(palette.skyBand) },
    }),
    [palette]
  );

  return (
    <mesh scale={-1} rotation={[0, 0, 0]}>
      <sphereGeometry args={[180, 24, 16]} />
      <shaderMaterial
        key={arena.biome}
        ref={matRef}
        side={THREE.BackSide}
        depthWrite={false}
        uniforms={uniforms}
        vertexShader={`
          varying vec3 vDir;
          void main() {
            vDir = normalize(position);
            gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0);
          }
        `}
        fragmentShader={`
          varying vec3 vDir;
          uniform float uTime;
          uniform vec3 uTop;
          uniform vec3 uHorizon;
          uniform vec3 uBand;
          void main() {
            float h = clamp(vDir.y * 0.5 + 0.5, 0.0, 1.0);
            vec3 low = uTop * 1.3;
            vec3 col = mix(low, uHorizon, smoothstep(0.0, 0.35, h));
            col = mix(col, uTop, smoothstep(0.35, 1.0, h));
            float band = smoothstep(0.28, 0.34, h) - smoothstep(0.34, 0.42, h);
            col += band * uBand * 0.55;
            gl_FragColor = vec4(col, 1.0);
          }
        `}
      />
    </mesh>
  );
}

export default function Sky() {
  const quality = useGameStore((s) => s.quality);
  const arena = useGameStore((s) => s.arena);
  const palette = BIOMES[arena.biome].palette;
  if (quality === "low") return <color attach="background" args={[palette.skyTop]} />;

  return (
    <>
      <Backdrop />
      <Stars radius={140} depth={60} count={quality === "high" ? 3200 : 1600} factor={3.2} saturation={0} fade speed={0.25} />
      <Sparkles count={quality === "high" ? 90 : 45} scale={[70, 14, 70]} size={2.2} speed={0.15} opacity={0.35} color={palette.grid} position={[0, 6, 0]} />
    </>
  );
}
