# Ember — Alcyone living-world CRPG

This is the Unity / C# rewrite of Ember. It is intentionally not a port of
the older Godot/Python prototype at `msbel5/ember-rpg`. That repo is now
read-only reference (see `docs/reference/`).

The project's goal is a **deterministic living-world CRPG**:

- one player, dwarf-fortress-style colony in the background
- every actor on a schedule, every item with provenance
- weather, season, plant growth, trade routes, faction politics
- combat and magic on top of a real economy
- LLM (`Qwen3:1.7B` local, larger via Copilot fallback) used only for
  flavour: NPC dialogue, DM narration, ambient barks
- the simulation is always authoritative; LLM never writes the world

The game will be open source and free.

## Status (2026-05-21) — playable vertical slice, ugly

The backend is **complete through Faz 12**. The Unity Editor can now
press-Play through 10 wired Faz scenes plus a MainMenu, save/load via
PlayerPrefs, walk/strike/cast on a deterministic simulation. **Visually
it is rough** — placeholder sprites, programmatic flat-color HUD,
several scenes have unreachable exit points, Faz 12 is visually
overcrowded. The game is shippable as a prototype but is not yet a
showcase build.

### What works (verified press-Play)
- MainMenu → New Game / Continue / Quit (cursor + EventSystem self-heal
  at Awake)
- Faz 3 (Smithing Overworld) → Faz 4 (Colony Needs) → Faz 5 (Season
  Farm) → Faz 6 (Trade Market) → Faz 7 (Combat Dungeon) → Faz 8 (Ritual
  Hall) → Faz 9 (Tavern Dialog) → Faz 10 (Oracle Shrine) → Faz 11
  (Showroom Overview) → Faz 12 (Tavern Flavour LLM)
- WASD movement, mouse-look, E interact, mouse-click melee, Alpha 1-5
  spell cast, Tab inventory, Esc menu
- Deterministic save/load round-trip (PlayerPrefs `ember.save.v1`)
- 1320 EditMode tests green under
  `tools/validation/fallback/ValidationFallbackHarness.csproj` (Faz 1-12
  Domain + Simulation + Data coverage)

### Known visual / authoring issues (Mami playtest 2026-05-21)
- Visuals are **placeholder quality**: programmatic flat-color HUD by
  design (CombatHud builds Image rectangles, no parchment frames yet)
- **Faz 12 Tavern Flavour** scene is overcrowded: too many GameObjects,
  hard to navigate
- **Some Faz scenes lack reachable exit points** to next scene —
  portal collider needs repositioning per-scene
- Player rig collision: capsule height is tight on Y=0.08, occasional
  floor clipping at Awake
- Some portal Labels still show pink shader-error on old scenes — the
  builder is fixed, but old scenes need rebuild via Ember → Scenes
  menu OR manual font assignment
- No character creation flow: New Game drops the player straight into
  Faz 3 with hardcoded SliceWorldFactory defaults — class / birthsign /
  name input is a deliberate post-Faz-13 sprint

### What does NOT exist yet
- Character creation (class, birthsign, name, custom skill picker)
- Tutorial / wake-up intro
- Visual polish (real sprites instead of programmatic rectangles, UI
  parchment frames, post-processing)
- Audio (BGM, SFX) — `EmberAmbientAudio` component is wired but no
  clips assigned
- Animation: actors are static billboards
- AI/DM real-runtime: `NarrationServices.cs` is test-wired only, not
  the live tick chain

### Next-up backlog
1. **Polish branch**: per-scene exit-point fix, Faz 12 declutter, HUD
   sprite frames (the audit calls them P2)
2. **Faz 13 — Character Creation sprint**: wake-up scene, name input,
   class picker, birthsign picker, custom skill priorities
3. **Faz 14 — InputSystem migration**: replace legacy `UnityEngine.Input`
   with `com.unity.inputsystem` (decision recorded in
   `docs/input-handling-decision.md`)
4. **Faz 15 — Visual pass**: real sprites, parchment HUD frames,
   ambient audio clips, post-process

