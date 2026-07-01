import { useEffect, useMemo, useRef } from "react";
import { useFrame, useThree } from "@react-three/fiber";
import * as THREE from "three";
import { useGameStore } from "../state/store";
import { damp, lerpAngle } from "../util/math";
import { fxBus } from "../net/fx";
import { inputManager } from "../input/InputManager";

const DIST = 9.5;
const HEIGHT = 5.4;
const LOOK_HEIGHT = 1.4;
const BASE_FOV = 52;
// Desktop uses a fixed ARPG-style orbit angle and a perfectly rigid rotation (position
// translates to follow the player, but the camera never rotates) rather than orbiting to
// stay behind the player's facing or using lookAt() toward a separately-damped target.
// This matters beyond taste: DesktopAimController raycasts the mouse through this same
// camera to compute aim yaw, and aim yaw drives player facing. Any rotation of the camera
// - even subtle wobble from a lookAt() target that lags position by a different damping
// rate during fast movement - changes the ray direction, which changes aim, which changes
// movement direction, which changes the camera's target again: a feedback loop that shows
// up as aim/movement direction silently drifting during sustained input. A perfectly rigid
// (translate-only) camera has no rotation to wobble, so the loop can't exist.
const DESKTOP_ORBIT_YAW = Math.PI * 0.15;
// Direction from the camera to its look-at point: horizontally back toward the player
// (opposite of the camera's offset-from-player vector), and down by the camera/look
// height difference - i.e. exactly the geometry of "camera sits behind and above the
// player, tilted down to look at them", but expressed as a fixed vector instead of a
// per-frame lookAt(player) so it never depends on the player's exact position.
const DESKTOP_LOOK_DIR = new THREE.Vector3(Math.sin(DESKTOP_ORBIT_YAW) * DIST, LOOK_HEIGHT - HEIGHT, Math.cos(DESKTOP_ORBIT_YAW) * DIST).normalize();

export default function CameraRig() {
  const { camera } = useThree();
  const room = useGameStore((s) => s.room);
  const mySessionId = useGameStore((s) => s.mySessionId);
  const smoothedYaw = useRef(0);
  const target = useRef(new THREE.Vector3());
  const desired = useRef(new THREE.Vector3());
  const shake = useRef(0);
  const fovPunch = useRef(0);
  const desktop = useMemo(() => !inputManager.isMobileLike(), []);

  useEffect(() => {
    const offs = ["hit", "ram", "downed"].map((t) =>
      fxBus.on(t, (data) => {
        if (!data.sessionId || data.sessionId === mySessionId) shake.current = Math.min(0.5, shake.current + 0.18);
      })
    );
    const offTransform = fxBus.on("transformStart", (data) => {
      if (data.sessionId === mySessionId) fovPunch.current = 1;
    });
    return () => {
      offs.forEach((o) => o());
      offTransform();
    };
  }, [mySessionId]);

  useFrame((_, dt) => {
    const state = room?.state as unknown as { players: Map<string, { x: number; z: number; yaw: number; alive: boolean }> } | undefined;
    const me = state?.players?.get(mySessionId) as { x: number; z: number; yaw: number } | undefined;

    const px = me?.x ?? 0;
    const pz = me?.z ?? 0;
    const pyaw = me?.yaw ?? 0;

    smoothedYaw.current = desktop ? DESKTOP_ORBIT_YAW : lerpAngle(smoothedYaw.current, pyaw, 4, dt);

    const back = new THREE.Vector3(Math.sin(smoothedYaw.current), 0, Math.cos(smoothedYaw.current)).multiplyScalar(-DIST);
    desired.current.set(px + back.x, HEIGHT, pz + back.z);

    if (shake.current > 0) {
      shake.current = Math.max(0, shake.current - dt * 1.6);
      desired.current.x += (Math.random() - 0.5) * shake.current;
      desired.current.y += (Math.random() - 0.5) * shake.current;
    }

    camera.position.x = damp(camera.position.x, desired.current.x, 6, dt);
    camera.position.y = damp(camera.position.y, desired.current.y, 6, dt);
    camera.position.z = damp(camera.position.z, desired.current.z, 6, dt);

    if (desktop) {
      // Rigid look direction: always "camera position + a constant offset", so rotation
      // never varies with the player's exact position - see DESKTOP_ORBIT_YAW comment.
      target.current.copy(camera.position).add(DESKTOP_LOOK_DIR);
    } else {
      target.current.set(
        damp(target.current.x, px, 8, dt),
        damp(target.current.y, LOOK_HEIGHT, 8, dt),
        damp(target.current.z, pz, 8, dt)
      );
    }
    camera.lookAt(target.current);

    if (fovPunch.current > 0) fovPunch.current = Math.max(0, fovPunch.current - dt * 2.4);
    const cam = camera as THREE.PerspectiveCamera;
    if (typeof cam.fov === "number") {
      const targetFov = BASE_FOV + fovPunch.current * 9;
      cam.fov = damp(cam.fov, targetFov, 9, dt);
      cam.updateProjectionMatrix();
    }
  });

  return null;
}
