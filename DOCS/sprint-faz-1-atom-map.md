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

- [ ] `Assets/Scripts/Domain/World/ActorStore.cs` :: `ActorStore` :: dictionary-backed registry over `ActorId -> ActorRecord` with Add/Get/TryGet/Remove/Contains/Count/Clear/Records, deterministic enumeration, default-id rejection [box=LIVING]
- [ ] `Assets/Tests/EditMode/World/ActorStoreTests.cs` :: tests :: pin Add/Get/TryGet/Remove/Contains/Count/Clear/Records contracts and default-id rejection [box=LIVING]
- [x] `Assets/Scripts/Domain/World/ActorStore.cs` :: deprecated-view shims :: `Player`/`Talker`/`Merchant`/`Guard`/`Enemy` resolved from a `ActorRole`/role-tag lookup over the store (lands in a follow-up PR before SliceWorldState consumers migrate) [box=LIVING] — landed via `RecordsByRole`/`FirstByRole`/`TryFirstByRole` (sprint faz-1 role-shims PR)
- [ ] `Assets/Scripts/Domain/World/SliceWorldState.cs` :: migrate consumers :: replace direct `Player`/`Talker`/`Merchant`/`Guard`/`Enemy` reads with store + view shim, mark fields `[Obsolete]` (follow-up PR) [box=LIVING]

## Sub-area: ItemStore (MATTER — primary)

- [ ] `Assets/Scripts/Domain/Inventory/ItemId.cs` :: `ItemId` :: readonly value handle (default = empty) [box=MATTER]
- [ ] `Assets/Scripts/Domain/Inventory/ItemRecord.cs` :: `ItemRecord` :: pure record carrying material + quality + slot kind [box=MATTER]
- [ ] `Assets/Scripts/Domain/World/ItemStore.cs` :: `ItemStore` :: dictionary-backed registry over `ItemId -> ItemRecord` mirroring `ActorStore` shape [box=MATTER]
- [ ] `Assets/Tests/EditMode/World/ItemStoreTests.cs` :: tests :: pin store contracts and default-id rejection [box=MATTER]

## Sub-area: SiteStore (WORLD — primary)

- [ ] `Assets/Scripts/Domain/World/SiteId.cs` :: `SiteId` :: readonly value handle [box=WORLD]
- [ ] `Assets/Scripts/Domain/World/SiteRecord.cs` :: `SiteRecord` :: pure record for region / settlement / dungeon (kind + name + grid bounds) [box=WORLD]
- [ ] `Assets/Scripts/Domain/World/SiteStore.cs` :: `SiteStore` :: dictionary-backed registry over `SiteId -> SiteRecord` [box=WORLD]
- [ ] `Assets/Tests/EditMode/World/SiteStoreTests.cs` :: tests :: pin store contracts and default-id rejection [box=WORLD]

## Sub-area: FactionStore (SOCIETY-seed)

- [ ] `Assets/Scripts/Domain/World/FactionId.cs` :: `FactionId` :: readonly value handle [box=SOCIETY]
- [ ] `Assets/Scripts/Domain/World/FactionRecord.cs` :: `FactionRecord` :: pure record (name + tags); empty seed populated in Faz 6 [box=SOCIETY]
- [ ] `Assets/Scripts/Domain/World/FactionStore.cs` :: `FactionStore` :: dictionary-backed registry [box=SOCIETY]
- [ ] `Assets/Tests/EditMode/World/FactionStoreTests.cs` :: tests :: pin store contracts [box=SOCIETY]

## Sub-area: WorldEvent log + ReasonTrace (PROCESS — primary)

- [ ] `Assets/Scripts/Domain/World/WorldEvent.cs` :: `WorldEvent` :: typed event payload (`tick`, `kind`, `actorId`, `siteId`, `reason`) [box=PROCESS]
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

- packet_id: `pkt_20260509204459_41ce3bbd63a2`
- resolver_key: `sha256:7cc6df815b0d6d4aedfad98eaf53ca7629a69148c393b941bb3517229f8e707c`

## Next increment after this PR

Continue Faz 1 with the deprecated-view shims (`Player`/`Talker`/...
resolved from `ActorStore`) so the next PR can begin migrating
`SliceWorldState` consumers. Rule 4 (world-store promotion) bans new
hard-coded slice fields; this lays the rail.