---

## Historical status (2026-05-09)

We were entering **Faz 0** — audit and realignment. Sprints 1 to 5 produced
real code (Domain + Simulation + Tests folders, magic effect resolution,
some combat scaffolding) but the cron loop drifted into a tight magic-test
matrix (PRs #62-#71) without producing player-visible capability.

Faz 0 lands the canonical maps and rules:

- `docs/alcyone-audit-2026-05-09.md` — what is in the repo, why the loop
  stalled, what to do about it
- `docs/mechanic-map-v1.md` — the **8-box living-world model** every
  sprint must target
- `docs/agent-rules-v2.md` — five new agent rules: product-visible
  increment, no speculative utility, data-driven effect, world-store
  promotion, playable proof
- `docs/ROADMAP.md` — the 12-phase plan from Core Store reset to LLM
  layer

Reference material from the older Godot/Python prototype lives in
`docs/reference/` so Captain (and human contributors) can decompose
against the canonical mechanic list rather than the bare magic enum.

## Engine + architecture (locked)

- **Engine**: Unity. msbel knows C#, the repo is already configured for
  it, and Unity gives us the fastest visual iteration.
- **Core**: Unity-independent `EmberCrpg.Domain` + `EmberCrpg.Simulation`
  + `EmberCrpg.Data` assemblies. They live under `Assets/Scripts/` and
  carry **no `using UnityEngine`**. Persistence does not yet have its
  own assembly; save/load lands in Faz 1 alongside the Core Store reset
  (planned name: `Ember.Core` umbrella, planned split:
  `Domain / Simulation / Persistence / Tests`).
- **Unity layer**: only Scene, Input, Camera, UI, ViewModels,
  ActorView/ItemView, DebugVisualizer. Today this lives in
  `Assets/Scripts/Presentation/` (asmdef `EmberCrpg.Presentation`).
- **Composition over inheritance**: `EntityId` plus components
  (`PositionComponent`, `MaterialComponent`, `InventoryComponent`,
  `VitalsComponent`, ...).
- **Determinism**: `DeterministicRng` seeded per tick. Replays must be
  bit-stable.
- **Data-driven**: new spells, new recipes, new buffs ship as data rows.
  C# changes only when a new operation kind is required.

## How sprints work

> The paths below under `/home/msbel/...` and `~/.openclaw/...` are
> Captain-side configuration on Mami's Raspberry Pi 5; they are **not**
> tracked files in this repo. Human contributors do not need them to
> build, test, or play the game. They are listed for transparency about
> how the agent crew schedules work.

1. The **Captain** cron (`@EMSPR`) reads
   `/home/msbel/.openclaw/workspace/CRON_CODES.md` and decomposes the
   active sprint phase against `docs/mechanic-map-v1.md`. Every atom
   row carries a box tag like `[box=PROCESS]`.
2. The **Builder** writes one atom (or a small bundle of atoms) and
   opens a PR.
3. The **Inspector** reviews. AI bot reviews (Copilot, CodeRabbit,
   ChatGPT-Codex) are queued at
   `~/.openclaw/state/pr_bot_reviews.jsonl` and Captain processes them
   before picking a fresh atom.
4. Sprint promotion is gated by `agent-rules-v2`: at least one
   product-visible PR per sprint, no new `SliceWorldState` named
   fields, no new `SpellEffectKind` enum entries.
5. Every fifth PR carries a `player can ...` acceptance sentence and
   either a screenshot, replay log, debug HUD dump, or a playtest
   note.

## Agent governance (must-read for Captain before every atom)

These four documents form the control plane that keeps Captain on-track
and prevents the Sprint-5 magic micro-loop from recurring. Captain reads
all four before kicking off any atom and before opening any PR.

- [docs/EMBER_VISION_NOTES_MAMI.md](docs/EMBER_VISION_NOTES_MAMI.md) —
  operating constraints (Phase fences), 9-point Vision anchors, and
  Mami's verbatim intent. Source of author intent. Mechanic docs remain
  canonical for implementation; this file is consulted when atom-map
  decomposition has ambiguity.
- [docs/agent-rules-v2.md](docs/agent-rules-v2.md) — Rules 1-9.
  Required reading. Rule 6 (hard fail paths) and Rule 8 (anti-drift
  halt) are the hard tripwires. Rule 9 specifies the mandatory PR body
  audit fields, enforced via `.github/PULL_REQUEST_TEMPLATE.md`.
- [docs/inspector-audit-checklist.md](docs/inspector-audit-checklist.md) —
  the checklist Inspector applies to every Captain PR. Captain
  self-checks against this before opening a PR. Failure-escalation
  table at the bottom tells Inspector when to revert versus when to
  request changes.
- [The active sprint atom map](docs/sprint-faz-4-atom-map.md) — top-of-
  file **Debt ledger** is a gate, not a footnote. Before kicking off
  the next atom, Captain takes one action against the ledger (close /
  advance / defer) and records it in the kickoff doc.

Mami territory: `Assets/Scenes/`, `Assets/Art/`, `Assets/Prefabs/`,
`docs/screenshots/`, the Unity-bound parts of `Assets/Scripts/Presentation/`,
and any binary asset. Captain ships in `agent/*` branches; Mami ships in
`mami/*` branches. Visual proof of `player can ...` acceptance sentences
is Mami's job, not Captain's — Captain proves with deterministic replay
logs and event-log dumps in C# tests, never with screenshots.

## Repo layout

Actual on-disk layout as of Faz 0:

```
alcyone-ember-rpg/
├─ Assets/
│  ├─ Scripts/
│  │  ├─ Domain/         pure C# domain types — asmdef EmberCrpg.Domain
│  │  ├─ Simulation/     systems, stores, tick loop — asmdef EmberCrpg.Simulation
│  │  ├─ Data/           data-driven definitions + loaders — asmdef EmberCrpg.Data
│  │  └─ Presentation/   Unity-only views, scene glue — asmdef EmberCrpg.Presentation
│  ├─ Tests/
│  │  └─ EditMode/       deterministic unit tests — asmdef EmberCrpg.Tests.EditMode
│  ├─ Scenes/            Unity scenes
│  └─ Art/               sprites, shaders, materials
├─ docs/                 sprint summaries, atom maps, audit, mechanic map,
│                        agent rules
├─ docs/
│  ├─ EMBER_VISION_BIBLE.md   the bible
│  ├─ ROADMAP.md              the 12-phase plan
│  ├─ architecture/           live architecture notes
│  ├─ mechanics/              live mechanic notes
│  └─ reference/              read-only snapshot from msbel5/ember-rpg
└─ tools/validation/     run-validation.sh + supporting harness
```

Planned (Faz 1, see `docs/ROADMAP.md`): split Persistence (save/load) and
the deterministic test scaffolding into their own assemblies, and move
the Core (`Domain` + `Simulation` + `Data` + `Persistence`) under an
`Ember.Core` umbrella so it is portable outside Unity. Until that lands,
the structure above is the source of truth.

## Five-minute orientation

A new contributor (or a refreshed Captain session) should be able to
answer these after reading `README.md` plus the three Faz 0 docs:

- What kind of game is this? — a deterministic living-world CRPG.
- What is in scope right now? — the Faz 0 reset, then `Faz 1 - Core
  Store` (ActorStore / ItemStore / SiteStore / FactionStore).
- What is reference / not yet built? — anything under `docs/reference/`
  is the older Godot/Python project. Read it for ideas; do not port it
  line-for-line.
- What is the next sprint's playable acceptance? — see the top of
  `docs/ROADMAP.md`.

## License

MIT. The game will be free. Anyone is welcome to fork, mod, or learn
from it.

## Maintainer

[@msbel5](https://github.com/msbel5) (Mami) plus the Alcyone agent
crew (Captain, Liaison, Builder, Inspector, Archivist) running on a
Raspberry Pi 5.
