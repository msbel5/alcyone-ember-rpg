# Sprint 5 — Shield-Buff Batch Totals PartitionMany (Predicate-Filter Overload)

## Scope

Single shippable increment: add a deterministic predicate-filtered overload of
`PartitionMany` to `ShieldBuffAbsorptionBatchTotals`, plus a matching
service-level wrapper on `ShieldBuffService`, plus xunit-style coverage. Pure
simulation library code, additive, no Unity dependency, no save/registry/tick
coupling.

## Why

`ShieldBuffAbsorptionBatchTotals` already exposes:

- `MergeMany(sequence)` — unfiltered cross-batch fold
- `MergeMany(sequence, predicate)` — predicate-filtered cross-batch fold
- `PartitionMany(sequence, predicate)` — cross-batch binary split into Included
  and Excluded buckets
- `GroupBy(map, keyExtractor)` and `GroupBy(map, keyExtractor, predicate)` —
  unfiltered and pre-filtered N-way group-by over a single batch result map

The natural sibling that was missing is the cross-batch analog of the
filter+group pattern already established on `GroupBy`: a `PartitionMany`
overload that **first** drops elements via a `filterPredicate` and **then**
buckets the survivors via the existing `includePredicate`. Without it, a future
combat damage-resolution pass or telemetry/UI surface that wants "side-versus
side split (offensive vs defensive) over a tagged subset (only sub-passes that
actually saw any absorption)" would have to either rebuild the sequence first
or run two filtered `MergeMany` passes, which is the same gap the binary
`PartitionMany` already closed for the unfiltered case. The new overload
closes the filtered case while keeping the same guard order and `Empty`
identity / commutative-sum semantics as `MergeMany`, `PartitionMany`, and
`GroupBy`.

## Surface

`Assets/Scripts/Simulation/Magic/ShieldBuffAbsorptionBatchTotals.cs`

- New factory
  `PartitionMany(IEnumerable<ShieldBuffAbsorptionBatchTotals>, Func<ShieldBuffAbsorptionBatchTotals,bool> includePredicate, Func<ShieldBuffAbsorptionBatchTotals,bool> filterPredicate)`.
- The existing two-arg `PartitionMany(sequence, includePredicate)` overload now
  forwards to the new factory with `filterPredicate: null`, so the binary
  surface keeps its exact behaviour and tests.
- Returns a `ShieldBuffAbsorptionBatchTotalsPartition`.
- Walks the sequence exactly once, in-place. For each non-null snapshot, if
  `filterPredicate` is null **or** returns true, the snapshot is routed by
  `includePredicate` into the Included bucket (predicate true) or the Excluded
  bucket (predicate false). When `filterPredicate` rejects an element it
  contributes to neither bucket.
- Null-sequence and null-include-predicate guards mirror the binary
  `PartitionMany`. `filterPredicate` is allowed to be null and means "keep
  everything", matching the established pattern from
  `MergeMany(seq, predicate)` where `predicate=null` means unfiltered.
- Null-element guard with indexed message is preserved and runs **before any
  predicate is consulted**, matching `MergeMany(sequence, predicate)` and the
  binary `PartitionMany(sequence, predicate)` exactly.
- `Included` plus `Excluded` (merged via the existing binary `Merge`) equals
  `MergeMany(sequence, filterPredicate)` because each surviving snapshot
  contributes to exactly one bucket.
- Empty input sequence and a sequence whose every element is filtered out
  return two `Empty` buckets.

`Assets/Scripts/Simulation/Magic/ShieldBuffService.cs`

- New wrapper `PartitionBatchTotalsMany(sequence, includePredicate, filterPredicate)`
  forwarding to the new factory. Pure delegation — no aggregation rules added
  at the service layer.

## Invariants pinned by tests

`Assets/Tests/EditMode/Magic/ShieldBuffServiceBatchAbsorptionTotalsPartitionManyFilterTests.cs`

- Null sequence on the factory and on the service wrapper both throw
  `ArgumentNullException`.
- Null `includePredicate` on the factory and on the service wrapper both throw
  `ArgumentNullException`.
- A null element throws `ArgumentException` with the index in the message even
  when both `includePredicate` and `filterPredicate` would have rejected it
  (guard order preserved).
- `filterPredicate: null` makes the new overload behave exactly like the
  existing binary `PartitionMany(sequence, includePredicate)` overload, bucket
  for bucket.
- `filterPredicate: _ => true` also matches the binary overload bucket for
  bucket.
- `filterPredicate: _ => false` returns two `Empty` buckets.
- For non-trivial predicates the Included-plus-Excluded `Merge` equals
  `MergeMany(sequence, filterPredicate)` (pre-filter sum invariance).
- Running the new overload is bucket-for-bucket equivalent to running the
  binary `PartitionMany(includePredicate)` over a hand pre-filtered sequence.
- Permutation invariance: the two buckets do not depend on input ordering
  under both predicates.
- Service wrapper does not mutate the underlying `ShieldBuffStateRegistry`
  — `GetTrackedActorIds`, remaining ticks, and magnitude are unchanged after
  the call.

## Out of scope

- No new save fields, no new registry mutation, no new tick driver coupling.
- No spell-pipeline wiring of partition results; this is library-level
  scaffolding only.
- No Unity-side rendering or HUD changes.

## Validation

Repo-evidence based: Unity/.NET execution remains unavailable in this
environment. Validation runs in fallback mode via
`tools/validation/run-validation.sh --mode fallback`.

## Thalamus

- `thalamus_packet=pkt_20260506071729_9c13415383c8`
- `vector_query_present=true`
- `query_path=text` (no vector route was selected by the hub)
