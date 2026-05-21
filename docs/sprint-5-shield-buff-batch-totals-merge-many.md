# Sprint 5 Shield Buff Batch Totals MergeMany

Date: 2026-05-06
Branch: `agent/sprint-5-shield-buff-batch-totals-merge-many`
Base: `34c5206` — Sprint 5 deterministic merge factory for batch
shield-buff absorption totals (PR #43) merged on `origin/main`

## Scope

This increment generalizes the existing pairwise
`ShieldBuffAbsorptionBatchTotals.Merge` factory and its
`ShieldBuffService.MergeBatchTotals` service wrapper from
"fold of two snapshots" to "fold of N snapshots" via a new
`MergeMany` overload that walks an arbitrary
`IEnumerable<ShieldBuffAbsorptionBatchTotals>` once and folds it into a
single deterministic totals snapshot using `Empty` as the additive
identity and the binary `Merge` as the combining operator. A future
combat damage-resolution pass, telemetry surface, or UI feedback layer
can fold a whole list of `AbsorbDamageForActors` batches (e.g. every
tick of an encounter, or every sub-pass of a multi-stage spell volley)
into one snapshot in a single call instead of chaining pairwise
`MergeBatchTotals` calls.

This is a strict pure aggregation seam over already-built snapshots; it
does not touch the registry, the per-actor buff bags, the input result
maps, or the trace contract.

Implemented:

- `ShieldBuffAbsorptionBatchTotals.MergeMany(totals)` static factory
  that folds an `IEnumerable<ShieldBuffAbsorptionBatchTotals>` into one
  snapshot. Pure: no Unity dependency, no presentation coupling, no
  registry read, no buff/tick mutation, no save coupling. Implemented
  as a left fold seeded with `Empty` using the binary `Merge` as the
  combining operator so the additive identity and field-wise integer
  sums stay consistent with `Merge` / `From` / `PartitionFrom` /
  `GroupBy`.
- `ShieldBuffService.MergeBatchTotalsMany(totals)` wrapper that
  delegates to the new factory so callers can stay on the service for
  the whole absorb-then-aggregate-then-fold flow.

Behavior:

- `null` totals sequence (factory and service) &rarr;
  `ArgumentNullException`.
- `null` element inside the sequence &rarr; `ArgumentException` whose
  message identifies the offending element index, so a future caller
  building telemetry across many ticks can locate the bad snapshot
  without scanning the whole sequence by hand.
- empty sequence &rarr; `Empty`. Singleton sequence &rarr; the single
  snapshot field-wise.
- `Empty` entries inside the sequence are absorbed without changing
  the result (left and right identity, inherited from `Merge`).
- chaining the binary `Merge` over the same elements in any order
  produces the same snapshot as `MergeMany`, which equals
  `From(union)` over the union of every source batch's per-actor
  result map (when the batches are over disjoint actor keys), because
  each per-actor entry contributes to exactly one source totals and
  the fold sums every contribution field-wise.
- permutation invariant: `MergeMany` is invariant under any
  permutation of the input sequence because every counter is a
  commutative integer sum, so a future telemetry surface can fold a
  set of per-tick snapshots in any order.
- registry-isolation invariant preserved: folding existing totals
  snapshots through `MergeBatchTotalsMany` never observes or mutates
  any `ShieldBuffStateRegistry`, the per-actor `ShieldBuffState`, the
  active-buff bags, the `RemainingTicks`, or the `Magnitude`.

## Files touched

- `Assets/Scripts/Simulation/Magic/ShieldBuffAbsorptionBatchTotals.cs`
  — new `MergeMany` static factory next to `Merge` / `Empty`.
- `Assets/Scripts/Simulation/Magic/ShieldBuffService.cs` — new
  `MergeBatchTotalsMany` service wrapper next to `MergeBatchTotals`.
- `Assets/Tests/EditMode/Magic/ShieldBuffServiceBatchAbsorptionTotalsMergeManyTests.cs`
  — new EditMode test fixture (12 tests) pinning argument guards,
  empty-sequence identity, singleton parity, parity with chained
  pairwise `Merge`, parity with `From(union)`, permutation
  invariance, `Empty`-element absorption, and registry isolation.

## Validation

- `git diff --check` &mdash; clean.
- `tools/validation/run-validation.sh --mode fallback` &mdash;
  `Passed!  - Failed: 0, Passed: 443, Skipped: 0, Total: 443` on the
  ValidationFallbackHarness (.NET 9). The 12 new MergeMany tests are
  included in the count and all pass.

## Out of scope

- Save/load coupling. `MergeMany` operates on already-computed
  snapshots and does not interact with `ShieldBuffSaveMapper`.
- Combat damage-resolution wiring. The fold is exposed as a pure
  aggregation seam; the encounter combat pipeline will call into it in
  a later increment.
- Presentation surfaces. UI/telemetry layers can call into
  `MergeBatchTotalsMany` over their preferred snapshot list; this
  increment ships only the seam.
- Predicate-filter / group-by variants of `MergeMany`. The existing
  filtered `From` / `GroupBy` overloads already cover the per-batch
  filtering surface; cross-batch filtered folds can be added later
  when an actual caller needs them.

## Next recommended increment

Wire `ShieldBuffService.MergeBatchTotalsMany` into a multi-tick
telemetry seam (e.g. a small rolling per-encounter aggregator that
collects per-tick `ComputeBatchTotals` snapshots and folds them at
end-of-encounter), or add a `MergeMany` GroupBy overload that folds a
sequence of per-bucket totals dictionaries from the existing
`GroupBy` factory.
