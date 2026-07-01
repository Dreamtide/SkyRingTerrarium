import { useEffect, useMemo, useRef } from "react";
import { useFrame } from "@react-three/fiber";
import * as THREE from "three";
import { PARTS_BY_ID, PLAYER_BASE } from "@dream/shared";
import { useGameStore } from "../state/store";
import { fxBus } from "../net/fx";
import { damp, hueToHex, lerpAngle } from "../util/math";
import NamePlate from "./NamePlate";

interface AnimState {
  speed: number;
  walk: number;
  transformT: number;
  overdrive: number;
  dashT: number;
}

function partHue(id: string, fallback: number): number {
  return id ? PARTS_BY_ID[id]?.colorHue ?? fallback : fallback;
}

function RobotForm({ color, anim, weaponHue, platingHue }: { color: string; anim: AnimState; weaponHue: number; platingHue: number }) {
  const leftLeg = useRef<THREE.Group>(null!);
  const rightLeg = useRef<THREE.Group>(null!);
  const leftArm = useRef<THREE.Group>(null!);
  const rightArm = useRef<THREE.Group>(null!);
  const torso = useRef<THREE.Group>(null!);
  const weaponGlow = useMemo(() => hueToHex(weaponHue, 0.75, 0.6), [weaponHue]);
  const platingGlow = useMemo(() => hueToHex(platingHue, 0.7, 0.55), [platingHue]);

  useFrame((_, dt) => {
    const swing = Math.sin(anim.walk * 7.2) * Math.min(1, anim.speed / 6) * 0.55;
    const counter = Math.sin(anim.walk * 7.2 + Math.PI) * Math.min(1, anim.speed / 6) * 0.55;
    if (leftLeg.current) leftLeg.current.rotation.x = damp(leftLeg.current.rotation.x, swing, 18, dt);
    if (rightLeg.current) rightLeg.current.rotation.x = damp(rightLeg.current.rotation.x, counter, 18, dt);
    if (leftArm.current) leftArm.current.rotation.x = damp(leftArm.current.rotation.x, counter * 0.6, 18, dt);
    if (rightArm.current) rightArm.current.rotation.x = damp(rightArm.current.rotation.x, swing * 0.6 - 0.2, 18, dt);
    if (torso.current) {
      const bob = Math.min(1, anim.speed / 6) < 0.05 ? Math.sin(anim.walk * 1.6) * 0.02 : Math.abs(Math.sin(anim.walk * 7.2)) * 0.035;
      torso.current.position.y = damp(torso.current.position.y, 1.05 + bob, 14, dt);
    }
  });

  return (
    <group>
      <group ref={torso} position={[0, 1.05, 0]}>
        <mesh castShadow>
          <boxGeometry args={[0.62, 0.68, 0.4]} />
          <meshStandardMaterial color={color} metalness={0.75} roughness={0.32} />
        </mesh>
        <mesh position={[0, 0.02, 0.21]}>
          <boxGeometry args={[0.26, 0.26, 0.05]} />
          <meshStandardMaterial color="#0a0e18" emissive={platingGlow} emissiveIntensity={1.4} />
        </mesh>
        <mesh position={[0, 0.5, 0]} castShadow>
          <boxGeometry args={[0.34, 0.32, 0.34]} />
          <meshStandardMaterial color="#141a26" metalness={0.6} roughness={0.4} />
        </mesh>
        <mesh position={[0, 0.5, 0.16]}>
          <boxGeometry args={[0.24, 0.08, 0.05]} />
          <meshStandardMaterial color="#0a0e18" emissive="#8fd3ff" emissiveIntensity={2.2} />
        </mesh>
        <mesh position={[-0.44, 0.22, 0]} castShadow>
          <boxGeometry args={[0.18, 0.42, 0.34]} />
          <meshStandardMaterial color="#141a26" emissive={platingGlow} emissiveIntensity={0.35} metalness={0.7} roughness={0.3} />
        </mesh>
        <mesh position={[0.44, 0.22, 0]} castShadow>
          <boxGeometry args={[0.18, 0.42, 0.34]} />
          <meshStandardMaterial color="#141a26" emissive={platingGlow} emissiveIntensity={0.35} metalness={0.7} roughness={0.3} />
        </mesh>

        <group ref={leftArm} position={[-0.44, 0.05, 0]}>
          <mesh position={[0, -0.32, 0]} castShadow>
            <boxGeometry args={[0.17, 0.55, 0.17]} />
            <meshStandardMaterial color="#1c2436" metalness={0.7} roughness={0.35} />
          </mesh>
        </group>
        <group ref={rightArm} position={[0.44, 0.05, 0]}>
          <mesh position={[0, -0.32, 0]} castShadow>
            <boxGeometry args={[0.17, 0.55, 0.17]} />
            <meshStandardMaterial color="#1c2436" metalness={0.7} roughness={0.35} />
          </mesh>
          <mesh position={[0.05, -0.62, 0.14]} rotation={[Math.PI / 2, 0, 0]} castShadow>
            <cylinderGeometry args={[0.05, 0.07, 0.4, 8]} />
            <meshStandardMaterial color="#0a0e18" emissive={weaponGlow} emissiveIntensity={1.6} metalness={0.5} roughness={0.4} />
          </mesh>
        </group>
      </group>

      <group ref={leftLeg} position={[-0.18, 0.78, 0]}>
        <mesh position={[0, -0.36, 0]} castShadow>
          <boxGeometry args={[0.2, 0.62, 0.22]} />
          <meshStandardMaterial color="#20293c" metalness={0.7} roughness={0.4} />
        </mesh>
        <mesh position={[0, -0.68, 0.06]} castShadow>
          <boxGeometry args={[0.24, 0.13, 0.34]} />
          <meshStandardMaterial color="#0e1420" metalness={0.6} roughness={0.5} />
        </mesh>
      </group>
      <group ref={rightLeg} position={[0.18, 0.78, 0]}>
        <mesh position={[0, -0.36, 0]} castShadow>
          <boxGeometry args={[0.2, 0.62, 0.22]} />
          <meshStandardMaterial color="#20293c" metalness={0.7} roughness={0.4} />
        </mesh>
        <mesh position={[0, -0.68, 0.06]} castShadow>
          <boxGeometry args={[0.24, 0.13, 0.34]} />
          <meshStandardMaterial color="#0e1420" metalness={0.6} roughness={0.5} />
        </mesh>
      </group>
    </group>
  );
}

