# Sprint 5 Shield Buff Batch Totals Group-By

Date: 2026-05-05
Branch: `agent/sprint-5-shield-buff-batch-totals-group-by`
Base: `fc12237` — Sprint 5 deterministic batch shield-buff absorption totals partition overload (PR #40) merged on `origin/main`

## Scope

This increment adds a deterministic single-pass N-way group-by seam over the
batch shield-buff absorption result map produced by
`ShieldBuffService.AbsorbDamageForActors` (introduced in earlier Sprint 5
slices). The group-by overload generalizes the binary `PartitionFrom` factory
so a future combat damage-resolution pass, telemetry surface, or UI feedback
layer can compute totals broken down by faction, region, or buff family from
one batch absorption call in a single deterministic walk over the per-actor
result map, instead of running one filtered `ComputeBatchTotals` call per
bucket.

This is a strict read-only post-pass over an already-computed absorption
result; it does not touch the registry, the per-actor buff bags, or the trace
contract.

Implemented:

- `ShieldBuffAbsorptionBatchTotals.GroupBy(IReadOnlyDictionary<string, ShieldBuffAbsorptionResult>, Func<string, ShieldBuffAbsorptionResult, string> keyExtractor)`
  static factory that walks the input map exactly once, validating each entry
  (non-empty actor key, non-null per-actor result) before consulting the
  key-extractor, then routes the entry to a per-group accumulator keyed by the
  string returned by the extractor. The factory itself rejects a null/empty
  group key returned by the extractor before accumulating anything for that
  entry.
- `ShieldBuffService.GroupBatchTotals(IReadOnlyDictionary<string, ShieldBuffAbsorptionResult>, Func<string, ShieldBuffAbsorptionResult, string> keyExtractor)`
  wrapper that delegates to the new factory so callers can stay on the service
  for the whole absorb-then-group flow.

Behavior:

- `null` result map &rarr; `ArgumentNullException` (factory and service).
- `null` key-extractor &rarr; `ArgumentNullException`. Like the partition
  surface, group-by has no "aggregate everything" interpretation when the
  extractor is missing because the group-by surface is inherently key-driven.
- whitespace/empty actor key in the input map &rarr; `ArgumentException`,
  even when the extractor would otherwise route the entry to a specific
  bucket.
- `null` per-actor result value &rarr; `ArgumentException`, even when the
  extractor would otherwise route the entry to a specific bucket.
- key-extractor returns a `null` or whitespace group key &rarr;
  `ArgumentException`, because group keys must be stable non-empty ids the
  caller can address.
- empty input map &rarr; empty `IReadOnlyDictionary<string, ShieldBuffAbsorptionBatchTotals>`,
  no buckets created, no exception.
- every entry contributes to exactly one bucket, so summing every bucket
  field across all keys equals the unfiltered `ComputeBatchTotals(map).Field`
  for every numeric field, including `ActorCount` and `ActorsWithAbsorption`.
- the per-bucket totals match the corresponding filtered
  `ComputeBatchTotals(map, predicate)` result for the same key membership.
- single-key passthrough: when the extractor returns the same key for every
  entry, the resulting dictionary contains exactly one bucket whose totals
  equal the unfiltered `ComputeBatchTotals(map)` result.
- aggregation order has no observable effect because the extractor is a pure
  per-entry mapping and each bucket accumulates commutative sums and counts.
- registry is not read by this method; the registry is not mutated; per-actor
  buff bags are not mutated; the input result map is not mutated; trace lists
  are not modified.
- the returned dictionary uses ordinal string comparison so group key lookup
  is stable across cultures.

Pure Simulation: no Unity types, no save coupling, no tick mutation, no
combat-pipeline call.

## Files

- `Assets/Scripts/Simulation/Magic/ShieldBuffAbsorptionBatchTotals.cs`
  &mdash; adds the `GroupBy` factory and a private `GroupAccumulator` helper.
- `Assets/Scripts/Simulation/Magic/ShieldBuffService.cs`
  &mdash; adds the `GroupBatchTotals` façade method delegating to the
  factory.
- `Assets/Tests/EditMode/Magic/ShieldBuffServiceBatchAbsorptionTotalsGroupByTests.cs`
  &mdash; pins argument guards, empty-input emptiness, single-key passthrough,
  N-way bucket-vs-filtered parity, sum-of-buckets-vs-unfiltered parity,
  parity vs the binary `PartitionFrom` contract, and a no-mutation guard
  on the registry.
- `docs/sprint-5-shield-buff-batch-totals-group-by.md` &mdash; this design
  note.

## Out of scope

- combat damage-resolution call sites that consume group totals
  (downstream wiring slice).
- save/load surface for grouped totals (still ephemeral per-call).
- any registry mutation, application, or tick-down behavior change.
- presentation-layer breakdowns (UI/HUD wiring is its own slice).
- group-key validation beyond non-empty/whitespace (caller still owns
  semantic group taxonomy).

## Validation

- local fallback: `tools/validation/run-validation.sh --mode fallback` PASS.
- GitHub Unity checks expected to pass on PR alongside the rest of the
  Sprint 5 magic foundation.

## Thalamus packet

- packet_id: `pkt_20260505201640_8159a096dfe2`
- query_path: vector
