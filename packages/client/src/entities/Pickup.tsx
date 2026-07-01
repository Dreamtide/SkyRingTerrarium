import { useMemo, useRef } from "react";
import { useFrame } from "@react-three/fiber";
import * as THREE from "three";
import { PARTS_BY_ID, RARITY_COLOR } from "@dream/shared";
import { useGameStore } from "../state/store";

export default function Pickup({ id }: { id: string }) {
  const room = useGameStore((s) => s.room);
  const groupRef = useRef<THREE.Group>(null!);
  const coreRef = useRef<THREE.Mesh>(null!);
  const placed = useRef(false);

  const { color, x, z } = useMemo(() => {
    const state = room?.state as { pickups: Map<string, { partId: string; x: number; z: number }> } | undefined;
    const p = state?.pickups?.get(id);
    const def = p ? PARTS_BY_ID[p.partId] : undefined;
    return { color: def ? RARITY_COLOR[def.rarity] : "#9fb4c7", x: p?.x ?? 0, z: p?.z ?? 0 };
  }, [room, id]);

  useFrame((frameState, dt) => {
    if (!groupRef.current) return;
    if (!placed.current) {
      groupRef.current.position.set(x, 0, z);
      placed.current = true;
    }
    groupRef.current.rotation.y += dt * 1.6;
    if (coreRef.current) coreRef.current.position.y = 0.75 + Math.sin(frameState.clock.elapsedTime * 2.4) * 0.12;
  });

  return (
    <group ref={groupRef}>
      <mesh ref={coreRef} position={[0, 0.75, 0]} castShadow>
        <octahedronGeometry args={[0.28, 0]} />
        <meshStandardMaterial color={color} emissive={color} emissiveIntensity={1.4} metalness={0.4} roughness={0.25} />
      </mesh>
      <pointLight color={color} intensity={0.9} distance={3} position={[0, 0.75, 0]} />
      <mesh rotation={[-Math.PI / 2, 0, 0]} position={[0, 0.03, 0]}>
        <ringGeometry args={[0.32, 0.42, 20]} />
        <meshBasicMaterial color={color} transparent opacity={0.55} />
      </mesh>
    </group>
  );
}