function VehicleForm({ color, anim, engineHue }: { color: string; anim: AnimState; engineHue: number }) {
  const body = useRef<THREE.Group>(null!);
  const thrusterGlow = useMemo(() => hueToHex(engineHue, 0.8, 0.6), [engineHue]);
  const wheelSpin = useRef(0);

  useFrame((_, dt) => {
    if (body.current) {
      const hover = Math.sin(anim.walk * 3.4) * 0.035;
      body.current.position.y = damp(body.current.position.y, 0.42 + hover, 10, dt);
      body.current.rotation.z = damp(body.current.rotation.z, THREE.MathUtils.clamp(-anim.speed * 0.012, -0.18, 0.18), 8, dt);
    }
    wheelSpin.current += dt * anim.speed * 2.2;
  });

  return (
    <group ref={body} position={[0, 0.42, 0]}>
      <mesh castShadow rotation={[0, Math.PI, 0]}>
        <coneGeometry args={[0.55, 1.9, 4]} />
        <meshStandardMaterial color={color} metalness={0.85} roughness={0.22} flatShading />
      </mesh>
      <mesh position={[0, -0.08, 0]} castShadow>
        <boxGeometry args={[0.9, 0.32, 1.3]} />
        <meshStandardMaterial color="#141a26" metalness={0.7} roughness={0.3} />
      </mesh>
      <mesh position={[0, 0.14, 0.35]}>
        <boxGeometry args={[0.5, 0.16, 0.5]} />
        <meshStandardMaterial color="#0a0e18" emissive="#8fd3ff" emissiveIntensity={1.8} />
      </mesh>
      <mesh position={[0, 0.02, -0.85]}>
        <cylinderGeometry args={[0.32, 0.32, 0.22, 10]} />
        <meshStandardMaterial
          color="#0a0e18"
          emissive={thrusterGlow}
          emissiveIntensity={2.2 + Math.min(1.5, anim.speed / 8) + anim.overdrive * 2}
        />
      </mesh>
    </group>
  );
}

