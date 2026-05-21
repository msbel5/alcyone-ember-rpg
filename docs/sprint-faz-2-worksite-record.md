# Sprint Faz 2 — WorksiteRecord pure component

_Date:_ 2026-05-12
_Branch:_ `agent/sprint-faz-2-worksite-record`
_Box:_ `[box=WORLD]` / `[box=PROCESS]`
_Thalamus:_ `pkt_20260512163304_3231f1e1dfa6` / `sha256:cddef5e203ed5a4a21cb318412b64738176fc34ea11841722529208f90a60e5b`
_Atom map:_ `DOCS/sprint-faz-2-atom-map.md`

## Increment goal

Land the first Worksite state atom bundle for Faz 2: a typed `WorksiteKind` sentinel/furnace enum and immutable `WorksiteRecord` site-cell component that RecipeSystem can later validate against.

## Files changed

- `Assets/Scripts/Domain/Process/WorksiteKind.cs` — adds the minimal typed worksite category (`None`, `Furnace`).
- `Assets/Scripts/Domain/Process/WorksiteRecord.cs` — adds a pure immutable worksite component with site id, grid position, kind, active flag, and `WithActive` replacement helper.
- `Assets/Tests/EditMode/Process/WorksiteRecordTests.cs` — pins constructor storage, empty-site rejection, `None` rejection, inactive state, and immutable active-state replacement.
- `DOCS/sprint-faz-2-atom-map.md` — checks off the WorksiteKind, WorksiteRecord, and WorksiteRecordTests atoms.
- `DOCS/sprint-faz-2-worksite-record.md` — records this sprint summary.

## Validation

- `git diff --check`: PASS (no output).
- `./tools/validation/run-validation.sh --mode fallback`: PASS — `fallback_exit_code=0`; `Passed: 791, Failed: 0, Skipped: 0`; Unity editor blocked because editor binary is not installed in this environment (`STATUS unity_editor=BLOCKED reason=not_found`); TRX `validation-output/fallback-test-results/fallback.trx`.

## Sprint accounting

- Atom rows landed in this increment: 3 (`WorksiteKind`, `WorksiteRecord`, `WorksiteRecordTests`).
- Bundle count impact: 1 small Worksite state bundle.
- Product-visible PR count for Faz 2: unchanged at 0; visible target remains `RecipeSystem` emitting an ordered EventLog line.

## Next increment

Continue Worksite state with `WorksiteStore` + tests, then use it in the smallest visible `RecipeSystem` EventLog slice for `SmeltIronIngot`.
