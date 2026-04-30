# Roadmap — Ember CRPG Unity

_Last updated:_ 2026-04-30
_Current branch:_ `main` + agent summary branches

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

Status: approved and merged

- split presentation seams so `SliceGameController` delegates to session/HUD/view helpers
- make the south door deterministic, saveable, and guard-gated
- add merchant stock plus a real Ember Shard → Gate Writ trade path
- add Sentinel Rook warning/clearance state distinct from Talker Ask About
- differentiate actor-role starting vitals/combat fields in pure Simulation
- add EditMode coverage for door, merchant, guard, save/load, and role differentiation

Output:
- `DOCS/sprint-2-summary.md`

## Sprint 3 — Validation hardening and memory-backed narrative depth

Status: approved and merged

Fresh scope counted from `0f36891` to merge commit `0f5a99b`:

- add Pi-local validation hardening with Unity detection and explicit .NET fallback mode
- document the validation workflow and the limits of fallback evidence
- add persistent NPC memory in clean domain/save/simulation layers
- add a memory-backed DM query service for narrative-state inspection
- unlock GitHub Unity PR checks by removing the failing check-posting path

Validation evidence:
- local `tools/validation/run-validation.sh --mode fallback` passed `73/73`
- GitHub PR #5 checks: EditMode Tests SUCCESS; PlayMode Tests + Screenshots SUCCESS; Build Linux64 SKIPPED; Test Summary SUCCESS; GitGuardian SUCCESS

Output:
- `DOCS/sprint-3-validation.md`
- `DOCS/sprint-3-summary.md`

Notes:
- local fallback is pure .NET domain/simulation/save corpus coverage, not a real local Unity EditMode run
- old `52f2e1e` / `116ae2e` branch-lineage work is not counted as fresh Sprint 3 output

## Sprint 4 — Multi-room dungeon, equipment UI, and atmosphere

Status: planned

Goal: turn the validated Sprint 3 memory/narrative substrate into a broader clean-room playable dungeon slice.

### Faz 1 — Validation baseline and branch hygiene
- start from `main` at or after `0f5a99b`
- keep fallback validation and GitHub Unity checks as the quality floor
- avoid importing old branch-lineage commits as fresh Sprint 4 work

### Faz 2 — Deterministic dungeon traversal rules
- define room graph contracts in pure simulation
- model exits, transitions, encounter/loot placement, and save/load invariants
- keep `UnityEngine` out of domain and simulation code

### Faz 3 — Multi-room procedural dungeon
- expand from one room to deterministic seeded multi-room generation
- add room templates, door/transition rules, and room-local NPC/item/enemy placement
- persist generated layout and room state across save/load

### Faz 4 — Equipment and inventory UI
- add player-facing inventory/equipment screens
- implement equip/unequip rules, slot constraints, and item instance clarity
- ensure equipped state affects at least one visible/stat-testable mechanic and persists

### Faz 5 — Audio and atmosphere
- add clean-room ambience/music/SFX hooks
- vary atmosphere by room or state where useful
- keep audio/presentation triggers decoupled from core simulation rules

### Sprint 4 acceptance
- deterministic seed produces a repeatable multi-room dungeon
- real 3D movement with smooth camera controls works without jank in at least a multi-room traversal
- player can traverse multiple rooms and return without corrupting room state
- NPCs, items, enemies, memory, and generated layout survive save/load round-trip
- inventory UI supports inspect, pickup/drop/use where available, and equip/unequip for at least one equipment slot
- equipment state changes a tested mechanic and is visible to the player
- real-time combat supports attack, wait, and block interactions; at least one enemy encounter exercises all three
- audio/atmosphere hooks work from presentation code without adding `UnityEngine` to domain/simulation
- local fallback validation passes; PR GitHub EditMode/PlayMode checks are green or explicitly explained
- a manual play-pass video demonstrates multi-room traversal, combat, inventory use, and save/load before final approval
- `DOCS/sprint-4-summary.md` records implementation, validation, and remaining risks before approval
