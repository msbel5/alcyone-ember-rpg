# Sprint 5 — Shield-Buff Batch Totals MergeMany (Filtered Overload)

## Scope

Single shippable increment: add a deterministic predicate-filtered overload for
`ShieldBuffAbsorptionBatchTotals.MergeMany` and a matching service-level wrapper
on `ShieldBuffService`, plus xunit-style coverage. Pure simulation library code,
additive, no Unity dependency, no save/registry/tick coupling.

## Why

`ShieldBuffAbsorptionBatchTotals` already exposes a predicate-filter pattern on
`From(map, predicate)` and on `GroupBy(map, keyExtractor, predicate)`, but
`MergeMany(sequence)` only had an unfiltered fold. A future combat damage-resolution
pass or telemetry/UI surface that wants to roll only a tagged subset of cross-batch
snapshots (e.g. only ticks where any actor absorbed damage, only sub-passes flagged
as offensive) had to rebuild the sequence first. The filtered overload closes that
gap with the same guard order and the same Empty-identity / commutative-sum
semantics as the existing fold.

## Surface

`Assets/Scripts/Simulation/Magic/ShieldBuffAbsorptionBatchTotals.cs`
- New overload `MergeMany(IEnumerable<ShieldBuffAbsorptionBatchTotals>, Func<ShieldBuffAbsorptionBatchTotals, bool>)`.
- Existing unfiltered `MergeMany(sequence)` now forwards to the filtered overload
  with `includePredicate: null`, so behaviour is unchanged.
- Null-sequence guard preserved. Null-element guard with indexed message preserved
  and runs **before** the predicate, matching the unfiltered overload's guard order.
- When `includePredicate == null` the fold matches the unfiltered overload exactly.
- Empty input sequence and a sequence whose every element is filtered out both
  return `Empty`.

`Assets/Scripts/Simulation/Magic/ShieldBuffService.cs`
- New wrapper `MergeBatchTotalsMany(sequence, includePredicate)` forwarding to the
  new factory overload. Pure delegation — no aggregation rules added at the service
  layer.

## Invariants pinned by tests

`Assets/Tests/EditMode/Magic/ShieldBuffServiceBatchAbsorptionTotalsMergeManyFilterTests.cs`

- Null sequence on the factory and on the service wrapper both throw
  `ArgumentNullException`.
- A null element throws `ArgumentException` with the index in the message even
  when `includePredicate` would have excluded it (guard order is preserved).
- `MergeMany(sequence, null)` matches the unfiltered `MergeMany(sequence)` overload.
- `MergeMany(sequence, _ => true)` matches the unfiltered overload.
- `MergeMany(sequence, _ => false)` returns `Empty`.
- Empty sequence returns `Empty`.
- Filtered fold matches an unfiltered fold over a hand-filtered copy of the same
  sequence (parity with pre-filter).
- Filtered fold is permutation-invariant under predicates that depend only on
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

- packet_id: `pkt_20260506051627_ac99ae5fe876`
- resolver_key: `sha256:980056cbba4a8faab131dce8ca7cf71f07c7b854391793ff8023f26cdc6d5458`
- query_path: vector (inline_vector present, dim 1024, model qwen3-embedding-0.6b-q4_0, namespace atoms.code)
- vector_query_present: true
