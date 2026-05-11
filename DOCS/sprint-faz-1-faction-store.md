# Sprint Faz 1 — FactionStore registry

_Date:_ 2026-05-11
_Branch:_ `agent/sprint-faz-1-faction-store`
_Box:_ `[box=SOCIETY]` (seed)
_Atom-map ref:_ `DOCS/sprint-faz-1-atom-map.md` — FactionStore sub-area

## Increment goal

Close the SOCIETY-seed sub-area by landing `FactionStore`: a
dictionary-backed registry over `FactionId -> FactionRecord` that
mirrors `ActorStore` / `ItemStore` / `SiteStore`. Same contract:
`Add` / `Get` / `TryGet` / `Remove` / `Contains` / `Count` /
`Clear` / `Records`, deterministic insertion-order enumeration, and
default-id rejection. Pure Domain — no Unity, no I/O.

## Files changed

- `Assets/Scripts/Domain/World/FactionStore.cs` (+ `.meta`)
  - Sealed class mirroring `SiteStore` shape verbatim, retyped to
    `FactionId` / `FactionRecord`.
  - Default-id rejection on `Add` / `Get`; `TryGet` / `Contains` /
    `Remove` return `false` on the empty sentinel.
  - `_byId` dictionary + `_order` list pair preserves insertion order
    across `Add` / `Remove` for stable `Records` enumeration.
- `Assets/Tests/EditMode/World/FactionStoreTests.cs` (+ `.meta`)
  - 14 NUnit tests mirroring `SiteStoreTests`: Add/Get round-trip,
    null-record rejection, duplicate-id rejection, missing-id
    `KeyNotFoundException`, empty-id `ArgumentException`,
    TryGet (missing / empty / known), Contains (registered / unknown /
    empty), Remove (known / missing / empty), Clear, insertion-order
    enumeration, post-Remove order preservation.
  - `MakeRecord` helper uses `Array.Empty<string>()` for tags so the
    test surface stays focused on the store contract (tag invariants
    are pinned by `FactionRecordTests`).

## Validation result

`tools/validation/run-validation.sh --mode fallback` — `PASS`,
`Passed: 722, Failed: 0, Skipped: 0` (no Unity editor binary on Pi;
fallback harness is the contract-pinning surface here).

## Agent-rules v2 compliance

- Rule 1 (product-visible increment): this PR adds a new public
  Domain registry type (`FactionStore`). It is not test-only — the
  type itself is the visible increment, and `SliceWorldState`
  consumers can begin migrating from hard-coded faction fields onto
  it in a follow-up PR.
- Rule 2 (no speculative utility): the store only exposes the same
  members `ActorStore` / `SiteStore` / `ItemStore` already expose; no
  fluent builders, batch helpers, or LINQ surface beyond the
  enumerable `Records` projection. Concrete consumers (the upcoming
  `SliceWorldState` migration) ship in the next PR.
- Rule 3 (data-driven effect): not applicable; no `SpellEffectKind`
  change.
- Rule 4 (world-store promotion): this PR is the SOCIETY-seed half
  of the world-store promotion path — it provides the registry that
  `SliceWorldState`'s hard-coded faction fields will migrate onto in
  a follow-up PR, per the Faz 1 atom-map.
- Rule 5 (playable proof): not the playable-proof slot. With this PR
  the four Faz 1 Core Store sub-areas (ActorStore / ItemStore /
  SiteStore / FactionStore) all have at least one merged primitive;
  the playable-proof PR can land once `SliceWorldState` consumers
  migrate and a guard can be spawned through the new stores.

## Thalamus packet

- packet_id: `pkt_20260511055127_3b7b253d3951`
- resolver_key: `sha256:6cecc1132d0e38e06a14aabef3b87c65c030c8a72a4c5194918c6449bb9eb9aa`
- category_filter: `["atoms.code", "atoms.plan", "atoms.memory"]`

## Next increment

`Assets/Scripts/Domain/World/SliceWorldState.cs` migration — replace
direct `Player` / `Talker` / `Merchant` / `Guard` / `Enemy` reads
with `ActorStore` + role-view shim lookups and mark the hard-coded
fields `[Obsolete]`, per atom-map LIVING sub-area row. After that,
the PROCESS-box `WorldEvent` / `ReasonTrace` / `WorldEventLog`
triad opens the path to the Faz 1 acceptance proof.
