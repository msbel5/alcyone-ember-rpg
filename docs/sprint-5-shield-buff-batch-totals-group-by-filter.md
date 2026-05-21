# Sprint 5 Shield Buff Batch Totals Filtered Group-By

Date: 2026-05-06
Branch: `agent/sprint-5-shield-buff-batch-totals-group-by-filter`
Base: `d73b0f7` — Sprint 5 deterministic batch shield-buff absorption totals group-by overload (PR #41) merged on `origin/main`

## Scope

This increment adds a deterministic single-pass filtered N-way group-by seam
over the batch shield-buff absorption result map produced by
`ShieldBuffService.AbsorbDamageForActors`. The filtered group-by overload
generalizes the unfiltered `GroupBy(map, keyExtractor)` factory so a future
combat damage-resolution pass, telemetry surface, or UI feedback layer can
compute N-way faction/region/buff-family totals over a tagged subset of one
batch absorption call (e.g. allies vs enemies, only over actors that
absorbed damage) in a single deterministic walk over the per-actor result
map, instead of running one filtered `ComputeBatchTotals` call per bucket
or two separate filter-then-group walks.

This is a strict read-only post-pass over an already-computed absorption
result; it does not touch the registry, the per-actor buff bags, or the
trace contract.

Implemented:

- `ShieldBuffAbsorptionBatchTotals.GroupBy(IReadOnlyDictionary<string, ShieldBuffAbsorptionResult>, Func<string, ShieldBuffAbsorptionResult, string> keyExtractor, Func<string, ShieldBuffAbsorptionResult, bool> includePredicate)`
  static factory that walks the input map exactly once, validating each
  entry (non-empty actor key, non-null per-actor result) before consulting
  the predicate, then routes only the entries that pass `includePredicate`
  to a per-group accumulator keyed by `keyExtractor`. The factory itself
  rejects a null/empty group key returned by the extractor before
  accumulating anything for that entry. When `includePredicate` is `null`
  the overload behaves exactly like the unfiltered `GroupBy(map,
  keyExtractor)` factory.
- `ShieldBuffService.GroupBatchTotals(IReadOnlyDictionary<string, ShieldBuffAbsorptionResult>, Func<string, ShieldBuffAbsorptionResult, string> keyExtractor, Func<string, ShieldBuffAbsorptionResult, bool> includePredicate)`
  wrapper that delegates to the new factory so callers can stay on the
  service for the whole absorb-then-filter-then-group flow.
- The unfiltered `GroupBy(map, keyExtractor)` factory and its service
  wrapper are re-expressed as a `null` predicate forwarding call into the
  new overload, keeping a single accumulation core.

Behavior:

- `null` result map &rarr; `ArgumentNullException` (factory and service).
- `null` key-extractor &rarr; `ArgumentNullException` (factory and
  service), even when `includePredicate` is supplied. Group-by has no
  "aggregate everything" interpretation when the extractor is missing
  because the group-by surface is inherently key-driven.
- `null` `includePredicate` &rarr; the overload behaves like the
  unfiltered `GroupBy(map, keyExtractor)` factory. The service overload
  preserves the same null-predicate parity.
- whitespace/empty actor key in the input map &rarr; `ArgumentException`,
  even when `includePredicate` would otherwise filter the entry out. The
  guard order matches the unfiltered group-by and the filtered
  `ComputeBatchTotals` overloads.
- `null` per-actor result value &rarr; `ArgumentException`, even when
  `includePredicate` would otherwise filter the entry out.
- key-extractor returns `null` or whitespace for an entry that passes
  `includePredicate` &rarr; `ArgumentException`, because group keys must
  be stable non-empty ids the caller can address. When the entry is
  filtered out by `includePredicate`, the extractor is not consulted at
  all.
- empty input map &rarr; empty
  `IReadOnlyDictionary<string, ShieldBuffAbsorptionBatchTotals>`, no
  buckets created, no exception.
- predicate filters every entry out &rarr; empty group dictionary, same
  contract as empty input.
- per-bucket totals match
  `ComputeBatchTotals(map, predicate)` where the predicate is the
  conjunction of `includePredicate` and "the entry routes to this bucket".
- summing every bucket field across all keys equals
  `ComputeBatchTotals(map, includePredicate).Field` for every numeric
  field (including `ActorCount` and `ActorsWithAbsorption`) because each
  included entry contributes to exactly one bucket.
- aggregation order has no observable effect because the predicate and
  extractor are pure per-entry mappings and each bucket accumulates
  commutative sums and counts.
- registry is not read by this method; the registry is not mutated; the
  per-actor buff bags are not mutated; the input result map is not
  mutated; trace lists are not modified.
- the returned dictionary uses ordinal string comparison so group key
  lookup is stable across cultures.

Pure Simulation: no Unity types, no save coupling, no tick mutation, no
combat-pipeline call.

## Files

- `Assets/Scripts/Simulation/Magic/ShieldBuffAbsorptionBatchTotals.cs`
  &mdash; adds the filtered `GroupBy(map, keyExtractor, includePredicate)`
  factory and re-expresses the existing unfiltered overload as a
  `null` predicate forwarder.
- `Assets/Scripts/Simulation/Magic/ShieldBuffService.cs`
  &mdash; adds the `GroupBatchTotals(map, keyExtractor, includePredicate)`
  façade method delegating to the factory.
- `Assets/Tests/EditMode/Magic/ShieldBuffServiceBatchAbsorptionTotalsGroupByFilterTests.cs`
  &mdash; pins argument guards (`null` map, `null` extractor, whitespace
  actor key, `null` per-actor result, null group key), empty-input
  emptiness, all-excluded emptiness, null-predicate parity vs the
  unfiltered group-by, sum-of-buckets parity vs filtered
  `ComputeBatchTotals`, per-bucket parity vs the equivalent two-condition
  filtered `ComputeBatchTotals`, and a no-mutation guard on the registry.
- `DOCS/sprint-5-shield-buff-batch-totals-group-by-filter.md` &mdash;
  this design note.

## Out of scope

- combat damage-resolution call sites that consume filtered group totals
  (downstream wiring slice).
- save/load surface for filtered grouped totals (still ephemeral
  per-call).
- any registry mutation, application, or tick-down behavior change.
- presentation-layer breakdowns (UI/HUD wiring is its own slice).
- group-key validation beyond non-empty/whitespace (caller still owns
  semantic group taxonomy).

## Validation

- local fallback: `tools/validation/run-validation.sh --mode fallback` PASS.
- GitHub Unity checks expected to pass on PR alongside the rest of the
  Sprint 5 magic foundation.

## Thalamus packet

- packet_id: `pkt_20260505211729_9d1712b9ebf9`
- resolver_key: `sha256:e26153709c8095136deb19a68172a9fd2ff66128cbd91def8a8df96e6efb691f`
- query_path: vector
- vector_query_present: true
