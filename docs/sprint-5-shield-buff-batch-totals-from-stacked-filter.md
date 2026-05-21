# Sprint 5 Shield Buff Batch Totals From Stacked Filter

Date: 2026-05-06
Branch: `agent/sprint-5-shield-buff-batch-totals-from-stacked-filter`
Base: `a2721e5` — stacked-filter (include + filter) cross-batch MergeMany
overload for shield-buff absorption totals (PR #53) merged on `origin/main`

## Scope

This increment adds a stacked-filter (include + filter) overload to the
map-level fold seam of the shield-buff absorption totals API. It mirrors
the stacked-filter pattern already established on:

- `ShieldBuffAbsorptionBatchTotals.PartitionFrom(map, includePredicate, filterPredicate)`
- `ShieldBuffAbsorptionBatchTotals.GroupBy(map, keyExtractor, includePredicate, filterPredicate)`
- `ShieldBuffAbsorptionBatchTotals.MergeMany(seq, includePredicate, filterPredicate)`

… and stacks it onto the most basic surface of the family — the
single-snapshot fold over a per-actor result map. With this overload,
every public surface in the batch-totals family now supports the same
two-stage filter contract: a cheap pre-filter that drops entries
outright, followed by the existing semantic include gate that selects
which kept entries get folded.

This is a strict pure aggregation extension; it does not touch the
registry, the per-actor buff bags, the input result map, the trace
contract, save/load, or the combat pipeline.

Implemented:

- `ShieldBuffAbsorptionBatchTotals.From(map, includePredicate, filterPredicate)`
  static factory. Walks the per-actor result map exactly once. Each
  entry is validated for non-empty actor key and non-null per-actor
  result before either predicate is consulted. `filterPredicate` is
  consulted before `includePredicate`; entries it rejects contribute to
  no fold. Surviving entries are then routed through the existing
  `includePredicate` gate. Pure: no Unity dependency, no presentation
  coupling, no registry read, no buff/tick mutation, no save coupling.
- The existing 2-arg `From(map, includePredicate)` is now a thin
  delegation to the 3-arg overload with `filterPredicate: null`,
  preserving its semantics by construction.
- `ShieldBuffService.ComputeBatchTotals(map, includePredicate, filterPredicate)`
  service seam — a pure delegation wrapper that forwards to the new
  factory so callers stay on the service for the whole
  absorb-then-aggregate flow.

Behavior:

- `null` map &rarr; `ArgumentNullException` on both factory and service
  even when both predicates are non-null.
- whitespace actor key or null per-actor result &rarr; `ArgumentException`
  before either predicate is consulted (guard order matches unfiltered
  `From(map)` and predicate-only `From(map, predicate)` exactly).
- `filterPredicate == null` and `includePredicate` non-null &rarr;
  behaves exactly like `From(map, includePredicate)`.
- both predicates null &rarr; behaves exactly like `From(map)`.
- `filterPredicate` returns false for every kept entry &rarr; returns
  the all-zero totals (every counter is zero, `ActorCount == 0`).
- `filterPredicate` returns true for every kept entry &rarr; result is
  field-wise equal to `From(map, includePredicate)`.
- `includePredicate` returns true for every entry &rarr; result is
  field-wise equal to `From(filteredMap)` where `filteredMap` is the
  caller-side hand-pre-filtered map. (Verified by parity test that
  builds `filtered` explicitly and compares field-by-field.)
- `filterPredicate` is consulted before `includePredicate`. Verified
  by counter test: when `filterPredicate` returns false the
  `includePredicate` lambda is never invoked.
- order independence: feeding the same logical entries in a different
  insertion order produces field-wise equal totals.
- registry isolation: calling the service seam does not change tracked
  actor ids, remaining-tick counters, or per-buff magnitudes.

Tests added (EditMode):

- `ShieldBuffServiceBatchAbsorptionTotalsFromStackedFilterTests`:
  - `From_StackedFilter_NullResultMap_Throws`
  - `ComputeBatchTotals_StackedFilter_NullResultMap_Throws`
  - `From_StackedFilter_WhitespaceActorKey_StillThrowsBeforeFilters`
  - `From_StackedFilter_NullActorResult_StillThrowsBeforeFilters`
  - `ComputeBatchTotals_StackedFilter_NullFilter_BehavesLikePredicateOnlyOverload`
  - `From_StackedFilter_BothPredicatesNull_MatchesUnfilteredFrom`
  - `ComputeBatchTotals_StackedFilter_FilterRejectsAll_TotalsAllZero`
  - `ComputeBatchTotals_StackedFilter_FilterAcceptsAll_ParityWithPredicateOnlyOverload`
  - `ComputeBatchTotals_StackedFilter_OnlyAbsorbingAllies_ParityWithFilteredThenIncluded`
  - `ComputeBatchTotals_StackedFilter_FilterConsultedBeforeIncludePredicate`
  - `ComputeBatchTotals_StackedFilter_OrderIndependent`
  - `ComputeBatchTotals_StackedFilter_DoesNotMutateRegistry`

## Out of scope

- Application of the new fold to the combat damage-resolution pipeline.
- Telemetry / UI surface that consumes the new fold.
- Save/load contract (totals remain a derived snapshot, not persisted).
- Tick-down / sweep behavior (untouched).

## Rationale

After the cross-batch `MergeMany` stacked-filter overload landed
(PR #53) the only batch-totals surface left without stacked-filter
parity was the most basic one: the single-map fold. Adding it closes
the symmetry across the whole family (`From` / `PartitionFrom` /
`GroupBy` at the map level, and `MergeMany` / `PartitionMany` /
`GroupByMany` at the cross-batch level), so a future combat
damage-resolution pass or telemetry/UI surface can fold a tagged subset
of a tagged subset of one batch absorption result map (e.g. only
allies that also absorbed damage) in a single deterministic walk
without rebuilding the map first.

## Thalamus packet

- packet_id: `pkt_20260506151704_6b2577997910`
- resolver_key: `sha256:ac19ece7c9f403a5cb4f2182623ebc4e13c370165787de94b108f44ac7f20238`
- query_path: vector
