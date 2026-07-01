import { useMemo, useRef } from "react";
import { Canvas, useFrame, useThree } from "@react-three/fiber";
import * as THREE from "three";
import { Sparkles } from "@react-three/drei";
import { mulberry32 } from "@dream/shared";
import { useGameStore } from "../../state/store";
import { MechRobotModel } from "../../entities/models/MechModel";
import DogModel from "../../entities/models/DogModel";
import { dampNumber as damp } from "./hubUtil";

/** Warm tropical sky dome: sun-washed gradient with a soft horizon haze band. */
function TropicalSky() {
  const uniforms = useMemo(() => ({ uTime: { value: 0 } }), []);
  const ref = useRef<THREE.ShaderMaterial>(null);
  useFrame((s) => {
    if (ref.current) ref.current.uniforms.uTime.value = s.clock.elapsedTime;
  });
  return (
    <mesh>
      <sphereGeometry args={[190, 24, 16]} />
      <shaderMaterial
        ref={ref}
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
            vec3 zenith = vec3(0.25, 0.5, 0.82);
            vec3 horizon = vec3(1.0, 0.78, 0.52);
            vec3 low = vec3(0.55, 0.62, 0.72);
            vec3 col = mix(low, horizon, smoothstep(0.0, 0.42, h));
            col = mix(col, zenith, smoothstep(0.44, 0.9, h));
            // sun disc, low in the west
            vec3 sunDir = normalize(vec3(-0.55, 0.22, -0.8));
            float sun = smoothstep(0.9985, 0.9995, dot(vDir, sunDir));
            float glow = pow(max(dot(vDir, sunDir), 0.0), 24.0);
            col += vec3(1.0, 0.85, 0.6) * (sun * 1.6 + glow * 0.55);
            gl_FragColor = vec4(col, 1.0);
          }
        `}
      />
    </mesh>
  );
}

/** Animated ocean: layered gerstner-ish ripples, depth gradient, shore foam, sun glint. */
function Ocean() {
  const matRef = useRef<THREE.ShaderMaterial>(null);
  const uniforms = useMemo(() => ({ uTime: { value: 0 } }), []);
  useFrame((s) => {
    if (matRef.current) matRef.current.uniforms.uTime.value = s.clock.elapsedTime;
  });
  return (
    <mesh rotation={[-Math.PI / 2, 0, 0]} position={[0, -0.08, 0]}>
      <planeGeometry args={[400, 400, 120, 120]} />
      <shaderMaterial
        ref={matRef}
        uniforms={uniforms}
        transparent
        vertexShader={`
          uniform float uTime;
          varying vec2 vUv;
          varying float vWave;
          void main() {
            vUv = uv;
            vec3 pos = position;
            float d = length(pos.xy);
            float w1 = sin(pos.x * 0.14 + uTime * 0.9) * cos(pos.y * 0.11 + uTime * 0.7);
            float w2 = sin((pos.x + pos.y) * 0.23 - uTime * 1.3) * 0.5;
            float w3 = sin(pos.y * 0.4 + uTime * 1.9) * 0.22;
            vWave = w1 + w2 + w3;
            pos.z += vWave * 0.35;
            gl_Position = projectionMatrix * modelViewMatrix * vec4(pos, 1.0);
          }
        `}
        fragmentShader={`
          uniform float uTime;
          varying vec2 vUv;
          varying float vWave;
          void main() {
            vec2 c = vUv - 0.5;
            float dist = length(c) * 400.0;
            vec3 deep = vec3(0.02, 0.22, 0.38);
            vec3 shallow = vec3(0.1, 0.62, 0.66);
            // island sits at center: shallower (brighter) water near shore
            vec3 col = mix(shallow, deep, smoothstep(14.0, 90.0, dist));
            col += vWave * 0.045;
            // shore foam ring
            float foam = smoothstep(15.5, 14.5, dist) * (0.55 + 0.45 * sin(uTime * 1.4 + dist * 2.2));
            col = mix(col, vec3(0.95, 0.98, 1.0), clamp(foam, 0.0, 0.85));
            // sun glint streak toward the west sun
            float glint = pow(max(0.0, sin(vUv.x * 90.0 + vWave * 5.0 + uTime * 0.6)), 18.0);
            float lane = smoothstep(0.32, 0.0, abs(c.y + c.x * 0.7));
            col += vec3(1.0, 0.8, 0.55) * glint * lane * 0.35;
            gl_FragColor = vec4(col, 0.94);
          }
        `}
      />
    </mesh>
  );
}

/** The beach island: sand dome with a wet-sand rim. */
function Island() {
  return (
    <group>
      <mesh rotation={[-Math.PI / 2, 0, 0]} position={[0, 0, 0]} receiveShadow>
        <circleGeometry args={[15, 48]} />
        <meshStandardMaterial color="#e8d5a8" roughness={0.9} metalness={0} />
      </mesh>
      {/* wet sand ring */}
      <mesh rotation={[-Math.PI / 2, 0, 0]} position={[0, 0.005, 0]}>
        <ringGeometry args={[12.6, 15, 48]} />
        <meshStandardMaterial color="#c9ae7e" roughness={0.7} metalness={0.05} transparent opacity={0.85} />
      </mesh>
      {/* gentle dune mounds */}
      {[
        [-6, -4, 2.6],
        [5, -6, 2.0],
        [7, 3, 1.6],
      ].map(([x, z, r], i) => (
        <mesh key={i} position={[x, -r * 0.72, z]} receiveShadow castShadow>
          <sphereGeometry args={[r, 16, 12]} />
          <meshStandardMaterial color="#eedcb0" roughness={0.95} />
        </mesh>
      ))}
    </group>
  );
}

function PalmTree({ x, z, lean, height, seed }: { x: number; z: number; lean: number; height: number; seed: number }) {
  const frondRef = useRef<THREE.Group>(null!);
  useFrame((s) => {
    if (frondRef.current) frondRef.current.rotation.z = Math.sin(s.clock.elapsedTime * 0.8 + seed) * 0.05;
  });
  const segments = 5;
  return (
    <group position={[x, 0, z]} rotation={[0, seed, lean]}>
      {Array.from({ length: segments }, (_, i) => (
        <mesh key={i} castShadow position={[Math.sin(i * 0.22) * 0.35, (i + 0.5) * (height / segments), 0]} rotation={[0, 0, i * 0.1]}>
          <cylinderGeometry args={[0.1 - i * 0.012, 0.12 - i * 0.012, height / segments + 0.08, 7]} />
          <meshStandardMaterial color="#8a6a45" roughness={0.9} />
        </mesh>
      ))}
      <group ref={frondRef} position={[Math.sin(segments * 0.2) * 0.4, height + 0.15, 0]}>
        {Array.from({ length: 7 }, (_, i) => {
          const ang = (i / 7) * Math.PI * 2;
          return (
            <mesh key={i} castShadow position={[Math.cos(ang) * 0.55, 0.08, Math.sin(ang) * 0.55]} rotation={[Math.sin(ang) * 0.85, -ang, Math.cos(ang) * 0.85]}>
              <coneGeometry args={[0.16, 1.7, 4]} />
              <meshStandardMaterial color="#3f9152" roughness={0.8} side={THREE.DoubleSide} />
            </mesh>
          );
        })}
        <mesh position={[0, -0.08, 0]}>
          <sphereGeometry args={[0.14, 8, 8]} />
          <meshStandardMaterial color="#6d5435" roughness={0.9} />
        </mesh>
      </group>
    </group>
  );
}

/** Garage bay + kennel hut - simple readable structures framing the beach. */
function HubStructures() {
  return (
    <group>
      {/* garage bay */}
      <group position={[-7.5, 0, 2]} rotation={[0, 0.8, 0]}>
        <mesh castShadow receiveShadow position={[0, 1.3, 0]}>
          <boxGeometry args={[4.4, 2.6, 3.4]} />
          <meshStandardMaterial color="#3c4a63" metalness={0.5} roughness={0.5} />
        </mesh>
        <mesh position={[0, 1.1, 1.71]}>
          <boxGeometry args={[3.0, 1.9, 0.05]} />
          <meshStandardMaterial color="#0a0e18" emissive="#4fa8ff" emissiveIntensity={0.5} />
        </mesh>
        <mesh position={[0, 2.75, 0]} castShadow>
          <boxGeometry args={[4.8, 0.18, 3.8]} />
          <meshStandardMaterial color="#28344a" metalness={0.6} roughness={0.4} />
        </mesh>
        <pointLight position={[0, 1.6, 2.4]} color="#7fc4ff" intensity={2} distance={7} />
      </group>
      {/* kennel hut */}
      <group position={[6.8, 0, 4]} rotation={[0, -0.7, 0]}>
        <mesh castShadow receiveShadow position={[0, 0.85, 0]}>
          <boxGeometry args={[2.4, 1.7, 2.2]} />
          <meshStandardMaterial color="#9c7b52" roughness={0.85} />
        </mesh>
        <mesh castShadow position={[0, 1.95, 0]} rotation={[0, Math.PI / 4, 0]}>
          <coneGeometry args={[2.1, 1.1, 4]} />
          <meshStandardMaterial color="#c98d4e" roughness={0.8} />
        </mesh>
        <mesh position={[0, 0.6, 1.11]}>
          <circleGeometry args={[0.5, 20]} />
          <meshStandardMaterial color="#241a10" />
        </mesh>
        <pointLight position={[0, 1.2, 1.6]} color="#ffd9a0" intensity={1.4} distance={5} />
      </group>
    </group>
  );
}

/** Your selected mech + dog, standing on the beach. */
function HubCharacters() {
  const profile = useGameStore((s) => s.profile);
  const mechAnim = useRef({ speed: 0, walk: 0, overdrive: 0 }).current;
  const dogAnim = useRef({ speed: 0, walk: 0, spirit: 1 }).current;
  const dogGroup = useRef<THREE.Group>(null!);

  useFrame((s, dt) => {
    mechAnim.walk += dt;
    dogAnim.walk += dt;
    // the dog trots a lazy circle around the mech
    if (dogGroup.current) {
      const t = s.clock.elapsedTime * 0.35;
      const r = 2.6 + Math.sin(t * 0.7) * 0.4;
      const x = Math.cos(t) * r;
      const z = 1.5 + Math.sin(t) * r * 0.6;
      const dx = x - dogGroup.current.position.x;
      const dz = z - dogGroup.current.position.z;
      dogAnim.speed = damp(dogAnim.speed, Math.hypot(dx, dz) / Math.max(dt, 0.001), 4, dt);
      dogGroup.current.position.x = x;
      dogGroup.current.position.z = z;
      if (Math.hypot(dx, dz) > 0.001) dogGroup.current.rotation.y = Math.atan2(dx, dz);
    }
  });

  const mechId = profile?.selectedMechId ?? "vanguard";
  const dog = profile?.dogs.find((d) => d.id === profile.selectedDogId) ?? profile?.dogs[0];

  return (
    <group position={[0, 0, 1]}>
      <group rotation={[0, Math.PI * 0.9, 0]}>
        <MechRobotModel mechId={mechId} color="#4fa8ff" anim={mechAnim} />
      </group>
      <group ref={dogGroup}>
        <DogModel formId={dog?.formId ?? "pup"} stage={dog?.stage ?? "pup"} anim={dogAnim} />
      </group>
    </group>
  );
}

function HubCamera() {
  const { camera } = useThree();
  useFrame((s) => {
    const t = s.clock.elapsedTime * 0.06;
    camera.position.set(Math.sin(t) * 10.5, 4.2 + Math.sin(t * 0.6) * 0.4, 9 + Math.cos(t) * 2.4);
    camera.lookAt(0, 1.2, 0.5);
  });
  return null;
}

/** Scattered beach props seeded once. */
function BeachProps() {
  const rng = useMemo(() => mulberry32(7), []);
  const rocks = useMemo(
    () =>
      Array.from({ length: 7 }, () => ({
        x: (rng() - 0.5) * 22,
        z: (rng() - 0.5) * 22,
        r: 0.25 + rng() * 0.55,
        rot: rng() * Math.PI,
      })).filter((r) => Math.hypot(r.x, r.z) > 4 && Math.hypot(r.x, r.z) < 13),
    [rng]
  );
  return (
    <group>
      {rocks.map((r, i) => (
        <mesh key={i} castShadow position={[r.x, r.r * 0.4, r.z]} rotation={[r.rot, r.rot * 2, 0]}>
          <dodecahedronGeometry args={[r.r, 0]} />
          <meshStandardMaterial color="#8f8578" roughness={0.9} flatShading />
        </mesh>
      ))}
    </group>
  );
}

export default function HubScene() {
  const quality = useGameStore((s) => s.quality);
  const dpr: [number, number] = quality === "high" ? [1, 2] : quality === "medium" ? [1, 1.5] : [1, 1];

  return (
    <Canvas
      shadows={quality !== "low"}
      dpr={dpr}
      gl={{ antialias: true, powerPreference: "high-performance" }}
      camera={{ fov: 48, near: 0.1, far: 400, position: [0, 4.2, 11] }}
      style={{ position: "absolute", inset: 0 }}
    >
      <TropicalSky />
      <hemisphereLight args={["#bfe3ff", "#e8d5a8", 0.75]} />
      <directionalLight
        position={[-30, 18, -40]}
        intensity={2.2}
        color="#ffdfb0"
        castShadow={quality !== "low"}
        shadow-mapSize={[1024, 1024]}
        shadow-camera-near={5}
        shadow-camera-far={120}
        shadow-camera-left={-25}
        shadow-camera-right={25}
        shadow-camera-top={25}
        shadow-camera-bottom={-25}
        shadow-bias={-0.0015}
      />
      <Ocean />
      <Island />
      <PalmTree x={-4.5} z={-5} lean={0.14} height={4.2} seed={1.3} />
      <PalmTree x={4} z={-6.5} lean={-0.1} height={3.6} seed={3.7} />
      <PalmTree x={-8.5} z={-2} lean={0.2} height={3.2} seed={5.1} />
      <PalmTree x={9.2} z={-0.5} lean={-0.16} height={4.4} seed={2.2} />
      <HubStructures />
      <BeachProps />
      <HubCharacters />
      {quality !== "low" && <Sparkles count={40} scale={[30, 8, 30]} size={1.6} speed={0.12} opacity={0.4} color="#fff6dd" position={[0, 4, 0]} />}
      <HubCamera />
      {/* No EffectComposer here: the bright daylight scene gains little from bloom, and
          we hit a renderer edge case where the composer blanked this (second) canvas on
          software GL. Native MSAA (antialias: true above) covers the AA instead. */}
    </Canvas>
  );
}
