# Sprint 5 Shield Buff Batch Totals Merge

Date: 2026-05-06
Branch: `agent/sprint-5-shield-buff-batch-totals-merge`
Base: `964f5e2` — Sprint 5 deterministic filtered group-by overload for batch
shield-buff absorption totals (PR #42) merged on `origin/main`

## Scope

This increment adds a deterministic cross-batch merge seam over the
already-computed `ShieldBuffAbsorptionBatchTotals` snapshots produced by
`ShieldBuffService.ComputeBatchTotals` and its filter / partition / group-by
overloads. The merge factory generalizes the existing `From` /
`PartitionFrom` / `GroupBy` aggregations from "one per-actor result map"
to "many already-computed totals snapshots" so a future combat
damage-resolution pass, telemetry surface, or UI feedback layer can fold
the totals of multiple `AbsorbDamageForActors` batches (e.g. across
ticks or across encounter sub-passes) into a single deterministic
snapshot without re-walking any original per-actor result map.

This is a strict pure aggregation over already-built snapshots; it does
not touch the registry, the per-actor buff bags, the input result maps,
or the trace contract.

Implemented:

- `ShieldBuffAbsorptionBatchTotals.Empty` static property — the additive
  identity for `Merge`. Every counter is zero. Matches `From` over an
  empty result map. A future rolling cross-batch aggregator can seed a
  multi-tick fold with `Empty` without special-casing the first
  iteration.
- `ShieldBuffAbsorptionBatchTotals.Merge(left, right)` static factory
  that returns a new totals object whose counters are the field-wise
  integer sums of `left` and `right`. Pure: no Unity dependency, no
  presentation coupling, no registry read, no buff/tick mutation, no
  save coupling.
- `ShieldBuffService.MergeBatchTotals(left, right)` wrapper that
  delegates to the new factory so callers can stay on the service for
  the whole absorb-then-aggregate-then-fold flow.

Behavior:

- `null` left totals &rarr; `ArgumentNullException` (factory and
  service).
- `null` right totals &rarr; `ArgumentNullException` (factory and
  service).
- `Empty` is both the left and right identity for `Merge`. For any
  totals `x`, `Merge(Empty, x)` and `Merge(x, Empty)` both equal `x`
  field-wise.
- `Merge` sums every numeric counter field-wise:
  `TotalIncomingDamage`, `TotalAbsorbedDamage`,
  `TotalRemainingDamage`, `ActorCount`, `ActorsWithAbsorption`,
  `TotalConsumedBuffEntries`, `TotalExpiredBuffEntries`.
- merging two totals built from disjoint per-actor result maps equals
  `From(unionMap)` of the two source maps, because each entry
  contributes to exactly one source totals and the merge sums the
  contributions.
- `Merge` is commutative: `Merge(a, b)` equals `Merge(b, a)` field-wise
  because every counter is a commutative integer sum.
- `Merge` is associative: `Merge(Merge(a, b), c)` equals
  `Merge(a, Merge(b, c))` field-wise for the same reason.
- registry is not read by this method; the registry is not mutated; the
  per-actor buff bags are not mutated; the input result maps used to
  build the source totals are not mutated; trace lists are not
  modified.

## Out of scope

- No new absorption-time mutation rule, no per-buff sort order, no
  faction/region/buff-family awareness baked into the merge — the
  caller composes those concerns through the existing
  `From` / `PartitionFrom` / `GroupBy` overloads before merging.
- No reduce-style `Sum(IEnumerable<...>)` overload yet; the binary
  `Merge` is enough to support a fold loop in the caller and keeps
  the surface minimal.
- No incremental "merge into mutable accumulator" surface; totals stay
  immutable, every merge returns a new snapshot.

## Tests

`Assets/Tests/EditMode/Magic/ShieldBuffServiceBatchAbsorptionTotalsMergeTests.cs`
covers:

- null-left / null-right argument guards on both the factory and the
  service wrapper.
- `Empty` exposes zero counters and matches `From` over an empty map.
- `Empty` is both the left and the right identity for `Merge`.
- `Merge` sums every counter field-wise across two real batch-derived
  totals snapshots built from independent registries.
- merging two batch totals equals `ComputeBatchTotals(union)` of their
  underlying disjoint per-actor result maps.
- `Merge` is commutative across two batch-derived totals.
- `Merge` is associative across three batch-derived totals.
- `MergeBatchTotals` does not mutate the originating `ShieldBuffStateRegistry`,
  per-actor buff bags, or remaining ticks / magnitudes.
