import { useMemo, useRef } from "react";
import { useFrame } from "@react-three/fiber";
import * as THREE from "three";
import { getMechClass } from "@dream/shared";
import { damp, hueToHex } from "../../util/math";

export interface MechAnim {
  /** smoothed ground speed, units/sec */
  speed: number;
  /** accumulating phase clock */
  walk: number;
  /** 0..1 while overdrive active */
  overdrive: number;
}

interface MechModelProps {
  mechId: string;
  color: string;
  anim: MechAnim;
  weaponHue?: number;
  platingHue?: number;
}

/**
 * Procedural mech, robot form. One rig shared by all classes (torso/arms/legs with a
 * walk gait, idle breathing, and lean-into-motion), with per-class proportions and
 * signature attachments so each silhouette reads instantly at gameplay camera distance:
 * Vanguard - balanced + antenna; Juggernaut - wide, hunched, slab shoulders;
 * Interceptor - slim with swept winglets; Artillery - shoulder cannon + braced stance.
 */
export function MechRobotModel({ mechId, color, anim, weaponHue = 0.02, platingHue = 0.58 }: MechModelProps) {
  const mech = getMechClass(mechId);
  const leftLeg = useRef<THREE.Group>(null!);
  const rightLeg = useRef<THREE.Group>(null!);
  const leftShin = useRef<THREE.Group>(null!);
  const rightShin = useRef<THREE.Group>(null!);
  const leftArm = useRef<THREE.Group>(null!);
  const rightArm = useRef<THREE.Group>(null!);
  const torso = useRef<THREE.Group>(null!);
  const head = useRef<THREE.Group>(null!);

  const accent = useMemo(() => hueToHex(mech.accentHue, 0.85, 0.6), [mech.accentHue]);
  const weaponGlow = useMemo(() => hueToHex(weaponHue, 0.75, 0.6), [weaponHue]);
  const platingGlow = useMemo(() => hueToHex(platingHue, 0.7, 0.55), [platingHue]);
  const darkColor = useMemo(() => new THREE.Color(color).multiplyScalar(0.45).getStyle(), [color]);

  // per-class proportions
  const p = useMemo(() => {
    switch (mech.silhouette) {
      case "juggernaut":
        return { torsoW: 0.82, torsoH: 0.66, shoulderW: 0.3, legW: 0.28, hunch: 0.12, scale: 1.12 };
      case "interceptor":
        return { torsoW: 0.48, torsoH: 0.62, shoulderW: 0.14, legW: 0.15, hunch: -0.04, scale: 0.95 };
      case "artillery":
        return { torsoW: 0.66, torsoH: 0.6, shoulderW: 0.22, legW: 0.24, hunch: 0.06, scale: 1.05 };
      default:
        return { torsoW: 0.62, torsoH: 0.68, shoulderW: 0.18, legW: 0.2, hunch: 0, scale: 1 };
    }
  }, [mech.silhouette]);

  useFrame((state, dt) => {
    const moveAmt = Math.min(1, anim.speed / 6);
    const gait = anim.walk * 7.2;
    const swing = Math.sin(gait) * moveAmt * 0.6;
    const counter = Math.sin(gait + Math.PI) * moveAmt * 0.6;
    const idle = Math.sin(state.clock.elapsedTime * 1.7) * (1 - moveAmt);

    if (leftLeg.current) leftLeg.current.rotation.x = damp(leftLeg.current.rotation.x, swing, 18, dt);
    if (rightLeg.current) rightLeg.current.rotation.x = damp(rightLeg.current.rotation.x, counter, 18, dt);
    // shins bend only on the back-swing, giving an actual knee articulation instead of stiff pendulum legs
    if (leftShin.current) leftShin.current.rotation.x = damp(leftShin.current.rotation.x, Math.max(0, -swing) * 1.4, 16, dt);
    if (rightShin.current) rightShin.current.rotation.x = damp(rightShin.current.rotation.x, Math.max(0, -counter) * 1.4, 16, dt);
    if (leftArm.current) leftArm.current.rotation.x = damp(leftArm.current.rotation.x, counter * 0.7 + idle * 0.02, 18, dt);
    if (rightArm.current) rightArm.current.rotation.x = damp(rightArm.current.rotation.x, swing * 0.7 - 0.25, 18, dt);
    if (torso.current) {
      const bob = moveAmt < 0.05 ? idle * 0.015 : Math.abs(Math.sin(gait)) * 0.045 * moveAmt;
      torso.current.position.y = damp(torso.current.position.y, 1.05 + bob, 14, dt);
      torso.current.rotation.x = damp(torso.current.rotation.x, p.hunch + moveAmt * 0.12, 8, dt);
      torso.current.rotation.z = damp(torso.current.rotation.z, Math.sin(gait) * 0.03 * moveAmt, 10, dt);
    }
    if (head.current) {
      head.current.rotation.x = damp(head.current.rotation.x, -moveAmt * 0.08 + idle * 0.015, 8, dt);
    }
  });

  const legLen = 0.62;

  return (
    <group scale={p.scale}>
      <group ref={torso} position={[0, 1.05, 0]}>
        {/* chest core */}
        <mesh castShadow>
          <boxGeometry args={[p.torsoW, p.torsoH, 0.42]} />
          <meshStandardMaterial color={color} metalness={0.8} roughness={0.28} />
        </mesh>
        {/* chest bevel plate */}
        <mesh position={[0, p.torsoH * 0.18, 0.2]} castShadow>
          <boxGeometry args={[p.torsoW * 0.7, p.torsoH * 0.42, 0.1]} />
          <meshStandardMaterial color={darkColor} metalness={0.7} roughness={0.35} />
        </mesh>
        {/* reactor core */}
        <mesh position={[0, 0.02, 0.24]}>
          <cylinderGeometry args={[0.09, 0.09, 0.06, 12]} />
          <meshStandardMaterial color="#0a0e18" emissive={platingGlow} emissiveIntensity={2.2} />
        </mesh>
        {/* waist */}
        <mesh position={[0, -p.torsoH * 0.62, 0]} castShadow>
          <boxGeometry args={[p.torsoW * 0.62, 0.18, 0.3]} />
          <meshStandardMaterial color={darkColor} metalness={0.7} roughness={0.4} />
        </mesh>
        {/* accent trim lines */}
        <mesh position={[0, -p.torsoH * 0.3, 0.215]}>
          <boxGeometry args={[p.torsoW * 0.85, 0.03, 0.02]} />
          <meshStandardMaterial color="#0a0e18" emissive={accent} emissiveIntensity={2} />
        </mesh>

        {/* head */}
        <group ref={head} position={[0, p.torsoH * 0.5 + 0.19, 0]}>
          <mesh castShadow>
            <boxGeometry args={[0.3, 0.26, 0.32]} />
            <meshStandardMaterial color="#141a26" metalness={0.65} roughness={0.35} />
          </mesh>
          <mesh position={[0, 0.01, 0.155]}>
            <boxGeometry args={[0.22, 0.07, 0.03]} />
            <meshStandardMaterial color="#0a0e18" emissive="#8fd3ff" emissiveIntensity={2.6} />
          </mesh>
          <mesh position={[0, 0.16, 0]} castShadow>
            <boxGeometry args={[0.34, 0.05, 0.2]} />
            <meshStandardMaterial color={color} metalness={0.8} roughness={0.3} />
          </mesh>
          {mech.silhouette === "vanguard" && (
            <mesh position={[0.14, 0.28, 0]} rotation={[0, 0, -0.15]}>
              <cylinderGeometry args={[0.008, 0.012, 0.24, 4]} />
              <meshStandardMaterial color="#1c2436" emissive={accent} emissiveIntensity={0.8} />
            </mesh>
          )}
        </group>

        {/* shoulders */}
        {[-1, 1].map((side) => (
          <mesh key={side} position={[side * (p.torsoW / 2 + p.shoulderW / 2 + 0.02), p.torsoH * 0.28, 0]} castShadow>
            <boxGeometry args={[p.shoulderW, mech.silhouette === "juggernaut" ? 0.5 : 0.38, mech.silhouette === "juggernaut" ? 0.48 : 0.36]} />
            <meshStandardMaterial color={darkColor} emissive={platingGlow} emissiveIntensity={0.3} metalness={0.75} roughness={0.3} />
          </mesh>
        ))}

        {/* interceptor winglets */}
        {mech.silhouette === "interceptor" &&
          [-1, 1].map((side) => (
            <mesh key={side} position={[side * (p.torsoW / 2 + 0.1), p.torsoH * 0.42, -0.18]} rotation={[0.5, 0, side * -0.7]} castShadow>
              <boxGeometry args={[0.05, 0.5, 0.16]} />
              <meshStandardMaterial color={color} emissive={accent} emissiveIntensity={0.6} metalness={0.85} roughness={0.2} />
            </mesh>
          ))}

        {/* artillery shoulder cannon */}
        {mech.silhouette === "artillery" && (
          <group position={[0.28, p.torsoH * 0.62, -0.05]} rotation={[-0.12, 0, 0]}>
            <mesh castShadow rotation={[Math.PI / 2, 0, 0]}>
              <cylinderGeometry args={[0.09, 0.11, 0.85, 10]} />
              <meshStandardMaterial color="#1c2436" metalness={0.8} roughness={0.3} />
            </mesh>
            <mesh position={[0, 0, 0.46]} rotation={[Math.PI / 2, 0, 0]}>
              <cylinderGeometry args={[0.06, 0.06, 0.1, 10]} />
              <meshStandardMaterial color="#0a0e18" emissive={weaponGlow} emissiveIntensity={2.4} />
            </mesh>
          </group>
        )}

        {/* juggernaut back exhausts */}
        {mech.silhouette === "juggernaut" &&
          [-1, 1].map((side) => (
            <mesh key={side} position={[side * p.torsoW * 0.28, p.torsoH * 0.52, -0.22]} castShadow>
              <cylinderGeometry args={[0.06, 0.08, 0.3, 8]} />
              <meshStandardMaterial color="#1c2436" emissive={accent} emissiveIntensity={0.9} metalness={0.7} roughness={0.35} />
            </mesh>
          ))}

        {/* arms */}
        <group ref={leftArm} position={[-(p.torsoW / 2 + p.shoulderW / 2 + 0.02), p.torsoH * 0.12, 0]}>
          <mesh position={[0, -0.24, 0]} castShadow>
            <boxGeometry args={[0.16, 0.4, 0.18]} />
            <meshStandardMaterial color="#1c2436" metalness={0.7} roughness={0.35} />
          </mesh>
          <mesh position={[0, -0.52, 0.02]} castShadow>
            <boxGeometry args={[0.14, 0.26, 0.16]} />
            <meshStandardMaterial color={color} metalness={0.75} roughness={0.3} />
          </mesh>
        </group>
        <group ref={rightArm} position={[p.torsoW / 2 + p.shoulderW / 2 + 0.02, p.torsoH * 0.12, 0]}>
          <mesh position={[0, -0.24, 0]} castShadow>
            <boxGeometry args={[0.16, 0.4, 0.18]} />
            <meshStandardMaterial color="#1c2436" metalness={0.7} roughness={0.35} />
          </mesh>
          {/* weapon forearm */}
          <mesh position={[0, -0.52, 0.06]} castShadow>
            <boxGeometry args={[0.16, 0.28, 0.2]} />
            <meshStandardMaterial color={darkColor} metalness={0.75} roughness={0.3} />
          </mesh>
          <mesh position={[0.02, -0.6, 0.2]} rotation={[Math.PI / 2, 0, 0]} castShadow>
            <cylinderGeometry args={[0.045, 0.06, 0.42, 8]} />
            <meshStandardMaterial color="#0a0e18" emissive={weaponGlow} emissiveIntensity={1.8} metalness={0.5} roughness={0.4} />
          </mesh>
        </group>
      </group>

      {/* legs with knee joints */}
      {[
        { side: -1, hip: leftLeg, shin: leftShin },
        { side: 1, hip: rightLeg, shin: rightShin },
      ].map(({ side, hip, shin }) => (
        <group key={side} ref={hip} position={[side * (p.torsoW * 0.3), 0.82, 0]}>
          <mesh position={[0, -legLen * 0.28, 0]} castShadow>
            <boxGeometry args={[p.legW, legLen * 0.55, 0.24]} />
            <meshStandardMaterial color="#20293c" metalness={0.7} roughness={0.4} />
          </mesh>
          <group ref={shin} position={[0, -legLen * 0.55, 0]}>
            <mesh position={[0, -legLen * 0.24, 0.02]} castShadow>
              <boxGeometry args={[p.legW * 0.85, legLen * 0.5, 0.2]} />
              <meshStandardMaterial color={darkColor} metalness={0.7} roughness={0.4} />
            </mesh>
            <mesh position={[0, -legLen * 0.5, 0.07]} castShadow>
              <boxGeometry args={[p.legW * 1.15, 0.12, 0.36]} />
              <meshStandardMaterial color="#0e1420" metalness={0.6} roughness={0.5} />
            </mesh>
            {/* knee light */}
            <mesh position={[0, 0.02, 0.11]}>
              <sphereGeometry args={[0.035, 8, 8]} />
              <meshStandardMaterial color="#0a0e18" emissive={accent} emissiveIntensity={1.6} />
            </mesh>
          </group>
        </group>
      ))}
    </group>
  );
}

