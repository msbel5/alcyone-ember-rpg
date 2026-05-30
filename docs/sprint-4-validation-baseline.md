# Sprint 4 Faz 1 — Validation Baseline and Branch Hygiene

This page is the Sprint 4 starting contract. It does not implement Sprint 4 gameplay features; it records the baseline that later phase branches must build from and validate against.

## Baseline

- Sprint 4 starts from `main` at `b05100026c081361cf6f4c660dfa77fe620d644f` (`b051000`), after Sprint 3 summary PR #6 was merged.
- Sprint 3 Phase 4/5/6 and summary work are already merged into that baseline.
- Latest known local baseline evidence on `main`: `tools/validation/run-validation.sh --mode fallback` passed `73/73`.

## Validation floor

Every Sprint 4 phase PR should record the best available evidence:

1. `git diff --check`
2. `tools/validation/check-sprint4-branch-hygiene.sh`
3. `tools/validation/run-validation.sh --mode fallback`
4. GitHub Unity EditMode/PlayMode checks when available, or an explicit explanation if blocked/skipped

Fallback validation is a pure .NET/NUnit domain, simulation, and save/load harness. A fallback PASS is useful regression evidence, but it is not a real Unity EditMode or PlayMode run and does not validate Unity serialization/import behavior, scenes, input, rendering, camera feel, audio, or manual playability.

## Branch hygiene guard

- New Sprint 4 work must be developed as phase branches from current `origin/main` and reviewed by PR to `main`.
- Old branch-lineage commits `52f2e1e` and `116ae2e` must not be imported, cherry-picked, rebased, or described as fresh Sprint 4 work.
- No code may be copied from `Reference/`, `references/`, or other upstream projects. Mechanics can be studied and re-expressed cleanly, with source citations when relevant.
- Faz 1 is documentation/validation hygiene only. Faz 2/3 traversal and dungeon feature work starts in later branches.

## Sprint 4 acceptance gates

Sprint 4 is not complete until the final summary has evidence for:

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
- `docs/sprint-4-summary.md` records implementation, validation, and remaining risks before approval
