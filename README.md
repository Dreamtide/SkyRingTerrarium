# D.R.E.A.M. — Dogs Ride Evolving Attack Mechs

A 3D co-op action roguelite for 1–5 players, playable in the browser on desktop, tablet or phone. Pilots
drop into procedurally-arranged arenas as transforming mechs (robot ⇄ vehicle), fight off Decepticon-style
drone waves, salvage parts to upgrade their mech, and raise a magic hound companion that evolves based on
the jobs you give it.

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
| Move | WASD / arrows | left virtual joystick |
| Attack (robot) / Overdrive (vehicle) | Space / left click | ⊙ button |
| Transform robot ⇄ vehicle | F / E | ⇄ button |
| Dash (brief i-frames) | Shift / K | ⚡ button |
| Assign dog task | 1–5 | bottom task row |

Facing always tracks your movement heading and the mech auto-targets the nearest enemy in front of it, so
there's no separate aim stick to fight with on mobile.

## Core systems

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
- **Co-op netcode**: the server is authoritative over a fixed-rate simulation; the client interpolates all
  entities (including the local player) toward the latest network state for smooth motion without full
  client-side prediction/reconciliation. This keeps the system simple and correct; see "Next steps" below
  for the natural follow-up.
- **Cross-device rendering**: quality tier (shadow resolution, DPR, postprocessing) is auto-selected from
  device type/CPU core count, and the whole HUD/touch-control layout is responsive down to phone width.

## Project layout

```
packages/
  shared/   game data & balance tables shared by client and server
  server/   Colyseus room, simulation (enemy AI, dog AI, combat), schema state
  client/   React + react-three-fiber game client, HUD, input, audio
```

## Known limitations / natural next steps

- No client-side prediction/reconciliation for the local player yet — fine at LAN/low latency, but a genuine
  next step for play over the open internet.
- No dedicated matchmaking/hosting config included; you provide the Node host + static host for a public
  deployment.
- Procedural low-poly art style is a deliberate scope choice (instant load, no asset pipeline) rather than
  hand-authored character art.
- Single JS bundle (~440KB gzipped) — code-splitting the Three.js/postprocessing chunk would improve first
  load on slow mobile connections.
