# Sprint 5 Magic Tick Driver

Date: 2026-05-05
Branch: `agent/sprint-5-magic-tick-driver`
Base: `59d581f` — Sprint 5 ShieldBuff tick-down merged on `origin/main`

## Scope

`SpellCooldownService.AdvanceTicks` and `ShieldBuffService.AdvanceTicks`
are now both in place as deterministic per-tick decay seams, but they
are independent: the wider simulation loop currently has to know about
both services and call them in the right order on the right state
containers. This slice introduces a thin pure-Simulation coordinator so
the outer tick loop can advance the entire Sprint 5 timed-magic surface
through one call.

This slice is orchestration-only. It does not introduce new decay
rules, does not change the application paths (cast/roll/effect
resolution/shield-buff application), does not alter save/load, and does
not call into combat resolution.

Implemented:

- `Assets/Scripts/Simulation/Magic/MagicTickDriver.cs` — new pure
  simulation coordinator:
  - Constructor takes `SpellCooldownService` and `ShieldBuffService`;
    both are required and null-checked.
  - `AdvanceTicks(SpellCooldownState cooldownState, ShieldBuffState
    shieldBuffState, int elapsedTicks)`:
    - null cooldown state → `ArgumentNullException`.
    - null shield-buff state → `ArgumentNullException`.
    - negative `elapsedTicks` → `ArgumentOutOfRangeException`.
    - `elapsedTicks == 0` → no-op (mirrors both underlying services).
    - Otherwise delegates to
      `SpellCooldownService.AdvanceTicks(cooldownState, elapsedTicks)`
      and then
      `ShieldBuffService.AdvanceTicks(shieldBuffState, elapsedTicks)`.
- `Assets/Tests/EditMode/Magic/MagicTickDriverTests.cs` — 11 EditMode
  tests pinning:
  - constructor null guards for both services.
  - `AdvanceTicks` null guards for both states.
  - negative elapsed throws.
  - zero elapsed is a no-op for both bags.
  - non-zero elapsed decays both bags by exactly the elapsed amount.
  - exact-elapsed expiry removes entries from both bags simultaneously.
  - empty bags are a safe no-op.
  - repeated calls accumulate decay across both bags.
  - parity test: driver-driven decay equals stand-alone
    `SpellCooldownService.AdvanceTicks` plus
    `ShieldBuffService.AdvanceTicks` on equivalent input state.

## Why this slice matters

Up to this point an outer simulation tick had to wire two separate
services and remember which container each owns. With the driver in
place there is one entry point — `MagicTickDriver.AdvanceTicks(...)` —
that any future combat or world tick loop can call without crossing
into Domain implementation details. This keeps the planned next slices
(actor-keyed shield wiring, damage absorption, encounter combat hookup)
focused on their own behavior rather than on per-tick plumbing.

## Bible Back-References

- `docs/EMBER_VISION_BIBLE.md` §3 Layer 3: pure Simulation seam, no
  Unity types, no presentation coupling.
- `docs/mechanics/MASTER_MECHANICS_BIBLE.md` §15: timed magic effects
  decay deterministically; the driver preserves that contract by
  delegating without modification.
- PRD Sprint 1 FR-06: deterministic save round-trip is preserved; the
  driver does not touch save state directly, it only forwards to
  services that already respect the save mappers.

## Validation

Commands:

```bash
git diff --check
./tools/validation/run-validation.sh --mode fallback
```

Measured result on this branch:
- `git diff --cached --check` produced no output.
- Fallback validation passed:
  `Passed: 262, Failed: 0, Skipped: 0, Total: 262`.

## Next dependent slices

- Actor-keyed shield-buff wiring so multiple casters can hold
  independent buff bags driven by this same coordinator.
- Damage absorption that consumes shield-buff magnitude on incoming
  damage, ahead of HP reduction.
- Combat-loop integration that calls `MagicTickDriver.AdvanceTicks`
  once per encounter tick.

## Thalamus

- packet_id: `pkt_20260505061745_87f067c568b1`
- resolver_key:
  `sha256:e8f4819ae22fe48dd6c4d2f1e8af30f161a911f4b365b58cc3414189b7d36881`
- inline_vector: present
- query_path: vector
- escalation_reason: complex planning/design task requires Captain
  decomposition (handled directly by Captain — small <100 LOC slice
  mirroring an existing pattern, no Builder spawn).
