# Sprint Faz 2 — Atom-map kickoff

_Date:_ 2026-05-12
_Branch:_ `agent/sprint-faz-2-atom-map`
_Box:_ `[box=meta]` kickoff for Faz 2 `[box=PROCESS]` + `[box=MATTER]`
_Atom-map:_ `DOCS/sprint-faz-2-atom-map.md`
_Thalamus:_ `pkt_20260511213524_1a8c556dd6e1` / `sha256:f03d726809ee9adaf77d58d3a2505c4575a3eb382bcdfd94b66a6e038050fee7`

## Increment goal

Open Faz 2 (`Recipe + Worksite`) truthfully after Faz 1 promotion by decomposing the first PROCESS/MATTER slice before writing gameplay code.

## Files changed

- `DOCS/sprint-faz-2-atom-map.md` — canonical Faz 2 atom map against `DOCS/mechanic-map-v1.md` and `DOCS/agent-rules-v2.md`.
- `DOCS/sprint-faz-2-atom-map-kickoff.md` — this concise sprint summary.

## Validation

- `git diff --check`: PASS (no output).
- `./tools/validation/run-validation.sh --mode fallback`: PASS — `Passed!  - Failed:     0, Passed:   759, Skipped:     0, Total:   759`; `fallback_exit_code=0`; log `validation-output/validation-20260511T213830Z.log`; TRX `validation-output/fallback-test-results/fallback.trx`.

## Sprint accounting

- Initial atom count: 21 unchecked implementation/proof atoms + 1 checked atom-map row.
- Bundle count so far: 0 landed bundles; 2 candidate bundles documented.
- Product-visible PR count so far: 0. This kickoff is intentionally non-visible; the first visible target is the `RecipeSystem` EventLog / smelt proof slice.

## Next increment

Start the first pure definition atom: `RecipeId` + `RecipeIngredient` with focused EditMode tests, unless the next bot-review queue item identifies a higher-priority defect.
