# Sprint 4 - Colony needs atom map

## Goal

Open Faz 4 by recording the colony-needs atom map before gameplay code begins.
This keeps the next sprint aligned with `docs/mechanic-map-v1.md`,
`docs/agent-rules-v2.md`, and the Faz 4 acceptance sentence in
`docs/ROADMAP.md`.

## Files changed

- `docs/sprint-faz-4-atom-map.md`
- `docs/sprint-4-colony-needs-atom-map.md`

## Behaviour

No gameplay code changed in this increment. The new atom map decomposes Faz 4
into small LIVING/PROCESS atoms for needs, mood, needs ticking, eat/sleep
recovery, job refusal, save/load, and final deterministic replay proof.

## Product-visible count

Product-visible PR count for this increment: 0. This is the sprint kickoff map;
the first product-visible Faz 4 PR should land through the needs tick/event-log
or final acceptance proof rails.

Acceptance target: `player can let an actor go three days without food, see mood fall, see them refuse to work, and recover after a meal`.

## Validation

- `git diff --check` and `git diff --cached --check` - passed.
- `./tools/validation/run-validation.sh --mode fallback` - passed at
  2026-05-15T20:51Z with fallback harness result:
  878 passed, 0 failed, 0 skipped.
- Unity editor status: blocked on this machine (`not_found`), as expected for
  the fallback validation path.

## Thalamus

- packet_id: `pkt_20260515204915_e1c5ad32792f`
- resolver_key: `sha256:b288c6a454567c4784185bc4de8dd77d791ba3aae1593887763f9bd529de2e50`
- AoT session: `pkt_20260515204915_e1c5ad32792f`

## Bot-review queue

No unaddressed `msbel5/alcyone-ember-rpg` review entries were found before
this increment.

## Next increment

Implement the pure `needs-primitives` bundle: `NeedKind`, `NeedValue`,
`ActorNeeds`, and focused EditMode tests.
