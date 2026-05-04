# Sprint 5 Spell Cooldown Foundation

Date: 2026-05-04
Branch: `agent/sprint-5-spell-cooldown-foundation`
Base: `69c30f5` — Sprint 5 roll doc evidence merged on `origin/main`

## Scope

This increment adds the first deterministic cooldown seam for Sprint 5 spells without coupling magic to
Unity runtime state or pretending timed buffs / resistances are finished. The new slice keeps existing
spell callers compatible by default while letting execution paths opt into cooldown state when they need
it.

Implemented:

- `SpellDefinition.CooldownTicks` with full backward compatibility:
  - existing constructors still default to `0`
  - new full constructor accepts explicit cooldown ticks
  - negative cooldowns are rejected at the domain boundary
- `SpellCooldownState` in pure `Simulation/Magic` for per-spell remaining tick tracking
- `SpellCooldownService` for deterministic cooldown queries, success-only cooldown start, and tick-down
  expiry over elapsed simulation ticks
- `SpellCastError.SpellOnCooldown` so cooldown refusal is stable and non-stringly typed
- `SpellCastingService` overloads that optionally accept cooldown state:
  - active cooldown rejects the cast before mana spend
  - successful committed casts start cooldown only after mana is spent
  - legacy callers that do not pass cooldown state keep prior behavior
- `SpellExecutionService` overloads that optionally accept cooldown state for both plain and roll-aware
  execution paths
- focused fallback tests covering:
  - default-zero compatibility and explicit cooldown persistence
  - cooldown state start/tick-down/expiry behavior
  - cast rejection while cooldown is active
  - cooldown start on successful casts only
  - no cooldown mutation on target rejection or roll fizzle

## Why this slice matters

Sprint 5 had honest caveats about cooldown state still being absent. That was fine until the execution
pipeline became richer: once the game can validate, route, roll, and resolve casts deterministically,
it also needs a deterministic answer to "can this spell be used again yet?" This increment ships that
answer without overreaching into active buff resolution or resistance systems.

## Bible Back-References

- `docs/EMBER_VISION_BIBLE.md` §3: Sprint 5 remains Layer 3 deterministic gameplay mechanics.
- `docs/EMBER_VISION_BIBLE.md` §8: this is another narrow, testable magic increment rather than a fake
  "full spell system" claim.
- `docs/mechanics/MASTER_MECHANICS_BIBLE.md` §14: spell casting remains a pure simulation concern with
  explicit rule-driven gating.

## Validation

Commands:

```bash
git diff --check
./tools/validation/run-validation.sh --mode fallback
```

Measured result on this branch:
- `git diff --check` passed with no output.
- Fallback validation passed: `Passed: 198, Failed: 0, Skipped: 0, Total: 198`.

## Caveats

- Starter catalog spells still default to zero cooldown; this slice ships the seam and execution guard,
  not a balance pass.
- Cooldown state is external to `ActorRecord` for now. A later Sprint 5 increment should decide whether
  cooldown/save state lives on actor records, a combat session aggregate, or another deterministic
  runtime container.
- Timed buffs like `Ember Ward`, resistances, saving throws, and AoE geometry are still later Sprint 5
  work.
- Local validation remains the pure .NET fallback harness, not a real local Unity Editor / PlayMode run.
