# Sprint 5 Spell Catalog Cooldowns

Date: 2026-05-05
Branch: `agent/sprint-5-spell-catalog-cooldowns`
Base: `380c7a2` — Sprint 5 cooldown persistence merged on `origin/main`

## Scope

The cooldown infrastructure (foundation, persistence, execution rejection) was already in place,
but the three starter catalog spells still declared `CooldownTicks=0`, so the entire feature was
exercised only by synthetic test spells. This slice closes that gap by assigning deterministic
cooldown ticks to the catalog and pinning the values in tests.

Implemented:

- `SliceSpellCatalog` now exposes three new constants and routes them through the 7-arg
  `SpellDefinition` constructor:
  - `FlameBoltCooldownTicks = 6` — bounded recast cadence for the offensive starter spell.
  - `MendingTouchCooldownTicks = 4` — lighter cadence for the touch heal.
  - `EmberWardCooldownTicks = 30` — matches the spell's own 30-tick `ShieldBuff` duration so the
    buff cannot legally double-stack via back-to-back self-recast once buff resolution lands.
- `SliceSpellCatalogTests` pins each cooldown to its constant and asserts the EmberWard
  cooldown-equals-duration invariant.
- `SpellExecutionServiceTests` adds an end-to-end check that casting the real catalog FlameBolt
  with a non-null `SpellCooldownState` starts the declared cooldown and that an immediate recast
  is rejected with `SpellExecutionError.CastRejected` / `SpellCastError.SpellOnCooldown`, with no
  mana spent on the rejected recast.

## Why this slice matters

Before this slice, the cooldown service, save mapper, and execution rejection paths were all green
in tests but unreachable from real gameplay because no catalog spell declared a positive cooldown.
That made the feature plumbing technically correct but functionally inert. Wiring values into the
catalog turns cooldowns into a real game-affecting constraint and gives future Sprint 5 increments
(timed buff resolution, resistance, saves) a working pacing layer to build on.

## Bible Back-References

- `docs/EMBER_VISION_BIBLE.md` §3: Sprint 5 stays in the Layer 3 deterministic gameplay band.
- `docs/EMBER_VISION_BIBLE.md` §8: this is another narrow, testable magic increment, not a
  full balance pass.
- `docs/mechanics/MASTER_MECHANICS_BIBLE.md` §14-§15: cooldown is part of the deterministic spell
  cost/pacing surface; the values chosen here are honest defaults, not a designer-tuned ladder.

## Validation

Commands:

```bash
git diff --check
./tools/validation/run-validation.sh --mode fallback
```

Measured result on this branch:
- `git diff --check` produced no output.
- Fallback validation passed: `Passed: 209, Failed: 0, Skipped: 0, Total: 209`. Previous baseline
  on `origin/main` was `207/207`; this slice added two new tests
  (`StarterSpells_ExposeDeterministicCooldownTicks`,
  `TryExecute_CatalogFlameBolt_WithCooldownState_StartsCatalogCooldownAndRejectsRecast`).

## Release Evidence

- Branch: `agent/sprint-5-spell-catalog-cooldowns`
- Local fallback baseline before slice: `207 / 207`
- Local fallback baseline after slice: `209 / 209`
- See PR for commit hashes and CI status when opened.

## Caveats

- Cooldown values (6 / 4 / 30) are honest deterministic defaults, not a playtested balance pass.
- Pre-existing 4-arg `SpellExecutionService.TryExecute` callers stay cooldown-free because they
  pass a `null` cooldownState; this slice does not retrofit cooldown state into those flows.
- EmberWard still cannot resolve end-to-end (its `ShieldBuff` is non-instantaneous) so the
  cooldown-equals-duration invariant is enforced at catalog level only; once timed buff resolution
  lands it will become a runtime invariant too.
- Local validation remains the pure .NET fallback harness, not a real local Unity Editor / PlayMode
  run.

## Thalamus Provenance

- `thalamus_packet_id`: `pkt_20260505012145_071b901d5c00`
- `thalamus_resolver_key`: `sha256:762720c0fbc5c0020c3da113f723420e483a3da51227fa15ae2319157609202e`
- Vector query was present (1024-dim, namespace `atoms.code`, `qwen3-embedding-0.6b-q4_0`).
