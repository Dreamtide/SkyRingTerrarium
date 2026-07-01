import { useMemo, useRef } from "react";
import { useFrame } from "@react-three/fiber";
import * as THREE from "three";
import { PUP_FORM, STAGE2_FORMS } from "@dream/shared";
import { damp, hueToHex } from "../../util/math";

export interface DogAnim {
  /** smoothed ground speed, units/sec */
  speed: number;
  /** accumulating phase clock */
  walk: number;
  /** 0..1 happiness/excitement (drives tail wag + ear perk); hub uses 1, combat derives from context */
  spirit: number;
}

export function dogFormVisual(formId: string, stage: string) {
  if (formId === "pup") return PUP_FORM.visual;
  const s2 = Object.values(STAGE2_FORMS).find((f) => f.id === formId);
  if (s2) return s2.visual;
  // Stage-3 hybrids fall back to a generic elevated look driven by stage.
  return {
    scale: 0.95,
    bodyHue: 0.62,
    glowHue: 0.66,
    hasWings: stage === "stage3",
    hasArmor: stage !== "pup",
    hasHorns: stage === "stage3",
    maneStyle: "aura" as const,
  };
}

/**
 * Procedural magic hound with a real quadruped trot gait (diagonal leg pairs), an
 * articulated head that dips with speed, floppy ears, wagging tail, and evolution
 * accessories (armor/wings/horns/aura) layered on by form. Shared between the hub
 * (idle, happy) and combat (task-driven) so the dog always looks like the same animal.
 */
