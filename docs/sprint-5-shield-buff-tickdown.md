# Sprint 5 Shield Buff Tick-Down

Date: 2026-05-05
Branch: `agent/sprint-5-shield-buff-tickdown`
Base: `b17f15b` — Sprint 5 ShieldBuff save mapper merged on `origin/main`

## Scope

`ShieldBuffState` is now a write surface (PR #27 application) and a saveable
container (PR #28 save mapper), but it has no decay path. Loaded or freshly
applied buffs sit forever at their declared `remainingTicks`. This slice
introduces a deterministic per-tick decay seam mirroring the existing
`SpellCooldownService.AdvanceTicks` pattern.

This slice is decay-only. It does not introduce damage absorption, does not
key buffs to a specific actor, does not change the application path, and
does not call into combat resolution. Those are intentionally future
slices.

Implemented:

- `Assets/Scripts/Simulation/Magic/ShieldBuffService.cs` — new pure
  simulation service:
  - `AdvanceTicks(ShieldBuffState shieldBuffState, int elapsedTicks)`:
    - null state → `ArgumentNullException`.
    - negative `elapsedTicks` → `ArgumentOutOfRangeException`.
    - `elapsedTicks == 0` → no-op.
    - For every tracked spell id, decrements `remainingTicks` by
      `elapsedTicks`, clamping at zero. Magnitude is preserved while the
      buff is alive; on expiry the entry is removed via the existing
      `ShieldBuffState.SetActiveBuff(id, 0, magnitude)` removal contract,
      keeping the state graph clean for save/load.
- `Assets/Tests/EditMode/Magic/ShieldBuffServiceTests.cs` — 11 EditMode
  tests pinning:
  - linear decay preserves magnitude.
  - exact expiry removes the entry.
  - over-expiry clamps at zero and removes the entry.
  - multiple buffs decay independently; only expired ones are dropped.
  - zero elapsed is a no-op.
  - empty state is a safe no-op.
  - negative elapsed throws.
  - null state throws.
  - zero-magnitude buffs decay and expire like any other entry.
  - repeated `AdvanceTicks` calls accumulate decay correctly.
  - already-expired entries are not resurrected by later advances.

## Why this slice matters

Up to this point the magic foundation could write timed shield buffs and
persist them across save/load, but the buffs never decayed. With the
service in place, the buff state graduates from a static container into a
proper timed-effect state machine that can be advanced by an outer tick
driver in a future slice. The next dependent slices — actor-keyed wiring
and damage absorption — now sit on a decay path that matches the
cooldown seam, so each can be reasoned about independently of timing.

## Bible Back-References

- `docs/EMBER_VISION_BIBLE.md` §3 Layer 3: pure Simulation seam, no
  Unity types, no presentation coupling.
- `docs/mechanics/MASTER_MECHANICS_BIBLE.md` §15: timed `ShieldBuff`
  effects expire deterministically once their remaining-tick budget is
  exhausted.
- PRD Sprint 1 FR-06: deterministic save round-trip is preserved; the
  decay path uses the existing `SetActiveBuff` removal contract that the
  save mapper already round-trips.

## Validation

Commands:

```bash
git diff --check
./tools/validation/run-validation.sh --mode fallback
```

Measured result on this branch:
- `git diff --check` produced no output.
- Fallback validation passed:
  `Passed: 251, Failed: 0, Skipped: 0, Total: 251`.
  Previous baseline on `origin/main` (after PR #28) was `240 / 240`;
  this slice added 11 new tests in `ShieldBuffServiceTests`.

## Release Evidence

- Branch: `agent/sprint-5-shield-buff-tickdown`
- Base commit: `b17f15b`
- Local fallback baseline before slice: `240 / 240` (per
  `DOCS/sprint-5-shield-buff-save-mapper.md`).
- Local fallback baseline after slice: `251 / 251`.
- 11 new EditMode tests in `ShieldBuffServiceTests`.
- See PR for commit hashes and CI status when opened.

## Caveats

- Decay-only. The service does not consult buff magnitude for damage
  absorption; combat damage application is unchanged this slice.
- No actor-keyed wiring. `ShieldBuffState` still hangs off
  `SliceWorldState.PlayerShieldBuffs`; non-player actors do not yet
  have their own state.
- No runtime driver. The slice ships the seam but does not yet call
  `AdvanceTicks` from a per-tick simulation loop. Tests exercise the
  service directly.
- Local validation remains the pure .NET fallback harness, not a real
  local Unity Editor / EditMode run. CI EditMode/PlayMode jobs cover the
  Unity side.

## Thalamus Provenance

- `thalamus_packet_id`: `pkt_20260505051718_15f66c8f72b8`
- `thalamus_resolver_key`: `sha256:037ef649fc9511189675fd5000a58fd8b00162c475cb00cc2f62abbfa9c72a2f`
- Vector query was present (1024-dim inline vector, namespaces
  `atoms.code,atoms.plan,atoms.memory`).
- `query_path`: vector
- `vector_query_present`: true
