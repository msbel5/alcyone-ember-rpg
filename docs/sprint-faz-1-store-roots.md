# Sprint Faz 1 — SliceWorldState store roots

_Date:_ 2026-05-11
_Branch:_ `agent/sprint-faz-1-store-roots`
_Box tags:_ `[box=LIVING]`, `[box=MATTER]`, `[box=WORLD]`, `[box=SOCIETY]`, `[box=PROCESS]`

## Increment goal

Add the missing runtime roots for Faz 1 stores on `SliceWorldState` so the next TIME-box save/load atom can serialize canonical stores directly instead of inventing new named slice fields.

This is a product-visible foundation increment: gameplay-facing code can now read the same world snapshot for `Actors`, `Items`, `Sites`, `Factions`, and `Events`, with actors already populated by the slice factory and the other roots ready for upcoming systems.

## Files changed

- `Assets/Scripts/Domain/World/SliceWorldState.cs`
  - Adds `Items`, `Sites`, `Factions`, and `Events` store/log roots next to the existing `Actors` store.
- `Assets/Tests/EditMode/World/SliceWorldStateActorViewTests.cs`
  - Pins fresh and factory-created world snapshots expose non-null empty store/log roots while keeping actor role views intact.
- `docs/sprint-faz-1-atom-map.md`
  - Records the prerequisite atom and updates the next save/load increment wording.

## Validation

- `git diff --check`: PASS (no whitespace errors)
- `./tools/validation/run-validation.sh --mode fallback`: PASS (`Passed: 754`, `Failed: 0`; Unity editor blocked/not installed, fallback harness green)

## Thalamus

- packet_id: `pkt_20260511184559_c138f4fe260d`
- resolver_key: `sha256:6c02d26981c9c2b5630150854c9f9ca669b2bc7e45a3131af67f56ff88c36a95`

## Product-visible count

- Faz 1 product-visible foundation PRs: +1 (runtime store roots exposed on the slice world snapshot).

## Next increment

Extend `SliceSaveMapper` and `SliceSaveData` to round-trip the runtime store roots (`Actors`, `Items`, `Sites`, `Factions`, `Events`) alongside legacy slice fields, then add `StoreRoundTripTests`.
