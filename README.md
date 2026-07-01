# D.R.E.A.M. — Dogs Ride Evolving Attack Mechs

A 3D co-op action roguelite for 1–5 players, playable in the browser on desktop, tablet or phone. From a
peaceful tropical oceanside home base, pilots deploy into procedurally-generated biome arenas as
transforming mechs (robot ⇄ vehicle), fight off Decepticon-style drone waves, vacuum up the cybersand that
enemies spill, and raise magic hound companions that evolve based on the jobs you give them. Everything you
earn persists: sand banks into permanent garage upgrades, hounds keep their growth and bond between runs.

See [`DESIGN.md`](./DESIGN.md) for the full design rationale.

## Tech stack

- **Client**: React + [react-three-fiber](https://docs.pmnd.rs/react-three-fiber) / Three.js, `@react-three/postprocessing`
  (bloom/vignette/AA), Zustand for state, Vite build. All meshes are procedural (no external art assets),
  so the whole game loads instantly over the network.
- **Server**: [Colyseus](https://colyseus.io/) authoritative game server (Node/TypeScript) running a fixed
  20Hz simulation tick — movement, combat, enemy AI, dog AI, wave/run state machine.
- **Shared**: a `@dream/shared` package with the balance constants, part/evolution data tables, and the
  seeded arena-layout generator, imported by both client and server so they always agree.
- Everything is plain WebSocket/WebGL — no native build required, so the same URL runs on a phone, a
  tablet, or a desktop browser.

## Running it locally

```bash
npm install
npm run dev          # starts the Colyseus server (:2567) and the Vite dev client (:5173) together
```

Open `http://localhost:5173`. To play with others on your LAN, open `http://<your-machine-ip>:5173` from
their devices (phones included) — the client auto-detects the server host from the page URL.

Other useful scripts:

```bash
npm run typecheck    # typechecks shared, server and client
npm run build         # production build of all three packages (client output in packages/client/dist)
npm run dev:server    # server only
npm run dev:client    # client only
```

To deploy for real multi-device play over the internet, host `packages/server` (any Node host) and
`packages/client/dist` (any static host), and set `VITE_SERVER_URL` at client build time to the server's
public `wss://` URL.

## Controls

Single-stick, auto-aim combat — the same scheme works well on a touchscreen, keyboard, or controller:

| Action | Desktop | Touch |
| --- | --- | --- |
| Move | WASD / arrows (aim-relative) | left virtual joystick |
| Aim | mouse cursor (crosshair reticle) | facing follows movement |
| Attack (robot) / Overdrive (vehicle) | Space / left click | ⊙ button |
| Transform robot ⇄ vehicle | F / E | ⇄ button |
| Dash (brief i-frames) | Shift / K | ⚡ button |
| Assign dog task | 1–5 | bottom task row |

On desktop the mech faces your mouse cursor independently of movement (twin-stick style, with a fixed
ARPG camera). On touch, facing follows the movement heading. Both schemes feed the same server-side
forward-cone auto-targeting, so combat feels equivalent across devices.

## Core systems

- **The hub**: a tropical oceanside island (animated ocean shader, sunset sky, palms, your mech and hound
  idling on the beach) that is the game's home screen. The Garage, Kennel and Deploy flows all open from a
  single sparse action row - the combat HUD itself has a Full / Minimal / Off setting for people who want
  pure gameplay on screen.
- **Cybersand meta-progression**: four sand types are earned by *how* you play - pyros from kills, ferrite
  from surviving hits, volt from ground covered, chroma from directing your hound - and destroyed enemies
  physically spill grains you must drive over to vacuum up. Banked sand feeds per-mech permanent upgrade
  tracks in the Garage, whose reserves render as a live falling-sand simulation (the pile's composition IS
  your wallet: grains pour in as you earn and drain as you spend).
- **Mech classes**: Vanguard (starter), Juggernaut, Interceptor and Artillery - distinct stats, silhouettes
  and alt-modes, unlocked with sand. Pick your frame in the Garage before deploying.
- **The kennel**: hounds persist between runs - affinity XP, evolved forms and bond level all carry over.
  Bond (grown by bringing a hound on runs and feeding it chroma treats) multiplies its task effectiveness.
  Adopt multiple pups, name them, and choose who rides along on each deployment.
- **Biome arenas**: each run's seed generates a unique arena - one of four biomes (Nebula Verge, Ember
  Wastes, Verdant Ruin, Glacier Rift) with its own palette, sky, fog and obstacle set, on a lobed
  non-circular boundary shared exactly between server collision, client prediction and the ground shader.
- **Transforming mech**: each pilot's mech has a Robot form (auto-aimed ranged attacks) and a Vehicle form
  (fast, contact-damage "ram" with an Overdrive burst). Swapping plays a snap/spin transform animation with
  a brief input lock, matching the source material's fantasy without needing a hand-authored rig.
- **Parts & upgrades**: four slots (chassis / weapon / engine / plating) with five rarity tiers. Parts drop
  from enemies and supply crates; walking over one (or your Scavenge/Magpie/Phantom hound fetching it for
  you) auto-equips it if it's a strict upgrade over what you're wearing — no inventory menu to fight with
  mid-fight, which matters a lot on a phone screen.
- **The magic hound**: assign it Guard / Hunt / Scavenge / Scout / Mend. Doing a job well earns that job's
  affinity XP; enough of one affinity evolves the pup into a task-specialised Stage 2 form, and enough of
  two complementary affinities unlocks a Stage 3 hybrid. The dog is a magical construct — Decepticon fire
  passes through it, so a Guard-built hound can freely draw aggro as a decoy.
- **Deployments**: a run is `Wave → Supply Cache breather → Wave → … → Boss → Victory/Defeat`, scaling with
  wave index and squad size. Downed pilots aren't out — a nearby ally auto-revives them over a few seconds.
- **Co-op netcode**: the server is authoritative over a fixed-rate simulation. Remote players/enemies/dogs
  are interpolated toward the latest network state; the *local* player is predicted immediately from input
  using `stepPlayerMovement` (in `@dream/shared`) - the exact same movement/collision code the server runs -
  then continuously nudged (and hard-snapped on a big mismatch, e.g. a wave-transition teleport) toward the
  authoritative position as it arrives. Input feels instant; the server still has final say over everything.
- **Juice**: hit-flashes and a real death animation on enemies, floating damage numbers, muzzle flashes,
  dash after-image trails, transform shockwaves + a camera FOV punch, screen damage flash + low-health
  vignette, and a kill feed - all driven off a small server → client `fx` event stream.
- **Cross-device rendering**: quality tier (shadow resolution, DPR, postprocessing, starfield/dust density)
  is auto-selected from device type/CPU core count and adjustable from the in-game settings panel; the whole
  HUD/touch-control layout is responsive down to phone width.

## Project layout

```
packages/
  shared/   game data, balance tables, biome/arena generation, movement & stats
            (shared by client and server so prediction can never drift)
  server/   Colyseus room, simulation (enemy AI, dog AI, combat), profile store + hub API
  client/   React + react-three-fiber game client, hub scene, HUD, input, audio
```

Player profiles (sand, unlocks, upgrades, hounds) are stored server-side in `data/profiles.json`,
keyed by a client-generated device id. All spending/unlock validation happens on the server.

## Known limitations / natural next steps

- Prediction reconciles by nudging/snapping position rather than replaying a buffered input history, so it
  won't perfectly hide very high latency the way a full reconciliation system would - a reasonable trade for
  a co-op (not competitive) game, and a clear next step if it's ever needed.
- No dedicated matchmaking/hosting config included; you provide the Node host + static host for a public
  deployment.
- Procedural low-poly art style is a deliberate scope choice (instant load, no asset pipeline) rather than
  hand-authored character art.
- Single JS bundle (~440KB gzipped) — code-splitting the Three.js/postprocessing chunk would improve first
  load on slow mobile connections.
