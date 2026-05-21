# Sprint 5 Shield Buff Batch Totals Partition

Date: 2026-05-05
Branch: `agent/sprint-5-shield-buff-batch-totals-partition`
Base: `2d8cfb3` — Sprint 5 deterministic batch shield-buff absorption totals predicate-filter overload (PR #39) merged on `origin/main`

## Scope

This increment adds a deterministic single-pass partition seam over the batch
shield-buff absorption result map produced by `ShieldBuffService.AbsorbDamageForActors`
(introduced in earlier Sprint 5 slices). A future combat damage-resolution pass,
telemetry surface, or UI feedback layer can now compute side-A vs side-B
(allies-only / enemies-only, absorbed-vs-untouched, hostile-vs-neutral, or any
other per-actor binary split) totals from one batch absorption call in a single
deterministic walk over the per-actor result map, instead of running two
filtered `ComputeBatchTotals` calls back-to-back.

This is a strict read-only post-pass over an already-computed absorption result;
it does not touch the registry, the per-actor buff bags, or the trace contract.

Implemented:

- `ShieldBuffAbsorptionBatchTotalsPartition` — small immutable two-bucket value
  carrying `Included` and `Excluded` totals as full
  `ShieldBuffAbsorptionBatchTotals` snapshots. Constructor rejects null buckets.
- `ShieldBuffAbsorptionBatchTotals.PartitionFrom(IReadOnlyDictionary<string, ShieldBuffAbsorptionResult>, Func<string, ShieldBuffAbsorptionResult, bool> includePredicate)`
  static factory that walks the input map exactly once, validating each entry
  (non-empty actor key, non-null per-actor result) before consulting the
  predicate, then routes the entry to either the included or the excluded
  bucket based on the predicate return.
- `ShieldBuffService.ComputeBatchTotalsPartition(IReadOnlyDictionary<string, ShieldBuffAbsorptionResult>, Func<string, ShieldBuffAbsorptionResult, bool> includePredicate)`
  wrapper that delegates to the new factory so callers can stay on the service
  for the whole absorb-then-partition flow.

Behavior:

- `null` result map &rarr; `ArgumentNullException` (factory and service).
- `null` predicate &rarr; `ArgumentNullException`. Unlike the existing
  filter overload, partition has no "aggregate everything" interpretation when
  the predicate is missing because the partition surface is inherently
  predicate-keyed.
- whitespace/empty actor key in the input map &rarr; `ArgumentException`,
  even when the predicate would otherwise route the entry to a specific
  bucket.
- `null` per-actor result value &rarr; `ArgumentException`, even when the
  predicate would otherwise route the entry to a specific bucket.
- predicate returns true &rarr; entry contributes to `Included` totals only.
- predicate returns false &rarr; entry contributes to `Excluded` totals only.
- every entry contributes to exactly one bucket, so
  `Included.Field + Excluded.Field` equals the unfiltered
  `ComputeBatchTotals(map).Field` for every numeric field, including
  `ActorCount` and `ActorsWithAbsorption`.
- aggregation order has no observable effect because the predicate is a pure
  per-entry filter and both buckets accumulate commutative sums and counts.
- registry is not read by this method; the registry is not mutated; per-actor
  buff bags are not mutated; the input result map is not mutated; trace lists
  are not modified.

Pure Simulation: no Unity types, no save coupling, no tick mutation, no
combat-pipeline call.

## Why this slice matters

PR #39 introduced a per-actor predicate-filter overload over the batch totals
aggregate. That overload solves "summarize one slice" cleanly, but the most
common combat HUD / telemetry use is "summarize both sides at once" — for
example, total damage absorbed by the player party vs total damage absorbed by
hostile actors in the same combat round. Without a partition surface, callers
either run `ComputeBatchTotals(map, predicate)` and then
`ComputeBatchTotals(map, !predicate)` (walking the input map twice and paying
the validation cost twice) or build two subset maps first (allocating two extra
dictionaries per round). The partition factory moves that pair-walk into one
deterministic helper next to the rest of the shield-buff API surface, keeps
strict input validation identical to the unfiltered factory, and preserves the
order-independence guarantee of the underlying sums and counts.

The partition surface is intentionally narrower than a general grouping API
(`Func<..., string?>` bucket-key selector returning a map of bucket-key to
totals): the binary split is the dominant Sprint 5 consumer (allies vs enemies,
absorbed vs untouched, party vs npc), it is much easier to validate
deterministically, and it composes cleanly with the existing per-actor
predicate filter overload that is already used by call-sites that genuinely
want a single-bucket slice.

## Bible Back-References

- `docs/EMBER_VISION_BIBLE.md` §3: Sprint 5 remains Layer 3 gameplay mechanics;
  this aggregate is pure Domain/Simulation orchestration with no AI dependency.
- `docs/EMBER_VISION_BIBLE.md` §8: Sprint 5 keeps shipping narrow, testable
  magic increments instead of pretending the full combat-absorption stack is
  wired.
- `docs/mechanics/MASTER_MECHANICS_BIBLE.md` §15: shield-buff absorption stays
  composable with the existing actor-keyed registry without changing per-buff
  resolution rules.

## Validation

- `git diff --check` clean
- `tools/validation/run-validation.sh --mode fallback`:
  `Passed: 391, Failed: 0, Skipped: 0, Total: 391` (12 new partition tests
  added on top of the 379-test baseline from PR #39)
- new EditMode test class `ShieldBuffServiceBatchAbsorptionTotalsPartitionTests`
  covers: null map, null predicate (factory and service), whitespace/empty
  actor key, null per-actor result, empty map zero-bucket pass, all-included
  bucket equals unfiltered batch totals, all-excluded bucket equals unfiltered
  batch totals, allies-vs-enemies side split, included-plus-excluded sums match
  unfiltered batch totals, parity vs running two filtered `ComputeBatchTotals`
  calls back-to-back, and registry non-mutation.
- Thalamus packet: `pkt_20260505191741_1e728529a696` (vector path, inline
  vector present, confidence 0.35, escalation_reason recorded).

## What this slice does NOT do

- it does not introduce a general grouping-by-key surface (multi-bucket
  totals); a binary split was sufficient for the dominant Sprint 5 use case
  and avoids hidden allocation costs.
- it does not change `ShieldBuffAbsorptionBatchTotals` field semantics or the
  unfiltered `From(map)` and predicate-filter `From(map, predicate)`
  factories — the partition factory is additive.
- it does not call into combat damage resolution; the partition seam is for
  read-only aggregation only, mirroring the rest of Sprint 5's shield-buff
  service surface.
- it does not introduce save/load coupling; no new persisted state is
  produced by this slice.
