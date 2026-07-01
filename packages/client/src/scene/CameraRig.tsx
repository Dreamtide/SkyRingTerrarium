import { useEffect, useRef } from "react";
import { useFrame, useThree } from "@react-three/fiber";
import * as THREE from "three";
import { useGameStore } from "../state/store";
import { damp, lerpAngle } from "../util/math";
import { fxBus } from "../net/fx";

const DIST = 9.5;
const HEIGHT = 5.4;
const LOOK_HEIGHT = 1.4;
const BASE_FOV = 52;

export default function CameraRig() {
  const { camera } = useThree();
  const room = useGameStore((s) => s.room);
  const mySessionId = useGameStore((s) => s.mySessionId);
  const smoothedYaw = useRef(0);
  const target = useRef(new THREE.Vector3());
  const desired = useRef(new THREE.Vector3());
  const shake = useRef(0);
  const fovPunch = useRef(0);

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

    smoothedYaw.current = lerpAngle(smoothedYaw.current, pyaw, 4, dt);

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

    target.current.set(
      damp(target.current.x, px, 8, dt),
      damp(target.current.y, LOOK_HEIGHT, 8, dt),
      damp(target.current.z, pz, 8, dt)
    );
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