export default function RobotPlayer({ sessionId, isLocal }: { sessionId: string; isLocal: boolean }) {
  const room = useGameStore((s) => s.room);
  const player = useGameStore((s) => s.players[sessionId]);
  const groupRef = useRef<THREE.Group>(null!);
  const lastPos = useRef(new THREE.Vector3());
  const anim = useRef<AnimState>({ speed: 0, walk: 0, transformT: 0, overdrive: 0, dashT: 0 }).current;
  const spinBoost = useRef(0);

  useEffect(() => {
    const off = fxBus.on("transformStart", (d) => {
      if (d.sessionId === sessionId) spinBoost.current = 1;
    });
    return () => off();
  }, [sessionId]);

  useFrame((_, dt) => {
    const state = room?.state as unknown as { players: Map<string, { x: number; z: number; yaw: number }> } | undefined;
    const p = state?.players?.get(sessionId);
    if (!p || !groupRef.current) return;
    const lerpRate = isLocal ? 16 : 10;
    groupRef.current.position.x = damp(groupRef.current.position.x, p.x, lerpRate, dt);
    groupRef.current.position.z = damp(groupRef.current.position.z, p.z, lerpRate, dt);

    const dx = groupRef.current.position.x - lastPos.current.x;
    const dz = groupRef.current.position.z - lastPos.current.z;
    const inst = Math.hypot(dx, dz) / Math.max(dt, 0.0001);
    anim.speed = damp(anim.speed, inst, 5, dt);
    lastPos.current.set(groupRef.current.position.x, 0, groupRef.current.position.z);

    if (spinBoost.current > 0) {
      spinBoost.current = Math.max(0, spinBoost.current - dt / PLAYER_BASE.transformLockSeconds);
      groupRef.current.rotation.y += dt * 26 * spinBoost.current;
      const s = 1 - Math.sin(spinBoost.current * Math.PI) * 0.85;
      groupRef.current.scale.setScalar(THREE.MathUtils.clamp(s, 0.18, 1));
    } else {
      groupRef.current.rotation.y = lerpAngle(groupRef.current.rotation.y, p.yaw, 12, dt);
      groupRef.current.scale.setScalar(damp(groupRef.current.scale.x, 1, 12, dt));
    }
    anim.walk += dt;
  });

  if (!player) return null;

  const weaponHue = partHue(player.loadout.weapon, 0.02);
  const engineHue = partHue(player.loadout.engine, 0.14);
  const platingHue = partHue(player.loadout.plating, 0.58);

  return (
    <group ref={groupRef}>
      <group rotation={[player.downed ? Math.PI / 2 : 0, 0, 0]} position={[0, player.downed ? 0.3 : 0, 0]}>
        {player.mode === "vehicle" ? (
          <VehicleForm color={player.color} anim={anim} engineHue={engineHue} />
        ) : (
          <RobotForm color={player.color} anim={anim} weaponHue={weaponHue} platingHue={platingHue} />
        )}
      </group>
      <NamePlate
        name={player.name}
        color={player.color}
        health={player.health}
        maxHealth={player.maxHealth}
        shield={player.shield}
        maxShield={player.maxShield}
        isLocal={isLocal}
        reviveProgress={player.reviveProgress}
      />
      {isLocal && (
        <mesh rotation={[-Math.PI / 2, 0, 0]} position={[0, 0.04, 0]}>
          <ringGeometry args={[0.65, 0.78, 24]} />
          <meshBasicMaterial color={player.color} transparent opacity={0.5} />
        </mesh>
      )}
    </group>
  );
}
