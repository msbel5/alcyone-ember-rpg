# Sprint Faz-1 — SiteRecord pure record + bounds containment

_Date:_ 2026-05-10 (Europe/Istanbul)
_Branch:_ `agent/sprint-faz-1-site-record` (stacked on `agent/sprint-faz-1-site-id`)
_Box tag:_ `[box=WORLD]`
_Sub-area continued:_ SiteStore (second atom of the WORLD-primary sub-area).

## Increment goal

Land `SiteRecord` (and its supporting `SiteKind` enum) as the pure-Domain
payload for the SiteStore sub-area so the next PR can introduce
`SiteStore` and pin the registry contract. Mirrors `InventoryItem`'s
defensive constructor pattern and `ActorRecord`'s pure-Domain shape:
no Unity, no I/O, immutable invariants pinned at construction.

## Files changed

- `Assets/Scripts/Domain/World/SiteKind.cs` (+ `.meta`) — tiny enum
  in the EquipmentSlot mould: `None = 0`, `Region`, `Settlement`,
  `Dungeon`. `None` is reserved as the empty sentinel and rejected by
  `SiteRecord`.
- `Assets/Scripts/Domain/World/SiteRecord.cs` (+ `.meta`) — sealed
  pure record carrying `(SiteId Id, SiteKind Kind, string Name,
  GridPosition MinBound, GridPosition MaxBound)`. Constructor pins
  invariants: empty `SiteId` rejected, `SiteKind.None` rejected,
  blank or whitespace name rejected, inverted bounds rejected
  (`maxBound` must be component-wise `>=` `minBound`). Adds a single
  read-only helper `Contains(GridPosition)` for inclusive bounds
  containment so the next SiteStore PR can route position queries
  through a record-owned predicate.
- `Assets/Tests/EditMode/World/SiteRecordTests.cs` (+ `.meta`) —
  eight NUnit tests mirroring the ItemId/SiteId test fixture shape:
  constructor stores fields, rejects empty id, rejects None kind,
  rejects blank name, rejects inverted bounds, contains an inside
  point, contains the inclusive boundary corners, rejects outside
  points.
- `docs/sprint-faz-1-atom-map.md` — checked off the SiteRecord atom
  row with a backlink to this PR.
- `docs/sprint-faz-1-site-record.md` — this summary.

## Validation result

`tools/validation/run-validation.sh --mode fallback` — runs the
EditMode harness; SiteRecord rides the existing pure-Domain assembly
(`Assets/Scripts/Domain/EmberCrpg.Domain.asmdef`) and the test fixture
rides `Assets/Tests/EditMode/EmberCrpg.Tests.EditMode.asmdef`, so no
new assembly references are introduced. CI EditMode + GitGuardian
checks gate the merge.

## Agent rules v2 alignment

- Rule 1 (product-visible increment): structural primitive that opens
  a record-shaped consumer for `SiteId`. The next PR (`SiteStore`)
  consumes `SiteRecord` directly, so this atom is not speculative.
- Rule 2 (no speculative utility): `SiteRecord` is a concrete
  prerequisite for `SiteStore`. The companion `Contains` helper is
  bounded to the same record (no fluent builders, no overload
  expansion, no batch wrappers).
- Rule 3 (data-driven effect rule): not in scope; spell pipeline
  untouched.
- Rule 4 (world-store promotion): `SiteRecord` is the payload type
  that `SiteStore` will key by `SiteId`. No new hard-coded slice
  fields added.
- Rule 5 (playable proof every fifth PR): this is the next PR after
  `SiteId`; the playable-proof PR remains scheduled later in the
  atom map (`docs/sprint-faz-1-acceptance.md`).

## Thalamus packet

- packet_id: `pkt_20260509220141_ba9dd06d0593`
- resolver_key:
  `sha256:9762423b428ce7ef33d839c29cf5706bc342f4b1c4b915837e88aed69db7d715`

## Next increment

Add `SiteStore` (dictionary-backed registry over `SiteId -> SiteRecord`
mirroring `ActorStore`) plus its mirrored EditMode test fixture so the
SiteStore sub-area gains its registry contract. After SiteStore lands,
return to the ItemStore sub-area (ItemRecord + ItemStore + tests) so
the four Faz-1 stores (Actor / Item / Site / Faction) advance in
lockstep before save/load round-trip work begins.
