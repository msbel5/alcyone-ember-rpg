# Faz 3 — Atom-map kickoff

_Date:_ 2026-05-14
_Branch:_ `agent/sprint-faz-3-job-assignment-atom-map`
_Box:_ `[box=PROCESS]` / `[box=LIVING]`
_Thalamus packet:_ `pkt_20260514123115_29f1c700862d`
_Resolver:_ `sha256:58a8f09009a23e253fc532730e6b8f560ee1f8847deac455cc24a17e49577d99`
_Atom map:_ `DOCS/sprint-faz-3-atom-map.md`

## Increment goal

Kick off Faz 3 by decomposing Job assignment into a mechanic-map-aligned atom
map before implementation. This keeps the next coding PRs small, testable, and
bound to the Faz 3 acceptance sentence:

`player can set 2 actors to "smith" priority 1, watch both queue at the furnace, and produce 4 ingots in a deterministic day`.

## Files changed

- `DOCS/sprint-faz-3-atom-map.md` — canonical Faz 3 atom map, bundle plan, promotion checklist, and next increment.
- `DOCS/sprint-faz-3-atom-map-kickoff.md` — this kickoff summary with validation/provenance.

## Validation

- `git diff --cached --check`: PASS (no whitespace errors).
- `./tools/validation/run-validation.sh --mode fallback`: PASS on 2026-05-14T12:36Z (`fallback_exit_code=0`, `Passed: 813, Failed: 0, Skipped: 0`; TRX `validation-output/fallback-test-results/fallback.trx`).

## Sprint accounting

- Checked atom-map rows this increment: 2 documentation rows.
- Product-visible PR count so far: 0. This kickoff is intentionally planning-only; the sprint still requires a visible/EventLog or playable-proof PR before promotion.
- Bundle count so far: 0 implementation bundles.

## Next increment

Implement the `job-primitives` bundle: `JobId`, `JobKind`, `JobPriority`, and focused EditMode tests. Do not add speculative utilities or event kinds until a concrete assignment-system consumer lands.
