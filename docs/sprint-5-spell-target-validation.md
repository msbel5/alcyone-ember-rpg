# Sprint 5 Spell Target Validation & Routing

Date: 2026-05-04
Branch: `agent/sprint-5-spell-target-validation`

## Scope

This increment adds the deterministic spell target validation/routing gate that sits between
`SpellCastingService` (mana spend) and `SpellEffectResolutionService` (vitality effects). It refuses
target shapes the resolver cannot honour yet (area kinds) and routes the supported shapes
(`CasterSelf`, `Touch`, `SingleTarget`) to the concrete `ActorRecord` the resolver should mutate.

It does NOT add buff state, resistance, success chance, cooldowns, AoE geometry, or replace cast-side
mana logic. Those land in subsequent Sprint 5 increments.

Implemented:

- `SpellTargetValidator` in pure `EmberCrpg.Simulation.Magic` (no Unity types).
- `SpellTargetValidationResult` and `SpellTargetValidationError` for stable, non-localized outcomes.
- Routing rules:
  - `CasterSelf` — accepts `null` or the caster as requested target; routes to caster. Refuses any
    non-caster target with `WrongTargetForSelfSpell`.
  - `Touch` — requires a non-null living target with Manhattan distance exactly `1` from the caster
    (orthogonal grid adjacency). Refuses self-tile, diagonal, or far targets with
    `TargetNotAdjacent`. Refuses null/dead targets with `InvalidTarget`.
  - `SingleTarget` — requires a non-null living target. Refuses null/dead targets with
    `InvalidTarget`. Range/line-of-sight is not enforced at this layer.
  - `AreaAroundCaster` and `AreaAtRange` — explicitly refused with `UnsupportedTargetKind` until
    the area resolution increment lands.
- Caster guards: null caster returns `InvalidCaster`; incapacitated caster returns `InvalidCaster`.
- Spell guard: null spell returns `InvalidSpell`.
- EditMode fallback tests covering each supported and refused path, plus an integration test
  proving the routed target flows cleanly into `SpellEffectResolutionService`.

## Bible Back-References

- `docs/EMBER_VISION_BIBLE.md` §3: Sprint 5 stays deterministic Layer 3 gameplay mechanics; the
  Domain/Simulation namespaces remain Unity-free.
- `docs/EMBER_VISION_BIBLE.md` §8: Sprint 5 magic foundation is incremental; area resolution is a
  later increment, so refusing area kinds keeps the foundation honest.
- `docs/mechanics/MASTER_MECHANICS_BIBLE.md` §14: target taxonomy (caster/self, touch, single,
  area-around-caster, area-at-range) drives the validator's enum routing.

## Validation

Commands:

```bash
git diff --check
./tools/validation/run-validation.sh --mode fallback
```

Result: see the artifact at
`/home/msbel/.openclaw/workspace/state/ember-s5-target-validation-builder.txt` for the exact
fallback harness counts and commit hash.

## Caveats

- Adjacency is enforced via `GridPosition.ManhattanDistanceTo == 1`, which is orthogonal-only. If
  diagonal adjacency is desired in a later increment, swap to a Chebyshev helper rather than
  loosening this check in place.
- The validator does not spend mana, mutate vitals, or check spell cost. Callers still go through
  `SpellCastingService.TryCast` for mana validation; the validator's job is purely target shape.
- Range/line-of-sight for `SingleTarget` is intentionally out of scope; that belongs to the
  ranged-spell increment alongside the area kinds.
