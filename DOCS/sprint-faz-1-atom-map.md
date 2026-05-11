# Faz 1 — Atom map (Core Store reset)

_Date:_ 2026-05-09
_Branch:_ `agent/sprint-faz-1-actor-store` (first PR of Faz 1)
_Primary Faz 1 boxes:_ `[box=WORLD]`, `[box=LIVING]`, `[box=MATTER]`.
_Support / seed boxes used in this map:_ `[box=SOCIETY]` (FactionStore seed), `[box=PROCESS]` (WorldEventLog + ReasonTrace), `[box=TIME]` (save/load round-trip), `[box=PLAYABLE]` (acceptance proof).
_Acceptance gate:_ `player can spawn a guard, talk to it, then walk to
a second site and watch the same guard remembered across save/load`
(from `docs/ROADMAP.md` Faz 1).

This atom map is the canonical decomposition of Faz 1 against
`DOCS/mechanic-map-v1.md` (8-box living-world model) and
`DOCS/agent-rules-v2.md` (five rules — product-visible increment,
no speculative utility, data-driven effects, world-store promotion,
playable proof). Per `CRON_CODES.md @EMSPR` PRD-V atomic decomposition
+ sprint promotion hard rule, Faz 1 is not a candidate for promotion
until every row below is checked off, every sub-area has at least one
merged PR, and `tools/validation/run-validation.sh` passes.

Format: `- [ ] file/path :: scope :: brief responsibility [box=...]`.

## Sub-area: ActorStore (LIVING — primary)

