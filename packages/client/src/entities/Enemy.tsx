import { useEffect, useRef, useState } from "react";
import { useFrame } from "@react-three/fiber";
import * as THREE from "three";
import { Billboard } from "@react-three/drei";
import { useGameStore } from "../state/store";
import { fxBus } from "../net/fx";
import { damp, lerpAngle } from "../util/math";

const TYPE_STYLE: Record<string, { color: string; emissive: string; scale: number; shape: "scrapling" | "strafer" | "brute" | "sniper" | "boss" }> = {
  scrapling: { color: "#4a1f1f", emissive: "#ff4d4d", scale: 0.62, shape: "scrapling" },
  strafer: { color: "#3a1f4a", emissive: "#c14dff", scale: 0.72, shape: "strafer" },
  brute: { color: "#3a2a12", emissive: "#ff9d4d", scale: 1.15, shape: "brute" },
  sniper: { color: "#122a3a", emissive: "#4dd9ff", scale: 0.68, shape: "sniper" },
  boss: { color: "#1a1220", emissive: "#ff2e6d", scale: 2.4, shape: "boss" },
};

const DEATH_DURATION = 0.4;

export default function Enemy({ id }: { id: string }) {
  const room = useGameStore((s) => s.room);
  const groupRef = useRef<THREE.Group>(null!);
  const bodyRef = useRef<THREE.Group>(null!);
  const healthBarRef = useRef<THREE.Mesh>(null!);
  const healthGroupRef = useRef<THREE.Group>(null!);
  const flashRef = useRef<THREE.Mesh>(null!);
  const spawnRef = useRef(0);
  const flash = useRef(0);
  const wasAlive = useRef(true);
  const deathT = useRef(-1);
  const [enemyType] = useState<string>(() => {
    const state = room?.state as { enemies: Map<string, { enemyType: string }> } | undefined;
    return state?.enemies?.get(id)?.enemyType ?? "scrapling";
  });

  useEffect(() => {
    const off = fxBus.on("damage", (d) => {
      if (d.target === "enemy" && d.enemyId === id) flash.current = 1;
    });
    return () => off();
  }, [id]);

  useFrame((_, dt) => {
    const state = room?.state as
      | { enemies: Map<string, { x: number; z: number; yaw: number; alive: boolean; health: number; maxHealth: number; enemyType: string; telegraph: boolean }> }
      | undefined;
    const e = state?.enemies?.get(id);
    if (!e || !groupRef.current) return;

    if (wasAlive.current && !e.alive) deathT.current = 0;
    wasAlive.current = e.alive;

    if (flash.current > 0) flash.current = Math.max(0, flash.current - dt * 5.5);
    if (flashRef.current) {
      const mat = flashRef.current.material as THREE.MeshBasicMaterial;
      mat.opacity = flash.current * 0.75;
    }

    if (deathT.current >= 0) {
      deathT.current += dt;
      const t = Math.min(1, deathT.current / DEATH_DURATION);
      groupRef.current.visible = true;
      if (healthGroupRef.current) healthGroupRef.current.visible = false;
      if (bodyRef.current) {
        const pop = Math.sin(Math.min(1, t * 2) * Math.PI * 0.5) * 1.25;
        const s = Math.max(0.0001, pop * (1 - t));
        bodyRef.current.scale.setScalar(s);
        bodyRef.current.rotation.y += dt * 14;
        bodyRef.current.position.y = t * 0.6;
      }
      if (t >= 1) {
        groupRef.current.visible = false;
        deathT.current = -1;
      }
      return;
    }

    groupRef.current.visible = e.alive;
    if (healthGroupRef.current) healthGroupRef.current.visible = true;
    groupRef.current.position.x = damp(groupRef.current.position.x, e.x, 9, dt);
    groupRef.current.position.z = damp(groupRef.current.position.z, e.z, 9, dt);
    groupRef.current.rotation.y = lerpAngle(groupRef.current.rotation.y, e.yaw, 8, dt);

    if (e.telegraph) {
      spawnRef.current = Math.min(1, spawnRef.current + dt * 2.2);
      if (bodyRef.current) {
        bodyRef.current.scale.setScalar((0.2 + 0.8 * (1 - Math.pow(1 - spawnRef.current, 2))) * (1 + flash.current * 0.12));
        bodyRef.current.position.y = (1 - spawnRef.current) * 3;
      }
    } else if (bodyRef.current) {
      bodyRef.current.scale.setScalar(damp(bodyRef.current.scale.x, 1 + flash.current * 0.12, 10, dt));
      bodyRef.current.position.y = damp(bodyRef.current.position.y, 0, 10, dt);
    }

    if (healthBarRef.current) {
      const pct = Math.max(0, e.health / Math.max(1, e.maxHealth));
      healthBarRef.current.scale.x = Math.max(0.001, pct);
      healthBarRef.current.position.x = -0.5 * (1 - pct);
    }
  });

  const style = TYPE_STYLE[enemyType] ?? TYPE_STYLE.scrapling;

  return (
    <group ref={groupRef}>
      <group ref={bodyRef}>
        <EnemyBody style={style} />
        <mesh ref={flashRef} position={[0, style.scale * 0.9, 0]}>
          <sphereGeometry args={[style.scale * 0.75, 10, 10]} />
          <meshBasicMaterial color="#ffffff" transparent opacity={0} depthWrite={false} />
        </mesh>
      </group>
      <group ref={healthGroupRef}>
        <Billboard position={[0, 1.5 * style.scale + 0.6, 0]}>
          <mesh>
            <planeGeometry args={[1, 0.08]} />
            <meshBasicMaterial color="#0a0e18" transparent opacity={0.75} />
          </mesh>
          <mesh ref={healthBarRef} position={[0, 0, 0.001]}>
            <planeGeometry args={[1, 0.07]} />
            <meshBasicMaterial color={style.emissive} />
          </mesh>
        </Billboard>
      </group>
    </group>
  );
}

