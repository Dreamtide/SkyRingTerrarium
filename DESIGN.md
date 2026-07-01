# D.R.E.A.M. — Design Notes

## Pitch

Dogs Ride Evolving Attack Mechs. 1–5 pilots, each a transforming mech with their own magic hound, drop into
a short co-op "deployment" against waves of Decepticon-style drones. Gear comes from the field, not a menu;
the dog's personality comes from how you play, not a level-up screen.

## Why these design choices

**Single-stick, auto-aim combat.** A twin-stick or mouse-aim scheme is the "obvious" choice for a shooter,
but it's the first thing that breaks on a touchscreen. Facing tracks movement heading, and both the ranged
attack (robot form) and the dog's Hunt task soft-lock the nearest enemy in a forward cone. This makes the
combat identical to learn on a phone, a gamepad, or a keyboard, which is exactly what "playable across
devices" needs to mean in practice, not just "the page loads on mobile."

**Auto-equip parts instead of an inventory screen.** Early prototyping logic considered a classic loot
inventory with a compare/equip UI. That's a lot of screen real estate and taps on a phone mid-firefight. Instead,
picking up a part (or having your Scavenge hound deliver one) auto-equips it only if it's a strict upgrade
over what's already worn, using a simple weighted stat score. You always improve, you're never worse off,
and there's nothing to manage. The five rarity tiers and part-hue-tinted mech accents still give the
"look how decked out my mech is" payoff without the menu.

**The dog evolves from play style, not XP levels.** Each of the five tasks (Guard / Hunt / Scavenge / Scout
/ Mend) accumulates its own affinity meter while the dog is actively doing that job well (not just assigned
to it — e.g. Guard only gains XP while something is actually threatening the guard point). Cross a first
threshold and the dominant affinity locks in a Stage 2 form; cross a second threshold with two affinities
close together and you get one of five hand-authored Stage 3 hybrids (e.g. Guard+Mend → **Sentinel Hound**,
which auto-shields the lowest-health ally). This is deliberately not a stat-max grind — it's meant to be
legible ("I kept sending her to fetch loot, and now she's a Magpie Hound with a huge pickup radius") and to
reward reading the situation each deployment rather than following one optimal build.

**The dog can't die.** It's a magic construct that Decepticon weapons pass through. This lets a Guard/Bastion
hound function as a real aggro-drawing tank without needing to model dog HP, downs, or dog-specific revives
— one less system, and it keeps the pet feeling like a companion rather than another fragile ally to babysit.

**Server-authoritative, client-interpolated.** For a co-op game (not a competitive shooter), simplicity and
correctness beat shaving off the last 50ms of input latency. The Colyseus room runs the entire simulation
(movement, collision, AI, combat, wave state) at a fixed 20Hz tick; clients send input and interpolate
everything — including their own mech — toward the authoritative state. This means there is exactly one
source of truth for "did that hit land," which matters a lot once you have up to 5 players and a boss doing
simultaneous math. Full client-side prediction for the local player is a natural, scoped follow-up (noted in
the README) once the game needs to hold up over real-world internet latency rather than LAN play.

**Procedural low-poly everything.** No character art, no rigging pipeline, no texture downloads — every
mech, hound, drone and prop is built from primitives at runtime and colored by data (player color, part
rarity/hue, dog evolution stage). This is a deliberate trade: it means the "high fidelity" budget goes into
lighting, bloom, materials, procedural animation (walk cycles, transform snap, hit flashes, dash trails) and
responsiveness instead of asset production, and it means the game is a few hundred KB and loads instantly on
any device rather than requiring a multi-hundred-MB download.

**Deployment structure.** `Wave → Supply Cache breather → Wave → … → Boss → Victory/Defeat`, scaling enemy
count/health with wave index and squad size. The "roguelight" framing (lighter than a full roguelike) comes
from: no permadeath within a run (downs are auto-revived by a nearby ally), a short session length (a
handful of waves + a boss), and a persistent Core Shard currency that's meant to seed a meta-progression
layer (cosmetic/starting-loadout unlocks) beyond what's implemented here.

## Data model (see `packages/shared/src`)

- `types.ts` / `balance.ts` — enums and every tunable number in one place, shared by client and server so
  client-side prediction math (movement speed, etc.) can never drift from the authoritative values.
- `parts.ts` — part archetypes × 5 rarities = the full loot table, plus the weighted-rarity roll function.
- `dogEvolution.ts` — the affinity thresholds, the five Stage 2 forms, and the five hand-authored Stage 3
  hybrids, plus a fallback "Prime Hound" for affinity spreads that don't match a defined hybrid.
- `waves.ts` / `arena.ts` — wave composition scaling and the seeded arena obstacle layout generator (same
  seed on client and server, so the client can render the exact layout the server is simulating collision
  against without sending the whole layout over the wire).
