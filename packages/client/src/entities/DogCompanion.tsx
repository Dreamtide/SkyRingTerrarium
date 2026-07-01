import { useRef } from "react";
import { useFrame } from "@react-three/fiber";
import * as THREE from "three";
import { Billboard } from "@react-three/drei";
import { useGameStore } from "../state/store";
import { damp, lerpAngle } from "../util/math";
import DogModel, { DogAnim } from "./models/DogModel";

// Plain colored shapes (not glyphs) so the task indicator never depends on font/glyph loading.
const TASK_COLOR: Record<string, string> = {
  idle: "#90a0bd",
  guard: "#ff9d4d",
  hunt: "#ff5d5d",
  scavenge: "#57d97b",
  scout: "#4fa8ff",
  mend: "#b563ff",
};

export default function DogCompanion({ ownerSessionId }: { ownerSessionId: string }) {
  const room = useGameStore((s) => s.room);
  const dog = useGameStore((s) => s.dogs[ownerSessionId]);
  const owner = useGameStore((s) => s.players[ownerSessionId]);
  const groupRef = useRef<THREE.Group>(null!);
  const lastPos = useRef(new THREE.Vector3());
  const anim = useRef<DogAnim>({ speed: 0, walk: 0, spirit: 0.4 }).current;

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
    anim.speed = damp(anim.speed, inst, 6, dt);
    lastPos.current.set(groupRef.current.position.x, 0, groupRef.current.position.z);
    anim.walk += dt;
    // spirit rises while actively working a task, easing tail/ear energy
    const working = dog && dog.task !== "idle";
    anim.spirit = damp(anim.spirit, working ? 0.85 : 0.4, 2, dt);
  });

  if (!dog || !owner) return null;

  return (
    <group ref={groupRef}>
      <DogModel formId={dog.formId} stage={dog.stage} anim={anim} />
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
