# Sprint 5 — Shield-Buff Batch Totals MergeMany Stacked-Filter Overload

## Scope
Adds the deterministic stacked-filter overload of the cross-batch
`ShieldBuffAbsorptionBatchTotals.MergeMany` fold, plus the matching
`ShieldBuffService.MergeBatchTotalsMany(seq, includePredicate, filterPredicate)`
seam, so a future combat damage-resolution pass or telemetry/UI surface
can fold a tagged subset of a tagged subset of cross-batch totals
snapshots in one deterministic walk without rebuilding the input
sequence first.

This closes the only remaining hole in the cross-batch / map-level
fold-and-bucket family for stacked-filter symmetry. Until this PR,
PartitionMany, GroupByMany, GroupBy, and PartitionFrom each had a
`(includePredicate, filterPredicate)` form while MergeMany was still
limited to the single-predicate (`includePredicate` only) overload.

Foundation only. No combat-pipeline wiring, no save/load coupling, no
registry mutation, no application or tick-down hooks.

## Surface

`Assets/Scripts/Simulation/Magic/ShieldBuffAbsorptionBatchTotals.cs`

```csharp
public static ShieldBuffAbsorptionBatchTotals MergeMany(
    IEnumerable<ShieldBuffAbsorptionBatchTotals> totals,
    Func<ShieldBuffAbsorptionBatchTotals, bool> includePredicate,
    Func<ShieldBuffAbsorptionBatchTotals, bool> filterPredicate);
```

`Assets/Scripts/Simulation/Magic/ShieldBuffService.cs`

```csharp
public ShieldBuffAbsorptionBatchTotals MergeBatchTotalsMany(
    IEnumerable<ShieldBuffAbsorptionBatchTotals> totals,
    Func<ShieldBuffAbsorptionBatchTotals, bool> includePredicate,
    Func<ShieldBuffAbsorptionBatchTotals, bool> filterPredicate);
```

The 2-arg `MergeMany(totals, includePredicate)` overload now delegates
to the 3-arg form with `filterPredicate: null`, so existing callers see
identical behaviour.

## Semantics

- `filterPredicate` is consulted before `includePredicate`. Any
  snapshot it rejects contributes to no fold — it is dropped outright,
  the same way `PartitionMany`, `GroupByMany`, `GroupBy`, and
  `PartitionFrom` already drop pre-filtered entries.
- When `filterPredicate` is null the overload behaves exactly like
  `MergeMany(totals, includePredicate)`. When both predicates are null
  it behaves exactly like the unfiltered `MergeMany(totals)` factory.
- Strict input contract is preserved: every element is validated for
  non-null with the same indexed message (`"Totals sequence element at
  index {i} must not be null."`) the unfiltered overload emits BEFORE
  either predicate is consulted, so the guard order matches the
  existing overloads exactly.
- Order independence holds: both predicates are pure per-element
  filters and the included counters remain commutative integer sums
  seeded with `Empty`. An empty sequence and a sequence whose every
  element is filtered out both return `Empty`.

## Tests

`Assets/Tests/EditMode/Magic/ShieldBuffServiceBatchAbsorptionTotalsMergeManyStackedFilterTests.cs`

Foundation-only NUnit coverage:

- argument guards (null sequence, null element with index in message
  even when filtered out)
- null-`filterPredicate` parity with the predicate-only MergeMany
  overload
- both-null parity with the unfiltered MergeMany overload
- `filterPredicate` always-false returns Empty
- `filterPredicate` always-true matches the predicate-only overload
- parity with a hand-pre-filtered MergeMany call
- filter-before-include guard order (include predicate is not
  consulted when filter rejects)
- permutation invariance under both predicates
- registry/buff isolation: `MergeBatchTotalsMany` does not mutate the
  underlying `ShieldBuffStateRegistry`

## Validation

`./tools/validation/run-validation.sh --mode fallback`

Run from the pure-C# NUnit fallback harness so the pin runs without a
Unity editor binary on this host. Real EditMode coverage will be
re-run in CI when Unity becomes available.

## Next Increment Suggestions

1. A by-source labelled cross-batch fold (`MergeMany` returning a
   `(label → totals)` dictionary in a single pass) — the natural step
   beyond stacked-filter is "tag while folding" instead of "fold then
   bucket".
2. A `FromMany(seq of result maps)` factory that walks a sequence of
   raw `IReadOnlyDictionary<string, ShieldBuffAbsorptionResult>`
   directly into a single totals snapshot, matching the cross-batch
   surface but without the From → MergeMany double walk.
