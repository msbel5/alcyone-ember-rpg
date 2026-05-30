# Sprint Faz 2 — Recipe primitives review fix

_Date:_ 2026-05-12
_Branch:_ `agent/sprint-faz-2-recipe-primitives`
_Box:_ `[box=PROCESS]` / `[box=MATTER]`
_Thalamus:_ `pkt_20260511224254_c77408bba8ae` / `sha256:b8de5869a950fac87baa85b8f79842e82106d6b0ded7a912f73ec67ccf90fcd5`
_Atom map:_ `docs/sprint-faz-2-atom-map.md`
_Bot review:_ Copilot inline comment on PR #99 discussion `r3222580834`

## Increment goal

Address the PR #99 Copilot review by preserving the invalid recipe ingredient quantity as the `ArgumentOutOfRangeException.ActualValue`, keeping the existing pure definition rail unchanged.

## Files changed

- `Assets/Scripts/Domain/Process/RecipeIngredient.cs` — passes the invalid `quantity` into `ArgumentOutOfRangeException`.
- `Assets/Tests/EditMode/Process/RecipeIngredientTests.cs` — asserts the zero and negative invalid values are preserved.
- `docs/sprint-faz-2-recipe-primitives-review-fix.md` — records this bot-review follow-up and validation evidence.

## Validation

- `git diff --check`: PASS (no output).
- `./tools/validation/run-validation.sh --mode fallback`: PASS — `Passed!  - Failed:     0, Passed:   770, Skipped:     0, Total:   770`; `fallback_exit_code=0`; TRX `validation-output/fallback-test-results/fallback.trx`.

## Sprint accounting

- Atom rows landed in this follow-up: 0 new atom-map rows; this is a review hardening fix for the existing `RecipeIngredient` atom.
- Bundle count impact: unchanged.
- Product-visible PR count for Faz 2: unchanged at 0; visible target remains `RecipeSystem` emitting an ordered EventLog line.

## Next increment

After PR #99 is merged, continue the Faz 2 pure definition rail with `RecipeOutput`, then `RecipeDef` + tests.
