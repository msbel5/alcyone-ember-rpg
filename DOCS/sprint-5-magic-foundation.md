# Sprint 5 Magic Foundation

Date: 2026-05-02
Branch: `agent/sprint-5-magic-foundation`

## Scope

Sprint 5 starts the deterministic magic layer without adding any LLM dependency to game runtime code. This increment adds:

- `MagicSchool`, `SpellEffectKind`, `SpellEffectSpec`, and `SpellDefinition` domain contracts.
- `SliceSpellCatalog` with three deterministic starter spell definitions.
- `SpellCastingService` with spell lookup, known-spell validation, incapacitated-caster rejection, mana affordability checks, and mana spend only on successful casts.
- EditMode fallback tests covering catalog determinism, domain validation, mana spend success, insufficient mana, unknown spell, unlearned spell, and invalid caster paths.

## Bible Back-References

- `docs/EMBER_VISION_BIBLE.md` §3: Sprint 5 belongs to deterministic gameplay mechanics, not AI orchestration.
- `docs/EMBER_VISION_BIBLE.md` §8: Sprint mapping calls out magic/spells as the current layer target.
- `docs/EMBER_VISION_BIBLE.md` §11: OpenMW spell references were used only as clean-room conceptual inspiration; no code was copied.
- `docs/mechanics/MASTER_MECHANICS_BIBLE.md` §14-§15: school and effect taxonomy informed the local shape.

## Validation

Command:

```bash
./tools/validation/run-validation.sh --mode fallback
```

Latest measured result for this increment: `Passed: 120, Failed: 0, Skipped: 0, Total: 120`.

## Caveats

Damage, healing, timed buff resolution, cooldown state, resistance, saving throws, and spell crafting are intentionally left for later Sprint 5 phases. Unity Editor validation is still blocked on this Pi because the Unity editor binary is not installed; the measured gate here is the pure C# fallback harness.
