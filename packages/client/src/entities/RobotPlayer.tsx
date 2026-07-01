import { useEffect, useRef, useState } from "react";
import { useFrame } from "@react-three/fiber";
import * as THREE from "three";
import { MovementStats, PARTS_BY_ID, PLAYER_BASE, computeMechStats, freshUpgradeLevels } from "@dream/shared";
import { useGameStore } from "../state/store";
import { fxBus } from "../net/fx";
import { damp, lerpAngle } from "../util/math";
import { LocalPredictor } from "../net/prediction";
import { inputManager } from "../input/InputManager";
import { MechRobotModel, MechVehicleModel, MechAnim } from "./models/MechModel";
import NamePlate from "./NamePlate";

function partHue(id: string, fallback: number): number {
  return id ? PARTS_BY_ID[id]?.colorHue ?? fallback : fallback;
}

export default function RobotPlayer({ sessionId, isLocal }: { sessionId: string; isLocal: boolean }) {
  const room = useGameStore((s) => s.room);
  const player = useGameStore((s) => s.players[sessionId]);
  const obstacles = useGameStore((s) => s.obstacles);
  const profile = useGameStore((s) => s.profile);
  const groupRef = useRef<THREE.Group>(null!);
  const lastPos = useRef(new THREE.Vector3());
  const anim = useRef<MechAnim>({ speed: 0, walk: 0, overdrive: 0 }).current;
  const spinBoost = useRef(0);
  const predictorRef = useRef<LocalPredictor | null>(null);
  const [displayMode, setDisplayMode] = useState(player?.mode ?? "robot");

  useEffect(() => {
    if (isLocal && !predictorRef.current) predictorRef.current = new LocalPredictor();
  }, [isLocal]);

  useEffect(() => {
    if (!isLocal && player) setDisplayMode(player.mode);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isLocal, player?.mode]);

  useEffect(() => {
    const offs = [
      fxBus.on("transformStart", (d) => {
        if (d.sessionId === sessionId) spinBoost.current = 1;
      }),
      fxBus.on("overdrive", (d) => {
        if (d.sessionId === sessionId) anim.overdrive = 1;
      }),
    ];
    return () => offs.forEach((o) => o());
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [sessionId]);

  useFrame((_, dt) => {
    const state = room?.state as
      | { players: Map<string, { x: number; z: number; yaw: number; mode: string; transforming: boolean }> }
      | undefined;
    const p = state?.players?.get(sessionId);
    if (!p || !groupRef.current || !player) return;

    let targetX = p.x;
    let targetZ = p.z;
    let targetYaw = p.yaw;

    if (isLocal && predictorRef.current) {
      const predictor = predictorRef.current;
      // Use our real garage upgrade levels so predicted speed matches the server's
      // authoritative speed exactly - otherwise engine upgrades would cause drift.
      const upgrades = profile?.mechUpgrades[player.mechId] ?? freshUpgradeLevels();
      const stats: MovementStats = computeMechStats(player.mechId, upgrades, player.loadout);
      const input = inputManager.sample();
      const events = predictor.step(input, stats, obstacles, dt);
      if (events.dashStart) fxBus.emit("dash", { sessionId, x: predictor.state.x, z: predictor.state.z, yaw: predictor.state.yaw });
      if (events.transformStart) fxBus.emit("transformStart", { sessionId, x: predictor.state.x, z: predictor.state.z });
      if (events.transformEnd) {
        fxBus.emit("transform", { sessionId, mode: predictor.state.mode });
        setDisplayMode(predictor.state.mode);
      }
      predictor.reconcile(p.x, p.z, p.yaw, p.mode, p.transforming);
      targetX = predictor.state.x;
      targetZ = predictor.state.z;
      targetYaw = predictor.state.yaw;
    }

    const lerpRate = isLocal ? 26 : 10;
    groupRef.current.position.x = damp(groupRef.current.position.x, targetX, lerpRate, dt);
    groupRef.current.position.z = damp(groupRef.current.position.z, targetZ, lerpRate, dt);

    const dx = groupRef.current.position.x - lastPos.current.x;
    const dz = groupRef.current.position.z - lastPos.current.z;
    const inst = Math.hypot(dx, dz) / Math.max(dt, 0.0001);
    anim.speed = damp(anim.speed, inst, 5, dt);
    lastPos.current.set(groupRef.current.position.x, 0, groupRef.current.position.z);
    if (anim.overdrive > 0) anim.overdrive = Math.max(0, anim.overdrive - dt * 0.8);

    if (spinBoost.current > 0) {
      spinBoost.current = Math.max(0, spinBoost.current - dt / PLAYER_BASE.transformLockSeconds);
      groupRef.current.rotation.y += dt * 26 * spinBoost.current;
      const s = 1 - Math.sin(spinBoost.current * Math.PI) * 0.85;
      groupRef.current.scale.setScalar(THREE.MathUtils.clamp(s, 0.18, 1));
    } else {
      groupRef.current.rotation.y = lerpAngle(groupRef.current.rotation.y, targetYaw, isLocal ? 18 : 12, dt);
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
        {displayMode === "vehicle" ? (
          <MechVehicleModel mechId={player.mechId} color={player.color} anim={anim} engineHue={engineHue} />
        ) : (
          <MechRobotModel mechId={player.mechId} color={player.color} anim={anim} weaponHue={weaponHue} platingHue={platingHue} />
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
