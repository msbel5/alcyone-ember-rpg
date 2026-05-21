# Sprint Faz-1 — SiteId pure value handle

_Date:_ 2026-05-10 (Europe/Istanbul)
_Branch:_ `agent/sprint-faz-1-site-id`
_Box tag:_ `[box=WORLD]`
_Sub-area opened:_ SiteStore (first atom of the WORLD-primary sub-area).

## Increment goal

Land `SiteId` as the smallest stable site handle (default = empty
sentinel) so the SiteStore sub-area in `sprint-faz-1-atom-map.md` has
its foundational primitive. Mirrors `ActorId` and `ItemId` byte for
byte: pure Domain value, no lookup, no allocation, no Unity
dependency, value semantics pinned by tests before any consumer
exists.

This is the cheap-model affordance pattern from `@EMSPR` PRD-V:
single pure value type + mirrored test fixture in the same sub-area
shape as the existing handles.

## Files changed

- `Assets/Scripts/Domain/Core/SiteId.cs` (+ `.meta`) — readonly
  struct mirroring `ActorId`/`ItemId`: ctor / `Value` / `IsEmpty` /
  `Equals` / `GetHashCode` / `ToString` / `==` / `!=`. Zero is
  reserved as the empty sentinel.
- `Assets/Tests/EditMode/Core/SiteIdTests.cs` (+ `.meta`) — seven
  tests mirroring `ItemIdTests`: constructor stores value, equal
  values compare equal, different values compare unequal, default is
  empty, equal values share hash code, empty `ToString` returns
  `SiteId.Empty`, non-empty `ToString` contains the raw value.
- `DOCS/sprint-faz-1-atom-map.md` — corrected the SiteStore sub-area
  path from `Domain/World/SiteId.cs` to `Domain/Core/SiteId.cs` (to
  match the existing ActorId/ItemId convention) and checked the row
  off. Also corrected and checked off the ItemId row, which already
  exists at `Domain/Core/ItemId.cs`.
- `DOCS/sprint-faz-1-site-id.md` — this summary.

## Validation result

`tools/validation/run-validation.sh --mode fallback` — see commit
diff for the run output. SiteId carries the same shape as ActorId
and ItemId; the harness already exercises the ActorId/ItemId test
families and now exercises SiteId with the mirrored fixtures.

## Agent rules v2 alignment

- Rule 1 (product-visible increment): structural primitive, not
  test-only. Opens the SiteStore sub-area and is required by every
  Faz-1 atom that needs a site handle (SiteRecord, SiteStore,
  WorldEvent, save-mapper).
- Rule 2 (no speculative utility): SiteId is concrete prerequisite
  for the SiteStore sub-area; not a speculative helper.
- Rule 4 (world-store promotion): lays the rail for `SiteStore` so
  later world state lands in a store rather than as a new
  hard-coded slice field.
- Rule 5 (playable proof every fifth PR): this is PR ~3 of Faz 1;
  the playable-proof PR remains scheduled later in the atom map.

## Thalamus packet

- packet_id: `pkt_20260509215147_99d590be2c51`
- resolver_key:
  `sha256:7bf2c50dfc4fe886a5c5017394b4e16fd7feb1af28e919f677a560b13884a612`

## Next increment

Add `SiteRecord` (pure record for region / settlement / dungeon —
kind + name + grid bounds) so the next PR can introduce
`SiteStore` and pin the registry contract. Continue widening the
SiteStore sub-area before turning to ItemRecord / ItemStore.
