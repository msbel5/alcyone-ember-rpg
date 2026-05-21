# Faz 2 — Recipe/worksite save service review fix

_Date:_ 2026-05-14
_Branch:_ `agent/sprint-faz-2-recipe-worksite-save`
_Packet:_ `pkt_20260514103804_61c974910401`
_Resolver:_ `sha256:596d1c19f91fda5ea8542600026f98b300e5f7ac0e4c286509facfadc6c35bcd`
_Atom-map rows:_ save/load TIME rail in `DOCS/sprint-faz-2-atom-map.md`.
_Bot review:_ Codex PR #106 P2 — production save/load did not populate or rehydrate process DTO fields.

## Increment goal

Wire the active worksite and recipe work-order DTOs through the production `JsonSliceSaveService` path instead of only proving manual DTO patching in a test.

## Files changed

- `Assets/Scripts/Data/Save/JsonSliceSaveService.cs` — carries the process-side `WorksiteStore`, serializes active recipe work orders, and rehydrates them with a caller-supplied recipe resolver.
- `Assets/Tests/EditMode/Save/RecipeWorksiteRoundTripTests.cs` — exercises `JsonSliceSaveService.SaveToJson` / `LoadFromJson` directly, then continues the loaded order to one `RecipeCompleted` event and one iron ingot.
- `docs/sprint-faz-2-recipe-worksite-save-review-fix.md` — records this review-fix increment.

## Validation

- `git diff --check` — PASS.
- `./tools/validation/run-validation.sh --mode fallback` — PASS (`fallback_exit_code=0`, `Passed: 813, Failed: 0`).

## Next increment

After PR #106 is updated and bot feedback is marked addressed, wait for remote checks/bot review before merge; then update the Faz 2 atom map row if the PR lands.

## Atom-map bookkeeping

- `DOCS/sprint-faz-2-atom-map.md` now marks the save/load TIME rows for `SliceSaveMapper`/`RecipeWorksiteRoundTripTests` as landed on `agent/sprint-faz-2-recipe-worksite-save`.
- Current cron packet: `pkt_20260514105821_bf14bff6ca68`; resolver: `sha256:62e3ced1a90e0ef1103cf7066036d2202562584822c0f076e7d11b86bdfcb3ee`.
