# Faz 2 — Recipe/worksite save rail

_Date:_ 2026-05-14
_Branch:_ `agent/sprint-faz-2-recipe-worksite-save`
_Packet:_ `pkt_20260514100543_0ae5b0863208`
_Resolver:_ `sha256:8f7b856d70c40ce7e38edaf7d701dd546a9f51d65ce3fa25ecb906430fe6f46e`
_Atom-map rows:_ save/load TIME rail in `DOCS/sprint-faz-2-atom-map.md`.

## Increment goal

Add the narrow Faz 2 save/load rail for active furnace process state: serialize an active worksite and a partially progressed recipe work order through the SliceSave DTO boundary, then prove the loaded order continues to one `RecipeCompleted` event and one iron ingot.

## Files changed

- `Assets/Scripts/Data/Save/SliceSaveData.cs` — adds DTO arrays for `WorksiteSaveData` and `RecipeWorkOrderSaveData`.
- `Assets/Scripts/Data/Save/SliceSaveMapper.cs` — maps `WorksiteStore` plus recipe work-order progress to/from save DTOs with caller-supplied recipe resolution.
- `Assets/Scripts/Simulation/Process/RecipeSystem.cs` — adds `RecipeWorkOrder.Resume(...)` for safe rehydration without consuming inputs again.
- `Assets/Tests/EditMode/Save/RecipeWorksiteRoundTripTests.cs` — pins active furnace progress JSON DTO round-trip and post-load completion.

## Validation

- `git diff --check` — PASS.
- `./tools/validation/run-validation.sh --mode fallback` — PASS (`fallback_exit_code=0`, `Passed: 813, Failed: 0`).

## Next increment

After review/merge, update the Faz 2 atom map rows for the save/load rail and assess whether a final Faz 2 promotion summary is now allowed by the promotion checklist.
