# Roadmap — Ember Alcyone living-world CRPG

_Last updated:_ 2026-05-09
_Authoritative branch:_ `main`
_Active branch:_ `realignment-faz-0`

This roadmap supersedes the previous Sprint 0 roadmap (kept as
`DOCS/sprint-0-recon.md` for history). It is aligned with
`DOCS/mechanic-map-v1.md` (the 8-box living-world model) and gated
by `DOCS/agent-rules-v2.md` (five agent rules).

The structure is **12 phases** (`Faz 0` through `Faz 12`). Each phase
maps to one or two boxes of the mechanic map. A phase is done when its
acceptance sentence (`player can ...`) is true on the Unity scene.

## Faz 0 — Audit and realignment (active)

**Boxes**: meta. **Status**: in progress.

Goal: stop the magic-test micro-loop. Land canonical maps and rules so
Captain decomposes against the mechanic map rather than the magic enum.

Deliverables:

- `DOCS/alcyone-audit-2026-05-09.md`
- `DOCS/mechanic-map-v1.md`
- `DOCS/agent-rules-v2.md`
- `docs/reference/` mirrors the canonical PRDs and architecture notes
  from `msbel5/ember-rpg`
- `README.md` rewritten
- `docs/ROADMAP.md` rewritten (this file)
- `CRON_CODES.md @EMSPR` updated with the five new rules

Acceptance: a fresh contributor opens the repo, reads `README.md`,
`docs/ROADMAP.md`, `DOCS/mechanic-map-v1.md`, and within five minutes
can answer the four orientation questions in `README.md`.

## Faz 1 — Core Store reset

**Boxes**: WORLD, LIVING, MATTER. **Status**: queued.

Goal: replace `SliceWorldState.Player / Talker / Merchant / Guard /
Enemy` named fields with proper stores.

Deliverables:

- `ActorStore : Dictionary<ActorId, ActorRecord>` with components
  (Position, Vitals, Skill, Inventory, Memory, Schedule)
- `ItemStore : Dictionary<ItemId, ItemRecord>` with material + quality
- `SiteStore : Dictionary<SiteId, SiteRecord>` for region / settlement
  / dungeon
- `FactionStore : Dictionary<FactionId, FactionRecord>` (empty seed,
  populated in Faz 6)
- `WorldEvent` typed event log + `ReasonTrace`
- Migration of existing `SliceWorldState` consumers; the named fields
  become deprecated views over the stores
- Deterministic save/load round-trip

Acceptance: `player can spawn a guard, talk to it, then walk to a
second site and watch the same guard remembered across save/load`.

## Faz 2 — Recipe + Worksite (first PROCESS slice)

**Boxes**: PROCESS, MATTER. **Status**: queued.

Goal: introduce the universal "transformation" verb. Smelt iron ingot
from ore + fuel at a furnace.

Deliverables:

- `RecipeDef` (input items, output items, worksite, skill, time)
- `Worksite` component on a site cell
- `RecipeSystem` tick: resolve eligible workers, consume input, advance
  timer, produce output, write `EventLog`
- A starter recipe: `SmeltIronIngot`
- Deterministic test: 2 ore + 1 fuel + 40 ticks = 1 IronIngot at
  furnace

Acceptance: `player can craft an iron ingot from ore and fuel and
watch the stockpile increase`.

## Faz 3 — Job assignment

**Boxes**: PROCESS, LIVING. **Status**: queued.

Goal: actors pick jobs. The economy starts to move on its own.

Deliverables:

- `JobBoard` per site
- `Schedule` component carries `currentJob`
- Priority + skill matching
- Pathing to the worksite
- Idle behaviour when no jobs
- A second recipe (`BakeBread`) so jobs compete

Acceptance: `player can set 2 actors to "smith" priority 1, watch
both queue at the furnace, and produce 4 ingots in a deterministic
day`.

## Faz 4 — Colony needs (hunger / fatigue / mood)

**Boxes**: LIVING, PROCESS. **Status**: queued.

Goal: actors need food and rest. The simulation gets pressure.

Deliverables:

- `Needs` component (hunger, fatigue, thirst)
- `Mood` component (single 0..100 scalar derived from needs + memory)
- `NeedsSystem` tick raises hunger/fatigue
- Eat / sleep recipes consume from `Inventory`, restore needs
- Refusal logic when hunger > threshold

Acceptance: `player can let an actor go three days without food, see
mood fall, see them refuse to work, and recover after a meal`.

## Faz 5 — Plant growth + Season

**Boxes**: TIME, PROCESS. **Status**: queued.

Goal: time has consequence. Spring planting, summer harvest.

Deliverables:

- `Season` enum + `GameTime` advance
- `Soil` + `Plant` components
- `PlantGrowthSystem` ticks each in-game day
- `WorldProcess` shape for non-crafting transformations
- Wheat seed -> sapling -> ripe -> harvested
- Snow halts growth

Acceptance: `player can wait until spring, plant wheat, harvest in
summer, and see food stockpile rise`.

## Faz 6 — Trade routes + Faction (first SOCIETY slice)

**Boxes**: SOCIETY, TIME. **Status**: queued.

