# Sprint Faz 2 — Smelt iron acceptance proof

_Date:_ 2026-05-12
_Branch:_ `agent/sprint-faz-2-smelt-iron-acceptance`
_PR:_ https://github.com/msbel5/alcyone-ember-rpg/pull/105
_Box:_ `[box=PLAYABLE]` / `[box=PROCESS]` / `[box=MATTER]` / `[box=TIME]`
_Thalamus:_ `pkt_20260512190154_c849680f3a6f` / `sha256:d21feee1a9410aebe91a0853f9e2e3b093086c07a25ce6090b6b93180e840daf`
_Atom map:_ `DOCS/sprint-faz-2-atom-map.md`

## Increment goal

Record the smallest player-facing Faz 2 proof now that the `RecipeSystem` visible slice exists: a deterministic replay path for smelting iron ore and fuel at an active furnace into an iron ingot, with the event log proving the transformation.

## Player acceptance sentence

`player can craft an iron ingot from ore and fuel and watch the stockpile increase`.

## Deterministic replay proof

1. Start with an active `WorksiteKind.Furnace` at `SiteId(7)`, `GridPosition(2, 3)`.
2. Seed the inventory with `2 iron_ore` and `1 fuel` stackable inputs.
3. Start the `SmeltIronIngot` recipe through `RecipeSystem.TryStart`.
4. Tick the order for the recipe's 40-tick duration.
5. Observe that the inventory receives one `iron_ingot` output and `WorldEventLog` appends a `RecipeCompleted` event with a `ReasonTrace` containing the recipe id, furnace worksite, and duration.

## Test anchor

The replay is pinned by EditMode fallback tests already in the visible RecipeSystem slice:

- `RecipeSystemTests.Tick_CompletesAfterFortyTicksAndProducesIronIngot`
- `RecipeEventLogTests.CompletingRecipe_AppendsOrderedRecipeCompletedEventWithReasonTrace`

These tests are the current deterministic stand-in for a Unity scene proof until the save/load and HUD-facing recipe state atoms land.

## Files changed

- `DOCS/sprint-faz-2-smelt-iron-acceptance.md` — adds the Faz 2 player-facing deterministic acceptance proof.
- `DOCS/sprint-faz-2-atom-map.md` — marks only the acceptance proof atom as landed and keeps save/load atoms open.

## Validation

- `git diff --check`: PASS.
- `./tools/validation/run-validation.sh --mode fallback`: PASS (`Passed: 812, Failed: 0`, `fallback_exit_code=0`, `validation-output/fallback-test-results/fallback.trx`).

## Sprint accounting

- Atom rows landed in this increment: 1 (`DOCS/sprint-faz-2-smelt-iron-acceptance.md`).
- Bundle count impact: +0; this is one proof atom, not a code bundle.
- Product-visible PR count for Faz 2: remains 1 code-visible PR, with this PR adding the required player-facing proof documentation.

## Next increment

Add the save/load runtime rail only after the recipe/worksite state has a safe world-root shape: serialize active worksite progress without adding forbidden hard-coded `SliceWorldState` role fields, then add a round-trip test for active recipe progress and produced stock.
