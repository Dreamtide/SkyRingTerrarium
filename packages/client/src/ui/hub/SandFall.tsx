import { useEffect, useRef } from "react";
import { SandWallet, SAND_COLOR } from "@dream/shared";

const GRID_W = 72;
const GRID_H = 44;
const CELL = 4;

/**
 * A live falling-sand simulation of the player's cybersand reserves. Each grain in the
 * pile is real inventory: the pile's composition mirrors the wallet's ratios, grains
 * pour in when sand is earned and drain out of the pile when spent - the wallet IS the
 * toy. Classic cellular-automaton rules (fall, then slide diagonally) at ~30fps on a
 * tiny grid, so it costs almost nothing.
 */
export default function SandFall({ wallet }: { wallet: SandWallet }) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const gridRef = useRef<Uint8Array>(new Uint8Array(GRID_W * GRID_H));
  const spawnQueue = useRef<number[]>([]);
  const walletRef = useRef(wallet);
  const targetCounts = useRef([0, 0, 0, 0]);

  useEffect(() => {
    walletRef.current = wallet;
    // Map the wallet to a target number of visible grains per type (log-ish so big
    // wallets don't overflow the pile but growth always shows).
    const types: (keyof SandWallet)[] = ["ferrite", "volt", "pyros", "chroma"];
    targetCounts.current = types.map((t) => Math.min(320, Math.round(Math.sqrt(Math.max(0, wallet[t])) * 6)));
  }, [wallet]);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext("2d")!;
    const grid = gridRef.current;
    const colors = [SAND_COLOR.ferrite, SAND_COLOR.volt, SAND_COLOR.pyros, SAND_COLOR.chroma];
    let raf = 0;
    let tick = 0;

    function currentCounts(): number[] {
      const counts = [0, 0, 0, 0];
      for (let i = 0; i < grid.length; i++) {
        if (grid[i] > 0) counts[grid[i] - 1]++;
      }
      return counts;
    }

    function step() {
      tick++;
      // settle: bottom-up scan, fall straight then diagonal
      for (let y = GRID_H - 2; y >= 0; y--) {
        const leftFirst = (tick + y) % 2 === 0;
        for (let x = 0; x < GRID_W; x++) {
          const i = y * GRID_W + x;
          const v = grid[i];
          if (v === 0) continue;
          const below = (y + 1) * GRID_W + x;
          if (grid[below] === 0) {
            grid[below] = v;
            grid[i] = 0;
            continue;
          }
          const d1 = leftFirst ? -1 : 1;
          const d2 = -d1;
          const bx1 = x + d1;
          const bx2 = x + d2;
          if (bx1 >= 0 && bx1 < GRID_W && grid[(y + 1) * GRID_W + bx1] === 0) {
            grid[(y + 1) * GRID_W + bx1] = v;
            grid[i] = 0;
          } else if (bx2 >= 0 && bx2 < GRID_W && grid[(y + 1) * GRID_W + bx2] === 0) {
            grid[(y + 1) * GRID_W + bx2] = v;
            grid[i] = 0;
          }
        }
      }

      // reconcile pile composition with the wallet: pour in missing grains, evaporate spent ones
      if (tick % 3 === 0) {
        const counts = currentCounts();
        for (let t = 0; t < 4; t++) {
          if (counts[t] < targetCounts.current[t]) spawnQueue.current.push(t + 1);
          else if (counts[t] > targetCounts.current[t]) {
            for (let i = 0; i < grid.length; i++) {
              if (grid[i] === t + 1) {
                grid[i] = 0;
                break;
              }
            }
          }
        }
      }
      const spawn = spawnQueue.current.shift();
      if (spawn) {
        const x = Math.floor(GRID_W * (0.3 + Math.random() * 0.4));
        if (grid[x] === 0) grid[x] = spawn;
        else spawnQueue.current.unshift(spawn);
      }

      // draw
      ctx.clearRect(0, 0, canvas!.width, canvas!.height);
      for (let y = 0; y < GRID_H; y++) {
        for (let x = 0; x < GRID_W; x++) {
          const v = grid[y * GRID_W + x];
          if (v === 0) continue;
          ctx.fillStyle = colors[v - 1];
          ctx.fillRect(x * CELL, y * CELL, CELL - 0.5, CELL - 0.5);
        }
      }
      raf = requestAnimationFrame(() => setTimeout(step, 16));
    }
    step();
    return () => cancelAnimationFrame(raf);
  }, []);

  return (
    <canvas
      ref={canvasRef}
      width={GRID_W * CELL}
      height={GRID_H * CELL}
      style={{ width: "100%", borderRadius: 10, background: "rgba(5,7,13,0.55)", border: "1px solid var(--border)" }}
    />
  );
}
