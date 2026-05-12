# Sprint Faz 2 — RecipeSystem review fix

_Date:_ 2026-05-12
_Branch:_ `agent/sprint-faz-2-recipe-system`
_PR:_ https://github.com/msbel5/alcyone-ember-rpg/pull/104
_Box:_ `[box=PROCESS]` / `[box=MATTER]`
_Thalamus:_ `pkt_20260512183153_8271016f5974` / `sha256:54066abca2ba8d54b1385a645f5b235385d8e1bca23451a7454a54960b09a11b`
_Atom map:_ `DOCS/sprint-faz-2-atom-map.md`

## Increment goal

Address Copilot review on PR #104 without widening the Faz 2 scope: make RecipeSystem output production explicitly unit-based and align input availability/consumption around non-equipment stackable inventory items.

## Files changed

- `Assets/Scripts/Simulation/Process/RecipeSystem.cs` — documents/enforces one-unit output factory results, validates output template ids, preflights output placement before completing the work order, ignores equipment for recipe input availability, and consumes stackable inputs through a dedicated inventory method.
- `Assets/Scripts/Domain/Inventory/InventoryState.cs` — adds `TryRemoveStackable` for recipe consumers that must not remove equipment instances sharing a template id.
- `Assets/Tests/EditMode/Process/RecipeSystemTests.cs` — pins rejection of bundled output factories, verifies output-placement failures leave the order retryable, and verifies same-template equipment survives input consumption.
- `DOCS/sprint-faz-2-recipe-system-review-fix.md` — records this review-fix increment.

## Validation

- `git diff --check`: PASS.
- `./tools/validation/run-validation.sh --mode fallback`: PASS (`Passed: 812, Failed: 0`, `fallback_exit_code=0`, `validation-output/fallback-test-results/fallback.trx`).

## Product-visible proof

No new player-facing behavior; this is a narrow review-hardening follow-up for the existing visible RecipeSystem EventLog slice.

## Sprint accounting

- Atom rows landed in this increment: 0 new map rows; hardens the already-landed `RecipeSystem` + tests atom.
- Bundle count impact: +0.
- Product-visible PR count for Faz 2 remains: 1.

## Next increment

After PR #104 is merged, continue the Save/load and player-facing proof sub-area: serialize active recipe/worksite progress only after runtime state exists, then add `DOCS/sprint-faz-2-smelt-iron-acceptance.md` with a deterministic replay proof for crafting iron ingot from ore + fuel at a furnace.
