# Sprint 5 Spell Effect — All-Six Instantaneous Bundle Test

Date: 2026-05-07
Branch: `agent/sprint-5-spell-effect-all-six-bundle-test`
Base: `27da3ae` — Sprint 5 DirectFatigue instantaneous spell effect kind
(PR #57) merged on `origin/main`.

Thalamus packet: `pkt_20260507071627_6cc3453d8f6b`
Resolver key: `sha256:000b6f0893f23b322de787017f8430ecde3982eb8fa1ab7a7577bc328578c1ab`
Vector query present: yes (inline_vector returned)
Query path: vector

## Scope

This is a **test-only** increment that pins compositional correctness
now that the Sprint 5 instantaneous effect matrix is complete. After
PR #57 there are six supported instantaneous verbs covering both
directions of all three vital pools:

- DirectDamage / RestoreHealth (health)
- DirectFatigue / RestoreFatigue (fatigue)
- DirectMana   / RestoreMana   (mana)

The previous bundled tests only paired one direct verb with its
matching restore (e.g. `DirectMana_BundledWithRestoreManaAggregatesIndependently`
in PR #56, `DirectFatigue_BundledWithRestoreFatigueAggregatesIndependently`
in PR #57). Health-side bundles existed but never alongside the new
mana/fatigue drains. There was no single test exercising **all six**
verbs in one cast and asserting that every counter axis aggregates
independently.

This increment fills that gap with one new test method. No production
code changes — the resolver, the result object, the enum, and the
validator are untouched. Pure regression coverage that locks in the
now-complete vital-pool matrix.

## Tests added

In `Assets/Tests/EditMode/Magic/SpellEffectResolutionServiceTests.cs`:

- `ResolveInstantaneousEffects_AllSixSupportedKindsBundledAggregateIndependently`
  — one cast with a six-effect spell `[DirectDamage 3, RestoreHealth 4,
  DirectFatigue 4, RestoreFatigue 3, DirectMana 5, RestoreMana 6]`
  against a target with `health=10/16, fatigue=8/12, mana=10/20` ends
  with `health=11, fatigue=7, mana=11` and counters
  `TotalDamage=3, TotalHealing=4, TotalDirectFatigueDamage=4,
  TotalRestoredFatigue=3, TotalDirectManaDamage=5, TotalRestoredMana=6`,
  `AppliedEffectCount=6`.

Magnitudes are picked so no pool clamps at floor or ceiling — every
delta is a clean per-effect mutation, so any future regression that
double-counts, cross-bleeds, or skips a verb shows up immediately as
either a wrong total or a wrong final pool value.

Existing 23 tests still pass unchanged.

## Validation

`tools/validation/run-validation.sh --mode fallback`

```
STATUS unity_editor=BLOCKED reason=not_found
STATUS fallback_harness=RUNNING note='Pure C# NUnit harness; not a Unity EditMode run.'
Passed!  - Failed: 0, Passed: 598, Skipped: 0, Total: 598, Duration: 810 ms
PASS fallback_harness
```

Test count went from 597 to 598 — exactly the one new test added.
This is the pure-.NET fallback corpus. Unity headless still requires
the GitHub PR check matrix.

## Out of scope

- No new SpellEffectKind, no new counter on the result object, no
  new validator branch. The six verbs are already wired.
- No spell catalog template — this is a coverage increment, not a
  content addition.
- Order-sensitive interactions (e.g. drain-then-restore that crosses
  the floor) are already pinned by the per-pair bundled tests; this
  test only proves cross-axis independence.
