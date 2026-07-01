import { useMemo, useRef } from "react";
import { useFrame } from "@react-three/fiber";
import * as THREE from "three";
import { Stars, Sparkles } from "@react-three/drei";
import { useGameStore } from "../state/store";

function Backdrop() {
  const matRef = useRef<THREE.ShaderMaterial>(null);
  useFrame((state) => {
    if (matRef.current) matRef.current.uniforms.uTime.value = state.clock.elapsedTime;
  });
  const uniforms = useMemo(() => ({ uTime: { value: 0 } }), []);

  return (
    <mesh scale={-1} rotation={[0, 0, 0]}>
      <sphereGeometry args={[180, 24, 16]} />
      <shaderMaterial
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
          void main() {
            float h = clamp(vDir.y * 0.5 + 0.5, 0.0, 1.0);
            vec3 top = vec3(0.015, 0.02, 0.05);
            vec3 horizon = vec3(0.09, 0.07, 0.16);
            vec3 low = vec3(0.02, 0.03, 0.06);
            vec3 col = mix(low, horizon, smoothstep(0.0, 0.35, h));
            col = mix(col, top, smoothstep(0.35, 1.0, h));
            float band = smoothstep(0.28, 0.34, h) - smoothstep(0.34, 0.42, h);
            col += band * vec3(0.35, 0.25, 0.55) * 0.5;
            gl_FragColor = vec4(col, 1.0);
          }
        `}
      />
    </mesh>
  );
}

export default function Sky() {
  const quality = useGameStore((s) => s.quality);
  if (quality === "low") return <color attach="background" args={["#05070d"]} />;

  return (
    <>
      <Backdrop />
      <Stars radius={140} depth={60} count={quality === "high" ? 3200 : 1600} factor={3.2} saturation={0} fade speed={0.25} />
      <Sparkles count={quality === "high" ? 90 : 45} scale={[70, 14, 70]} size={2.2} speed={0.15} opacity={0.35} color="#8fc3ff" position={[0, 6, 0]} />
    </>
  );
}