Goal: settlements move goods between each other on their own.

Deliverables:

- `Settlement` site type with `Stock` and `Demand`
- `TravelEdge` between settlements with danger and travel time
- `TradeRouteSystem` ticks daily: pick caravan, transit, deliver
- `Price` derived from local demand + supply + faction
- `Caravan` actor with route memory
- Banditry / shortage edge cases (basic)

Acceptance: `player can stand at the city gate, watch a caravan
arrive from a nearby settlement, see prices drop after delivery`.

## Faz 7 — Combat + Equipment integration

**Boxes**: CRPG, MATTER. **Status**: queued.

Goal: existing combat scaffolding (Sprint 4 Faz 2) plugged into the
new stores.

Deliverables:

- `WeaponItem` + `ArmorItem` extend `ItemRecord`
- `Equipment` component on `ActorRecord`
- `CombatSystem` reads equipment, computes hit, damage, durability
- Weapon wears, armor protects, both can break
- Death drops inventory back into the world

Acceptance: `player can equip a sword, fight a bandit in the woods,
loot the body, return to town`.

## Faz 8 — Data-driven magic (the unblock)

**Boxes**: CRPG. **Status**: queued, unlocks the magic micro-loop.

Goal: promote magic from the `SpellEffectKind` enum to data rows.

Deliverables:

- `EffectDefinition` (effect_id, operation, resource, mode, magnitude
  formula, duration, target rule, save rule)
- Operation handlers: `ModifyResourceOperation`,
  `ApplyBuffOperation`, `SpawnLightOperation`,
  `ModifyDispositionOperation`, `ApplyConditionOperation`,
  `MoveItemOperation`, `StartWorldProcessOperation`
- Re-express the existing 7 enum entries as data rows
- One new spell shipped purely as data (no C# change) to prove the
  pipeline
- The existing test matrix migrates row-by-row

Acceptance: `player can cast a new spell that exists only in data
(no C# branch added), see it succeed and write to the EventLog`.

## Faz 9 — Dialogue + Memory + Faction reputation

**Boxes**: CRPG, LIVING, SOCIETY. **Status**: queued.

Goal: NPCs remember and react.

Deliverables:

- `MemoryComponent` writes from `EventLog`
- `Disposition` per actor pair, modified by memory + faction
- Dialogue topics gated by disposition, memory, faction
- Crime ledger: theft / assault witnessed -> faction guard reacts
- Persuade / intimidate skill checks roll against disposition

Acceptance: `player can witness an NPC remember a crime committed
two days ago and refuse to trade`.

## Faz 10 — DM Query API (LLM-readable views)

**Boxes**: AI / DM. **Status**: queued.

Goal: expose deterministic typed views the LLM can read without ever
mutating state.

Deliverables:

- `ActorView`, `MemoryView`, `WorldEvent`, `ReasonTrace` projections
- `Query` / `Chance` / `Roll` / `Mutation` typed tools
- Mutation always routes through validation + system tick
- Mock LLM client that exercises the API in tests
- A debug HUD dump shows what the LLM would see

Acceptance: `player can press F9, see the same world snapshot the
LLM has, including memory facts and faction state`.

## Faz 11 — Unity visual layer (parallel from Faz 1)

**Boxes**: Unity-only (presentation). **Status**: incremental.

Goal: every Faz 1-10 system has a visible representation in Unity.

Deliverables (per phase):

- `ActorView` reads `ActorRecord` and renders sprite, position,
  vitals bar
- `ItemView` reads `ItemStore`, renders dropped items
- `SiteView` renders region map, settlements, travel edges
- `DebugHUD` shows tick number, season, weather, faction overlay
- Camera + Input bindings minimal

Note: Faz 11 PRs run in parallel with Faz 1-10. Each system gets a
view as it lands. Unity work never blocks Core work.

Acceptance: every previous phase has a one-screenshot Unity proof in
`DOCS/screenshots/<phase>.png`.

## Faz 12 — LLM / NPC fallback flavour

**Boxes**: AI / DM. **Status**: queued, last layer.

Goal: bring the local Qwen3:1.7B (or Copilot fallback) online for
flavour-only NPC speech and ambient narration.

Deliverables:

- `NpcAgent` reads `ActorView` and proposes lines via `say_line`
- `DmAgent` proposes ambient narration via the DM Query API
- All proposals validated; nothing writes the world directly
- Cost ceiling: every flavour call is local-first; cloud fallback
  only when local fails

Acceptance: `player can stand in a tavern, hear three NPCs
exchange context-aware lines, none of which mutate the world`.

## Sprint scoping rule

A sprint targets one phase, occasionally two adjacent phases (for
example `Faz 1` plus `Faz 11` Unity views for the new stores). A
sprint may not skip phases. A sprint that produces zero
product-visible PRs is failed and is not a candidate for promotion.

## Out of scope (for now)

- Multiplayer
- Procedural narrative generators above the DM layer
- Mod tooling (will follow once `EffectDefinition` and `RecipeDef`
  are stable)
- Pi-side runtime of the actual game (Pi runs the agent crew, not
  the game)
