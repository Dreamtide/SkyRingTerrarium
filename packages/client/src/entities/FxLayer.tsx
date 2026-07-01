import { useEffect, useRef } from "react";
import { useFrame } from "@react-three/fiber";
import * as THREE from "three";
import { fxBus } from "../net/fx";

interface Burst {
  active: boolean;
  x: number;
  y: number;
  z: number;
  t: number;
  dur: number;
  color: THREE.Color;
  scale: number;
}

interface Tracer {
  active: boolean;
  from: THREE.Vector3;
  to: THREE.Vector3;
  t: number;
  dur: number;
  color: THREE.Color;
}

interface Shockwave {
  active: boolean;
  x: number;
  z: number;
  t: number;
  dur: number;
  maxRadius: number;
  color: THREE.Color;
}

const BURST_COUNT = 26;
const TRACER_COUNT = 18;
const SHOCKWAVE_COUNT = 6;

const TYPE_COLOR: Record<string, string> = {
  hit: "#ff5d5d",
  death: "#ffb62e",
  ram: "#ff9d4d",
  dash: "#4fa8ff",
  collect: "#57d97b",
  evolve: "#b563ff",
  revive: "#57d97b",
  downed: "#ff5d5d",
  bite: "#ff8d4d",
};

export default function FxLayer() {
  const bursts = useRef<Burst[]>(
    Array.from({ length: BURST_COUNT }, () => ({ active: false, x: 0, y: 0, z: 0, t: 0, dur: 0.4, color: new THREE.Color("#fff"), scale: 1 }))
  );
  const tracers = useRef<Tracer[]>(
    Array.from({ length: TRACER_COUNT }, () => ({ active: false, from: new THREE.Vector3(), to: new THREE.Vector3(), t: 0, dur: 0.14, color: new THREE.Color("#fff") }))
  );
  const shockwaves = useRef<Shockwave[]>(
    Array.from({ length: SHOCKWAVE_COUNT }, () => ({ active: false, x: 0, z: 0, t: 0, dur: 0.5, maxRadius: 2.5, color: new THREE.Color("#fff") }))
  );
  const burstMeshes = useRef<(THREE.Mesh | null)[]>([]);
  const tracerMeshes = useRef<(THREE.Mesh | null)[]>([]);
  const shockwaveMeshes = useRef<(THREE.Mesh | null)[]>([]);
  const burstCursor = useRef(0);
  const tracerCursor = useRef(0);
  const shockwaveCursor = useRef(0);

  function spawnBurst(x: number, y: number, z: number, color: string, scale = 1, dur = 0.4) {
    const i = burstCursor.current;
    burstCursor.current = (i + 1) % BURST_COUNT;
    const b = bursts.current[i];
    b.active = true;
    b.x = x;
    b.y = y;
    b.z = z;
    b.t = 0;
    b.dur = dur;
    b.scale = scale;
    b.color.set(color);
  }

  function spawnTracer(from: { x: number; y: number; z: number }, to: { x: number; y: number; z: number }, color: string) {
    const i = tracerCursor.current;
    tracerCursor.current = (i + 1) % TRACER_COUNT;
    const t = tracers.current[i];
    t.active = true;
    t.from.set(from.x, from.y, from.z);
    t.to.set(to.x, to.y, to.z);
    t.t = 0;
    t.color.set(color);
  }

  function spawnShockwave(x: number, z: number, color: string, maxRadius = 2.5, dur = 0.5) {
    const i = shockwaveCursor.current;
    shockwaveCursor.current = (i + 1) % SHOCKWAVE_COUNT;
    const s = shockwaves.current[i];
    s.active = true;
    s.x = x;
    s.z = z;
    s.t = 0;
    s.dur = dur;
    s.maxRadius = maxRadius;
    s.color.set(color);
  }

  useEffect(() => {
    const offs = [
      fxBus.on("shot", (d) => {
        const from = d.from as { x: number; y: number; z: number };
        const to = d.to as { x: number; y: number; z: number };
        spawnTracer(from, to, "#8fd3ff");
        spawnBurst(to.x, to.y, to.z, "#8fd3ff", 0.5, 0.2);
        spawnBurst(from.x, from.y, from.z, "#eaf6ff", 0.4, 0.11); // muzzle flash
      }),
      fxBus.on("hit", (d) => spawnBurst((d.x as number) ?? 0, 1, (d.z as number) ?? 0, TYPE_COLOR.hit, 0.7, 0.28)),
      fxBus.on("death", (d) => spawnBurst((d.x as number) ?? 0, 1, (d.z as number) ?? 0, TYPE_COLOR.death, 1.6, 0.5)),
      fxBus.on("ram", (d) => spawnBurst((d.x as number) ?? 0, 0.8, (d.z as number) ?? 0, TYPE_COLOR.ram, 1.1, 0.32)),
      fxBus.on("collect", (d) => spawnBurst((d.x as number) ?? 0, 1, (d.z as number) ?? 0, TYPE_COLOR.collect, 0.9, 0.4)),
      fxBus.on("sand", (d) => spawnBurst((d.x as number) ?? 0, 0.3, (d.z as number) ?? 0, "#ffd76e", 0.45, 0.3)),
      fxBus.on("bite", (d) => spawnBurst((d.x as number) ?? 0, 0.6, (d.z as number) ?? 0, TYPE_COLOR.bite, 0.5, 0.22)),
      fxBus.on("dash", (d) => {
        const x = (d.x as number) ?? 0;
        const z = (d.z as number) ?? 0;
        const yaw = (d.yaw as number) ?? 0;
        const fx = Math.sin(yaw);
        const fz = Math.cos(yaw);
        for (let i = 1; i <= 3; i++) {
          const back = i * 0.9;
          spawnTracer({ x: x - fx * back, y: 0.85, z: z - fz * back }, { x: x - fx * (back + 0.75), y: 0.85, z: z - fz * (back + 0.75) }, "#bfe6ff");
        }
        spawnBurst(x, 0.6, z, "#bfe6ff", 0.7, 0.22);
      }),
      fxBus.on("transformStart", (d) => {
        spawnShockwave((d.x as number) ?? 0, (d.z as number) ?? 0, "#8fd3ff", 3.2, 0.55);
      }),
      fxBus.on("evolve", (d) => {
        spawnShockwave((d.x as number) ?? 0, (d.z as number) ?? 0, "#b563ff", 2.6, 0.7);
        spawnBurst((d.x as number) ?? 0, 1, (d.z as number) ?? 0, TYPE_COLOR.evolve, 1.4, 0.6);
      }),
    ];
    return () => offs.forEach((o) => o());
  }, []);

  useFrame((_, dt) => {
    bursts.current.forEach((b, i) => {
      const mesh = burstMeshes.current[i];
      if (!mesh) return;
      if (!b.active) {
        mesh.visible = false;
        return;
      }
      b.t += dt;
      if (b.t >= b.dur) {
        b.active = false;
        mesh.visible = false;
        return;
      }
      const p = b.t / b.dur;
      mesh.visible = true;
      mesh.position.set(b.x, b.y, b.z);
      const s = b.scale * (0.3 + p * 1.4);
      mesh.scale.setScalar(s);
      const mat = mesh.material as THREE.MeshBasicMaterial;
      mat.opacity = 1 - p;
      mat.color = b.color;
    });

    tracers.current.forEach((t, i) => {
      const mesh = tracerMeshes.current[i];
      if (!mesh) return;
      if (!t.active) {
        mesh.visible = false;
        return;
      }
      t.t += dt;
      if (t.t >= t.dur) {
        t.active = false;
        mesh.visible = false;
        return;
      }
      const p = t.t / t.dur;
      mesh.visible = true;
      const mid = t.from.clone().lerp(t.to, 0.5);
      mesh.position.copy(mid);
      mesh.lookAt(t.to);
      mesh.rotateX(Math.PI / 2);
      const len = t.from.distanceTo(t.to);
      mesh.scale.set(1, len, 1);
      const mat = mesh.material as THREE.MeshBasicMaterial;
      mat.opacity = (1 - p) * 0.9;
      mat.color = t.color;
    });

    shockwaves.current.forEach((s, i) => {
      const mesh = shockwaveMeshes.current[i];
      if (!mesh) return;
      if (!s.active) {
        mesh.visible = false;
        return;
      }
      s.t += dt;
      if (s.t >= s.dur) {
        s.active = false;
        mesh.visible = false;
        return;
      }
      const p = s.t / s.dur;
      mesh.visible = true;
      mesh.position.set(s.x, 0.06, s.z);
      const r = 0.3 + p * s.maxRadius;
      mesh.scale.set(r, r, 1);
      const mat = mesh.material as THREE.MeshBasicMaterial;
      mat.opacity = (1 - p) * 0.8;
      mat.color = s.color;
    });
  });

  return (
    <group>
      {bursts.current.map((_, i) => (
        <mesh key={`b${i}`} ref={(el) => (burstMeshes.current[i] = el)} visible={false}>
          <sphereGeometry args={[0.22, 8, 8]} />
          <meshBasicMaterial color="#fff" transparent opacity={0} depthWrite={false} />
        </mesh>
      ))}
      {tracers.current.map((_, i) => (
        <mesh key={`t${i}`} ref={(el) => (tracerMeshes.current[i] = el)} visible={false}>
          <cylinderGeometry args={[0.02, 0.02, 1, 5]} />
          <meshBasicMaterial color="#fff" transparent opacity={0} depthWrite={false} />
        </mesh>
      ))}
      {shockwaves.current.map((_, i) => (
        <mesh key={`s${i}`} ref={(el) => (shockwaveMeshes.current[i] = el)} rotation={[-Math.PI / 2, 0, 0]} visible={false}>
          <ringGeometry args={[0.82, 1, 32]} />
          <meshBasicMaterial color="#fff" transparent opacity={0} depthWrite={false} side={THREE.DoubleSide} />
        </mesh>
      ))}
    </group>
  );
}
