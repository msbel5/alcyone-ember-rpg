# Sprint Faz 1 — Store save/load round-trip bot-review follow-up

_Date:_ 2026-05-11
_Branch:_ `agent/sprint-faz-1-store-save-roundtrip`
_PR:_ #95
_Box tags:_ `[box=TIME]`, `[box=LIVING]`

## Increment goal

Address PR #95 Copilot review feedback on the canonical actor-store save/load path so `SliceSaveMapper` treats a non-null `actors` payload as authoritative, including an explicitly empty actor array, and avoids zero-length allocation helpers.

## Files changed

- `Assets/Scripts/Data/Save/SliceSaveMapper.cs`
  - Loads legacy named actor fields only when the canonical `actors` payload is absent.
  - Treats `actors = []` as an explicit empty `ActorStore` instead of falling back to legacy fields.
  - Replaces repeated `new T[0]` fallbacks with `Array.Empty<T>()`.
- `Assets/Tests/EditMode/Save/StoreRoundTripTests.cs`
  - Pins explicit-empty canonical actor stores.
  - Pins skipping malformed legacy actor DTOs when canonical actor-store data is present.

## Validation

- `git diff --check`: PASS (no whitespace errors).
- `./tools/validation/run-validation.sh --mode fallback`: PASS (`Passed: 758`, `Failed: 0`; Unity editor blocked/not installed, fallback harness green).

## Thalamus

- packet_id: `pkt_20260511193120_d5a140813115`
- resolver_key: `sha256:3796022dd3c0ffa24f6644272cce1d42997b28a8393c080f967f4ca670118f55`

## Bot-review handling

- Copilot comment on canonical actor-store preference: fixed by treating `data.actors != null` as the presence check and skipping legacy actor DTO loading when present.
- Copilot comment on empty-array allocations: fixed with `Array.Empty<T>()` fallbacks.

## Next increment

After PR #95 is updated/merged, add `docs/sprint-faz-1-acceptance.md` with a deterministic replay log, debug-HUD dump, screenshot, or one-paragraph playtest note proving guard spawn + talk + memory + second-site continuity across save/load.
