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

Status: approved (repo-evidence based; local Unity/.NET execution unavailable)

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

## Sprint 2 — Interaction refinement and presentation cleanup

Status: implemented on branch, pending Inspector review

- split presentation seams so `SliceGameController` delegates to session/HUD/view helpers
- make the south door deterministic, saveable, and guard-gated
- add merchant stock plus a real Ember Shard → Gate Writ trade path
- add Sentinel Rook warning/clearance state distinct from Talker Ask About
- differentiate actor-role starting vitals/combat fields in pure Simulation
- add EditMode coverage for door, merchant, guard, save/load, and role differentiation

Output:
- `DOCS/sprint-2-summary.md`

## Sprint 3 — Validation hardening and simulation depth

Status: planned

- run Unity-capable validation: EditMode, then manual slice pass
- harden inventory identity flow (replace hardcoded shard id path with safer item-id generation)
- decide whether CI should also watch agent branches or rely on PR-to-main as the gate
- continue presentation seam cleanup if `SliceGameSession` should be split again
- begin deeper simulation work: persistent NPC memory, richer room templates, faction/reputation hooks, expanded item/equipment state, and DM query tiers beyond shell level