export default function DogModel({ formId, stage, anim }: { formId: string; stage: string; anim: DogAnim }) {
  const visual = useMemo(() => dogFormVisual(formId, stage), [formId, stage]);
  const bodyColor = useMemo(() => hueToHex(visual.bodyHue, 0.55, 0.5), [visual.bodyHue]);
  const glowColor = useMemo(() => hueToHex(visual.glowHue, 0.8, 0.6), [visual.glowHue]);

  const bodyRef = useRef<THREE.Group>(null!);
  const headRef = useRef<THREE.Group>(null!);
  const tailRef = useRef<THREE.Group>(null!);
  const earL = useRef<THREE.Group>(null!);
  const earR = useRef<THREE.Group>(null!);
  // legs: FL, FR, BL, BR - trot pairs are (FL,BR) and (FR,BL)
  const legs = [useRef<THREE.Group>(null!), useRef<THREE.Group>(null!), useRef<THREE.Group>(null!), useRef<THREE.Group>(null!)];

  useFrame((state, dt) => {
    const moveAmt = Math.min(1, anim.speed / 5);
    const gait = anim.walk * 9;
    const idleT = state.clock.elapsedTime;

    const phases = [0, Math.PI, Math.PI, 0]; // FL, FR, BL, BR - diagonal pairs in sync
    legs.forEach((leg, i) => {
      if (!leg.current) return;
      const target = Math.sin(gait + phases[i]) * moveAmt * 0.75;
      leg.current.rotation.x = damp(leg.current.rotation.x, target, 20, dt);
    });

    if (bodyRef.current) {
      const bob = moveAmt > 0.05 ? Math.abs(Math.sin(gait)) * 0.045 * moveAmt : Math.sin(idleT * 2.2) * 0.012;
      bodyRef.current.position.y = damp(bodyRef.current.position.y, 0.42 + bob, 16, dt);
      bodyRef.current.rotation.z = damp(bodyRef.current.rotation.z, Math.sin(gait) * 0.04 * moveAmt, 12, dt);
    }
    if (headRef.current) {
      // head lowers into a run, sniffs around when idle
      const idleLook = moveAmt < 0.1 ? Math.sin(idleT * 0.7) * 0.25 : 0;
      headRef.current.rotation.x = damp(headRef.current.rotation.x, moveAmt * 0.3 - anim.spirit * 0.08, 8, dt);
      headRef.current.rotation.y = damp(headRef.current.rotation.y, idleLook, 4, dt);
    }
    if (tailRef.current) {
      const wagSpeed = 4 + anim.spirit * 10 + moveAmt * 4;
      tailRef.current.rotation.y = Math.sin(idleT * wagSpeed) * (0.3 + anim.spirit * 0.4);
      tailRef.current.rotation.x = damp(tailRef.current.rotation.x, -0.4 - anim.spirit * 0.35, 8, dt);
    }
    const earFlop = Math.sin(gait) * moveAmt * 0.2;
    if (earL.current) earL.current.rotation.z = damp(earL.current.rotation.z, 0.35 - anim.spirit * 0.25 + earFlop, 10, dt);
    if (earR.current) earR.current.rotation.z = damp(earR.current.rotation.z, -0.35 + anim.spirit * 0.25 - earFlop, 10, dt);
  });

  const s = visual.scale;

  return (
    <group scale={s}>
      <group ref={bodyRef} position={[0, 0.42, 0]}>
        {/* body: chest + hindquarters for a real dog profile instead of one capsule */}
        <mesh castShadow position={[0, 0.02, 0.1]} rotation={[Math.PI / 2, 0, 0]}>
          <capsuleGeometry args={[0.21, 0.42, 4, 10]} />
          <meshStandardMaterial color={bodyColor} metalness={0.25} roughness={0.55} emissive={glowColor} emissiveIntensity={0.15} />
        </mesh>
        <mesh castShadow position={[0, 0.05, 0.3]}>
          <sphereGeometry args={[0.2, 12, 10]} />
          <meshStandardMaterial color={bodyColor} metalness={0.25} roughness={0.5} emissive={glowColor} emissiveIntensity={0.18} />
        </mesh>
        <mesh castShadow position={[0, 0, -0.2]}>
          <sphereGeometry args={[0.19, 12, 10]} />
          <meshStandardMaterial color={bodyColor} metalness={0.25} roughness={0.55} emissive={glowColor} emissiveIntensity={0.12} />
        </mesh>

        {/* head group */}
        <group ref={headRef} position={[0, 0.18, 0.44]}>
          <mesh castShadow>
            <sphereGeometry args={[0.16, 12, 10]} />
            <meshStandardMaterial color={bodyColor} metalness={0.25} roughness={0.5} />
          </mesh>
          {/* snout */}
          <mesh castShadow position={[0, -0.04, 0.14]}>
            <boxGeometry args={[0.12, 0.1, 0.14]} />
            <meshStandardMaterial color={bodyColor} metalness={0.2} roughness={0.55} />
          </mesh>
          <mesh position={[0, -0.02, 0.215]}>
            <sphereGeometry args={[0.028, 8, 8]} />
            <meshStandardMaterial color="#0a0e18" roughness={0.3} />
          </mesh>
          {/* eyes */}
          {[-1, 1].map((side) => (
            <mesh key={side} position={[side * 0.075, 0.045, 0.125]}>
              <sphereGeometry args={[0.032, 8, 8]} />
              <meshStandardMaterial color="#0a0e18" emissive={glowColor} emissiveIntensity={2.6} />
            </mesh>
          ))}
          {/* ears */}
          <group ref={earL} position={[-0.1, 0.13, -0.02]}>
            <mesh castShadow position={[0, 0.08, 0]} rotation={[0.15, 0, 0]}>
              <coneGeometry args={[0.05, 0.17, 6]} />
              <meshStandardMaterial color={bodyColor} metalness={0.2} roughness={0.6} />
            </mesh>
          </group>
          <group ref={earR} position={[0.1, 0.13, -0.02]}>
            <mesh castShadow position={[0, 0.08, 0]} rotation={[0.15, 0, 0]}>
              <coneGeometry args={[0.05, 0.17, 6]} />
              <meshStandardMaterial color={bodyColor} metalness={0.2} roughness={0.6} />
            </mesh>
          </group>
          {/* horns (evolved) */}
          {visual.hasHorns &&
            [-1, 1].map((side) => (
              <mesh key={side} position={[side * 0.07, 0.16, 0.05]} rotation={[0.35, 0, side * -0.3]}>
                <coneGeometry args={[0.025, 0.16, 6]} />
                <meshStandardMaterial color={glowColor} emissive={glowColor} emissiveIntensity={1.6} />
              </mesh>
            ))}
        </group>

        {/* tail */}
        <group ref={tailRef} position={[0, 0.12, -0.36]}>
          <mesh castShadow position={[0, 0.05, -0.1]} rotation={[0.7, 0, 0]}>
            <coneGeometry args={[0.05, 0.3, 6]} />
            <meshStandardMaterial color={bodyColor} emissive={glowColor} emissiveIntensity={0.5} />
          </mesh>
        </group>

        {/* armor plate (evolved) */}
        {visual.hasArmor && (
          <mesh castShadow position={[0, 0.16, 0.02]}>
            <boxGeometry args={[0.34, 0.12, 0.5]} />
            <meshStandardMaterial color="#1c2436" metalness={0.75} roughness={0.3} emissive={glowColor} emissiveIntensity={0.35} />
          </mesh>
        )}

        {/* wings (evolved) */}
        {visual.hasWings &&
          [-1, 1].map((side) => (
            <mesh key={side} position={[side * 0.24, 0.14, -0.02]} rotation={[0.15, side * 0.25, side * 0.55]}>
              <planeGeometry args={[0.36, 0.2]} />
              <meshStandardMaterial color={glowColor} emissive={glowColor} emissiveIntensity={1.2} transparent opacity={0.72} side={THREE.DoubleSide} />
            </mesh>
          ))}

        {/* mane aura */}
        {visual.maneStyle === "aura" && (
          <mesh position={[0, 0.1, 0.32]}>
            <torusGeometry args={[0.2, 0.03, 8, 18]} />
            <meshStandardMaterial color={glowColor} emissive={glowColor} emissiveIntensity={1.4} transparent opacity={0.6} />
          </mesh>
        )}
        {visual.maneStyle === "spikes" &&
          [0, 1, 2].map((i) => (
            <mesh key={i} castShadow position={[0, 0.2, 0.18 - i * 0.14]} rotation={[-0.5 + i * 0.12, 0, 0]}>
              <coneGeometry args={[0.035, 0.12, 5]} />
              <meshStandardMaterial color="#1c2436" emissive={glowColor} emissiveIntensity={0.8} />
            </mesh>
          ))}
        {visual.maneStyle === "flame" && (
          <mesh position={[0, 0.22, 0.28]}>
            <coneGeometry args={[0.09, 0.2, 7]} />
            <meshStandardMaterial color={glowColor} emissive={glowColor} emissiveIntensity={2} transparent opacity={0.75} />
          </mesh>
        )}
      </group>

      {/* legs with lower joints */}
      {[
        [-0.13, 0.32],
        [0.13, 0.32],
        [-0.13, -0.22],
        [0.13, -0.22],
      ].map(([x, z], i) => (
        <group key={i} ref={legs[i]} position={[x, 0.34, z]}>
          <mesh castShadow position={[0, -0.14, 0]}>
            <cylinderGeometry args={[0.045, 0.035, 0.26, 6]} />
            <meshStandardMaterial color={bodyColor} metalness={0.25} roughness={0.6} />
          </mesh>
          <mesh castShadow position={[0, -0.28, 0.01]}>
            <sphereGeometry args={[0.05, 8, 6]} />
            <meshStandardMaterial color="#1c2436" metalness={0.4} roughness={0.5} />
          </mesh>
        </group>
      ))}

      <pointLight color={glowColor} intensity={0.6} distance={2.4} position={[0, 0.5, 0]} />
    </group>
  );
}
