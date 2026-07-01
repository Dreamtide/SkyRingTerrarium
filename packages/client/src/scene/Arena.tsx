import { useMemo, useRef } from "react";
import { useFrame } from "@react-three/fiber";
import * as THREE from "three";
import { ARENA_RADIUS, ArenaDecor, ArenaObstacle, BIOMES, BiomePalette } from "@dream/shared";
import { useGameStore } from "../state/store";

function Obstacle({ o, palette }: { o: ArenaObstacle; palette: BiomePalette }) {
  const { x, z, radius, height, kind } = o;
  switch (kind) {
    case "ring":
      return (
        <group position={[x, 0, z]}>
          <mesh castShadow receiveShadow position={[0, height / 2, 0]}>
            <torusGeometry args={[radius, 0.35, 8, 20]} />
            <meshStandardMaterial color={palette.rockColor} emissive={palette.ringColor} emissiveIntensity={0.4} metalness={0.6} roughness={0.35} />
          </mesh>
        </group>
      );
    case "crystal":
      return (
        <group position={[x, 0, z]}>
          <mesh castShadow position={[0, height / 2, 0]} rotation={[0.12, 0.5, -0.08]}>
            <coneGeometry args={[radius * 0.55, height, 5]} />
            <meshStandardMaterial color={palette.rockColor} emissive={palette.rockEmissive} emissiveIntensity={0.8} metalness={0.3} roughness={0.15} flatShading />
          </mesh>
          <mesh castShadow position={[radius * 0.4, height * 0.22, radius * 0.2]} rotation={[-0.2, 0.9, 0.3]}>
            <coneGeometry args={[radius * 0.3, height * 0.5, 5]} />
            <meshStandardMaterial color={palette.rockColor} emissive={palette.rockEmissive} emissiveIntensity={0.6} metalness={0.3} roughness={0.2} flatShading />
          </mesh>
          <pointLight color={palette.rockEmissive} intensity={0.8} distance={5} position={[0, height * 0.6, 0]} />
        </group>
      );
    case "tree":
      return (
        <group position={[x, 0, z]}>
          <mesh castShadow position={[0, height * 0.32, 0]}>
            <cylinderGeometry args={[radius * 0.16, radius * 0.24, height * 0.64, 7]} />
            <meshStandardMaterial color="#3a2f22" roughness={0.9} />
          </mesh>
          {[0.55, 0.75, 0.92].map((h, i) => (
            <mesh key={i} castShadow position={[0, height * h, 0]}>
              <coneGeometry args={[radius * (0.85 - i * 0.22), height * 0.36, 7]} />
              <meshStandardMaterial color={palette.rockColor} emissive={palette.rockEmissive} emissiveIntensity={0.25} roughness={0.7} flatShading />
            </mesh>
          ))}
        </group>
      );
    case "monolith":
      return (
        <group position={[x, 0, z]} rotation={[0, x * 0.7, 0.04]}>
          <mesh castShadow receiveShadow position={[0, height / 2, 0]}>
            <boxGeometry args={[radius * 0.9, height, radius * 0.5]} />
            <meshStandardMaterial color={palette.rockColor} metalness={0.4} roughness={0.6} flatShading />
          </mesh>
          <mesh position={[0, height * 0.62, radius * 0.26]}>
            <boxGeometry args={[radius * 0.5, height * 0.5, 0.04]} />
            <meshStandardMaterial color="#0a0e18" emissive={palette.rockEmissive} emissiveIntensity={1.1} />
          </mesh>
        </group>
      );
    case "crate":
      return (
        <group position={[x, 0, z]}>
          <mesh castShadow receiveShadow position={[0, height / 2, 0]}>
            <boxGeometry args={[radius * 1.3, height, radius * 1.3]} />
            <meshStandardMaterial color={palette.crateColor} emissive={palette.crateEmissive} emissiveIntensity={0.18} metalness={0.5} roughness={0.6} />
          </mesh>
        </group>
      );
    default: // pillar
      return (
        <group position={[x, 0, z]}>
          <mesh castShadow receiveShadow position={[0, height / 2, 0]}>
            <cylinderGeometry args={[radius * 0.55, radius * 0.7, height, 8]} />
            <meshStandardMaterial color={palette.rockColor} emissive={palette.rockEmissive} emissiveIntensity={0.18} metalness={0.5} roughness={0.6} />
          </mesh>
          <mesh position={[0, height * 0.86, 0]}>
            <cylinderGeometry args={[radius * 0.58, radius * 0.58, 0.08, 8]} />
            <meshStandardMaterial color="#0a0e18" emissive={palette.rockEmissive} emissiveIntensity={1.2} />
          </mesh>
        </group>
      );
  }
}

