import { useEffect, useRef } from "react";
import { useFrame, useThree } from "@react-three/fiber";
import * as THREE from "three";
import { useGameStore } from "../state/store";
import { inputManager } from "../input/InputManager";

/**
 * Desktop-only: converts the mouse cursor into an aim yaw for the local mech, feeding
 * it into InputManager every frame so facing is independent of movement.
 *
 * Deliberately uses the raycast ray's *direction* rather than intersecting it with the
 * ground plane. Ground-plane intersection distance blows up as the ray approaches
 * parallel to the ground (i.e. whenever the cursor is anywhere near the horizon on
 * screen), which made aim sensitivity wildly non-uniform - barely responsive near the
 * top of the screen, hair-trigger near the bottom. The ray direction itself has no such
 * singularity and gives smooth, uniform sensitivity across the whole screen.
 */
export default function DesktopAimController() {
  const { camera, pointer } = useThree();
  const room = useGameStore((s) => s.room);
  const mySessionId = useGameStore((s) => s.mySessionId);
  const raycaster = useRef(new THREE.Raycaster());

  useEffect(() => {
    return () => inputManager.setDesktopAim(null);
  }, []);

  useFrame(() => {
    const state = room?.state as unknown as { players: Map<string, { x: number; z: number }> } | undefined;
    const me = state?.players?.get(mySessionId);
    if (!me) return;
    raycaster.current.setFromCamera(pointer, camera);
    const dir = raycaster.current.ray.direction;
    if (Math.hypot(dir.x, dir.z) < 0.02) return;
    inputManager.setDesktopAim(Math.atan2(dir.x, dir.z));
  });

  return null;
}
