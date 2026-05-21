# Sprint Faz-1 — FactionId pure value handle

_Date:_ 2026-05-11 (Europe/Istanbul)
_Branch:_ `agent/sprint-faz-1-faction-id`
_Box tag:_ `[box=SOCIETY]`
_Sub-area opened:_ FactionStore (first atom of the SOCIETY-seed sub-area).

## Increment goal

Land `FactionId` as the smallest stable faction handle (default =
empty sentinel) so the FactionStore sub-area in
`sprint-faz-1-atom-map.md` has its foundational primitive. Mirrors
`ActorId`, `ItemId`, and `SiteId` byte for byte: pure Domain value,
no lookup, no allocation, no Unity dependency, value semantics pinned
by tests before any consumer exists.

Same cheap-model affordance pattern as the prior SiteId / SiteRecord /
ItemRecord increments: single pure value type plus mirrored test
fixture inside one sub-area shape.

## Files changed

- `Assets/Scripts/Domain/Core/FactionId.cs` (+ `.meta`) — readonly
  struct mirroring `ActorId`/`ItemId`/`SiteId`: ctor / `Value` /
  `IsEmpty` / `Equals` / `GetHashCode` / `ToString` / `==` / `!=`.
  Zero is reserved as the empty no-faction sentinel.
- `Assets/Tests/EditMode/Core/FactionIdTests.cs` (+ `.meta`) — seven
  tests mirroring `SiteIdTests`: constructor stores value, equal
  values compare equal, different values compare unequal, default is
  empty, equal values share hash code, empty `ToString` returns
  `FactionId.Empty`, non-empty `ToString` contains the raw value.
- `DOCS/sprint-faz-1-atom-map.md` — corrected the FactionStore
  sub-area path from `Domain/World/FactionId.cs` to
  `Domain/Core/FactionId.cs` (matching the ActorId/ItemId/SiteId
  convention) and checked the FactionId row off. Appended the
  FactionId Thalamus packet metadata. Replaced the trailing "next
  increment" note so it points at `FactionRecord` next.
- `DOCS/sprint-faz-1-faction-id.md` — this summary.

## Validation result

`tools/validation/run-validation.sh --mode fallback` — fallback
harness exercises the EditMode Core test family on the active
branch. FactionId carries the same shape as ActorId / ItemId /
SiteId, and the new `FactionIdTests` fixture mirrors the existing
`SiteIdTests` family one-for-one.

## Agent rules v2 alignment

- Rule 1 (product-visible increment): structural primitive opening
  the SOCIETY-seed sub-area. Not a test-only PR — adds a new
  Domain primitive consumed by every future Faction* atom. Three
  of the last five Faz-1 PRs (#82 SiteRecord, #83 SiteStore, #84
  ItemRecord, #85 ItemStore, this PR) added concrete primitives,
  not test fixtures, keeping rule 1's two-PR test-only cap clear.
- Rule 2 (no speculative utility): FactionId is the concrete
  prerequisite for FactionRecord / FactionStore in the atom map.
  Not a speculative helper.
- Rule 4 (world-store promotion): lays the rail for FactionStore so
  later faction state lands in a store rather than as a new
  hard-coded `SliceWorldState` field.
- Rule 5 (playable proof every fifth PR): playable-proof PR remains
  scheduled later in the atom map; this is another structural-rail
  PR, not the proof PR.

## Thalamus packet

- packet_id: `pkt_20260510234830_913d417ed6e7`
- resolver_key:
  `sha256:0db90954d88677a0dafaa3fc6aa0216dadbf5cfe8296260dffa40fea0d640940`

## Next increment

Add `FactionRecord` (pure record carrying `name` + `tags`, mirroring
the `SiteRecord` / `ItemRecord` shape) so the next PR can introduce
`FactionStore` and pin the registry contract under the same shape as
the other three Faz-1 stores.
