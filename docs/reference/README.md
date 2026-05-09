# Reference — older Godot/Python prototype

This folder is a **read-only mirror** of selected docs from
`msbel5/ember-rpg` (the older Godot/Python prototype) at
`main` as of 2026-05-09.

It is here so Captain (and human contributors) can decompose against
the canonical mechanic list and PRDs rather than re-inventing them.
The Unity/C# rewrite (this repo) is **not** a port. Treat these docs
as design intent, not a code spec.

## What is in here

```
reference/
├─ UPSTREAM_README.md           the upstream README at clone time
├─ PRD_IMPLEMENTATION_MATRIX.md the upstream PRD index
├─ architecture/                canonical architecture notes
│  ├─ ember_mechanics_canon_v1.md   THE canonical mechanics doc
│  ├─ MECHANISM_DIAGRAM.md          architectural diagram
│  ├─ automation_stack.md
│  ├─ creation_state_machine.md
│  ├─ KERNEL_DIRECT_UI_CUTOVER.md
│  ├─ reference_notes.md
│  └─ runtime_authority.md
├─ prd/                         curated active PRDs (~31 docs)
│   PRD_STANDARD, PRD_PLAYABILITY_RESCUE, PRD_systems_closure,
│   actor_kernel + actor_record_authority + architecture_actor_runtime,
│   colony_simulation_v2, world_state_kernel, world_data_registries,
│   area_map, fast_visual_tick,
│   item_system_kernel, material_item_kernel,
│   job_reaction_kernel_v2, data_externalization,
│   history_and_factions, macro_society_runtime, store_trade,
│   kernel_combat_engine + combat_resolution, dialog_system,
│   effect_system, spell_system, medical_system,
│   level_progression, stat_unification,
│   gamescript_ai, hybrid_commander_loop,
│   save_schema_v4, game_state, pathfinding
└─ handoffs/                    upstream copilot handoff prompts
```

## How to use this

- Read `architecture/ember_mechanics_canon_v1.md` first if you want the
  big picture.
- Read `architecture/MECHANISM_DIAGRAM.md` for the system diagram.
- Read the relevant PRD when starting a Faz N sprint, but treat the
  Godot/Python implementation suggestions as ideas, not constraints —
  this repo uses Unity + Pure C# core (see top-level `README.md`).
- Cross-reference with `DOCS/mechanic-map-v1.md` (the 8-box model) to
  see which box a given upstream PRD belongs to.

## What is **not** in here

The upstream repo has Godot scenes, Python FastAPI backend, Asset
files. None of those are mirrored. If you need them, read upstream
directly at https://github.com/msbel5/ember-rpg.

## License

Inherited from upstream
[msbel5/ember-rpg](https://github.com/msbel5/ember-rpg). Per the
upstream README (mirrored here as `UPSTREAM_README.md`):

> Source code is open; shipped game assets remain proprietary unless
> explicitly marked otherwise.

This `docs/reference/` mirror contains only documentation files
(Markdown PRDs, architecture notes, the upstream README). It does
**not** include any upstream source code, assets, or LICENSE file. If
you need a verifiable license for derivative work, fetch the upstream
LICENSE (if present) from
https://github.com/msbel5/ember-rpg directly. The Alcyone Unity
rewrite (this repo, top-level `README.md`) is MIT and that license
applies to all code originating in this repo.
