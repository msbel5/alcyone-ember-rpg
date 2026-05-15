# Sprint 3 - Job assignment acceptance

## Goal

Complete the final Faz 3 playable-proof atom by recording the deterministic
job-assignment replay and the final `player can ...` acceptance sentence.

## Files changed

- `DOCS/sprint-faz-3-job-assignment-acceptance.md`
- `DOCS/sprint-3-job-assignment-acceptance.md`
- `DOCS/sprint-faz-3-atom-map.md`

## Behaviour

No gameplay code changed in this increment. The underlying Faz 3 implementation
already has deterministic assignment, competition, event-log, and save/load
tests. This increment makes that proof explicit so the sprint can be reviewed
against the playable acceptance gate instead of only implementation atoms.

## Product-visible count

Product-visible PR count for this increment: 1. The player-facing proof is the
deterministic event-log/save-load replay documented in
`DOCS/sprint-faz-3-job-assignment-acceptance.md`.

Acceptance sentence: `player can set 2 actors to "smith" priority 1, watch both queue at the furnace, and produce 4 ingots in a deterministic day`.

## Validation

- `git diff --check` - passed.
- `./tools/validation/run-validation.sh --mode fallback` - passed at
  2026-05-15T16:07Z (fallback harness: 878 passed, 0 failed, 0 skipped;
  Unity editor blocked: not found).

## Thalamus

- packet_id: `pkt_20260515160510_f936c3bdc2ce`
- resolver_key: `sha256:3704ccf03e10f2cb8881b2043e569eb7c666b8a4805ec4cc40c60a6419413e8f`

## Next increment

After this PR merges, promote Faz 3 only if the atom map and promotion checklist
are green on `main`; otherwise keep working the remaining unchecked promotion
item.
