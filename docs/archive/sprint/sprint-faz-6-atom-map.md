# Faz 6 - Atom map (Trade + Faction)

_Date:_ 2026-05-20
_Branch:_ `mami/faz-6-12-atom-maps`
_Primary boxes:_ `SOCIETY`, `MATTER`
_Roadmap:_ `docs/ROADMAP.md`
_Mechanic map:_ `docs/mechanic-map-v1.md`
_Execution ledger:_ `docs/faz-5-12-execution-ledger.md`
_Agent rules:_ `docs/agent-rules-v2.md`
_Vision notes:_ `docs/EMBER_VISION_NOTES_MAMI.md`
_Inspector checklist:_ `docs/inspector-audit-checklist.md`

## Vision anchors

This sprint serves:

1. Living-world over showroom: factions, prices, and caravans tick even when the player is travelling between sites.
2. Deterministic-first, LLM-last: reputation deltas and trade routes resolve through typed data and tests; LLM stays out.
5. Data-driven extension: faction relations and trade goods land as rows, not enum branches.
6. Composition over inheritance: faction state is a component of an actor or a site, not an inherited base class.
8. Systemic interaction: faction reputation feeds NPC dialogue (Faz 9) and DM "Consult Fate" (Faz 10) WITHOUT this sprint implementing either.

## Phase fences

- No Memory state before Faz 9 (FactionRecord stays passive; no recall API).
- No shared NPC / party / DM tool surface before Faz 10.
- No LLM fallback wiring before Faz 12.
- No procedural genesis (faction seeds are fixture rows for now).
- No multiverse / 100K-year / interplanetary implementation.
- No free-text dialogue parsing before Faz 9.

## Debt ledger action

Faz 6 does not consume `CO-02` / `CO-03` / `CO-06` / `CO-07`. These carry forward, owned by their original PROCESS box. Each Faz 6 kickoff PR must still report one of `CO-XX-closed | CO-XX-advanced | CO-XX-deferred-to-faz-N | none-ledger-empty` in the PR audit fields.

## Sprint goal

Target acceptance sentence from `docs/ROADMAP.md`:

`player can pay a caravan to carry iron to a neighbouring site, watch the price respond, and feel reputation shift between factions`.

Faz 6 makes economy and society reactive: prices change with supply, caravans move stock between sites, transactions adjust reputation, and shortages emit replayable events.

## Atomic decomposition

| Atom | Primary box | File / class | Responsibility | Closing proof | Status |
|---:|---|---|---|---|---|
| 1 | SOCIETY | `Assets/Scripts/Domain/World/FactionRelationKind.cs` | Stable-string relation codes (`allied / friendly / neutral / hostile / war`). No enum branches; new codes ship as data. | `FactionRelationKindTests` | queued |
| 2 | SOCIETY | `Assets/Scripts/Domain/World/FactionReputation.cs` | Bounded `-100..+100` scalar with `Decay`, `Apply(delta)`, IEquatable. | `FactionReputationTests` | queued |
| 3 | SOCIETY | `Assets/Scripts/Domain/World/FactionStore.cs` :: extensions | `WithReputation(FactionId a, FactionId b, FactionReputation)` + `GetReputation` symmetric lookup. | `FactionStoreReputationTests` | queued |
| 4 | SOCIETY | `Assets/Scripts/Simulation/World/FactionReputationSystem.cs` | Apply deltas from trade / theft / aid events; emit `FactionReputationChanged` event. | `FactionReputationSystemTests` | queued |
| 5 | MATTER | `Assets/Scripts/Domain/Process/PriceLedger.cs` | Per-site, per-item current price scalar with deterministic supply/demand response. | `PriceLedgerTests` | queued |
| 6 | MATTER | `Assets/Scripts/Domain/Process/StockpileComponent.cs` | Site-scoped inventory count by item; `Add / Remove / Get(itemId)`. | `StockpileComponentTests` | queued |
| 7 | SOCIETY | `Assets/Scripts/Domain/World/TradeRouteDef.cs` | Data row: origin site, destination site, item id, quantity, cadence. | `TradeRouteDefTests` | queued |
| 8 | SOCIETY | `Assets/Scripts/Domain/World/CaravanInstance.cs`, `CaravanState` | Runtime caravan with current site, route progress, payload. | `CaravanInstanceTests` | queued |
| 9 | SOCIETY | `Assets/Scripts/Simulation/World/CaravanSystem.cs` | Tick caravans along their route, transfer payload between stockpiles, emit `CaravanArrived`. | `CaravanSystemTests` | queued |
| 10 | MATTER | `Assets/Scripts/Simulation/World/PriceUpdateSystem.cs` | Recompute price from stockpile counts on transfer / shortage; emit `PriceChanged`. | `PriceUpdateSystemTests` | queued |
| 11 | SOCIETY | `Assets/Scripts/Domain/Process/TradeTransaction.cs` + `TradeService` | Trade verb: pay X currency, receive Y item, adjust both stockpiles, emit reputation delta. | `TradeServiceTests` | queued |
| 12 | TIME | `Assets/Scripts/Data/Save` faction/price/caravan save mappers | Round-trip faction, price, caravan, and stockpile state without `SliceWorldState` named fields. | `FactionTradeRoundTripTests` | queued |
| 13 | SOCIETY | `Assets/Scripts/Simulation/World/ShortageDetector.cs` | When stockpile drops below threshold, emit `ShortageDetected` event referencing site + item. | `ShortageDetectorTests` | queued |
| 14 | SOCIETY | `Assets/Tests/EditMode/World/FazSixTradeAcceptanceTests.cs` | Deterministic replay: caravan moves iron, price rises at destination, reputation shifts +5, shortage event at origin. | acceptance replay note | queued |

## Suggested bundles

1. `faction-primitives` — Atoms 1, 2, 3 (relation kind, reputation, store extension).
2. `faction-reputation-system` — Atom 4 + reputation-event tests.
3. `price-stockpile` — Atoms 5, 6, 10.
4. `caravan-runtime` — Atoms 7, 8, 9.
5. `trade-service` — Atom 11.
6. `trade-faction-save-and-acceptance` — Atoms 12, 13, 14.

## Promotion checklist

- [ ] Every Faz 6 atom row above is checked off.
- [ ] Each sub-area has at least one merged PR.
- [ ] `./tools/validation/run-validation.sh --mode fallback` passes on the promotion branch.
- [ ] Product-visible PR count is greater than zero.
- [ ] Faz 6 promotion summary reports Debt ledger status — open rows migrated forward.

## Next increment

Implement the `faction-primitives` bundle first: Atoms 1, 2, 3 with their tests. Keep relation codes data-driven, reputation purely numeric, store extension symmetric.
