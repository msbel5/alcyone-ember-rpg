# Sprint Faz 2 — RecipeSystem visible slice

_Date:_ 2026-05-12
_Branch:_ `agent/sprint-faz-2-recipe-system`
_PR:_ pending
_Box:_ `[box=PROCESS]` / `[box=MATTER]` / `[box=TIME]` / `[box=PLAYABLE]`
_Thalamus:_ `pkt_20260512182245_50ced2e83e39` / `sha256:fb5f0604997918a5753c37c549c3b790e6abf8ab952a8da6d613eae21b3fc16a`
_Atom map:_ `DOCS/sprint-faz-2-atom-map.md`

## Increment goal

Ship the smallest product-visible Faz 2 `RecipeSystem` slice: an active furnace consumes `2 iron_ore + 1 fuel`, advances for 40 deterministic ticks, produces `1 iron_ingot`, and appends an ordered `WorldEventLog` line with `ReasonTrace`.

## Files changed

- `Assets/Scripts/Simulation/Process/RecipeSystem.cs` — adds the narrow pure runtime system and `RecipeWorkOrder` state for one active recipe execution.
- `Assets/Scripts/Domain/World/WorldEventKind.cs` — adds `RecipeCompleted` only alongside the concrete `RecipeSystem` emitter.
- `Assets/Tests/EditMode/Process/RecipeSystemTests.cs` — covers active furnace start, input consumption, failure paths, 40-tick completion, and output production.
- `Assets/Tests/EditMode/Process/RecipeEventLogTests.cs` — pins the ordered `WorldEventLog` recipe completion event and causal `ReasonTrace`.
- `DOCS/sprint-faz-2-atom-map.md` — marks the recipe execution atoms as landed and updates the next increment.
- `DOCS/sprint-faz-2-recipe-system.md` — records this increment.

## Validation

- `git diff --check`: PASS.
- `./tools/validation/run-validation.sh --mode fallback`: PASS (`Passed: 809, Failed: 0`, `fallback_exit_code=0`, `validation-output/fallback-test-results/fallback.trx`).

## Product-visible proof

`player can craft an iron ingot from ore and fuel and watch the event log record the completed smelt.`

The proof is deterministic EditMode coverage: `RecipeEventLogTests.CompletingRecipe_AppendsOrderedRecipeCompletedEventWithReasonTrace`.

## Sprint accounting

- Atom rows landed in this increment: 4 (`RecipeSystem`, `RecipeSystemTests`, `WorldEventKind.RecipeCompleted`, `RecipeEventLogTests`).
- Bundle count impact: +1 bundle for the tightly-coupled recipe execution/EventLog slice.
- Product-visible PR count for Faz 2: 1 (first EventLog-emitting PROCESS slice).

## Next increment

Add the save/load and player-facing proof sub-area: serialize active recipe/worksite progress only after runtime state exists, then add `DOCS/sprint-faz-2-smelt-iron-acceptance.md` with a deterministic replay proof for crafting iron ingot from ore + fuel at a furnace.
