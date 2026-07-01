import { useEffect, useRef } from "react";
import { useFrame } from "@react-three/fiber";
import * as THREE from "three";
import { fxBus } from "../net/fx";

// Numbers are drawn onto small canvas textures rather than using a text-layout
// library, so this has zero dependency on any font being loadable over the
// network (see the earlier drei/troika Unicode-glyph incident in NamePlate).
const POOL_SIZE = 24;
const LIFETIME = 0.85;

interface Slot {
  active: boolean;
  t: number;
  x: number;
  y: number;
  z: number;
  driftX: number;
  driftZ: number;
  canvas: HTMLCanvasElement;
  ctx: CanvasRenderingContext2D;
  texture: THREE.CanvasTexture;
}

function makeSlot(): Slot {
  const canvas = document.createElement("canvas");
  canvas.width = 128;
  canvas.height = 64;
  const ctx = canvas.getContext("2d")!;
  const texture = new THREE.CanvasTexture(canvas);
  texture.minFilter = THREE.LinearFilter;
  texture.magFilter = THREE.LinearFilter;
  return { active: false, t: 0, x: 0, y: 0, z: 0, driftX: 0, driftZ: 0, canvas, ctx, texture };
}

function drawNumber(slot: Slot, text: string, color: string, big: boolean) {
  const { ctx, canvas } = slot;
  ctx.clearRect(0, 0, canvas.width, canvas.height);
  ctx.font = `900 ${big ? 34 : 27}px system-ui, sans-serif`;
  ctx.textAlign = "center";
  ctx.textBaseline = "middle";
  ctx.lineWidth = 7;
  ctx.lineJoin = "round";
  ctx.strokeStyle = "rgba(5,7,13,0.92)";
  ctx.strokeText(text, canvas.width / 2, canvas.height / 2);
  ctx.fillStyle = color;
  ctx.fillText(text, canvas.width / 2, canvas.height / 2);
  slot.texture.needsUpdate = true;
}

export default function DamageNumbers() {
  const slotsRef = useRef<Slot[] | null>(null);
  if (!slotsRef.current) slotsRef.current = Array.from({ length: POOL_SIZE }, makeSlot);
  const slots = slotsRef.current;
  const spriteRefs = useRef<(THREE.Sprite | null)[]>([]);
  const cursor = useRef(0);

  useEffect(() => {
    const off = fxBus.on("damage", (d) => {
      const amount = Math.round((d.amount as number) ?? 0);
      if (amount <= 0) return;
      const target = d.target as string;
      const i = cursor.current;
      cursor.current = (i + 1) % POOL_SIZE;
      const slot = slots[i];
      slot.active = true;
      slot.t = 0;
      slot.x = (d.x as number) + (Math.random() - 0.5) * 0.5;
      slot.y = (d.y as number) ?? 1;
      slot.z = (d.z as number) + (Math.random() - 0.5) * 0.5;
      slot.driftX = (Math.random() - 0.5) * 0.8;
      slot.driftZ = (Math.random() - 0.5) * 0.8;
      const big = amount >= 25;
      const color = target === "player" ? "#ff6b6b" : big ? "#ffd23f" : "#fff4d6";
      drawNumber(slot, String(amount), color, big);
    });
    return () => off();
  }, [slots]);

  useFrame((_, dt) => {
    slots.forEach((slot, i) => {
      const sprite = spriteRefs.current[i];
      if (!sprite) return;
      if (!slot.active) {
        sprite.visible = false;
        return;
      }
      slot.t += dt;
      if (slot.t >= LIFETIME) {
        slot.active = false;
        sprite.visible = false;
        return;
      }
      const p = slot.t / LIFETIME;
      sprite.visible = true;
      sprite.position.set(slot.x + slot.driftX * p, slot.y + p * 1.5, slot.z + slot.driftZ * p);
      const grow = p < 0.15 ? p / 0.15 : 1;
      const scale = 0.5 * grow;
      sprite.scale.set(scale * 1.5, scale * 0.75, 1);
      const mat = sprite.material as THREE.SpriteMaterial;
      mat.opacity = p > 0.55 ? Math.max(0, 1 - (p - 0.55) / 0.45) : 1;
    });
  });

  return (
    <group>
      {slots.map((slot, i) => (
        <sprite key={i} ref={(el) => (spriteRefs.current[i] = el)} visible={false}>
          <spriteMaterial map={slot.texture} transparent depthWrite={false} />
        </sprite>
      ))}
    </group>
  );
}
