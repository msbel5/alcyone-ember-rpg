# Sprint Faz 2 — WorksiteStore registry

_Date:_ 2026-05-12
_Branch:_ `agent/sprint-faz-2-worksite-store`
_Box:_ `[box=WORLD]` / `[box=PROCESS]`
_Thalamus:_ `pkt_20260512172605_70e9bd2bfe2e` / `sha256:27b28c9ecc4741edc2e79c1b1a200f96245e731fb218e152dd71df9d735e76a4`
_Atom map:_ `docs/sprint-faz-2-atom-map.md`

## Increment goal

Land the remaining Worksite state atom bundle for Faz 2: `WorksiteStore`, a pure dictionary-backed registry keyed by `SiteId` + `GridPosition` so the next `RecipeSystem` slice can resolve a furnace worksite deterministically.

## Files changed

- `Assets/Scripts/Domain/Process/WorksiteStore.cs` — adds deterministic site-cell lookup, duplicate/default-site rejection, remove/clear, and insertion-order enumeration for `WorksiteRecord`.
- `Assets/Tests/EditMode/Process/WorksiteStoreTests.cs` — pins add/get/try-get/remove/clear/enumeration behavior and same-position/different-site semantics.
- `docs/sprint-faz-2-atom-map.md` — checks off the WorksiteStore atom rows and records this packet metadata.
- `docs/sprint-faz-2-worksite-store.md` — records this sprint summary.

## Validation

- `git diff --check`: PASS (no output after trailing-blank-line fix).
- `./tools/validation/run-validation.sh --mode fallback`: PASS — `fallback_exit_code=0`; `Passed: 800, Failed: 0, Skipped: 0`; TRX `validation-output/fallback-test-results/fallback.trx`.

## Sprint accounting

- Atom rows landed in this increment: 2 (`WorksiteStore`, `WorksiteStoreTests`).
- Bundle count impact: 1 small Worksite state bundle.
- Product-visible PR count for Faz 2: unchanged at 0; visible target remains `RecipeSystem` emitting an ordered recipe `EventLog` line.

## Next increment

Add the smallest visible `RecipeSystem` slice for `SmeltIronIngot`: validate a furnace + recipe definition, consume 2 `iron_ore` + 1 `fuel`, produce 1 `iron_ingot` after 40 ticks, and append an ordered `WorldEventLog` line.
