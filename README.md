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

## Status (2026-05-09)

We are entering **Faz 0** — audit and realignment. Sprints 1 to 5 produced
real code (Domain + Simulation + Tests folders, magic effect resolution,
some combat scaffolding) but the cron loop drifted into a tight magic-test
matrix (PRs #62-#71) without producing player-visible capability.

Faz 0 lands the canonical maps and rules:

- `DOCS/alcyone-audit-2026-05-09.md` — what is in the repo, why the loop
  stalled, what to do about it
- `DOCS/mechanic-map-v1.md` — the **8-box living-world model** every
  sprint must target
- `DOCS/agent-rules-v2.md` — five new agent rules: product-visible
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
- **Core**: Unity-independent `Ember.Core` library. Domain + Simulation
  + Persistence + Tests live under `Assets/Ember/` with **no
  `using UnityEngine`** anywhere in Core.
- **Unity layer**: only Scene, Input, Camera, UI, ViewModels,
  ActorView/ItemView, DebugVisualizer.
- **Composition over inheritance**: `EntityId` plus components
  (`PositionComponent`, `MaterialComponent`, `InventoryComponent`,
  `VitalsComponent`, ...).
- **Determinism**: `DeterministicRng` seeded per tick. Replays must be
  bit-stable.
- **Data-driven**: new spells, new recipes, new buffs ship as data rows.
  C# changes only when a new operation kind is required.

## How sprints work

1. The **Captain** cron (`@EMSPR`) reads
   `/home/msbel/.openclaw/workspace/CRON_CODES.md` and decomposes the
   active sprint phase against `DOCS/mechanic-map-v1.md`. Every atom
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

## Repo layout

```
alcyone-ember-rpg/
├─ Assets/Ember/
│  ├─ Domain/        pure C# core types, no Unity
│  ├─ Simulation/    systems, stores, tick loop
│  ├─ Persistence/   save/load
│  ├─ Tests/         deterministic unit + integration tests
│  └─ Unity/         scene, view, input — only this references UnityEngine
├─ DOCS/             sprint summaries, atom maps, audit, mechanic map,
│                    agent rules
└─ docs/
   ├─ EMBER_VISION_BIBLE.md   the bible
   ├─ ROADMAP.md              the 12-phase plan
   ├─ architecture/           live architecture notes
   ├─ mechanics/              live mechanic notes
   └─ reference/              read-only snapshot from msbel5/ember-rpg
```

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
