import { Suspense } from "react";
import { Billboard, Text } from "@react-three/drei";
import { PLAYER_BASE } from "@dream/shared";

export default function NamePlate({
  name,
  color,
  health,
  maxHealth,
  shield,
  maxShield,
  isLocal,
  reviveProgress,
}: {
  name: string;
  color: string;
  health: number;
  maxHealth: number;
  shield: number;
  maxShield: number;
  isLocal: boolean;
  reviveProgress: number;
}) {
  const hp = Math.max(0, health / Math.max(1, maxHealth));
  const sp = Math.max(0, shield / Math.max(1, maxShield));
  return (
    <Billboard position={[0, 2.55, 0]}>
      {!isLocal && (
        <Suspense fallback={null}>
          <Text fontSize={0.24} color={color} outlineWidth={0.015} outlineColor="#000" anchorY="bottom" position={[0, 0.28, 0]}>
            {name}
          </Text>
        </Suspense>
      )}
      <group position={[0, 0.12, 0]}>
        <mesh position={[0, 0.09, 0]}>
          <planeGeometry args={[1.1, 0.07]} />
          <meshBasicMaterial color="#0a0e18" transparent opacity={0.7} />
        </mesh>
        <mesh position={[-0.55 + (sp * 1.1) / 2, 0.09, 0.001]}>
          <planeGeometry args={[Math.max(0.001, sp * 1.1), 0.06]} />
          <meshBasicMaterial color="#4fa8ff" />
        </mesh>
        <mesh position={[0, 0, 0]}>
          <planeGeometry args={[1.1, 0.09]} />
          <meshBasicMaterial color="#0a0e18" transparent opacity={0.8} />
        </mesh>
        <mesh position={[-0.55 + (hp * 1.1) / 2, 0, 0.001]}>
          <planeGeometry args={[Math.max(0.001, hp * 1.1), 0.08]} />
          <meshBasicMaterial color={hp > 0.5 ? "#57d97b" : hp > 0.25 ? "#ffb62e" : "#ff5d5d"} />
        </mesh>
      </group>
      {reviveProgress > 0 && (
        <group position={[0, -1.9, 0]}>
          <mesh>
            <ringGeometry args={[0.45, 0.55, 24, 1, 0, (reviveProgress / PLAYER_BASE.reviveSeconds) * Math.PI * 2]} />
            <meshBasicMaterial color="#57d97b" side={2} />
          </mesh>
        </group>
      )}
    </Billboard>
  );
}
