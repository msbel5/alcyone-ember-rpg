# Sprint 5 — Shield-buff batch totals: stacked-filter cross-batch GroupByMany overload

## Goal

Stack the existing predicate-filter pattern on top of the cross-batch
group-by surface so a future combat damage-resolution pass or
telemetry/UI surface can compute N-way per-group totals (e.g. by tick
phase, by encounter id) over a tagged subset of cross-batch snapshots
(e.g. only sub-passes flagged offensive that also registered any
absorption) in a single deterministic walk without rebuilding the
sequence first. Mirrors `PartitionMany(seq, includePredicate,
filterPredicate)` at the GroupByMany level — the increment that the
prior `GroupByMany(seq, keyExtractor, includePredicate)` doc explicitly
flagged as out of scope and explicitly invited as a future stacked
overload.

## Surfaces touched

`Assets/Scripts/Simulation/Magic/ShieldBuffAbsorptionBatchTotals.cs`

- New 4-arg overload
  `GroupByMany(IEnumerable<ShieldBuffAbsorptionBatchTotals>,
   Func<ShieldBuffAbsorptionBatchTotals,string> keyExtractor,
   Func<ShieldBuffAbsorptionBatchTotals,bool> includePredicate,
   Func<ShieldBuffAbsorptionBatchTotals,bool> filterPredicate)`.
- Existing 3-arg overload now delegates to the new 4-arg overload with
  `filterPredicate: null`, so all prior callers keep their semantics
  byte-for-byte.
- `filterPredicate` is consulted **before** `includePredicate` so a
  pre-filter rejection short-circuits the per-group decision (this is
  pinned by a sentinel test that has `includePredicate` throw if it is
  ever called).
- Strict input contract is preserved: every element is validated for
  non-null with the same indexed message every other cross-batch
  overload emits, and that guard runs before either predicate or the
  keyExtractor is consulted.
- When `filterPredicate` is `null` the new overload behaves exactly
  like `GroupByMany(totals, keyExtractor, includePredicate)`. When both
  predicates are `null` it behaves exactly like
  `GroupByMany(totals, keyExtractor)`.
- The keyExtractor must still return a non-empty stable group key, and
  group keys still use ordinal string comparison.
- Union of all per-bucket totals (merged together) equals the
  pre-stacked-filtered `MergeMany(totals, snap =>
  (filterPredicate == null || filterPredicate(snap)) &&
  (includePredicate == null || includePredicate(snap)))` result because
  each kept snapshot still contributes to exactly one bucket.
- Pure aggregation: no Unity dependency, no presentation coupling, no
  registry read, no buff/tick mutation, no save coupling.

`Assets/Scripts/Simulation/Magic/ShieldBuffService.cs`

- New seam
  `GroupBatchTotalsByMany(IEnumerable<ShieldBuffAbsorptionBatchTotals>,
   Func<ShieldBuffAbsorptionBatchTotals,string> keyExtractor,
   Func<ShieldBuffAbsorptionBatchTotals,bool> includePredicate,
   Func<ShieldBuffAbsorptionBatchTotals,bool> filterPredicate)`
  forwards to the new totals factory. Pure delegation, no new
  aggregation rules, no registry read, no buff/tick mutation, no save
  coupling.

`Assets/Tests/EditMode/Magic/ShieldBuffServiceBatchAbsorptionTotalsGroupByManyStackedFilterTests.cs`

- Argument guards (null sequence, null keyExtractor, null element with
  indexed message before any predicate or keyExtractor is consulted,
  whitespace-only group key on a kept snapshot but **not** on a
  snapshot rejected by either predicate).
- Null-`filterPredicate` equivalence with the 3-arg overload.
- Both-predicates-null equivalence with the unfiltered overload.
- Empty-sequence, fully-filter-rejected, and fully-include-rejected
  sequences all return an empty dictionary.
- Sentinel test: `filterPredicate` is consulted before
  `includePredicate` (achieved by making `includePredicate` throw if
  called).
- Single-bucket equivalence to stacked-filter `MergeMany`.
- Multi-bucket equivalence to per-bucket pre-stacked-filtered
  `MergeMany`.
- Bucket-union equals stacked-filter `MergeMany` over the whole
  sequence.
- Permutation invariance.
- Service seam does not mutate the registry.

## Out of scope

- Save/load mapping (lives in PRD `ShieldBuffSaveMapper`).
- Application/tick-down/expiry pipelines.
- Any predicate count beyond `(filter + key + secondary filter)`. If a
  future increment needs more layers, it can stack on top of this
  overload without changing this surface.
- Unity rendering / UI binding.

## Test plan

- `./tools/validation/run-validation.sh --mode fallback`.
- New EditMode tests pass under the validation harness.

## References

- Bible: `docs/EMBER_VISION_BIBLE.md` §3 Layer 3 Magic.
- Mechanics: `docs/mechanics/MASTER_MECHANICS_BIBLE.md` §15 Magic
  effects.
- Prior increments: PRs #41–#49 (single-batch GroupBy / filtered
  GroupBy / cross-batch MergeMany / filtered MergeMany /
  PartitionMany / filtered PartitionMany / GroupByMany / filtered
  GroupByMany).
