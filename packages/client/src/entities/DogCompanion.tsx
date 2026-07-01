import { useMemo, useRef } from "react";
import { useFrame } from "@react-three/fiber";
import * as THREE from "three";
import { Billboard } from "@react-three/drei";
import { PUP_FORM, STAGE2_FORMS } from "@dream/shared";
import { useGameStore } from "../state/store";
import { damp, hueToHex, lerpAngle } from "../util/math";

// Plain colored shapes (not glyphs) so the task indicator never depends on font/glyph loading.
const TASK_COLOR: Record<string, string> = {
  idle: "#90a0bd",
  guard: "#ff9d4d",
  hunt: "#ff5d5d",
  scavenge: "#57d97b",
  scout: "#4fa8ff",
  mend: "#b563ff",
};

function formVisual(formId: string, stage: string) {
  if (formId === "pup") return PUP_FORM.visual;
  const s2 = Object.values(STAGE2_FORMS).find((f) => f.id === formId);
  if (s2) return s2.visual;
  // stage3 hybrids/prime aren't individually exported by id from shared in a flat map;
  // fall back to a generic elevated visual driven by stage so unknown hybrids still look evolved.
  return { scale: 0.95, bodyHue: 0.62, glowHue: 0.66, hasWings: stage === "stage3", hasArmor: stage !== "pup", hasHorns: stage === "stage3", maneStyle: "aura" as const };
}

export default function DogCompanion({ ownerSessionId }: { ownerSessionId: string }) {
  const room = useGameStore((s) => s.room);
  const dog = useGameStore((s) => s.dogs[ownerSessionId]);
  const owner = useGameStore((s) => s.players[ownerSessionId]);
  const groupRef = useRef<THREE.Group>(null!);
  const legPhase = useRef(0);
  const lastPos = useRef(new THREE.Vector3());
  const speedRef = useRef(0);
  const tailRef = useRef<THREE.Mesh>(null!);

  useFrame((_, dt) => {
    const state = room?.state as unknown as { dogs: Map<string, { x: number; z: number; yaw: number }> } | undefined;
    const d = state?.dogs?.get(ownerSessionId);
    if (!d || !groupRef.current) return;
    groupRef.current.position.x = damp(groupRef.current.position.x, d.x, 10, dt);
    groupRef.current.position.z = damp(groupRef.current.position.z, d.z, 10, dt);
    groupRef.current.rotation.y = lerpAngle(groupRef.current.rotation.y, d.yaw, 10, dt);

    const dx = groupRef.current.position.x - lastPos.current.x;
    const dz = groupRef.current.position.z - lastPos.current.z;
    const inst = Math.hypot(dx, dz) / Math.max(dt, 0.0001);
    speedRef.current = damp(speedRef.current, inst, 6, dt);
    lastPos.current.set(groupRef.current.position.x, 0, groupRef.current.position.z);
    legPhase.current += dt * (2 + speedRef.current * 1.6);
    if (tailRef.current) tailRef.current.rotation.y = Math.sin(legPhase.current * (speedRef.current > 1 ? 6 : 2)) * 0.5;
    const bob = Math.abs(Math.sin(legPhase.current * (speedRef.current > 1 ? 3.6 : 1.2))) * (speedRef.current > 1 ? 0.05 : 0.01);
    groupRef.current.position.y = bob;
  });

  const visual = useMemo(() => formVisual(dog?.formId ?? "pup", dog?.stage ?? "pup"), [dog?.formId, dog?.stage]);
  const bodyColor = useMemo(() => hueToHex(visual.bodyHue, 0.55, 0.5), [visual.bodyHue]);
  const glowColor = useMemo(() => hueToHex(visual.glowHue, 0.8, 0.6), [visual.glowHue]);

  if (!dog || !owner) return null;
  const scale = visual.scale;

  return (
    <group ref={groupRef}>
      <group scale={scale}>
        <mesh castShadow position={[0, 0.42, 0]}>
          <capsuleGeometry args={[0.24, 0.5, 4, 8]} />
          <meshStandardMaterial color={bodyColor} metalness={0.3} roughness={0.55} emissive={glowColor} emissiveIntensity={0.18} />
        </mesh>
        <mesh castShadow position={[0, 0.55, 0.42]}>
          <sphereGeometry args={[0.19, 12, 10]} />
          <meshStandardMaterial color={bodyColor} metalness={0.3} roughness={0.5} />
        </mesh>
        <mesh position={[0, 0.58, 0.58]}>
          <sphereGeometry args={[0.05, 8, 8]} />
          <meshStandardMaterial color="#0a0e18" emissive={glowColor} emissiveIntensity={3} />
        </mesh>
        {visual.hasHorns && (
          <>
            <mesh position={[-0.08, 0.72, 0.5]} rotation={[0.3, 0, -0.3]}>
              <coneGeometry args={[0.03, 0.18, 6]} />
              <meshStandardMaterial color={glowColor} emissive={glowColor} emissiveIntensity={1.5} />
            </mesh>
            <mesh position={[0.08, 0.72, 0.5]} rotation={[0.3, 0, 0.3]}>
              <coneGeometry args={[0.03, 0.18, 6]} />
              <meshStandardMaterial color={glowColor} emissive={glowColor} emissiveIntensity={1.5} />
            </mesh>
          </>
        )}
        {visual.hasArmor && (
          <mesh position={[0, 0.44, -0.05]}>
            <boxGeometry args={[0.34, 0.2, 0.4]} />
            <meshStandardMaterial color="#1c2436" metalness={0.7} roughness={0.3} emissive={glowColor} emissiveIntensity={0.3} />
          </mesh>
        )}
        {visual.hasWings && (
          <>
            <mesh position={[-0.22, 0.5, -0.1]} rotation={[0, 0, 0.5]}>
              <planeGeometry args={[0.32, 0.18]} />
              <meshStandardMaterial color={glowColor} emissive={glowColor} emissiveIntensity={1.2} transparent opacity={0.75} side={THREE.DoubleSide} />
            </mesh>
            <mesh position={[0.22, 0.5, -0.1]} rotation={[0, 0, -0.5]}>
              <planeGeometry args={[0.32, 0.18]} />
              <meshStandardMaterial color={glowColor} emissive={glowColor} emissiveIntensity={1.2} transparent opacity={0.75} side={THREE.DoubleSide} />
            </mesh>
          </>
        )}
        <mesh ref={tailRef} position={[0, 0.48, -0.32]}>
          <coneGeometry args={[0.06, 0.34, 6]} />
          <meshStandardMaterial color={bodyColor} emissive={glowColor} emissiveIntensity={0.4} />
        </mesh>
        {[-0.14, 0.14].map((x) =>
          [-0.18, 0.18].map((z) => (
            <mesh key={`${x}-${z}`} position={[x, 0.14, z]} castShadow>
              <boxGeometry args={[0.08, 0.28, 0.08]} />
              <meshStandardMaterial color={bodyColor} metalness={0.3} roughness={0.6} />
            </mesh>
          ))
        )}
        <pointLight color={glowColor} intensity={0.6} distance={2.4} position={[0, 0.5, 0]} />
      </group>
      <Billboard position={[0, 1.35, 0]}>
        <mesh>
          <circleGeometry args={[0.09, 16]} />
          <meshBasicMaterial color={TASK_COLOR[dog.task] ?? TASK_COLOR.idle} />
        </mesh>
        <mesh position={[0, 0, -0.001]}>
          <ringGeometry args={[0.09, 0.13, 16]} />
          <meshBasicMaterial color="#0a0e18" />
        </mesh>
      </Billboard>
    </group>
  );
}
