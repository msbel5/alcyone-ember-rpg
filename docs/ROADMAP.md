# Roadmap — Ember CRPG Unity

_Last updated:_ 2026-04-30
_Current branch:_ `agent/sprint0-recon-sprint1-slice`

## Sprint 0 — Recon and planning

Status: done

- inspect repo scaffold, bible, architecture, and references
- document prior-attempt lessons
- fix branch hygiene
- write Sprint 1 PRD
- define the smallest viable scope adjustment for combat mode conflict

Output:
- `DOCS/sprint-0-recon.md`
- `docs/PRD_SPRINT_1_TINY_VERTICAL_SLICE.md`

## Sprint 1 — Tiny playable vertical slice

Status: in progress

Goal: prove this Unity rewrite can move, spawn, interact, fight, save, and test from a clean scaffold.

### Phase 1 — Actor kernel
- 6 Ember stats: MIG / AGI / END / MND / INS / PRE
- `Actor`, `GridPosition`
- `Health`, `Fatigue`, `Mana`
- deterministic actor state tests

### Phase 2 — Deterministic encounter combat
- DFU-inspired percentage hit chance
- damage + armor + body part targeting weights
- one enemy encounter loop
- note: Sprint 1 uses a slice-only turn loop to satisfy the requested vertical slice; this does **not** rewrite the repo’s long-term RTWP architecture yet

### Phase 3 — Slice world
- one procedural room
- three NPC archetypes
- doors
- 10-slot inventory
- one pickup item
- one enemy encounter entry
- Ask About-capable talk NPC

### Phase 4 — Persistence and narrative shells
- save/load JSON
- Ask About shell
- Ask DM shell
- Think shell

### Sprint 1 acceptance
- player moves with WASD + mouse look
- one procedural dungeon room exists
- three NPC types exist
- item pickup works
- one enemy deterministic combat works
- save/load JSON works
- NUnit tests cover mechanics systems
- `DOCS/sprint-1-summary.md` written

## Sprint 2 — Architecture reconciliation

Status: planned

- decide whether combat stays encounter-turn-based for Ember or returns to the repo’s RTWP lock
- if RTWP wins, convert Sprint 1 combat logic into pause-step services and keep the math layer
- add richer item/equipment and dialogue state
- replace prototype bootstrap with proper scene/prefab composition

## Sprint 3 — Deeper simulation

Status: planned

- persistent NPC memory
- faction/reputation hooks
- more room templates
- quest hooks
- expanded inventory/equipment
- DM query tiers beyond shell level
