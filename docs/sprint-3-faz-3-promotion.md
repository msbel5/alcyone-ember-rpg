# Sprint 3 - Faz 3 promotion

## Goal

Promote Faz 3 after the job-assignment acceptance proof merged into `main`.
This increment is evidence-only: it records the final atom count, merged bundle
coverage, product-visible proof, validation result, and next sprint direction.

## Files changed

- `DOCS/sprint-faz-3-atom-map.md`
- `DOCS/sprint-3-faz-3-promotion.md`

## Promotion evidence

- Final atom count: 40 implementation atoms checked in
  `DOCS/sprint-faz-3-atom-map.md`.
- Bundle count: 6 suggested bundles represented by merged PRs:
  `job-primitives`, `job-board`, `actor-job-state`, `assignment-system`,
  `competition-proof`, and `job-save-proof`.
- Product-visible PR count: 2. PR #116 added job assignment/completion event-log
  proof, and PR #119 added the deterministic playable acceptance proof.
- Active bot-review queue: no unaddressed `msbel5/alcyone-ember-rpg` entries
  found before this increment.

## Merged PR coverage

- #108 `sprint faz 3: map job assignment atoms and add primitives`
- #109 `feat: add actor job state bundle`
- #112 `sprint 3: add job assignment system`
- #113 `sprint 3: require job recipe inputs`
- #114 `Sprint 3: start claimed recipe work`
- #115 `Sprint 3: tick assigned jobs`
- #116 `Sprint 3: add competition fixtures and job event log`
- #118 `sprint 3: persist job save roundtrip`
- #119 `Sprint 3: job assignment acceptance proof`

## Validation

- `git diff --check` - passed.
- `./tools/validation/run-validation.sh --mode fallback` - passed at
  2026-05-15T16:19Z on `agent/sprint-3-faz-3-promotion-summary` with fallback harness result:
  878 passed, 0 failed, 0 skipped.
- Unity editor status: blocked on this machine (`not_found`), as expected for
  the fallback validation path.

## Acceptance sentence

`player can set 2 actors to "smith" priority 1, watch both queue at the furnace, and produce 4 ingots in a deterministic day`.

## Thalamus

- packet_id: `pkt_20260515121719_3fd30f4f5521`
- resolver_key: `sha256:9199996e3c05fd7be506140f1e1c1f617ca46f58fdf77adc9f8e62d335d11fa4`

## Next increment

Start Faz 4 by writing the colony-needs atom map against
`DOCS/mechanic-map-v1.md`, with a product-visible `player can ...` acceptance
sentence before implementation work begins.
