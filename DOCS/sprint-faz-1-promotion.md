# Sprint Faz 1 — Promotion verification

_Date:_ 2026-05-11
_Branch:_ `agent/sprint-faz-1-promotion`
_Box:_ `[box=META]` promotion gate over Faz 1 `[box=WORLD]`, `[box=LIVING]`, `[box=MATTER]`, `[box=SOCIETY]`, `[box=PROCESS]`, `[box=TIME]`, and `[box=PLAYABLE]`
_Atom-map:_ `DOCS/sprint-faz-1-atom-map.md`
_Thalamus:_ `pkt_20260511210013_f56b937c99ed` / `sha256:079e34daa357165123c215fcfa1b2390360a992f177c3850b180c1c31b098d57`

## Increment goal

Verify the final Faz 1 promotion gate after the acceptance replay proof merged,
without adding new gameplay scope. This is the handoff increment that closes
Core Store reset and makes the next sprint factory run eligible to open Faz 2
(`Recipe + Worksite`).

## Promotion evidence

- Atom-map completion: `DOCS/sprint-faz-1-atom-map.md` has 27/27 atom rows
  checked.
- Sub-area coverage: ActorStore, ItemStore, SiteStore, FactionStore,
  WorldEventLog/ReasonTrace, runtime store roots, save/load round-trip, and the
  PLAYABLE acceptance proof all have merged PR evidence recorded in the atom map.
- Bot-review queue: `/home/msbel/.openclaw/state/pr_bot_reviews.jsonl` has 0
  unaddressed active entries for `msbel5/alcyone-ember-rpg` at verification time.
- Product-visible PR count: at least 1; PR #96 is the explicit deterministic
  replay/playable proof, and the store-root PRs expose canonical world state to
  gameplay code.

## Validation

- `git diff --check`: PASS (no output).
- `./tools/validation/run-validation.sh --mode fallback`: PASS — `Passed!  - Failed:     0, Passed:   759, Skipped:     0, Total:   759`; `fallback_exit_code=0`; log `validation-output/validation-20260511T210350Z.log`.

## Sprint accounting

- Final atom count: 27/27 atom-map rows checked.
- Bundle count: 0 promotion bundles; Faz 1 landed as small, independent PRs.
- Promotion status: Faz 1 is verified as promotion-ready on this branch.

## Files changed

- `DOCS/sprint-faz-1-promotion.md` — this promotion verification summary.
- `DOCS/sprint-faz-1-atom-map.md` — promotion checklist marked with evidence.

## Next increment

Open Faz 2 from `docs/ROADMAP.md`: decompose `RecipeDef`, `Worksite`,
`RecipeSystem`, `SmeltIronIngot`, and the deterministic 40-tick proof against
`DOCS/mechanic-map-v1.md` before writing the first Faz 2 gameplay atom.
