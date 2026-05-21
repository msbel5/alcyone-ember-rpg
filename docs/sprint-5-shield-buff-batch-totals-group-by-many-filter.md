# Sprint 5 — Shield-Buff Batch Totals GroupByMany Filter (Cross-Batch)

## Scope

Single shippable increment: add a deterministic predicate-filtered
overload of the cross-batch `GroupByMany` factory on
`ShieldBuffAbsorptionBatchTotals`, plus a matching service-level
wrapper on `ShieldBuffService`, plus NUnit coverage. Pure simulation
library code, additive, no Unity dependency, no save/registry/tick
coupling. The unfiltered `GroupByMany(totals, keyExtractor)` is
refactored to delegate to the new overload with a `null` predicate so
both call paths share one deterministic walk.

## Why

`ShieldBuffAbsorptionBatchTotals` already exposes a layered family of
cross-batch folds:

- `MergeMany(sequence)` and `MergeMany(sequence, includePredicate)`
  — unfiltered and predicate-filtered cross-batch fold.
- `PartitionMany(sequence, includePredicate)` /
  `PartitionMany(sequence, includePredicate, filterPredicate)`
  — unfiltered and predicate-filtered cross-batch binary partition.
- `GroupByMany(sequence, keyExtractor)`
  — unfiltered cross-batch N-way group-by.
- Single-batch counterpart already has the filtered
  `GroupBy(map, keyExtractor, includePredicate)`.

The natural missing sibling is the predicate-filtered cross-batch
group-by: route each kept snapshot of a sequence into a per-group
bucket while skipping snapshots the caller has tagged out (e.g. only
sub-passes flagged offensive, or only ticks where any actor absorbed
damage). Without it, a future combat damage-resolution pass or
telemetry/UI surface that wants "per tick-phase totals over a tagged
subset" would have to either (a) materialize a filtered sequence first
and then call the unfiltered `GroupByMany`, or (b) run one
filtered `MergeMany` per discovered group key. Both are second-pass
work the new overload eliminates while keeping the same guard order,
`Empty` identity, and commutative-sum semantics as the existing
overloads.

## Surface

`Assets/Scripts/Simulation/Magic/ShieldBuffAbsorptionBatchTotals.cs`

- New factory
  `GroupByMany(IEnumerable<ShieldBuffAbsorptionBatchTotals>,
   Func<ShieldBuffAbsorptionBatchTotals,string> keyExtractor,
   Func<ShieldBuffAbsorptionBatchTotals,bool> includePredicate)`.
- Existing unfiltered
  `GroupByMany(IEnumerable<ShieldBuffAbsorptionBatchTotals>,
   Func<ShieldBuffAbsorptionBatchTotals,string> keyExtractor)`
  is refactored to delegate to the new overload with
  `includePredicate: null`. No change in observable behaviour.
- Returns `IReadOnlyDictionary<string, ShieldBuffAbsorptionBatchTotals>`
  with ordinal string-keyed buckets.
- Walks the sequence exactly once, in-place. For each non-null
  snapshot, the predicate is consulted; rejected snapshots contribute
  to no bucket. For each kept snapshot, the keyExtractor is consulted
  to produce a non-empty stable group key and the snapshot is merged
  into the per-key bucket via the existing binary `Merge`.
- Null-sequence and null-keyExtractor guards mirror the existing
  cross-batch overloads and run before the predicate is consulted.
- Null-element guard with indexed message is preserved and runs
  before either the predicate or the keyExtractor is consulted,
  matching `MergeMany(sequence, predicate)` and
  `PartitionMany(sequence, includePredicate, filterPredicate)`.
- Whitespace-only group keys throw `ArgumentException` referencing
  `keyExtractor` — but **only on a kept snapshot**, because the
  predicate runs first and a filtered-out snapshot never has its
  group key materialized. This matches the single-batch filtered
  `GroupBy(map, keyExtractor, predicate)` semantics.
- A `null` predicate behaves exactly like the unfiltered
  `GroupByMany(totals, keyExtractor)` factory.
- Empty input sequence and a fully filtered-out sequence both return
  an empty dictionary.
- The merge of all per-bucket totals equals
  `MergeMany(sequence, includePredicate)` because each kept snapshot
  contributes to exactly one bucket and the per-bucket counters
  remain commutative integer sums.

`Assets/Scripts/Simulation/Magic/ShieldBuffService.cs`

- New seam
  `GroupBatchTotalsByMany(IEnumerable<ShieldBuffAbsorptionBatchTotals>,
   Func<ShieldBuffAbsorptionBatchTotals,string> keyExtractor,
   Func<ShieldBuffAbsorptionBatchTotals,bool> includePredicate)`
  forwards to the new totals factory. Pure delegation, no new
  aggregation rules, no registry read, no buff/tick mutation, no
  save coupling.

`Assets/Tests/EditMode/Magic/ShieldBuffServiceBatchAbsorptionTotalsGroupByManyFilterTests.cs`

- Argument guards (null sequence, null keyExtractor, null element
  with indexed message, whitespace-only group key on a kept snapshot
  but not on a filtered-out snapshot).
- `null`-predicate equivalence with unfiltered `GroupByMany`.
- Empty-sequence and fully-filtered-out behaviour: both return an
  empty dictionary.
- Single-bucket equivalence to filtered `MergeMany`.
- Multi-bucket equivalence to per-bucket pre-filtered `MergeMany`.
- Bucket-union equals filtered `MergeMany` over the whole sequence.
- Permutation invariance.
- Service seam does not mutate the registry.

## Out of scope

- Save/load mapping (lives in PRD `ShieldBuffSaveMapper`).
- Application/tick-down/expiry pipelines.
- Triple-predicate cross-batch group-by (filter + key + secondary
  filter). If a future increment needs that, it can stack on top of
  the new overload without changing this surface.
- Unity rendering / UI binding.

## Test plan

- `./tools/validation/run-validation.sh --mode fallback`.
- New EditMode tests pass under the validation harness.

## References

- Bible: `docs/EMBER_VISION_BIBLE.md` §3 Layer 3 Magic.
- Mechanics: `docs/mechanics/MASTER_MECHANICS_BIBLE.md` §15 Magic
  effects.
- Prior increments: PRs #41–#48 (single-batch GroupBy / filtered
  GroupBy / cross-batch MergeMany / filtered MergeMany /
  PartitionMany / filtered PartitionMany / GroupByMany).