/** Procedural mech, vehicle form - per-class chassis so the alt-mode matches the fantasy. */
export function MechVehicleModel({ mechId, color, anim, engineHue = 0.14 }: { mechId: string; color: string; anim: MechAnim; engineHue?: number }) {
  const mech = getMechClass(mechId);
  const body = useRef<THREE.Group>(null!);
  const accent = useMemo(() => hueToHex(mech.accentHue, 0.85, 0.6), [mech.accentHue]);
  const thrusterGlow = useMemo(() => hueToHex(engineHue, 0.8, 0.6), [engineHue]);
  const darkColor = useMemo(() => new THREE.Color(color).multiplyScalar(0.45).getStyle(), [color]);

  useFrame((_, dt) => {
    if (!body.current) return;
    const hover = Math.sin(anim.walk * 3.4) * 0.035;
    body.current.position.y = damp(body.current.position.y, 0.42 + hover, 10, dt);
    body.current.rotation.z = damp(body.current.rotation.z, THREE.MathUtils.clamp(-anim.speed * 0.012, -0.18, 0.18), 8, dt);
    body.current.rotation.x = damp(body.current.rotation.x, THREE.MathUtils.clamp(anim.speed * 0.006, 0, 0.08), 8, dt);
  });

  const isJug = mech.silhouette === "juggernaut";
  const isInt = mech.silhouette === "interceptor";
  const isArt = mech.silhouette === "artillery";

  return (
    <group ref={body} position={[0, 0.42, 0]}>
      {/* main hull */}
      {isInt ? (
        <mesh castShadow rotation={[0, Math.PI, 0]}>
          <coneGeometry args={[0.42, 2.2, 4]} />
          <meshStandardMaterial color={color} metalness={0.9} roughness={0.15} flatShading />
        </mesh>
      ) : (
        <mesh castShadow rotation={[0, Math.PI, 0]}>
          <coneGeometry args={[isJug ? 0.68 : 0.55, isJug ? 1.6 : 1.9, 4]} />
          <meshStandardMaterial color={color} metalness={0.85} roughness={0.22} flatShading />
        </mesh>
      )}
      {/* under-hull */}
      <mesh position={[0, -0.08, 0]} castShadow>
        <boxGeometry args={[isJug ? 1.15 : 0.9, 0.32, isJug ? 1.5 : 1.3]} />
        <meshStandardMaterial color="#141a26" metalness={0.7} roughness={0.3} />
      </mesh>
      {/* treads for juggernaut */}
      {isJug &&
        [-1, 1].map((side) => (
          <mesh key={side} position={[side * 0.62, -0.12, 0]} castShadow>
            <boxGeometry args={[0.22, 0.3, 1.55]} />
            <meshStandardMaterial color="#0e1420" metalness={0.5} roughness={0.6} />
          </mesh>
        ))}
      {/* interceptor wings */}
      {isInt &&
        [-1, 1].map((side) => (
          <mesh key={side} position={[side * 0.55, 0.02, 0.25]} rotation={[0, side * -0.35, side * 0.1]} castShadow>
            <boxGeometry args={[0.8, 0.04, 0.4]} />
            <meshStandardMaterial color={darkColor} emissive={accent} emissiveIntensity={0.5} metalness={0.85} roughness={0.2} />
          </mesh>
        ))}
      {/* artillery barrel */}
      {isArt && (
        <mesh position={[0, 0.22, 0.5]} rotation={[Math.PI / 2 - 0.14, 0, 0]} castShadow>
          <cylinderGeometry args={[0.07, 0.09, 1.1, 10]} />
          <meshStandardMaterial color="#1c2436" metalness={0.8} roughness={0.3} />
        </mesh>
      )}
      {/* canopy */}
      <mesh position={[0, 0.14, 0.35]}>
        <boxGeometry args={[0.5, 0.16, 0.5]} />
        <meshStandardMaterial color="#0a0e18" emissive="#8fd3ff" emissiveIntensity={1.8} />
      </mesh>
      {/* thruster */}
      <mesh position={[0, 0.02, isJug ? -0.78 : -0.85]}>
        <cylinderGeometry args={[0.32, 0.32, 0.22, 10]} />
        <meshStandardMaterial color="#0a0e18" emissive={thrusterGlow} emissiveIntensity={2.2 + Math.min(1.5, anim.speed / 8) + anim.overdrive * 2} />
      </mesh>
      {/* accent strip */}
      <mesh position={[0, 0.08, 0]}>
        <boxGeometry args={[isJug ? 1.16 : 0.92, 0.02, 0.02]} />
        <meshStandardMaterial color="#0a0e18" emissive={accent} emissiveIntensity={2} />
      </mesh>
    </group>
  );
}
