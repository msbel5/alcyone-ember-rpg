# Sprint 5 — Shield Buff Batch Totals: Stacked-Filter Map-Level PartitionFrom

## Scope

Add a stacked-filter overload to the map-level `PartitionFrom` factory and the
`ComputeBatchTotalsPartition` service seam so callers can split a per-actor batch
absorption result map side-versus-side (Included / Excluded) over a pre-tagged
subset (e.g. only actors that absorbed damage) in a single deterministic pass —
without rebuilding the result map first. Mirrors the stacked-filter pattern
already established on:

- `ShieldBuffAbsorptionBatchTotals.PartitionMany(seq, includePredicate, filterPredicate)` (cross-batch binary partition)
- `ShieldBuffAbsorptionBatchTotals.GroupByMany(seq, keyExtractor, includePredicate, filterPredicate)` (cross-batch N-way group-by)
- `ShieldBuffAbsorptionBatchTotals.GroupBy(map, keyExtractor, includePredicate, filterPredicate)` (map-level N-way group-by, merged in PR #51)

This closes the matrix gap: the only remaining map-level partition surface that
did not yet stack a `filterPredicate` on top of its existing `includePredicate`
binary split.

## Public surface

### `ShieldBuffAbsorptionBatchTotals` (`Assets/Scripts/Simulation/Magic/ShieldBuffAbsorptionBatchTotals.cs`)

- New overload:

  ```csharp
  public static ShieldBuffAbsorptionBatchTotalsPartition PartitionFrom(
      IReadOnlyDictionary<string, ShieldBuffAbsorptionResult> resultsByActorId,
      Func<string, ShieldBuffAbsorptionResult, bool> includePredicate,
      Func<string, ShieldBuffAbsorptionResult, bool> filterPredicate);
  ```

- Existing 2-arg overload now delegates to the new 3-arg overload with
  `filterPredicate: null`. Public behavior is unchanged for current callers.

### `ShieldBuffService` (`Assets/Scripts/Simulation/Magic/ShieldBuffService.cs`)

- New forwarding seam:

  ```csharp
  public ShieldBuffAbsorptionBatchTotalsPartition ComputeBatchTotalsPartition(
      IReadOnlyDictionary<string, ShieldBuffAbsorptionResult> resultsByActorId,
      Func<string, ShieldBuffAbsorptionResult, bool> includePredicate,
      Func<string, ShieldBuffAbsorptionResult, bool> filterPredicate);
  ```

  Pure delegation; no new aggregation rules, no registry read, no buff/tick
  mutation, no save coupling.

## Semantics

- **Strict input contract is unchanged.** Every map entry is still validated
  for non-empty actor key and non-null per-actor result *before* either
  predicate is consulted. Guard order matches the unfiltered `PartitionFrom`
  and the `From` overloads exactly.
- **Filter consulted before include.** When `filterPredicate` rejects an entry,
  it contributes to *neither* bucket and `includePredicate` is not consulted
  for that entry.
- **`filterPredicate == null` → unfiltered binary partition.** With a null
  filter the new overload behaves exactly like the existing 2-arg overload.
- **Identity:** `Included + Excluded == From(map, filterPredicate)` because
  every kept entry contributes to exactly one bucket.
- **Order independence** still holds: both predicates are pure per-entry
  filters and the bucket counters remain commutative integer sums.

## Tests

`Assets/Tests/EditMode/Magic/ShieldBuffServiceBatchAbsorptionTotalsPartitionFromStackedFilterTests.cs`
covers:

- Argument guards: null map, null `includePredicate` (factory + service).
- Validation guard order: whitespace actor key and null per-actor result still
  throw `ArgumentException` even when `filterPredicate` would reject the
  entry.
- `filterPredicate: null` → parity with the 2-arg `ComputeBatchTotalsPartition`.
- All-rejecting filter → both buckets all-zero.
- All-accepting filter → parity with the 2-arg `ComputeBatchTotalsPartition`.
- Stacked filter (`absorbedAny`) on a 4-actor map → parity with the manual
  reference (filter the map first, then `PartitionFrom`).
- Identity check: `Included + Excluded` totals equal
  `ComputeBatchTotals(perActor, filterPredicate)` field-by-field.
- Filter-before-include guard order: when `filterPredicate` rejects, the
  `includePredicate` callback is never invoked for that entry.
- Order independence: reordering the input map yields field-equal partition
  totals.
- Registry non-mutation: tracked actor ids, remaining ticks, and magnitudes
  are unchanged after the call.

## Out of scope

- Save/load coverage of partition outputs.
- Combat damage-resolution integration / telemetry-pipeline wiring.
- Tick-down or buff-application mutation tests.
- Cross-batch surfaces (already covered by previously merged sprint
  increments).

## Related sprint history

- PR #51: stacked-filter map-level `GroupBy` overload — same pattern applied
  to the N-way group-by surface.
- PR #50: stacked-filter cross-batch `GroupByMany` overload.
- PR #47: predicate-filtered cross-batch `PartitionMany` overload (3-arg).