- [x] `Assets/Scripts/Domain/World/ActorStore.cs` :: `ActorStore` :: dictionary-backed registry over `ActorId -> ActorRecord` with Add/Get/TryGet/Remove/Contains/Count/Clear/Records, deterministic enumeration, default-id rejection [box=LIVING] — landed via `agent/sprint-faz-1-actor-store` (PR #79, merge `a347efe`)
- [x] `Assets/Tests/EditMode/World/ActorStoreTests.cs` :: tests :: pin Add/Get/TryGet/Remove/Contains/Count/Clear/Records contracts and default-id rejection [box=LIVING] — landed alongside PR #79
- [x] `Assets/Scripts/Domain/World/ActorStore.cs` :: deprecated-view shims :: `Player`/`Talker`/`Merchant`/`Guard`/`Enemy` resolved from a `ActorRole`/role-tag lookup over the store (lands in a follow-up PR before SliceWorldState consumers migrate) [box=LIVING] — landed via `RecordsByRole`/`FirstByRole`/`TryFirstByRole` (sprint faz-1 role-shims PR)
- [x] `Assets/Scripts/Domain/World/SliceWorldState.cs` :: store-backed named actor views :: `Player`/`Talker`/`Merchant`/`Guard`/`Enemy` are deprecated properties over `ActorStore.FirstByRole(...)`, so existing consumers read the store-backed shim while new work targets `Actors` directly [box=LIVING] — landed via `agent/sprint-faz-1-slice-actor-views`; pinned by `Assets/Tests/EditMode/World/SliceWorldStateActorViewTests.cs`

## Sub-area: ItemStore (MATTER — primary)

- [x] `Assets/Scripts/Domain/Core/ItemId.cs` :: `ItemId` :: readonly value handle (default = empty) [box=MATTER] — pre-existing in Core/ (path corrected from Inventory/ to match ActorId convention); pinned by `Assets/Tests/EditMode/Core/ItemIdTests.cs`
- [x] `Assets/Scripts/Domain/Inventory/ItemRecord.cs` :: `ItemRecord` :: pure record carrying material + quality + slot kind [box=MATTER] — landed via `agent/sprint-faz-1-item-record` together with `Assets/Scripts/Domain/Inventory/ItemMaterial.cs` (enum: Wood / Iron / Cloth) and `Assets/Scripts/Domain/Inventory/ItemQuality.cs` (enum: Common / Fine / Masterwork); pinned by `Assets/Tests/EditMode/Inventory/ItemRecordTests.cs` mirroring `SiteRecordTests`
- [x] `Assets/Scripts/Domain/World/ItemStore.cs` :: `ItemStore` :: dictionary-backed registry over `ItemId -> ItemRecord` mirroring `ActorStore` shape [box=MATTER] — landed via `agent/sprint-faz-1-item-store`; insertion-order enumeration mirrored from `SiteStore`/`ActorStore`
- [x] `Assets/Tests/EditMode/World/ItemStoreTests.cs` :: tests :: pin store contracts and default-id rejection [box=MATTER] — landed with the ItemStore PR covering Add/Get/TryGet/Remove/Contains/Count/Clear/Records + default-id rejection

## Sub-area: SiteStore (WORLD — primary)

- [x] `Assets/Scripts/Domain/Core/SiteId.cs` :: `SiteId` :: readonly value handle [box=WORLD] — landed via `agent/sprint-faz-1-site-id` (path corrected from World/ to Core/ to match ActorId/ItemId convention); pinned by `Assets/Tests/EditMode/Core/SiteIdTests.cs`
- [x] `Assets/Scripts/Domain/World/SiteRecord.cs` :: `SiteRecord` :: pure record for region / settlement / dungeon (kind + name + grid bounds) [box=WORLD] — landed via `agent/sprint-faz-1-site-record` together with `Assets/Scripts/Domain/World/SiteKind.cs` (enum: Region / Settlement / Dungeon); pinned by `Assets/Tests/EditMode/World/SiteRecordTests.cs`
- [x] `Assets/Scripts/Domain/World/SiteStore.cs` :: `SiteStore` :: dictionary-backed registry over `SiteId -> SiteRecord` [box=WORLD] — landed via `agent/sprint-faz-1-site-store` (PR #83, merge `227ed95`); mirrors `ActorStore` shape; insertion-order enumeration verified
- [x] `Assets/Tests/EditMode/World/SiteStoreTests.cs` :: tests :: pin store contracts and default-id rejection [box=WORLD] — landed with PR #83 covering Add/Get/TryGet/Remove/Contains/Count/Clear/Records, default-id rejection, and bounds-check coverage

## Sub-area: FactionStore (SOCIETY-seed)

- [x] `Assets/Scripts/Domain/Core/FactionId.cs` :: `FactionId` :: readonly value handle [box=SOCIETY] — landed via `agent/sprint-faz-1-faction-id` (path corrected from World/ to Core/ to match ActorId/ItemId/SiteId convention); pinned by `Assets/Tests/EditMode/Core/FactionIdTests.cs`
- [x] `Assets/Scripts/Domain/World/FactionRecord.cs` :: `FactionRecord` :: pure record (name + tags); empty seed populated in Faz 6 [box=SOCIETY] — landed via `agent/sprint-faz-1-faction-record`; insertion-order tag bag with defensive copy + `HasTag` lookup, mirroring `SiteRecord`/`ItemRecord` shape; pinned by `Assets/Tests/EditMode/World/FactionRecordTests.cs`
- [x] `Assets/Scripts/Domain/World/FactionStore.cs` :: `FactionStore` :: dictionary-backed registry [box=SOCIETY] — landed via `agent/sprint-faz-1-faction-store` (PR #88, merge `6c164eb`); mirrors `ActorStore`/`SiteStore`/`ItemStore` shape with deterministic insertion-order enumeration
- [x] `Assets/Tests/EditMode/World/FactionStoreTests.cs` :: tests :: pin store contracts [box=SOCIETY] — landed alongside PR #88 covering Add/Get/TryGet/Remove/Contains/Count/Clear/Records + default-id rejection

## Sub-area: WorldEvent log + ReasonTrace (PROCESS — primary)

- [x] `Assets/Scripts/Domain/World/WorldEvent.cs` :: `WorldEvent` :: typed event payload (`tick`, `kind`, `actorId`, `siteId`, `reason`) [box=PROCESS] — landed via `agent/sprint-faz-1-world-event` (PR #89, merge `b659733`); pinned by `Assets/Tests/EditMode/World/WorldEventTests.cs`; ships alongside `Assets/Scripts/Domain/World/WorldEventKind.cs` seed enum (None / ActorSpawned / ActorTalked / SiteEntered) covering the Faz 1 acceptance-gate event categories
- [x] `Assets/Scripts/Domain/World/ReasonTrace.cs` :: `ReasonTrace` :: causal-chain record attached to an event [box=PROCESS] — landed via `agent/sprint-faz-1-reason-trace`; ordered, root-first immutable cause chain with `Causes` / `Depth` / `RootCause` / `LeafCause` / `HasCause`, defensive copy + blank/empty rejection mirroring `FactionRecord` / `SiteRecord`; pinned by `Assets/Tests/EditMode/World/ReasonTraceTests.cs`
- [x] `Assets/Scripts/Domain/World/WorldEventLog.cs` :: `WorldEventLog` :: append-only log over `WorldEvent` with deterministic enumeration [box=PROCESS] — landed via `agent/sprint-faz-1-world-event-log`; List-backed chronicle with `Append` (null rejected), `Count`, `IsEmpty`, and a `ReadOnlyCollection`-wrapped `Events` view so callers cannot downcast back to a mutable list; insertion-order preserved even when ticks decrease (chronicle, not sorter)
- [x] `Assets/Tests/EditMode/World/WorldEventLogTests.cs` :: tests :: pin append + deterministic enumeration [box=PROCESS] — landed alongside the log; covers empty-log state, single append, multi append insertion order, decreasing-tick insertion order, null rejection, live view tracking later appends, and `Events` view immutability. Reason-trace preservation is covered by the landed trace-attachment PR #92.
- [x] `Assets/Scripts/Domain/World/WorldEvent.cs` :: `ReasonTrace` attachment :: carry optional causal chain on each world event so `WorldEventLog` can preserve why the event happened [box=PROCESS] — landed via `agent/sprint-faz-1-world-event-reason-trace`; pinned by `WorldEventTests` and `WorldEventLogTests`


## Sub-area: Runtime store roots (WORLD/LIVING/MATTER/SOCIETY/PROCESS prerequisite)

- [x] `Assets/Scripts/Domain/World/SliceWorldState.cs` :: core store roots :: `Actors` / `Items` / `Sites` / `Factions` / `Events` are initialized as canonical runtime roots so Faz 1 save/load can serialize stores directly without adding new named slice fields [box=LIVING][box=MATTER][box=WORLD][box=SOCIETY][box=PROCESS] — landed via `agent/sprint-faz-1-store-roots`
- [x] `Assets/Tests/EditMode/World/SliceWorldStateActorViewTests.cs` :: tests :: pin fresh `SliceWorldState` and factory-created worlds expose non-null empty Item/Site/Faction/Event roots while preserving existing store-backed actor views [box=LIVING][box=MATTER][box=WORLD][box=SOCIETY][box=PROCESS] — landed with `agent/sprint-faz-1-store-roots`

## Sub-area: Save/load round-trip (TIME — primary)

- [x] `Assets/Scripts/Data/Save/SliceSaveMapper.cs` :: extend mapper :: serialize `ActorStore` / `ItemStore` / `SiteStore` / `FactionStore` / `WorldEventLog` alongside `SliceWorldState` (migration-friendly: write both, read both, prefer stores) [box=TIME] — landed via `agent/sprint-faz-1-store-save-roundtrip`; DTO arrays live in `SliceSaveData`, canonical actor-store data overrides legacy actor fields on load
- [x] `Assets/Tests/EditMode/Save/StoreRoundTripTests.cs` :: tests :: pin deterministic round-trip for the four stores + event log [box=TIME] — landed with `agent/sprint-faz-1-store-save-roundtrip`; covers JSON round-trip for actors/items/sites/factions/events and canonical actor-store preference

## Sub-area: Acceptance proof (PLAYABLE — every fifth PR)

- [ ] `DOCS/sprint-faz-1-acceptance.md` :: `player can ...` :: scene screenshot OR deterministic replay log OR debug-HUD dump showing a guard spawned + talked to + remembered after save/load + walking to a second site (rule 5: playable proof) [box=PLAYABLE]

## Promotion checklist

- [ ] every Faz 1 atom above is checked off
- [ ] every sub-area has at least one merged PR
- [ ] `tools/validation/run-validation.sh --mode fallback` passes on the active branch
- [ ] sprint summary file recording final atom count + bundle count
- [ ] product-visible PR count for Faz 1 ≥ 1 (the playable-proof PR closes this)
- [ ] this PR (the first Faz 1 PR) does NOT count as test-only against rule 1's two-PR cap because it adds a new domain primitive (`ActorStore`); the next two PRs may be test-only before rule 1 forces a visible increment
- [ ] This PR counts as product-visible foundation because the legacy slice actor accessors now read through `ActorStore`, exposing the LIVING store to existing gameplay code without adding new slice fields

## This atom map

- [ ] `DOCS/sprint-faz-1-atom-map.md` :: this file :: canonical Faz 1 decomposition required by sprint promotion hard rule

## Thalamus packet

- packet_id: `pkt_20260509204459_41ce3bbd63a2` (initial ActorStore PR)
- resolver_key: `sha256:7cc6df815b0d6d4aedfad98eaf53ca7629a69148c393b941bb3517229f8e707c`
- packet_id (SiteStore PR): `pkt_20260510225845_f00d001dd7e0`
- resolver_key (SiteStore PR): `sha256:330b38f56b1931e0946787867ba8f3800d6ccf1425ddcd2eb519cfe958b14e2b`
- packet_id (ItemRecord PR): `pkt_20260510231640_676878a28180`
- resolver_key (ItemRecord PR): `sha256:6024f95514fc0b2dc719ca79bc78baeecbd125766fc11ac895fc99ac92b30519`
- packet_id (ItemStore PR): `pkt_20260510234148_8fc90621f4a4`
- resolver_key (ItemStore PR): `sha256:5bf9c0606d5aa98ff18c8bb23bd5faff0e9f2bc81218695467adaaf004fc7b64`
- packet_id (FactionId PR): `pkt_20260510234830_913d417ed6e7`
- resolver_key (FactionId PR): `sha256:0db90954d88677a0dafaa3fc6aa0216dadbf5cfe8296260dffa40fea0d640940`
- packet_id (FactionRecord PR): `pkt_20260511001218_a215b9da9279`
- resolver_key (FactionRecord PR): `sha256:3a7e0d9766c3e410ec208832885407539df2b6f603b0ebff3b80f8a20601b3eb`
- packet_id (WorldEvent PR): `pkt_20260511061314_b24015092ce0`
- resolver_key (WorldEvent PR): `sha256:5d01905609fbe9d722d881387679daccfc861627ad1d18bdfd1590fdf2f395a8`
- packet_id (ReasonTrace PR): `pkt_20260511062642_d1b0146ad836`
- resolver_key (ReasonTrace PR): `sha256:5aaeab7ba3e5041ca669832ed854c75992c15df6f16d35c96b89d0ea28e30a2f`
- packet_id (ReasonTrace merge-into-main reconcile): `pkt_20260511064710_ba637c1502d2`
- resolver_key (ReasonTrace merge-into-main reconcile): `sha256:b4abd821178e14d46b98cbf1dc57ae4461a3b61f67837f5085796a5904c56e2e`
- packet_id (WorldEventLog PR): `pkt_20260511070129_dd3de05281dd`
- resolver_key (WorldEventLog PR): `sha256:8f215f88ffe4d580619c5ef284ca9d66a01b2286a79c642c2c5bd8bc7e4a2826`
- packet_id (WorldEvent ReasonTrace attachment PR): `pkt_20260511165159_ff7b1d23db09`
- resolver_key (WorldEvent ReasonTrace attachment PR): `sha256:78d30d0a1413c7305c41d3cc24d827b2fabd484ded4e023472adb5b0296b0355`
- packet_id (SliceWorldState store-backed actor views PR): `pkt_20260511180152_36cf9ee4aad3`
- resolver_key (SliceWorldState store-backed actor views PR): `sha256:527e964761013636e732d49f4d9979886c83eaee103289da50a3996366a9af6d`
- packet_id (SliceWorldState store roots PR): `pkt_20260511184559_c138f4fe260d`
- resolver_key (SliceWorldState store roots PR): `sha256:6c02d26981c9c2b5630150854c9f9ca669b2bc7e45a3131af67f56ff88c36a95`
- packet_id (Store save round-trip PR): `pkt_20260511190607_d724944dd3fa`
- resolver_key (Store save round-trip PR): `sha256:a8e560bbfe6f80911154a072cd4a71a10631da041ab333a1edf7a6317283ff77`

## Next increment after this PR

With `WorldEventLog` landed on `agent/sprint-faz-1-world-event-log`,
the WorldEvent-log sub-area now has all three primitive pieces:
`WorldEvent` (PR #89), `ReasonTrace` (PR #90), and the append-only
`WorldEventLog` chronicle.

The TIME-box save/load atom is now landed: canonical `Actors`,
`Items`, `Sites`, `Factions`, and `Events` store roots write through
`SliceSaveData`, load back through `SliceSaveMapper`, and prefer
canonical actor-store payloads over legacy named actor fields.

The remaining open Faz 1 atom is the PLAYABLE-box acceptance proof:
add `DOCS/sprint-faz-1-acceptance.md` with a deterministic replay log,
debug-HUD dump, screenshot, or playtest note showing guard spawn, talk,
memory, and second-site continuity across save/load.

The LIVING-box `SliceWorldState` store-backed actor-view migration is
now landed: legacy `Player`/`Talker`/... accessors are obsolete views
over `ActorStore`, and new code should target `Actors` directly.

Per the agent-rules-v2 playable-proof rule, the next sprint factory run
should close the `DOCS/sprint-faz-1-acceptance.md` atom instead of
adding another pure infrastructure slice.
