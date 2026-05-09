# Sprint Faz 1 — ActorStore role-view shims

_Date:_ 2026-05-10
_Branch:_ `agent/sprint-faz-1-actor-store-role-shims`
_Atom-map row:_ `Assets/Scripts/Domain/World/ActorStore.cs :: deprecated-view shims [box=LIVING]`
_Mechanic-map box:_ `[box=LIVING]`
_Agent rules v2 alignment:_ rule 4 (world-store promotion) — lays the rail
for replacing `SliceWorldState.Player/Talker/Merchant/Guard/Enemy` with
`ActorStore` lookups; rule 2 (no speculative utility) — concrete consumer
is the very next Faz 1 PR (SliceWorldState migration), backlinked here.

## Increment goal

Add the role-view shim methods to `ActorStore` so the next Faz 1 PR can
read `Player` / `Talker` / `Merchant` / `Guard` / `Enemy` from the store
by `ActorRole` rather than from named slice fields. This atom is one of
the two test-only-eligible PRs in the rule 1 cap, but it adds three new
domain primitives (not just tests), so it is **not** test-only.

## Files changed

- `Assets/Scripts/Domain/World/ActorStore.cs`
  - `IEnumerable<ActorRecord> RecordsByRole(ActorRole role)` —
    deterministic insertion-order enumeration of records carrying the
    role; empty sequence when none match; lazy enumeration (no eager
    list allocation).
  - `ActorRecord FirstByRole(ActorRole role)` — strict variant that
    throws `InvalidOperationException` when no record matches; mirrors
    `Get(ActorId)`.
  - `bool TryFirstByRole(ActorRole role, out ActorRecord record)` —
    safe variant that returns `false` and a `null` record when no
    record matches; mirrors `TryGet(ActorId, out)`.
- `Assets/Tests/EditMode/World/ActorStoreTests.cs` — eight new tests:
  - `RecordsByRole_OnlyMatchingRoleInInsertionOrder`
  - `RecordsByRole_NoMatch_ReturnsEmpty`
  - `RecordsByRole_EmptyStore_ReturnsEmpty`
  - `FirstByRole_ReturnsFirstInInsertionOrder`
  - `FirstByRole_NoMatch_Throws`
  - `TryFirstByRole_KnownRole_ReturnsFirstAndTrue`
  - `TryFirstByRole_NoMatch_ReturnsFalseAndNull`
  - `TryFirstByRole_EmptyStore_ReturnsFalseAndNull`
- `DOCS/sprint-faz-1-atom-map.md` — checked off the deprecated-view
  shims row with a backlink.

## Validation

`./tools/validation/run-validation.sh --mode fallback` →
`Passed! - Failed: 0, Passed: 640, Skipped: 0, Total: 640`
(+8 over the post-PR-#79 baseline of 632).

## Thalamus

- packet_id: `pkt_20260509211634_84b168e8ac6b`
- resolver_key: `sha256:b546e96d5e24eb69b5b1ee7b6a3a801b729666fed0d009ba719741a0ef6ec03e`

## Bundle / atom counts

- atom count this PR: 1 (single deprecated-view-shims atom decomposed
  into three method shims; bundled because they are siblings of the
  same shape — `RecordsByRole` + `FirstByRole` + `TryFirstByRole` —
  matching the @EMSPR PRD-V acceptable-bundle pattern "a method plus
  its variants").
- bundle count this PR: 1.

## Next increment

Migrate `SliceWorldState` consumers off the named slice fields. Replace
direct reads of `SliceWorldState.Player` / `Talker` / `Merchant` /
`Guard` / `Enemy` with `actorStore.TryFirstByRole(ActorRole.X, out var
record)` (or the strict `FirstByRole(...)` where the slice currently
non-null-asserts), then mark the named fields `[Obsolete]`. That PR is
the concrete consumer that satisfies rule 2 (no speculative utility)
for this one.
