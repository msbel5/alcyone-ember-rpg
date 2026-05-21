# Sprint 5 — Shield-Buff Batch Totals PartitionMany (Cross-batch Predicate Split)

## Scope

Single shippable increment: add a deterministic `PartitionMany(sequence, predicate)`
factory to `ShieldBuffAbsorptionBatchTotals` and a matching service-level wrapper on
`ShieldBuffService`, plus xunit-style coverage. Pure simulation library code,
additive, no Unity dependency, no save/registry/tick coupling.

## Why

`ShieldBuffAbsorptionBatchTotals` already exposes `PartitionFrom(map, predicate)`
(map-level binary split into Included/Excluded) and `MergeMany(sequence, predicate)`
(cross-batch predicate-filtered fold). What was missing is the cross-batch analogue
of `PartitionFrom`: a single deterministic walk over an arbitrary sequence of
already-computed batch totals snapshots that returns BOTH buckets — Included and
Excluded — as complete `ShieldBuffAbsorptionBatchTotals` snapshots. A future combat
damage-resolution pass or telemetry/UI surface that wants to summarize a side-versus
side split of an already-folded sequence (e.g. ticks where any actor absorbed damage
versus ticks where none did, or sub-passes flagged offensive versus defensive) had to
either run two filtered `MergeMany` passes or rebuild the sequence twice. The new
overload closes that gap with the same guard order and Empty-identity / commutative
sum semantics as `MergeMany` and `PartitionFrom`.

## Surface

`Assets/Scripts/Simulation/Magic/ShieldBuffAbsorptionBatchTotals.cs`
- New factory `PartitionMany(IEnumerable<ShieldBuffAbsorptionBatchTotals>, Func<ShieldBuffAbsorptionBatchTotals, bool>)`.
- Returns a `ShieldBuffAbsorptionBatchTotalsPartition`. The `Included` bucket is the
  fold of snapshots for which the predicate returned true; the `Excluded` bucket is
  the fold of snapshots for which the predicate returned false.
- Null-sequence and null-predicate guards mirror `PartitionFrom(map, predicate)`.
  Null-element guard with indexed message is preserved and runs **before** the
  predicate, matching the `MergeMany(sequence, predicate)` guard order exactly.
- `Included` plus `Excluded` (merged via the existing binary `Merge`) equals the
  unfiltered `MergeMany(sequence)` fold because each snapshot contributes to exactly
  one bucket.
- Empty input sequence and a sequence whose every element is filtered to one side
  return `Empty` for the other bucket.

`Assets/Scripts/Simulation/Magic/ShieldBuffService.cs`
- New wrapper `PartitionBatchTotalsMany(sequence, includePredicate)` forwarding to the
  new factory. Pure delegation — no aggregation rules added at the service layer.

## Invariants pinned by tests

`Assets/Tests/EditMode/Magic/ShieldBuffServiceBatchAbsorptionTotalsPartitionManyTests.cs`

- Null sequence on the factory and on the service wrapper both throw
  `ArgumentNullException`.
- Null predicate on the factory and on the service wrapper both throw
  `ArgumentNullException`.
- A null element throws `ArgumentException` with the index in the message even
  when `includePredicate` would have excluded it (guard order preserved).
- Empty sequence returns a partition whose Included and Excluded are both `Empty`.
- `PartitionMany(sequence, _ => true)` puts the entire fold into Included
  (matching `MergeMany(sequence)`) and leaves Excluded as `Empty`.
- `PartitionMany(sequence, _ => false)` puts the entire fold into Excluded
  (matching `MergeMany(sequence)`) and leaves Included as `Empty`.
- For a non-trivial predicate the Included-plus-Excluded `Merge` equals the
  unfiltered `MergeMany(sequence)` total — bucket conservation across the split.
- Each bucket also matches a hand-filtered `MergeMany` over the same predicate.
- Both buckets are permutation-invariant under predicates that depend only on
  per-snapshot fields.
- Service wrapper does not mutate the originating registry's tracked actors,
  remaining ticks, or magnitudes.

## Out of scope

- No save/load coverage.
- No application / tick-down / combat-pipeline coverage.
- No new aggregation rules at the service layer beyond delegation.
- No UI / presentation hookup.

## Validation

- `git diff --check`
- `./tools/validation/run-validation.sh --mode fallback`

## Thalamus

- packet_id: `pkt_20260506061756_9cdd296ce71f`
- resolver_key: `sha256:b2da4ae05b76d443783df63a7bb10265010946ff269d297e5366ea34e6b0c805`
- query_path: vector (inline_vector present, dim 1024, model qwen3-embedding-0.6b-q4_0, namespace atoms.code)
- vector_query_present: true
