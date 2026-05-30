# Sprint Faz 2 — Recipe primitives

_Date:_ 2026-05-12
_Branch:_ `agent/sprint-faz-2-recipe-primitives`
_Box:_ `[box=PROCESS]` / `[box=MATTER]`
_Thalamus:_ `pkt_20260511222722_4531e566a532` / `sha256:aef062b231ad626b049d9166098ecef0131cad4633ea6727d52c0efaf3805b09`
_Atom map:_ `docs/sprint-faz-2-atom-map.md`

## Increment goal

Start the Faz 2 pure definition rail with the smallest deterministic recipe handles and input rows: `RecipeId` and `RecipeIngredient`, plus focused EditMode tests. This intentionally avoids `RecipeSystem`, inventory mutation, save/load, and EventLog output until `RecipeDef` exists.

## Files changed

- `Assets/Scripts/Domain/Process/RecipeId.cs` — pure value handle for deterministic recipe lookup, with zero as the empty sentinel.
- `Assets/Scripts/Domain/Process/RecipeIngredient.cs` — pure input row describing item/material tag + positive quantity.
- `Assets/Tests/EditMode/Process/RecipeIdTests.cs` — value semantics, default sentinel, equality operators, hash, debug label.
- `Assets/Tests/EditMode/Process/RecipeIngredientTests.cs` — tag storage/normalization and constructor invariant tests.
- `docs/sprint-faz-2-atom-map.md` — checks off the four landed atoms and points the next increment at `RecipeOutput` / `RecipeDef`.

## Validation

- `git diff --check`: PASS (no output).
- `./tools/validation/run-validation.sh --mode fallback`: PASS — `Passed!  - Failed:     0, Passed:   770, Skipped:     0, Total:   770`; `fallback_exit_code=0`; TRX `validation-output/fallback-test-results/fallback.trx`.

## Sprint accounting

- Atom rows landed in this PR: 4 (`RecipeId`, `RecipeIdTests`, `RecipeIngredient`, `RecipeIngredientTests`).
- Bundle count: 1 pure definition bundle.
- Product-visible PR count for Faz 2: 0 so far; the first visible target remains `RecipeSystem` writing an ordered EventLog line for smelt progress.

## Next increment

Add `RecipeOutput`, then `RecipeDef` + tests so the later `RecipeSystem` atom can consume a complete deterministic recipe shape.
