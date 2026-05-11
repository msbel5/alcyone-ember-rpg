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
- [ ] `Assets/Scripts/Domain/World/SliceWorldState.cs` :: migrate consumers :: replace direct `Player`/`Talker`/`Merchant`/`Guard`/`Enemy` reads with store + view shim, mark fields `[Obsolete]` (follow-up PR) [box=LIVING]

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

- [x] `Assets/Scripts/Domain/World/WorldEvent.cs` :: `WorldEvent` :: typed event payload (`tick`, `kind`, `actorId`, `siteId`, `reason`) [box=PROCESS] — landed via `agent/sprint-faz-1-world-event`; pinned by `Assets/Tests/EditMode/World/WorldEventTests.cs`; ships alongside `Assets/Scripts/Domain/World/WorldEventKind.cs` seed enum (None / ActorSpawned / ActorTalked / SiteEntered) covering the Faz 1 acceptance-gate event categories
- [ ] `Assets/Scripts/Domain/World/ReasonTrace.cs` :: `ReasonTrace` :: causal-chain record attached to an event [box=PROCESS]
- [ ] `Assets/Scripts/Domain/World/WorldEventLog.cs` :: `WorldEventLog` :: append-only log over `WorldEvent` with deterministic enumeration [box=PROCESS]
- [ ] `Assets/Tests/EditMode/World/WorldEventLogTests.cs` :: tests :: pin append + deterministic enumeration + reason-trace round-trip [box=PROCESS]

## Sub-area: Save/load round-trip (TIME — primary)

- [ ] `Assets/Scripts/Data/Save/SliceSaveMapper.cs` :: extend mapper :: serialize `ActorStore` / `ItemStore` / `SiteStore` / `FactionStore` / `WorldEventLog` alongside `SliceWorldState` (migration-friendly: write both, read both, prefer stores) [box=TIME]
- [ ] `Assets/Tests/EditMode/Save/StoreRoundTripTests.cs` :: tests :: pin deterministic round-trip for the four stores + event log [box=TIME]

## Sub-area: Acceptance proof (PLAYABLE — every fifth PR)

- [ ] `DOCS/sprint-faz-1-acceptance.md` :: `player can ...` :: scene screenshot OR deterministic replay log OR debug-HUD dump showing a guard spawned + talked to + remembered after save/load + walking to a second site (rule 5: playable proof) [box=PLAYABLE]

## Promotion checklist

- [ ] every Faz 1 atom above is checked off
- [ ] every sub-area has at least one merged PR
- [ ] `tools/validation/run-validation.sh --mode fallback` passes on the active branch
- [ ] sprint summary file recording final atom count + bundle count
- [ ] product-visible PR count for Faz 1 ≥ 1 (the playable-proof PR closes this)
- [ ] this PR (the first Faz 1 PR) does NOT count as test-only against rule 1's two-PR cap because it adds a new domain primitive (`ActorStore`); the next two PRs may be test-only before rule 1 forces a visible increment

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

## Next increment after this PR

With `WorldEvent` (the PROCESS-box typed event payload) landed
alongside its seed `WorldEventKind` enum and pinned tests, the
WorldEvent log sub-area now has its primitive in place. The next Faz 1
atom is `ReasonTrace` (`Assets/Scripts/Domain/World/ReasonTrace.cs`)
— a pure causal-chain record that can be attached to a `WorldEvent`.
After `ReasonTrace`, `WorldEventLog` (`Assets/Scripts/Domain/World/
WorldEventLog.cs`) lands the append-only log over `WorldEvent` with
deterministic enumeration; tests follow in
`Assets/Tests/EditMode/World/WorldEventLogTests.cs`.
