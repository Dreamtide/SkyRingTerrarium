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

const BURST_COUNT = 26;
const TRACER_COUNT = 18;

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
  const burstMeshes = useRef<(THREE.Mesh | null)[]>([]);
  const tracerMeshes = useRef<(THREE.Mesh | null)[]>([]);
  const burstCursor = useRef(0);
  const tracerCursor = useRef(0);

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

  useEffect(() => {
    const offs = [
      fxBus.on("shot", (d) => {
        const from = d.from as { x: number; y: number; z: number };
        const to = d.to as { x: number; y: number; z: number };
        spawnTracer(from, to, "#8fd3ff");
        spawnBurst(to.x, to.y, to.z, "#8fd3ff", 0.5, 0.2);
      }),
      fxBus.on("hit", (d) => spawnBurst((d.x as number) ?? 0, 1, (d.z as number) ?? 0, TYPE_COLOR.hit, 0.7, 0.28)),
      fxBus.on("death", (d) => spawnBurst((d.x as number) ?? 0, 1, (d.z as number) ?? 0, TYPE_COLOR.death, 1.6, 0.5)),
      fxBus.on("ram", (d) => spawnBurst((d.x as number) ?? 0, 0.8, (d.z as number) ?? 0, TYPE_COLOR.ram, 1.1, 0.32)),
      fxBus.on("collect", (d) => spawnBurst((d.x as number) ?? 0, 1, (d.z as number) ?? 0, TYPE_COLOR.collect, 0.9, 0.4)),
      fxBus.on("bite", (d) => spawnBurst((d.x as number) ?? 0, 0.6, (d.z as number) ?? 0, TYPE_COLOR.bite, 0.5, 0.22)),
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
    </group>
  );
}
