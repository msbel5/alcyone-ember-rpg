# Sprint Faz 1 — FactionRecord pure record

_Date:_ 2026-05-11
_Branch:_ `agent/sprint-faz-1-faction-record`
_Box:_ `[box=SOCIETY]` (seed)
_Atom-map ref:_ `DOCS/sprint-faz-1-atom-map.md` — FactionStore sub-area

## Increment goal

Land the second SOCIETY-seed primitive `FactionRecord`: a pure
`EmberCrpg.Domain.World` record carrying a `FactionId`, a display
name, and an insertion-ordered tag bag. Mirrors `SiteRecord` and
`ItemRecord` shape so the upcoming `FactionStore` rides the
registry contract already used by `ActorStore` / `SiteStore` /
`ItemStore`. No Unity, no I/O, no serialization concerns — pure
Domain record with defensive constructor.

## Files changed

- `Assets/Scripts/Domain/World/FactionRecord.cs` (+ `.meta`)
  - Sealed class with `(FactionId id, string name, IEnumerable<string> tags)` ctor.
  - Empty-id, blank-name, null-tags, and blank-tag-entry rejection.
  - Defensive copy of supplied tag enumerable into an internal `string[]`.
  - `IReadOnlyList<string> Tags` projection preserves insertion order.
  - `HasTag(string)` is a case-sensitive exact-match probe; rejects null/blank.
- `Assets/Tests/EditMode/World/FactionRecordTests.cs` (+ `.meta`)
  - 10 NUnit tests pinning: constructor field storage, empty-id rejection,
    blank-name rejection, null-tags rejection, blank-tag-entry rejection,
    empty-tag-bag acceptance, insertion-order preservation, defensive copy
    against caller mutation, known-tag `HasTag` truth, unknown/case-mismatch/
    blank/null `HasTag` falsity.

## Validation result

`tools/validation/run-validation.sh --mode fallback` — `PASS`,
`Passed: 706, Failed: 0, Skipped: 0` (no Unity editor binary on Pi;
fallback harness is the contract-pinning surface here).

## Agent-rules v2 compliance

- Rule 1 (product-visible increment): this PR is a small Domain
  primitive addition (`FactionRecord`). It is *not* test-only — it
  adds a new public type that the next Faz 1 increment (`FactionStore`)
  consumes. It does not count against the two-test-only-PR cap.
- Rule 2 (no speculative utility): the type only exposes what
  `FactionStore` needs in the next PR (`Id`, `Name`, `Tags`, `HasTag`).
  No fluent builders, no batch helpers, no group-by overloads.
- Rule 3 (data-driven effect): not applicable; no `SpellEffectCode`
  change.
- Rule 4 (world-store promotion): not applicable; no
  `SliceWorldState` field added. This PR adds Domain payload for the
  forthcoming `FactionStore`.
- Rule 5 (playable proof): not the playable-proof slot. Counts as PR
  4 of Faz 1 (after `ActorStore`, `ActorStore` role-shims,
  `SiteId`, `SiteRecord`, `SiteStore`, `ItemRecord`, `ItemStore`,
  `FactionId`); the playable-proof PR for SOCIETY-seed material can
  land once `FactionStore` registers a guard with a faction.

## Thalamus packet

- packet_id: `pkt_20260511001218_a215b9da9279`
- resolver_key: `sha256:3a7e0d9766c3e410ec208832885407539df2b6f603b0ebff3b80f8a20601b3eb`
- category_filter: `["atoms.code", "atoms.plan", "atoms.memory"]`
- confidence: 0.361 (low; routine-level escalation_reason notes the
  Builder + tests expectation already executed by Captain inline)

## Next increment

`Assets/Scripts/Domain/World/FactionStore.cs` — dictionary-backed
registry over `FactionId -> FactionRecord` matching the
`ActorStore` / `SiteStore` / `ItemStore` contract
(`Add` / `Get` / `TryGet` / `Remove` / `Contains` / `Count` / `Clear`
/ `Records`, deterministic insertion-order enumeration, default-id
rejection). Pinned by `Assets/Tests/EditMode/World/FactionStoreTests.cs`
mirroring the existing store-test shape.
