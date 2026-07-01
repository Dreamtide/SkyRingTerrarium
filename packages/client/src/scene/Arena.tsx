import { useMemo, useRef } from "react";
import { useFrame } from "@react-three/fiber";
import * as THREE from "three";
import { ARENA_RADIUS, generateArenaLayout } from "@dream/shared";
import { useGameStore } from "../state/store";

function Obstacle({ x, z, radius, height, kind }: { x: number; z: number; radius: number; height: number; kind: string }) {
  const color = kind === "pillar" ? "#2b3a5c" : kind === "crate" ? "#4a5a3a" : "#3a2b5c";
  const emissive = kind === "crate" ? "#8fae3e" : kind === "ring" ? "#8a5cff" : "#3d5ba8";
  return (
    <group position={[x, 0, z]}>
      {kind === "ring" ? (
        <mesh castShadow receiveShadow position={[0, height / 2, 0]}>
          <torusGeometry args={[radius, 0.35, 8, 20]} />
          <meshStandardMaterial color={color} emissive={emissive} emissiveIntensity={0.4} metalness={0.6} roughness={0.35} />
        </mesh>
      ) : (
        <mesh castShadow receiveShadow position={[0, height / 2, 0]}>
          {kind === "pillar" ? (
            <cylinderGeometry args={[radius * 0.55, radius * 0.7, height, 8]} />
          ) : (
            <boxGeometry args={[radius * 1.3, height, radius * 1.3]} />
          )}
          <meshStandardMaterial color={color} emissive={emissive} emissiveIntensity={0.18} metalness={0.5} roughness={0.6} />
        </mesh>
      )}
    </group>
  );
}

function Ground() {
  const gridRef = useRef<THREE.ShaderMaterial>(null);
  useFrame((state) => {
    if (gridRef.current) gridRef.current.uniforms.uTime.value = state.clock.elapsedTime;
  });

  const uniforms = useMemo(
    () => ({
      uTime: { value: 0 },
      uRadius: { value: ARENA_RADIUS },
    }),
    []
  );

  return (
    <mesh rotation={[-Math.PI / 2, 0, 0]} receiveShadow position={[0, 0, 0]}>
      <circleGeometry args={[ARENA_RADIUS + 6, 64]} />
      <shaderMaterial
        ref={gridRef}
        uniforms={uniforms}
        vertexShader={`
          varying vec2 vPos;
          void main() {
            vPos = position.xy;
            gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0);
          }
        `}
        fragmentShader={`
          varying vec2 vPos;
          uniform float uTime;
          uniform float uRadius;
          void main() {
            float d = length(vPos);
            vec3 base = mix(vec3(0.03,0.05,0.09), vec3(0.05,0.08,0.14), smoothstep(0.0, uRadius, d));
            float grid = 0.0;
            vec2 g = abs(fract(vPos * 0.1) - 0.5);
            float line = min(g.x, g.y);
            grid = smoothstep(0.045, 0.0, line) * 0.18;
            float edge = smoothstep(uRadius, uRadius - 3.0, d);
            float ring = smoothstep(uRadius+0.4, uRadius, d) - smoothstep(uRadius, uRadius-0.4, d);
            vec3 col = base + grid * vec3(0.3,0.55,1.0) * edge;
            col += vec3(0.4,0.7,1.0) * clamp(ring,0.0,1.0) * (0.6 + 0.4*sin(uTime*2.0));
            float vign = smoothstep(uRadius+6.0, uRadius*0.3, d);
            col *= mix(0.35, 1.0, vign);
            gl_FragColor = vec4(col, 1.0);
          }
        `}
      />
    </mesh>
  );
}

export default function Arena() {
  const seed = useGameStore((s) => s.hud.seed || 1);
  const obstacles = useMemo(() => generateArenaLayout(seed), [seed]);

  return (
    <group>
      <Ground />
      {obstacles.map((o, i) => (
        <Obstacle key={i} {...o} />
      ))}
    </group>
  );
}
