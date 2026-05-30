# Sprint 5 Shield Buff Batch Totals Filter

Date: 2026-05-05
Branch: `agent/sprint-5-shield-buff-batch-totals-filter`
Base: `dc74e07` — Sprint 5 deterministic batch shield-buff absorption totals aggregate (PR #38) merged on `origin/main`

## Scope

This increment adds a per-actor predicate-filter overload to the deterministic
`ShieldBuffAbsorptionBatchTotals` aggregate seam introduced in PR #38, plus the
matching service-level wrapper on `ShieldBuffService.ComputeBatchTotals`. A future
combat damage-resolution pass, telemetry surface, or UI feedback layer can now
summarize a side-specific (allies-only / enemies-only), absorbed-only, or any
other per-actor slice of one batch absorption call without re-walking the
per-actor result map. This is a strict read-only subset post-pass over an
already-computed absorption result; it does not touch the registry, the per-actor
buff bags, or the trace contract.

Implemented:

- `ShieldBuffAbsorptionBatchTotals.From(IReadOnlyDictionary<string, ShieldBuffAbsorptionResult>, Func<string, ShieldBuffAbsorptionResult, bool> includePredicate)`
  static overload that aggregates only the entries for which `includePredicate`
  returns true. A `null` predicate aggregates every validated entry, matching the
  unfiltered `From(map)` factory exactly.
- `ShieldBuffService.ComputeBatchTotals(IReadOnlyDictionary<string, ShieldBuffAbsorptionResult>, Func<string, ShieldBuffAbsorptionResult, bool> includePredicate)`
  wrapper that delegates to the new factory overload so callers can stay on the
  service for the whole absorb-then-summarize-subset flow.

Behavior:

- `null` result map &rarr; `ArgumentNullException` (predicate overload, both factory and service).
- whitespace/empty actor key in the input map &rarr; `ArgumentException`, even
  when the predicate would otherwise filter the entry out.
- `null` per-actor result value &rarr; `ArgumentException`, even when the
  predicate would otherwise filter the entry out.
- `null` predicate &rarr; aggregates every validated entry, identical to
  `From(map)` and `ComputeBatchTotals(map)`.
- predicate returns false for an entry &rarr; entry is skipped: it does NOT
  contribute to any total, and it does NOT count toward `ActorCount`.
- `ActorCount` on the predicate overload describes the included subset, not the
  size of the input map, because the totals describe the included subset.
- aggregation order has no observable effect because the predicate is a pure
  per-entry filter and the totals remain commutative sums and counts.
- registry is not read by this method; the registry is not mutated; per-actor
  buff bags are not mutated; the input result map is not mutated; trace lists
  are not modified.

Pure Simulation: no Unity types, no save coupling, no tick mutation, no
combat-pipeline call.

## Why this slice matters

PR #38 produced a single-shot aggregate over a whole batch absorption result.
Most call-sites that want to react to one batch absorption call also want a
per-side or absorbed-only slice — for example, a combat HUD that surfaces total
damage absorbed by the player party, or a telemetry hook that only counts
shields that actually caught damage. Without a filter overload, callers either
walk the per-actor result map themselves (defeating the point of the aggregate)
or build a subset map first (allocating an extra dictionary per slice). This
overload moves that walk into one deterministic helper next to the rest of the
shield-buff API surface, keeps strict input validation identical to the
unfiltered factory, and preserves the order-independence guarantee of the
underlying sums and counts.

## Bible Back-References

- `docs/EMBER_VISION_BIBLE.md` §3: Sprint 5 remains Layer 3 gameplay mechanics;
  this aggregate is pure Domain/Simulation orchestration with no AI dependency.
- `docs/EMBER_VISION_BIBLE.md` §8: Sprint 5 keeps shipping narrow, testable
  magic increments instead of pretending the full combat-absorption stack is
  wired.
- `docs/mechanics/MASTER_MECHANICS_BIBLE.md` §15: shield-buff absorption stays
  composable with the existing actor-keyed registry without changing per-buff
  resolution rules.

## Validation

- `git diff --check` clean
- `tools/validation/run-validation.sh --mode fallback` recorded in this run; see
  RUN_STATUS in the captain run summary.

## Thalamus Handoff

- thalamus_packet=`pkt_20260505171704_25e157494813`
- vector_query_present=true (1024-dim inline vector returned)
- query_path=vector

## Files

- `Assets/Scripts/Simulation/Magic/ShieldBuffAbsorptionBatchTotals.cs` — predicate-overload `From`.
- `Assets/Scripts/Simulation/Magic/ShieldBuffService.cs` — predicate-overload `ComputeBatchTotals` wrapper.
- `Assets/Tests/EditMode/Magic/ShieldBuffServiceBatchAbsorptionTotalsTests.cs` — predicate-overload coverage.
