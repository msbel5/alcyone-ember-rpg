# Sprint 0 Recon — Ember CRPG Unity

_Date:_ 2026-04-30
_Branch:_ `agent/sprint0-recon-sprint1-slice`
_Status:_ complete

## 1. What exists right now

Repo evidence says this project is still at scaffold stage, not gameplay stage.

- `README.md:5-11` says the repo currently contains folder structure, copied references, and placeholder C# files.
- `git log --oneline` shows only two commits: sandbox init and import of the Unity rewrite scaffold.
- `Assets/Scripts/Domain/Core/` contains only `ActorId.cs`, `ItemId.cs`, `GameTime.cs` plus matching EditMode tests.
- There are no committed `.unity` scenes, no prefabs, and no gameplay systems under `Simulation/` or `Presentation/` yet.

## 2. Locked architecture already in repo

The repo already committed strong architectural decisions. Sprint 1 should build on them, not silently drift.

- `docs/mechanics/MASTER_MECHANICS_BIBLE.md:9-17`
  - real-time with pause
  - deterministic world
  - unified `Actor` + `Item`
  - LLM as narrator only
  - persistent NPC memory
- `docs/mechanics/ARCHITECTURE.md:7-15`
  - same five design pillars repeated and marked locked
- `docs/mechanics/ARCHITECTURE.md:414-425`
  - implementation order is explicit:
    1. stats
    2. vitals
    3. save/load wiring
    4. encumbrance
    5. movement
    6. skills
    7. weapons/armor
    8. inventory
    9. combat
    10. skill advancement
    11. level-up
    12. rest/recovery
    13-17 DM and memory surfaces

## 3. Lessons from the prior attempt in this repo

### L-01 — Good primitives exist, but they stopped too early
The prior pass established some quality standards:
- pure Domain + Simulation asmdefs with `noEngineReferences: true`
- NUnit EditMode tests for primitives
- small files with design-note comments

That is useful and should be preserved.

But the repo stalled after three primitives. There is no bridge from those primitives to a playable slice.

### L-02 — The repo is on the right architecture, but not yet on the user’s requested slice
The current architecture assumes RTWP and explicitly rejects a turn-based combat state machine:
- `docs/mechanics/MASTER_MECHANICS_BIBLE.md:10`
- `docs/mechanics/ARCHITECTURE.md:9`
- `docs/mechanics/ARCHITECTURE.md:434`

The user addendum for Sprint 1 explicitly requires one enemy with turn-based combat. That is a real conflict and must be called out instead of ignored.

### L-03 — Branch hygiene was already off
Sprint work was sitting on `main` when recon started. That violated the repo rule from `README-alcyone-original.md` and the active user instruction to stay on `agent/*`.

Fix applied before implementation:
- created `agent/sprint0-recon-sprint1-slice`

### L-04 — Reference material is abundant; runtime code is not
This repo has a lot of source material:
- mechanics bible + audit + architecture + micro mapping
- old backend data in `Reference/OldBackendData/`
- many PRDs in `Reference/PRDs/`

So Sprint 1 risk is not “missing design.”
The risk is translating scope into a thin, testable, deterministic C# slice without overbuilding.

## 4. Approved Sprint 1 deviation budget

### Conflict
The architecture says RTWP only; the user success criteria says one enemy turn-based combat must work.

### Approved adjustment
Sprint 1 will implement a **bounded deterministic encounter turn loop** under an approved deviation budget instead of redefining the whole game architecture.

Interpretation:
- exploration stays lightweight real-time bootstrap level
- combat entry switches into a deterministic alternating-turn encounter service
- this is treated as an approved Sprint 1 scope decision, not a silent architecture drift
- the deviation is documented in the Sprint 1 PRD, roadmap, code comments, and Sprint 1 summary so Inspector can review scope conformance instead of debating legitimacy

Why this is the smallest adjustment:
- it satisfies the user’s stated success criteria
- it preserves deterministic-first mechanics
- it avoids inventing a full RTWP combat stack before the repo has even basic actor/item/inventory state

## 5. Sprint 1 target slice

Implement the thinnest playable path that proves the repo can move from scaffold to interaction:

1. Player runtime bootstrap with WASD + mouse look
2. One procedural room
3. One player actor with 6 stats and vitals
4. Three NPC types
5. One talkable NPC with Ask About seed data
6. One pickup item and 10-slot inventory
7. One enemy with deterministic encounter combat
8. Save/load JSON for slice state
9. Ask About / Ask DM / Think skeleton surfaces
10. NUnit coverage for Domain + Simulation systems

## 6. Rules carried into implementation

- deterministic-first; no AI dependency for mechanics
- each file gets a short header comment/docstring block
- soft cap around 100 lines per file; split by responsibility
- atomic commits only
- no binaries/assets committed
- Inspector approval required before calling the sprint done

## 7. Immediate implementation plan

### Captain
- write Sprint 1 PRD and roadmap
- lock the approved combat deviation budget in writing
- delegate heavy mechanics implementation to Builder

### Builder
- implement Domain/Simulation slice first
- keep Presentation tiny: bootstrap + player movement + room wiring
- add tests per system
- commit in small chunks

### Inspector
- verify Bible/architecture alignment
- explicitly review conformance to the approved encounter-loop deviation budget
- reject anything undocumented or untested

### Archivist
- write `docs/sprint-1-summary.md` after implementation and inspection

## 8. Exit criteria for Sprint 0

Sprint 0 is considered complete when:
- recon is written down
- roadmap exists
- Sprint 1 PRD exists
- branch hygiene is fixed
- Builder has an explicit handoff rooted in repo evidence

These conditions are now satisfied.
