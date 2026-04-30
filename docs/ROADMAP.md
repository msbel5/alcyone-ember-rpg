# Roadmap — Ember CRPG Unity

_Last updated:_ 2026-04-30
_Current branch:_ `agent/sprint0-recon-sprint1-slice`

## Sprint 0 — Recon and planning

Status: done

- inspect repo scaffold, bible, architecture, and references
- document prior-attempt lessons
- fix branch hygiene
- write Sprint 1 PRD
- define and record the approved Sprint 1 combat deviation budget

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
- approved deviation budget: Sprint 1 uses a bounded encounter turn loop as the accepted combat mode for this slice; the long-term RTWP architecture remains the default outside Sprint 1

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

## Sprint 2 — Architecture follow-through

Status: planned

- decide whether the approved Sprint 1 encounter loop graduates into a broader Ember combat model or is adapted back into RTWP pause-step services
- if RTWP wins long-term, keep Sprint 1's deterministic combat math and remap only the encounter orchestration layer
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
