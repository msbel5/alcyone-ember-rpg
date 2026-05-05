# Sprint 5 Shield Buff Application

Date: 2026-05-05
Branch: `agent/sprint-5-shield-buff-application`
Base: `cc8b8c5` — Sprint 5 ShieldBuffState foundation merged on `origin/main`

## Scope

`ShieldBuffState` (merged in PR #26) introduced the per-spell remaining-tick +
magnitude container, but nothing yet writes into it. `SpellEffectResolutionService`
still rejects every cast that contains a non-instantaneous effect with
`NonInstantaneousEffect`, including the only catalog spell that has a timed
buff: `EmberWard` (`ShieldBuff` magnitude 4, duration 30 ticks).

This slice is the application bridge between a successful cast and the buff
state container. It does not change instantaneous resolution semantics, does
not introduce tick-down, does not key buffs to actors, and does not touch
save/load. Those are intentionally future slices.

Implemented:

- `Assets/Scripts/Simulation/Magic/SpellEffectResolutionError.cs` — adds
  `InvalidBuffState = 5` so the buff path can report a missing state container
  without sharing the `InvalidCast` / `InvalidTarget` codes used by
  instantaneous resolution.
- `Assets/Scripts/Simulation/Magic/ShieldBuffApplicationResult.cs` — narrow
  result object parallel in shape to `SpellEffectResolutionResult`:
  - `Success`, `Error`, `Spell`, `AppliedBuffCount`, `TotalAppliedMagnitude`,
    `TotalAppliedDurationTicks`, `Message`.
  - `Ok(...)` and `Fail(error, spell, message)` factories.
- `Assets/Scripts/Simulation/Magic/SpellEffectResolutionService.cs` — adds
  `ApplyShieldBuffs(SpellCastResult castResult, ShieldBuffState shieldBuffState)`:
  - rejects null / failed casts with `InvalidCast` (state untouched)
  - rejects null `ShieldBuffState` with `InvalidBuffState` (new)
  - iterates `Spell.Effects`: every `ShieldBuff` with `DurationTicks > 0` is
    written to `shieldBuffState` keyed by `spell.TemplateId` via
    `SetActiveBuff(remainingTicks, magnitude)`
  - skips instantaneous `ShieldBuff` effects and every non-buff effect kind
  - returns `AppliedBuffCount` plus magnitude/duration totals across applied
    entries; the existing `ResolveInstantaneousEffects` path is unchanged and
    still rejects non-instantaneous effects with `NonInstantaneousEffect`.
- `Assets/Tests/EditMode/Magic/ShieldBuffApplicationServiceTests.cs` — 10
  EditMode tests pinning:
  - EmberWard cast records `RemainingTicks = 30, Magnitude = 4` under the
    `ember_ward` template id.
  - Re-casting EmberWard overwrites a stale entry instead of stacking.
  - A custom multi-buff spell applies both buff effects and totals are
    consistent with `SetActiveBuff` overwrite semantics.
  - A mixed `DirectDamage + RestoreHealth + ShieldBuff` spell only writes the
    timed buff; the other effect kinds are ignored on the buff path.
  - An instantaneous `ShieldBuff` effect (duration 0) is treated as a no-op
    on the buff path and leaves the state untouched.
  - A `FlameBolt` cast leaves the buff state empty.
  - Null cast / failed cast / null state cases reject with the correct error
    codes and never mutate the state container.
  - Calling `ApplyShieldBuffs` for `EmberWard` does not unblock the existing
    `ResolveInstantaneousEffects` rejection path: the same cast still fails
    with `NonInstantaneousEffect` and target vitals stay at their starting
    value.

## Why this slice matters

The Sprint 5 magic foundation has now graduated from "we have a buff state
container" to "a successful cast can write into it." The next dependent slice
— actor-keyed wrapping (so the world knows whose buffs these are) and
tick-down resolution (so the buff actually decays) — is now isolated work on
top of a stable application surface. By keeping `ResolveInstantaneousEffects`
unchanged, callers and tests of the existing path are not broken. The buff
write surface is small enough to plug into save/load later without retrofitting
resolution semantics.

## Bible Back-References

- `docs/EMBER_VISION_BIBLE.md` §3: pure-Domain `ShieldBuffState` is mutated
  from `Simulation.Magic`; no Unity types cross the boundary.
- `docs/EMBER_VISION_BIBLE.md` §8: another narrow Sprint 5 magic increment;
  not a balance change and not a runtime/HUD change.
- `docs/mechanics/MASTER_MECHANICS_BIBLE.md` §15: timed `ShieldBuff` effects
  finally have a writer service; opcode-style verb stays in the Domain enum.

## Validation

Commands:

```bash
git diff --check
./tools/validation/run-validation.sh --mode fallback
```

Measured result on this branch:
- `git diff --check` produced no output.
- Fallback validation passed: `Passed: 230, Failed: 0, Skipped: 0, Total: 230`.
  Previous baseline on `origin/main` (after PR #26) was `220 / 220`; this
  slice added 10 new tests in `ShieldBuffApplicationServiceTests`.

## Release Evidence

- Branch: `agent/sprint-5-shield-buff-application`
- Local fallback baseline before slice: `220 / 220`
- Local fallback baseline after slice: `230 / 230`
- See PR for commit hashes and CI status when opened.

## Caveats

- Application-only. `ResolveInstantaneousEffects` still refuses
  non-instantaneous effects; callers needing a single entry point will be the
  job of a later orchestrator slice.
- No tick-down. Buff entries written here will sit in `ShieldBuffState`
  forever until the next slice introduces a per-tick decay step.
- No actor-keyed wiring. `ShieldBuffState` is still a free-standing object;
  attaching one per `ActorRecord` is a separate decision.
- No damage absorption. Active shield magnitudes are recorded but not yet
  consulted by combat damage application.
- No save/load integration. A follow-up `ShieldBuffSaveMapper` slice
  (parallel to `SpellCooldownSaveMapper`) will handle persistence.
- Local validation remains the pure .NET fallback harness, not a real local
  Unity Editor / EditMode run.

## Thalamus Provenance

- `thalamus_packet_id`: `pkt_20260505031822_14797fa94bc6`
- `thalamus_resolver_key`: `sha256:67cd5a9ce3fe46ada6bf071e2716233785456c8676fe130da306adc5616a3c56`
- Vector query was present (1024-dim, namespace `atoms.code`,
  `qwen3-embedding-0.6b-q4_0`).
- `query_path`: vector
- `vector_query_present`: true
