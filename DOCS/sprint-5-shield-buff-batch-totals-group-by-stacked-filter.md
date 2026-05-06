# Sprint 5 - Shield Buff Batch Totals GroupBy Stacked-Filter

## Increment
Added a stacked-filter (filter + key + secondary filter) overload to the map-level
`ShieldBuffAbsorptionBatchTotals.GroupBy` factory and the matching
`ShieldBuffService.GroupBatchTotals` seam. Mirrors the stacked-filter pattern already
shipped on the cross-batch `GroupByMany(seq, keyExtractor, includePredicate, filterPredicate)`
overload (PR #50) and on `PartitionMany(seq, includePredicate, filterPredicate)`.

The new overload walks the per-actor result map exactly once, applies `filterPredicate`
before `includePredicate`, and routes each kept entry into a per-group bucket keyed by
the caller-supplied `keyExtractor`. The 3-arg `GroupBy` overload now delegates to the new
4-arg version with `filterPredicate: null`, preserving its existing semantics.

## Branch & Commit
- Branch: `agent/sprint-5-shield-buff-batch-totals-group-by-stacked-filter`
- Base: `main` (commit `bee2fb1`)

## Files Touched
- `Assets/Scripts/Simulation/Magic/ShieldBuffAbsorptionBatchTotals.cs`
  Added 4-arg `GroupBy(map, keyExtractor, includePredicate, filterPredicate)` overload;
  redirected the 3-arg overload to the 4-arg via `filterPredicate: null`.
- `Assets/Scripts/Simulation/Magic/ShieldBuffService.cs`
  Added 4-arg `GroupBatchTotals` seam delegating to the new factory.
- `Assets/Tests/EditMode/Magic/ShieldBuffServiceBatchAbsorptionTotalsGroupByStackedFilterTests.cs`
  New EditMode test class covering argument guards, null-predicate parity vs the 3-arg
  and unfiltered overloads, empty/filter-rejects-all/include-rejects-all paths,
  filterPredicate-before-includePredicate evaluation order, per-bucket parity vs
  two-condition filtered `ComputeBatchTotals`, sum-of-buckets parity vs stacked-filter
  `ComputeBatchTotals`, and registry no-mutation.

## Strict Input Contract (preserved)
- `null` map -> `ArgumentNullException` (before either predicate or keyExtractor).
- `null` keyExtractor -> `ArgumentNullException`.
- whitespace actor key -> `ArgumentException` before predicates.
- `null` per-actor result -> `ArgumentException` before predicates.
- whitespace group key on a kept entry -> `ArgumentException`; whitespace on
  filter- or include-rejected entries does not throw because the keyExtractor is
  not consulted on rejected entries.
- Group keys use `StringComparer.Ordinal`.

## Validation
- `git diff --check`: clean.
- `./tools/validation/run-validation.sh --mode fallback`:
  Passed: 547, Failed: 0, Skipped: 0 (Duration 759 ms).
- Unity editor: BLOCKED (not_found). Fallback harness path documented in
  `tools/validation/run-validation.sh --help`.

## Thalamus Routing Evidence
- packet_id: `pkt_20260506111624_fe5ef0e54f78`
- resolver_key: `sha256:faa73d02380f57077a6e97c7de4381c55040acf880341d90d67016528ae5435c`
- inline_vector_present: true (1024-dim, namespace `atoms.code`, model
  `qwen3-embedding-0.6b-q4_0`)
- query_path: vector
- confidence: 0.353 (escalated to Captain decomposition; chose smallest shippable
  symmetric overload to keep churn minimal).

## Out of scope
No save/load, application, tick-down, or combat-pipeline coupling. Pure aggregation
seam, no Unity dependency, no registry read, no buff/tick mutation.

## Next Recommended Increment
A symmetric stacked-filter on `MergeMany(seq, includePredicate, filterPredicate)` to
complete the cross-batch parity set, or pivot to wiring the existing GroupBy seams
into a Sprint 5 telemetry/UI consumer rather than adding more overload symmetry.
