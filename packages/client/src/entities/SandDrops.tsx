import { useMemo, useRef } from "react";
import { useFrame } from "@react-three/fiber";
import * as THREE from "three";
import { SAND_COLOR, SandWallet } from "@dream/shared";
import { useGameStore } from "../state/store";

function SandDrop({ id }: { id: string }) {
  const room = useGameStore((s) => s.room);
  const groupRef = useRef<THREE.Group>(null!);
  const settled = useRef(false);
  const fallY = useRef(1.6 + Math.random() * 0.8);

  const info = useMemo(() => {
    const state = room?.state as { sandDrops: Map<string, { sandType: string; amount: number; x: number; z: number }> } | undefined;
    const s = state?.sandDrops?.get(id);
    return {
      color: SAND_COLOR[(s?.sandType as keyof SandWallet) ?? "pyros"] ?? "#ff8a4d",
      x: s?.x ?? 0,
      z: s?.z ?? 0,
      big: (s?.amount ?? 1) >= 4,
    };
  }, [room, id]);

  useFrame((state, dt) => {
    if (!groupRef.current) return;
    if (!settled.current) {
      groupRef.current.position.set(info.x, fallY.current, info.z);
      fallY.current -= dt * 4.5;
      if (fallY.current <= 0.12) {
        fallY.current = 0.12;
        settled.current = true;
      }
    } else {
      groupRef.current.position.y = 0.12 + Math.sin(state.clock.elapsedTime * 3 + info.x) * 0.02;
    }
  });

  const r = info.big ? 0.13 : 0.08;
  return (
    <group ref={groupRef} position={[info.x, fallY.current, info.z]}>
      <mesh castShadow>
        <dodecahedronGeometry args={[r, 0]} />
        <meshStandardMaterial color={info.color} emissive={info.color} emissiveIntensity={1.1} roughness={0.4} metalness={0.3} />
      </mesh>
      <mesh rotation={[-Math.PI / 2, 0, 0]} position={[0, -0.1, 0]}>
        <circleGeometry args={[r * 1.8, 10]} />
        <meshBasicMaterial color={info.color} transparent opacity={0.28} depthWrite={false} />
      </mesh>
    </group>
  );
}

/** Grains of cybersand spilled by destroyed enemies - drive near them to vacuum them up. */
export default function SandDrops() {
  const sandDropIds = useGameStore((s) => s.sandDropIds);
  return (
    <>
      {sandDropIds.map((id) => (
        <SandDrop key={id} id={id} />
      ))}
    </>
  );
}
