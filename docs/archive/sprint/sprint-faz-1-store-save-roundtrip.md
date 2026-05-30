# Sprint Faz 1 — Store save/load round-trip

_Date:_ 2026-05-11
_Branch:_ `agent/sprint-faz-1-store-save-roundtrip`
_Box tags:_ `[box=TIME]`, `[box=LIVING]`, `[box=MATTER]`, `[box=WORLD]`, `[box=SOCIETY]`, `[box=PROCESS]`

## Increment goal

Extend the Faz 1 save/load rail so canonical runtime roots (`Actors`, `Items`, `Sites`, `Factions`, `Events`) round-trip through `SliceSaveData` alongside legacy slice fields.

This is a product-visible foundation increment: `player can save and load a world snapshot whose guard, item, site, faction, and event-log state survives through the canonical store roots instead of only through legacy named slice fields.`

## Files changed

- `Assets/Scripts/Data/Save/SliceSaveData.cs`
  - Adds DTO arrays for canonical actors, item records, site records, faction records, and world events.
- `Assets/Scripts/Data/Save/SliceSaveMapper.cs`
  - Writes store roots into the save DTO, restores them on load, and prefers canonical actor-store payloads over legacy actor fields when present.
- `Assets/Tests/EditMode/Save/StoreRoundTripTests.cs`
  - Pins JSON round-trip for `ActorStore`, `ItemStore`, `SiteStore`, `FactionStore`, and `WorldEventLog` plus actor-store preference over legacy named fields.
- `docs/sprint-faz-1-atom-map.md`
  - Marks the TIME-box save/load atoms landed and points the next run at the PLAYABLE acceptance proof.

## Validation

- `git diff --check`: PASS (no whitespace errors)
- `./tools/validation/run-validation.sh --mode fallback`: PASS (`Passed: 756`, `Failed: 0`; Unity editor blocked/not installed, fallback harness green)

## Thalamus

- packet_id: `pkt_20260511190607_d724944dd3fa`
- resolver_key: `sha256:a8e560bbfe6f80911154a072cd4a71a10631da041ab333a1edf7a6317283ff77`

## Product-visible count

- Faz 1 product-visible foundation PRs: +1 (canonical store-root state now survives save/load and is readable by gameplay-facing code after restore).

## Next increment

Add `docs/sprint-faz-1-acceptance.md` with a deterministic replay log, debug-HUD dump, screenshot, or one-paragraph playtest note proving guard spawn + talk + memory + second-site continuity across save/load.
