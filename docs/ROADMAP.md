# Roadmap — Ember CRPG Unity

_Last updated:_ 2026-04-30
_Current branch:_ `agent/sprint-3-validation-and-depth`

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

Status: approved and merged to `main`

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

Status: approved and merged to `main`

- split presentation seams so `SliceGameController` delegates to session/HUD/view helpers
- make the south door deterministic, saveable, and guard-gated
- add merchant stock plus a real Ember Shard → Gate Writ trade path
- add Sentinel Rook warning/clearance state distinct from Talker Ask About
- differentiate actor-role starting vitals/combat fields in pure Simulation
- add EditMode coverage for door, merchant, guard, save/load, and role differentiation

Output:
- `DOCS/sprint-2-summary.md`

## Sprint 3 — Validation hardening and simulation depth

Status: implemented on branch, Inspector-approved, awaiting PR-to-main review

### Shipped on this branch
- hardened item identity flow with stable per-world item ids
- persistent NPC memory and save/load support
- three deterministic room templates
- `city_watch` reputation hook and guard attitude shaping
- `weapon` / `armor` equipment slots with separate equipped state
- deterministic DM query tiers 1-3 over current world state, room layout, equipment, and memory
- HUD surfacing for layout, guard attitude, and equipped items

### Verified on this branch
- clean diff hygiene (`git diff --check`)
- no `UnityEngine` in Domain/Simulation
- supplemental pure-C# NUnit harness pass: `79/79`
- Inspector verdict: APPROVED

### Still blocked
- direct Unity EditMode execution on this Pi (no local Unity editor binary found)
- real/manual in-engine slice pass on this Pi

### Deferred follow-up
- run the true Unity EditMode suite on a machine with the matching editor
- capture a real manual slice pass with screenshots/notes
- decide whether CI should validate agent branches directly or stay PR-to-main only
- tune guard reputation curve if playtesting says the hostility ramp is too sharp
- widen equipment/faction systems only when the slice grows beyond the current scope
