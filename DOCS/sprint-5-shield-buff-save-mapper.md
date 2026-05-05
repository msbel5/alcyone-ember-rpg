# Sprint 5 Shield Buff Save Mapper

Date: 2026-05-05
Branch: `agent/sprint-5-shield-buff-save-mapper`
Base: `3f61e44` — Sprint 5 ShieldBuffApplication merged on `origin/main`

## Scope

`ShieldBuffState` is now both a container (PR #26) and a write surface for
successful casts (PR #27). It still has no persistence path: a save/load
round-trip drops every active shield buff on the floor. This slice closes
that gap with a save mapper that mirrors the existing
`SpellCooldownSaveMapper` pattern.

This slice is persistence-only. It does not introduce tick-down, does not
key buffs to a specific actor, does not change the application path, and
does not alter combat damage application. Those are intentionally future
slices.

Implemented:

- `Assets/Scripts/Domain/World/SliceWorldState.cs` — adds
  `PlayerShieldBuffs` field (`new ShieldBuffState()` default), siblinged
  next to `PlayerSpellCooldowns`.
- `Assets/Scripts/Data/Save/SliceSaveData.cs` — adds two `[Serializable]`
  DTOs and one slice field:
  - `ShieldBuffSaveData { ShieldBuffEntrySaveData[] entries }`.
  - `ShieldBuffEntrySaveData { string spellTemplateId; int remainingTicks; int magnitude }`.
  - `playerShieldBuffs` field on `SliceSaveData`.
- `Assets/Scripts/Data/Save/ShieldBuffSaveMapper.cs` — new pure mapper:
  - `ToData(ShieldBuffState state)`:
    - null state → empty `entries[]` (never null).
    - non-null → ordered ascending by `spellTemplateId` under
      `StringComparer.Ordinal` for deterministic JSON.
    - emits one `ShieldBuffEntrySaveData` per tracked spell id with both
      `remainingTicks` and `magnitude`.
  - `ToState(ShieldBuffSaveData data)`:
    - null DTO or null entries → empty state (never null).
    - skips null entries, blank `spellTemplateId`, `remainingTicks <= 0`,
      and `magnitude < 0`.
    - calls `state.SetActiveBuff(spellTemplateId, remainingTicks, magnitude)`
      so existing domain validation still applies.
- `Assets/Scripts/Data/Save/SliceSaveMapper.cs` — wires the new mapper
  into both `ToData` and `ToWorld` parallel to `playerSpellCooldowns`.
- `Assets/Tests/EditMode/Save/ShieldBuffSaveMapperTests.cs` — 9 EditMode
  tests pinning:
  - null state → empty `entries[]`.
  - empty state → empty `entries[]`.
  - non-empty state → ordinal ordering by `spellTemplateId` and
    preservation of both `remainingTicks` and `magnitude` per entry.
  - null DTO → empty state.
  - DTO with null `entries` → empty state.
  - skip rules: null entry, blank id, zero `remainingTicks`, negative
    `magnitude` are all dropped on load.
  - zero `magnitude` is preserved (a 0-magnitude shield is still a tracked
    timed presence; only `remainingTicks <= 0` removes the entry).
  - round-trip preserves every `(remainingTicks, magnitude)` pair across
    multiple buffs.
  - round-trip of an empty state rebuilds an empty state.
- `Assets/Tests/EditMode/Save/JsonSliceSaveServiceTests.cs` — extends the
  existing slice service round-trip test to seed two `ShieldBuffState`
  entries (`ember_ward` 30/4 and `ash.bind` 6/1) and assert they survive
  `JsonSliceSaveService.SaveToJson` → `LoadFromJson`. Adds a new
  `SaveAndLoad_FreshWorld_StartsWithNoShieldBuffs` test mirroring the
  existing fresh-world cooldown test.

## Why this slice matters

Up to this point the magic foundation could write to a buff state at
runtime but every save flush threw the buffs away on the next load. With
the mapper in place, the buff state graduates into the saveable world
graph alongside `SpellCooldownState`. The next dependent slices —
tick-down resolution, actor-keyed wiring, and damage absorption — now sit
on a state graph that survives save/load, so each can be reasoned about
independently of persistence.

## Bible Back-References

- `docs/EMBER_VISION_BIBLE.md` §3: `ShieldBuffState` remains in
  `Domain.Magic` and the mapper lives in `Data.Save`; no Unity types
  cross the Domain/Simulation boundary.
- `docs/EMBER_VISION_BIBLE.md` §8: another narrow Sprint 5 magic
  increment; not a balance change and not a runtime/HUD change.
- `docs/mechanics/MASTER_MECHANICS_BIBLE.md` §15: timed `ShieldBuff`
  effects now persist across save/load alongside cooldowns.
- PRD Sprint 1 FR-06: deterministic save round-trip is preserved; the
  ordinal ordering on `spellTemplateId` keeps JSON byte-stable for any
  given state.

## Validation

Commands:

```bash
git diff --check
./tools/validation/run-validation.sh --mode fallback
```

Measured result on this branch:
- `git diff --check` produced no output.
- Fallback validation passed:
  `Passed: 240, Failed: 0, Skipped: 0, Total: 240`.
  Previous baseline on `origin/main` (after PR #27) was `230 / 230`;
  this slice added 10 new tests:
  - 9 in `ShieldBuffSaveMapperTests`.
  - 1 in `JsonSliceSaveServiceTests` (`SaveAndLoad_FreshWorld_StartsWithNoShieldBuffs`).
  - The existing `SaveAndLoad_RoundTripsDoorMerchantGuardAndEnemyState`
    test was extended in-place with 6 new asserts on shield-buff
    persistence; the test count for that test stays at 1.

## Release Evidence

- Branch: `agent/sprint-5-shield-buff-save-mapper`
- Local fallback baseline before slice: `230 / 230`
- Local fallback baseline after slice: `240 / 240`
- See PR for commit hashes and CI status when opened.

## Caveats

- Persistence-only. The mapper does not assert that `magnitude > 0`; the
  Domain `SetActiveBuff` allows zero-magnitude buffs and so does the
  load path. If a future balance pass forbids zero-magnitude buffs, the
  filter belongs in the Domain, not in the save layer.
- No tick-down. Loaded buff entries will sit forever in `ShieldBuffState`
  until the next slice introduces a per-tick decay step.
- No actor-keyed wiring. `PlayerShieldBuffs` lives directly on
  `SliceWorldState` parallel to `PlayerSpellCooldowns`; non-player
  actors do not yet have their own `ShieldBuffState`.
- No damage absorption. Loaded buff magnitudes are restored but not yet
  consulted by combat damage application.
- Local validation remains the pure .NET fallback harness, not a real
  local Unity Editor / EditMode run. CI EditMode/PlayMode jobs cover the
  Unity side.

## Thalamus Provenance

- `thalamus_packet_id`: `pkt_20260505041740_a777cb99cb15`
- `thalamus_resolver_key`: `sha256:5938688c605fe453d3fd2080cdbfa07322c364961eedc7311133467cf196d95e`
- Vector query was present (1024-dim, namespaces
  `atoms.code,atoms.plan,atoms.memory`).
- `query_path`: vector
- `vector_query_present`: true
