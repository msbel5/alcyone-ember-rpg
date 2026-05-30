# Sprint Faz 2 — RecipeDef pure definition

_Date:_ 2026-05-12
_Branch:_ `agent/sprint-faz-2-recipe-def`
_Box:_ `[box=PROCESS]` / `[box=MATTER]`
_Thalamus:_ `pkt_20260512155456_1e0b33ca071d` / `sha256:8412025a3dc3a147474a87cabd5502591a859a9356bbdd9114d87d9bc5255b5a`
_Atom map:_ `docs/sprint-faz-2-atom-map.md`

## Increment goal

Land the final pure recipe-definition atom before runtime execution: `RecipeDef`, a PROCESS/MATTER definition that composes inputs, outputs, worksite kind, skill tag, and deterministic tick duration.

## Files changed

- `Assets/Scripts/Domain/Process/RecipeDef.cs` — adds the pure definition with empty-id rejection, row validation, tag trimming, positive duration validation, defensive row copies, and read-only projections.
- `Assets/Tests/EditMode/Process/RecipeDefTests.cs` — pins constructor storage, trimming, invalid inputs, defensive copies, read-only row projections, and the canonical `SmeltIronIngot` shape.
- `docs/sprint-faz-2-atom-map.md` — checks off the RecipeDef atom rows and redirects the next increment toward a product-visible RecipeSystem EventLog slice.
- `docs/sprint-faz-2-recipe-def.md` — records this sprint summary and validation evidence.

## Validation

- `git diff --check`: PASS (no output).
- `./tools/validation/run-validation.sh --mode fallback`: PASS — `fallback_exit_code=0`; `Passed: 786, Failed: 0, Skipped: 0`; Unity editor blocked because editor binary is not installed in this environment (`STATUS unity_editor=BLOCKED reason=not_found`).

## Sprint accounting

- Atom rows landed in this increment: 2 (`RecipeDef`, `RecipeDefTests`).
- Bundle count impact: 1 small pure-definition bundle.
- Product-visible PR count for Faz 2: unchanged at 0. This is now the end of the pure-definition rail; the next PR should be product-visible via a RecipeSystem `EventLog` line.

## Next increment

Stop pure-definition-only work. Add the smallest visible `RecipeSystem` slice for `SmeltIronIngot`: validate a furnace + recipe definition, consume 2 `iron_ore` + 1 `fuel`, produce 1 `iron_ingot` after 40 ticks, and append an ordered `WorldEventLog` line.
