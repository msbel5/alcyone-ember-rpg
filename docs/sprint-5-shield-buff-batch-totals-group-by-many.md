# Sprint 5 — Shield-Buff Batch Totals GroupByMany (Cross-Batch)

## Scope

Single shippable increment: add a deterministic cross-batch `GroupByMany`
factory to `ShieldBuffAbsorptionBatchTotals`, plus a matching service-level
wrapper on `ShieldBuffService`, plus xunit-style coverage. Pure simulation
library code, additive, no Unity dependency, no save/registry/tick coupling.

## Why

`ShieldBuffAbsorptionBatchTotals` already exposes:

- `Merge(a, b)` and `MergeMany(sequence)` / `MergeMany(sequence, predicate)`
  — binary fold and unfiltered/filtered cross-batch folds
- `PartitionMany(sequence, includePredicate)` /
  `PartitionMany(sequence, includePredicate, filterPredicate)`
  — unfiltered/filtered cross-batch binary partition
- `GroupBy(map, keyExtractor)` and `GroupBy(map, keyExtractor, predicate)`
  — unfiltered/filtered single-batch N-way group-by over a result map

The natural sibling that was missing is the cross-batch analog of the
single-batch `GroupBy`: a factory that takes an arbitrary sequence of
already-computed batch absorption totals snapshots and routes each snapshot
into a per-group bucket keyed by a caller-supplied keyExtractor, combining
each bucket via the existing binary `Merge`. Without it, a future combat
damage-resolution pass or telemetry/UI surface that wants "per tick-phase
totals" or "per encounter-id totals" or "per faction totals" across a
sequence of sub-pass snapshots would have to either run multiple filtered
`MergeMany` passes (one per group key it has to discover separately) or
rebuild the sequence by group manually. The new `GroupByMany` overload
closes that gap while keeping the same guard order, `Empty` identity, and
commutative-sum semantics as `MergeMany`, `PartitionMany`, and the existing
single-batch `GroupBy`.

## Surface

`Assets/Scripts/Simulation/Magic/ShieldBuffAbsorptionBatchTotals.cs`

- New factory
  `GroupByMany(IEnumerable<ShieldBuffAbsorptionBatchTotals>, Func<ShieldBuffAbsorptionBatchTotals,string> keyExtractor)`.
- Returns
  `IReadOnlyDictionary<string, ShieldBuffAbsorptionBatchTotals>` with
  ordinal string-keyed buckets.
- Walks the sequence exactly once, in-place. For each non-null snapshot,
  the keyExtractor is consulted to produce a non-empty stable group key and
  the snapshot is merged into the per-key bucket using the existing binary
  `Merge` (so `Empty` is the per-bucket identity).
- Null-sequence and null-keyExtractor guards mirror the existing
  cross-batch overloads.
- Null-element guard with indexed message is preserved and runs **before
  the keyExtractor is consulted**, matching `MergeMany(sequence, predicate)`
  and `PartitionMany(sequence, includePredicate, filterPredicate)` exactly.
- Whitespace-only group keys throw `ArgumentException` referencing
  `keyExtractor`, mirroring the existing single-batch
  `GroupBy(map, keyExtractor)` factory.
- Empty input sequence returns an empty dictionary.
- The merge of all per-bucket totals equals `MergeMany(sequence)` because
  each snapshot contributes to exactly one bucket and the per-bucket
  counters remain commutative integer sums.

`Assets/Scripts/Simulation/Magic/ShieldBuffService.cs`

- New wrapper `GroupBatchTotalsByMany(sequence, keyExtractor)` forwarding
  to the new factory. Pure delegation — no aggregation rules added at the
  service layer.

## Invariants pinned by tests

`Assets/Tests/EditMode/Magic/ShieldBuffServiceBatchAbsorptionTotalsGroupByManyTests.cs`

- Null sequence on the factory and on the service wrapper both throw
  `ArgumentNullException`.
- Null `keyExtractor` on the factory and on the service wrapper both throw
  `ArgumentNullException`.
- A null element throws `ArgumentException` with the index in the message
  even when the keyExtractor would have produced a key (guard order
  preserved).
- Whitespace-only group key throws `ArgumentException` (`keyExtractor`
  parameter).
- Empty input sequence returns an empty dictionary.
- A single-bucket keyExtractor (constant key) is bucket-equal to
  `MergeMany(sequence)`.
- A two-bucket keyExtractor is bucket-for-bucket equal to running
  `MergeMany` over per-bucket pre-filtered subsequences.
- The merge of all per-bucket totals equals the unfiltered
  `MergeMany(sequence)` total (bucket-union invariance).
- Permutation invariance: per-key buckets do not depend on input ordering.
- Service wrapper does not mutate the underlying `ShieldBuffStateRegistry`
  — `GetTrackedActorIds`, remaining ticks, and magnitude are unchanged
  after the call.

## Out of scope

- No filtered `GroupByMany(sequence, keyExtractor, predicate)` overload
  yet. This is the unfiltered foundation; the filtered overload is the
  natural follow-up sprint increment.
- No save fields, registry mutation, or tick driver coupling.
- No spell-pipeline wiring of group-by results; this is library-level
  scaffolding only.
- No Unity-side rendering or HUD changes.

## Validation

Repo-evidence based: Unity/.NET execution remains unavailable in this
environment. Validation runs in fallback mode via
`tools/validation/run-validation.sh --mode fallback`. The full fallback
harness (493 tests including the 12 new GroupByMany tests) reports
`Passed: 493, Failed: 0`.

## Thalamus

- `thalamus_packet=pkt_20260506081655_e72cb026a20c`
- `thalamus_resolver_key=sha256:28f3972cf4da383805f223812ca4e56bc8e671926360063ecc31a62bba1a0945`
- `vector_query_present=false`
- `query_path=text` (no vector route was selected by the hub)
