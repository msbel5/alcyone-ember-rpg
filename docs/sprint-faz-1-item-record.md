# Sprint Faz 1 — ItemRecord (MATTER-box primitive)

_Date:_ 2026-05-11
_Branch:_ `agent/sprint-faz-1-item-record`
_Box:_ `[box=MATTER]`
_Atom-map:_ `DOCS/sprint-faz-1-atom-map.md` (ItemStore sub-area)

## Increment goal

Land the MATTER-box pure-Domain primitive `ItemRecord` together with
its two value enums (`ItemMaterial`, `ItemQuality`) so the next Faz 1
PR can stand `ItemStore` up on the same shape as `ActorStore` and
`SiteStore` without any speculative scope.

## Files changed

- `Assets/Scripts/Domain/Inventory/ItemMaterial.cs` (+meta) — enum
  `None=0 / Wood / Iron / Cloth`, mirrors `SiteKind`'s shape.
- `Assets/Scripts/Domain/Inventory/ItemQuality.cs` (+meta) — enum
  `None=0 / Common / Fine / Masterwork`, mirrors `SiteKind`'s shape.
- `Assets/Scripts/Domain/Inventory/ItemRecord.cs` (+meta) — pure
  record over `ItemId + ItemMaterial + ItemQuality + EquipmentSlot`,
  defensive constructor mirrors `SiteRecord`, `IsEquipment` view
  shim derived from `EquipmentSlot`.
- `Assets/Tests/EditMode/Inventory/ItemRecordTests.cs` (+meta) —
  NUnit pins for field storage, `IsEquipment`, `Slot=None` legal
  non-equipment path, and the three sentinel rejections.
- `DOCS/sprint-faz-1-atom-map.md` — check off `ItemRecord` row,
  re-point "Next increment after this PR" line at `ItemStore`,
  record this run's Thalamus packet alongside ActorStore's.

## Agent rules v2 application

- Rule 1 (product-visible): pure-Domain MATTER primitive; not
  test-only — adds a new consumable record + two enums that the
  next PR will use as `ItemStore`'s payload. Counts toward visible
  progress in the same shape as `ActorStore` / `SiteRecord`.
- Rule 2 (no speculative utility): no helpers, no overloads, no
  builders. `IsEquipment` is the same one-liner view shim
  `InventoryItem` already exposes, so it has a concrete consumer.
- Rule 3 (data-driven effect): n/a — no magic surface touched.
- Rule 4 (world-store promotion): no new named field added to
  `SliceWorldState`; this is the MATTER store's payload, not a
  slice extension.
- Rule 5 (playable proof): scheduled for the every-fifth-PR cadence
  per the Faz 1 atom map's PLAYABLE sub-area.

## Validation result

- `./tools/validation/run-validation.sh --mode fallback` —
  **Passed! Failed: 0, Passed: 661, Skipped: 0, Total: 661**
  (six new `ItemRecordTests` pins included; baseline reflects this
  branch being cut from `main` before PR #83 SiteStore merges).

## Thalamus packet

- packet_id: `pkt_20260510231640_676878a28180`
- resolver_key: `sha256:6024f95514fc0b2dc719ca79bc78baeecbd125766fc11ac895fc99ac92b30519`

## Next increment

`Assets/Scripts/Domain/World/ItemStore.cs` + matching
`Assets/Tests/EditMode/World/ItemStoreTests.cs`, mirroring
`ActorStore` / `SiteStore` registry contract over `ItemId ->
ItemRecord` with default-id rejection and deterministic
insertion-order enumeration.
