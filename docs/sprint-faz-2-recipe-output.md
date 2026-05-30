# Sprint Faz 2 — RecipeOutput pure row

_Date:_ 2026-05-12
_Branch:_ `agent/sprint-faz-2-recipe-output`
_Box:_ `[box=PROCESS]` / `[box=MATTER]`
_Thalamus:_ `pkt_20260512151432_3f3e838ea4f3` / `sha256:51368bad478cc9049ff6efb3539955fe2559a6f8a0186140c5a324b0adc2b0ce`
_Atom map:_ `docs/sprint-faz-2-atom-map.md`

## Increment goal

Land the next Faz 2 pure definition atom: `RecipeOutput`, the MATTER/PROCESS row that describes produced item tags, material, quality, and quantity before `RecipeDef` composes inputs and outputs.

## Files changed

- `Assets/Scripts/Domain/Process/RecipeOutput.cs` — adds the pure output row with tag trimming, material/quality sentinel rejection, and positive quantity validation.
- `Assets/Tests/EditMode/Process/RecipeOutputTests.cs` — pins storage, trimming, sentinel rejection, and invalid quantity diagnostics.
- `docs/sprint-faz-2-atom-map.md` — checks off the two RecipeOutput atom rows and records the next increment warning.
- `docs/sprint-faz-2-recipe-output.md` — records this sprint summary and validation evidence.

## Validation

- `git diff --check`: PASS (no output).
- `./tools/validation/run-validation.sh --mode fallback`: PASS — `Passed!  - Failed:     0, Passed:   775, Skipped:     0, Total:   775`; `fallback_exit_code=0`; TRX `validation-output/fallback-test-results/fallback.trx`.

## Sprint accounting

- Atom rows landed in this increment: 2 (`RecipeOutput`, `RecipeOutputTests`).
- Bundle count impact: 1 small pure-definition bundle.
- Product-visible PR count for Faz 2: unchanged at 0. This remains acceptable only as a dependency rail; the next implementation should pin `RecipeDef` and then widen quickly toward the smallest visible `RecipeSystem` EventLog slice.

## Next increment

Add `RecipeDef` + focused tests for constructor invariants, defensive input/output copies, worksite kind/skill tag/tick duration, and the `SmeltIronIngot` shape. After that, stop pure-definition-only work and target the smallest product-visible `RecipeSystem` EventLog line.