function DecorLayer({ decor, palette }: { decor: ArenaDecor[]; palette: BiomePalette }) {
  return (
    <group>
      {decor.map((d, i) => {
        if (d.kind === "shard")
          return (
            <mesh key={i} position={[d.x, d.scale * 0.3, d.z]} rotation={[0.3, d.rot, 0.15]}>
              <tetrahedronGeometry args={[d.scale * 0.32, 0]} />
              <meshStandardMaterial color={palette.rockColor} emissive={palette.rockEmissive} emissiveIntensity={0.7} flatShading />
            </mesh>
          );
        if (d.kind === "tuft")
          return (
            <mesh key={i} position={[d.x, d.scale * 0.14, d.z]} rotation={[0, d.rot, 0]}>
              <coneGeometry args={[d.scale * 0.12, d.scale * 0.42, 4]} />
              <meshStandardMaterial color={palette.grid} emissive={palette.grid} emissiveIntensity={0.35} transparent opacity={0.8} />
            </mesh>
          );
        return (
          <mesh key={i} position={[d.x, d.scale * 0.1, d.z]} rotation={[d.rot, d.rot * 2, 0]}>
            <dodecahedronGeometry args={[d.scale * 0.18, 0]} />
            <meshStandardMaterial color={palette.rockColor} roughness={0.9} flatShading />
          </mesh>
        );
      })}
    </group>
  );
}

function Ground() {
  const arena = useGameStore((s) => s.arena);
  const palette = BIOMES[arena.biome].palette;
  const gridRef = useRef<THREE.ShaderMaterial>(null);
  useFrame((state) => {
    if (gridRef.current) gridRef.current.uniforms.uTime.value = state.clock.elapsedTime;
  });

  const uniforms = useMemo(
    () => ({
      uTime: { value: 0 },
      uRadius: { value: ARENA_RADIUS },
      uShape: { value: new THREE.Vector4(arena.shape.a1, arena.shape.k1, arena.shape.p1, arena.shape.base) },
      uShape2: { value: new THREE.Vector3(arena.shape.a2, arena.shape.k2, arena.shape.p2) },
      uInner: { value: new THREE.Color(palette.groundInner) },
      uOuter: { value: new THREE.Color(palette.groundOuter) },
      uGrid: { value: new THREE.Color(palette.grid) },
    }),
    [arena, palette]
  );

  return (
    <mesh rotation={[-Math.PI / 2, 0, 0]} receiveShadow position={[0, 0, 0]}>
      <circleGeometry args={[ARENA_RADIUS + 10, 96]} />
      <shaderMaterial
        key={arena.seed}
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
          uniform vec4 uShape;  // a1, k1, p1, base
          uniform vec3 uShape2; // a2, k2, p2
          uniform vec3 uInner;
          uniform vec3 uOuter;
          uniform vec3 uGrid;

          float hash(vec2 p) {
            return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
          }

          // must match boundaryRadius() in @dream/shared/arena.ts exactly
          float boundary(float theta) {
            return uRadius * (uShape.w + uShape.x * sin(uShape.y * theta + uShape.z) + uShape2.x * sin(uShape2.y * theta + uShape2.z));
          }

          void main() {
            float d = length(vPos);
            float theta = atan(vPos.y, vPos.x);
            float rim = boundary(theta);

            float n = (hash(floor(vPos * 0.6)) - 0.5) * 0.03;
            vec3 col = mix(uInner, uOuter, smoothstep(0.0, rim, d)) + n;

            vec2 g1 = abs(fract(vPos * 0.1) - 0.5);
            float grid1 = smoothstep(0.032, 0.0, min(g1.x, g1.y)) * 0.09;
            vec2 g2 = abs(fract(vPos * 0.02) - 0.5);
            float grid2 = smoothstep(0.009, 0.0, min(g2.x, g2.y)) * 0.2;

            float edge = smoothstep(rim, rim - 3.0, d);
            col += (grid1 + grid2) * uGrid * edge;

            float pulse = fract(uTime * 0.1);
            float ringWave = smoothstep(0.025, 0.0, abs(d - pulse * rim * 1.2)) * (1.0 - pulse) * edge;
            col += ringWave * uGrid * 0.5;

            // boundary ring glow
            float ring = smoothstep(rim + 0.5, rim, d) - smoothstep(rim, rim - 0.5, d);
            col += uGrid * clamp(ring, 0.0, 1.0) * (0.7 + 0.3 * sin(uTime * 2.0));

            // void falloff beyond the rim
            float vign = smoothstep(rim + 7.0, rim * 0.3, d);
            col *= mix(0.18, 1.0, vign);
            gl_FragColor = vec4(col, 1.0);
          }
        `}
      />
    </mesh>
  );
}

export default function Arena() {
  const arena = useGameStore((s) => s.arena);
  const palette = BIOMES[arena.biome].palette;

  return (
    <group>
      <Ground />
      {arena.obstacles.map((o, i) => (
        <Obstacle key={`${arena.seed}-${i}`} o={o} palette={palette} />
      ))}
      <DecorLayer decor={arena.decor} palette={palette} />
    </group>
  );
}
