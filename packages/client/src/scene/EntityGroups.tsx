import { useGameStore } from "../state/store";
import RobotPlayer from "../entities/RobotPlayer";
import DogCompanion from "../entities/DogCompanion";
import Enemy from "../entities/Enemy";
import Pickup from "../entities/Pickup";

export function Players() {
  const playerIds = useGameStore((s) => s.playerIds);
  const mySessionId = useGameStore((s) => s.mySessionId);
  return (
    <>
      {playerIds.map((id) => (
        <RobotPlayer key={id} sessionId={id} isLocal={id === mySessionId} />
      ))}
    </>
  );
}

export function Dogs() {
  const playerIds = useGameStore((s) => s.playerIds);
  return (
    <>
      {playerIds.map((id) => (
        <DogCompanion key={id} ownerSessionId={id} />
      ))}
    </>
  );
}

export function Enemies() {
  const enemyIds = useGameStore((s) => s.enemyIds);
  return (
    <>
      {enemyIds.map((id) => (
        <Enemy key={id} id={id} />
      ))}
    </>
  );
}

export function Pickups() {
  const pickupIds = useGameStore((s) => s.pickupIds);
  return (
    <>
      {pickupIds.map((id) => (
        <Pickup key={id} id={id} />
      ))}
    </>
  );
}