function EnemyBody({ style }: { style: (typeof TYPE_STYLE)[string] }) {
  const s = style.scale;
  if (style.shape === "brute") {
    return (
      <group scale={s}>
        <mesh castShadow position={[0, 0.75, 0]}>
          <dodecahedronGeometry args={[0.7, 0]} />
          <meshStandardMaterial color={style.color} emissive={style.emissive} emissiveIntensity={0.5} metalness={0.6} roughness={0.5} flatShading />
        </mesh>
        <mesh position={[0, 0.8, 0.55]}>
          <sphereGeometry args={[0.12, 8, 8]} />
          <meshStandardMaterial color="#0a0e18" emissive={style.emissive} emissiveIntensity={3} />
        </mesh>
      </group>
    );
  }
  if (style.shape === "sniper") {
    return (
      <group scale={s}>
        <mesh castShadow position={[0, 0.9, 0]} rotation={[Math.PI, 0, 0]}>
          <coneGeometry args={[0.32, 1.1, 5]} />
          <meshStandardMaterial color={style.color} emissive={style.emissive} emissiveIntensity={0.4} metalness={0.7} roughness={0.3} flatShading />
        </mesh>
        <mesh position={[0, 0.5, 0.4]} rotation={[Math.PI / 2, 0, 0]}>
          <cylinderGeometry args={[0.04, 0.04, 0.7, 6]} />
          <meshStandardMaterial color="#0a0e18" emissive={style.emissive} emissiveIntensity={2} />
        </mesh>
      </group>
    );
  }
  if (style.shape === "boss") {
    return (
      <group scale={s}>
        <mesh castShadow position={[0, 1.2, 0]}>
          <icosahedronGeometry args={[0.9, 0]} />
          <meshStandardMaterial color={style.color} emissive={style.emissive} emissiveIntensity={0.55} metalness={0.75} roughness={0.3} flatShading />
        </mesh>
        <mesh position={[0, 1.2, 0]}>
          <torusGeometry args={[1.15, 0.08, 8, 24]} />
          <meshStandardMaterial color="#0a0e18" emissive={style.emissive} emissiveIntensity={2} />
        </mesh>
        <mesh position={[0, 1.55, 0.6]}>
          <sphereGeometry args={[0.18, 10, 10]} />
          <meshStandardMaterial color="#0a0e18" emissive={style.emissive} emissiveIntensity={3.5} />
        </mesh>
        <pointLight color={style.emissive} intensity={2} distance={6} position={[0, 1.4, 0]} />
      </group>
    );
  }
  if (style.shape === "strafer") {
    return (
      <group scale={s}>
        <mesh castShadow position={[0, 0.9, 0]}>
          <octahedronGeometry args={[0.5, 0]} />
          <meshStandardMaterial color={style.color} emissive={style.emissive} emissiveIntensity={0.5} metalness={0.6} roughness={0.4} flatShading />
        </mesh>
        <mesh position={[0.35, 0.9, 0]} rotation={[0, 0, Math.PI / 2]}>
          <cylinderGeometry args={[0.05, 0.05, 0.5, 6]} />
          <meshStandardMaterial color="#0a0e18" emissive={style.emissive} emissiveIntensity={2} />
        </mesh>
      </group>
    );
  }
  return (
    <group scale={s}>
      <mesh castShadow position={[0, 0.55, 0]}>
        <boxGeometry args={[0.55, 0.55, 0.55]} />
        <meshStandardMaterial color={style.color} emissive={style.emissive} emissiveIntensity={0.5} metalness={0.6} roughness={0.5} flatShading />
      </mesh>
      <mesh position={[0, 0.55, 0.29]}>
        <boxGeometry args={[0.2, 0.1, 0.05]} />
        <meshStandardMaterial color="#0a0e18" emissive={style.emissive} emissiveIntensity={3} />
      </mesh>
    </group>
  );
}
