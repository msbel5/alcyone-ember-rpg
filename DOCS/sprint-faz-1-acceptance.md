# Sprint Faz 1 — Acceptance replay proof

_Date:_ 2026-05-11
_Branch:_ `agent/sprint-faz-1-acceptance-proof`
_Box:_ `[box=PLAYABLE]` with replay coverage across `[box=LIVING]`, `[box=WORLD]`, `[box=PROCESS]`, and `[box=TIME]`
_Atom-map:_ `DOCS/sprint-faz-1-atom-map.md`
_Thalamus:_ `pkt_20260511195718_3b201e02afe2` / `sha256:7b422f4dd607a04ac435e6b2eadffa47fec9ea21cd357eea509ca785a9368767`

## Increment goal

Close the remaining Faz 1 PLAYABLE atom with a deterministic replay proof for
the roadmap acceptance sentence:

> player can spawn a guard, talk to it, then walk to a second site and watch
> the same guard remembered across save/load.

## Replay proof

`Assets/Tests/EditMode/World/Faz1AcceptanceReplayTests.cs` performs this
replay without Unity-only APIs:

1. `SliceWorldFactory.Create(3110)` creates the store-backed guard
   `Sentinel Rook` and the initial dungeon room.
2. The replay seeds two `SiteStore` entries: `Ember Gatehouse` and
   `Ashford Approach`.
3. It appends a `WorldEventKind.ActorSpawned` event for the guard with a
   `ReasonTrace` of `factory-seed-3110 -> store-backed-guard`.
4. The player moves next to the guard and calls `GuardInteractionService`;
   the first output is the no-writ challenge and a guard memory event is
   recorded.
5. JSON save/load round-trips the world; a second guard interaction after
   load returns the remembered line: `remembers your first unwrit request`.
6. The replay moves to a second dungeon room/site and appends a
   `WorldEventKind.SiteEntered` event with reason trace
   `save-load -> guard-memory-confirmed -> walk-to-second-site`.
7. A final JSON save/load asserts the same guard id, two persisted guard
   passage-request memories, the second room visited flag, the second site,
   and the ordered event log: `ActorSpawned`, `ActorTalked`, `SiteEntered`.

## Files changed

- `Assets/Tests/EditMode/World/Faz1AcceptanceReplayTests.cs` — deterministic
  Faz 1 acceptance replay test.
- `Assets/Tests/EditMode/World/Faz1AcceptanceReplayTests.cs.meta` — Unity meta
  stub for the test file.
- `DOCS/sprint-faz-1-acceptance.md` — this proof/summary.
- `DOCS/sprint-faz-1-atom-map.md` — marks the PLAYABLE acceptance atom landed
  and records packet metadata.

## Validation

- `git diff --check`: PASS (no output).
- `./tools/validation/run-validation.sh --mode fallback`: PASS — `Passed!  - Failed:     0, Passed:   759, Skipped:     0, Total:   759`; `fallback_exit_code=0`; log `validation-output/validation-20260511T200553Z.log`.

## Sprint accounting

- Final atom count: 27/27 atom-map rows checked in `DOCS/sprint-faz-1-atom-map.md` on this branch.
- Bundle count: 0 for this PR; this is a single acceptance-proof atom.
- Product-visible PR count for Faz 1: at least 1; this PR is the explicit
  playable-proof PR and the prior store PRs exposed world state through
  canonical stores.

## Next increment

If validation and review pass, Faz 1 has its acceptance proof. The next sprint
factory run should verify all Faz 1 atom-map rows are checked and decide whether
to promote Faz 1 or open Faz 2 (`Recipe + Worksite`) from `docs/ROADMAP.md`.
