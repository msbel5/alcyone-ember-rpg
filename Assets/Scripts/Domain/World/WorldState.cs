// Design note:
// WorldState groups every deterministic slice system into one saveable pure object graph.
// Inputs: room, actors, inventories, pickups, door, guard, and narrative shell state.
// Outputs: a single runtime snapshot for tests, presentation wrappers, and JSON mapping.
// Bible reference: PRD Sprint 1 FR-03 through FR-07, Sprint 2 FR-02 through FR-05.
using System;
using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Domain.AiDm;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Magic;
using EmberCrpg.Domain.Memory;
using EmberCrpg.Domain.Narrative;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.Quest;
using EmberCrpg.Domain.Worldgen;

namespace EmberCrpg.Domain.World
{
    /// <summary>Pure aggregate state for the playable vertical slice.</summary>
    public sealed class WorldState : EmberCrpg.Domain.Core.IWorldNavigability
    {
        public GameTime Time;
        public int RoomSeed;
        public ProceduralRoom Room;
        public GeneratedDungeonLayout Dungeon;
        public int CurrentRoomId;
        public int PlayerRoomId;
        public int TalkerRoomId;
        public int MerchantRoomId;
        public int GuardRoomId;
        public int EnemyRoomId;
        public int PickupRoomId;
        public ActorStore Actors = new ActorStore();
        public ItemStore Items = new ItemStore();
        public SiteStore Sites = new SiteStore();
        public FactionStore Factions = new FactionStore();
        public WorldEventLog Events = new WorldEventLog();
        public QuestStore Quests = new QuestStore();
        // The generated open-world overland map (PRD_overland_map_v1). Deterministic from the world seed,
        // so it is regenerable on load — set during SeedWorld, reference-copied by CopyFrom.
        public EmberCrpg.Domain.Overland.OverlandMap Overland;
        public PriceLedger Prices = new PriceLedger();
        public List<StockpileComponent> Stockpiles = new List<StockpileComponent>();
        public List<TradeRouteDef> TradeRoutes = new List<TradeRouteDef>();
        public List<CaravanInstance> Caravans = new List<CaravanInstance>();
        public List<ToolCallTraceRecord> ToolCallTrace = new List<ToolCallTraceRecord>();
        public List<LlmProposalLogEntry> LlmProposalLog = new List<LlmProposalLogEntry>();
        public List<NpcSeedRecord> NpcSeeds = new List<NpcSeedRecord>();
        public WorldProfile WorldProfile;

        // SOUL-01: the production-economy stores now live on the world root so the
        // per-tick systems (PlantGrowthSystem / JobAssignmentSystem / PriceUpdateSystem)
        // tick canonical state instead of side-stores carried by the save bridge. Their
        // absence here was the SOUL-01/02 root cause: worksites/jobs/plants/soils existed
        // only in JsonSliceSaveService and were therefore never advanced.
        public ComponentStore<PlantComponent> Plants = new ComponentStore<PlantComponent>();
        public ComponentStore<SoilComponent> Soils = new ComponentStore<SoilComponent>();
        public JobBoard Jobs = new JobBoard();
        public WorksiteStore Worksites = new WorksiteStore();

        /// <summary>
        /// EMB-013: re-establish the non-null collection/store invariants after a deserialize or a
        /// reflection-based restore. A corrupt or partial save (or a future field the loader did not
        /// populate) can leave a store or list null; RestoreStateJson copies fields verbatim, so that
        /// null would reach the live world and the next tick would NullReference. Calling this right
        /// after a restore guarantees every store/list is at least an empty instance, so a bad save
        /// degrades gracefully instead of crashing the run.
        /// </summary>
        public void EnsureInvariants()
        {
            WorldContracts ??= new List<EmberCrpg.Domain.Quest.WorldQuestRecord>();
            WorldQuestStates ??= new Dictionary<ulong, EmberCrpg.Domain.Quest.QuestState>();
            MainQuest ??= new EmberCrpg.Domain.Quest.MainQuestState();
            MainQuest.EnsureInvariants();
            Actors ??= new ActorStore();
            Items ??= new ItemStore();
            Sites ??= new SiteStore();
            Factions ??= new FactionStore();
            Events ??= new WorldEventLog();
            Quests ??= new QuestStore();
            Prices ??= new PriceLedger();
            Stockpiles ??= new List<StockpileComponent>();
            TradeRoutes ??= new List<TradeRouteDef>();
            Caravans ??= new List<CaravanInstance>();
            ToolCallTrace ??= new List<ToolCallTraceRecord>();
            LlmProposalLog ??= new List<LlmProposalLogEntry>();
            NpcSeeds ??= new List<NpcSeedRecord>();
            Plants ??= new ComponentStore<PlantComponent>();
            Soils ??= new ComponentStore<SoilComponent>();
            Jobs ??= new JobBoard();
            Worksites ??= new WorksiteStore();
            // Save/raw-restore boundary: these are canonical mutable roots too. Empty instances
            // are safer than a null that crashes the first post-load interaction/tick.
            PlayerInventory ??= new InventoryState(10);
            PlayerEquipment ??= new EquipmentState();
            MerchantInventory ??= new InventoryState(32);
            PlayerKnownSpellIds ??= new List<string>();
            Pickups ??= new List<RoomPickup>();
            DungeonRoomStates ??= new List<DungeonRoomState>();
            DungeonDoorStates ??= new List<DungeonDoorState>();
            Topics ??= new List<AskAboutTopic>();
            NpcMemory ??= new NpcMemoryStore();
            CompanionIds ??= new List<ActorId>();
            GuardPursuits ??= new List<PursuitRecord>();
            HuntTargets ??= new List<HuntTargetRecord>();
            Reservations ??= new ReservationLedger();
            // Derived (site,tag)/actor indexes are never serialized; a restored ledger is blind without them.
            Reservations.RebuildIndexes();
            // B10 §A3: Blocked is derived too — the hydration hook repopulates it, but never let it be null.
            Blocked ??= new BlockedCellSet();
            WorkOrders ??= new WorkOrderLedger();
            // W34: the jobId index is derived, never serialized — rebuild or the resume path is blind.
            WorkOrders.RebuildIndexes();
            ActionLog ??= new EmberCrpg.Domain.Actors.Actions.ActionLogRing();
            Critters ??= new List<AmbientCritter>();
            Rumors ??= new List<RumorEntry>();
            SiteUnrest ??= new List<SiteUnrestRecord>();
            PlayerSpellCooldowns ??= new SpellCooldownState();
            PlayerShieldBuffs ??= new ShieldBuffState();
            HealOrphanPlants();
        }

        /// <summary>B10 §A5: allocation-free nav-view accessor threaded into MovementService.
        /// Returns the canonical open view when the derived blocker set is empty; otherwise this
        /// WorldState remains the authoritative blocker probe. Both views feed MovementService.</summary>
        public EmberCrpg.Domain.Core.IWorldNavigability NavView
            => Blocked == null || Blocked.Count == 0
                ? EmberCrpg.Domain.Core.MovementService.OpenNav
                : this;

        // B10 §A3: IWorldNavigability impl — CIVILIAN nav only sees Blocked cells. Room walls stay
        // the dungeon slice's business (RoomMovementService already consults ProceduralRoom.IsWalkable
        // there); folding Room into this view would silently force room-perimeter rules onto every
        // village actor whose home sits on the (0, y) or (x, 0) edge (Gate1/Gate8 froze the crowd).
        bool EmberCrpg.Domain.Core.IWorldNavigability.IsWalkable(EmberCrpg.Domain.Actors.GridPosition cell)
        {
            return Blocked == null || !Blocked.Contains(cell);
        }

        // Corner-cut rule: refuse the diagonal iff BOTH orthogonal neighbours between `from` and
        // `to` are blocked. Standard "no squeezing through a wall crack". Cheap: two Blocked probes.
        bool EmberCrpg.Domain.Core.IWorldNavigability.BlocksDiagonal(EmberCrpg.Domain.Actors.GridPosition from, EmberCrpg.Domain.Actors.GridPosition to)
        {
            if (Blocked == null) return false;
            var xNeighbour = new EmberCrpg.Domain.Actors.GridPosition(to.X, from.Y);
            var yNeighbour = new EmberCrpg.Domain.Actors.GridPosition(from.X, to.Y);
            return Blocked.Contains(xNeighbour) && Blocked.Contains(yNeighbour);
        }

        // W33-01 §9.4: a plant no soil links to (pre-W33 factories/saves) could never be
        // harvested once the fiat harvest step retired — it would wait ripe FOREVER. Synthesize
        // its soil deterministically (Plants.Rows order; id = OrphanSoilBase + plantId), the
        // same normalize-on-load family as RebuildIndexes. Idempotent: a healed plant is linked.
        private void HealOrphanPlants()
        {
            const ulong OrphanSoilBase = 600_000UL;
            var linked = new HashSet<ulong>();
            foreach (var soilRow in Soils.Rows)
                if (soilRow.Value != null && !soilRow.Value.PlantId.IsEmpty)
                    linked.Add(soilRow.Value.PlantId.Value);
            List<PlantComponent> orphans = null;
            foreach (var plantRow in Plants.Rows)
                if (plantRow.Value != null && !linked.Contains(plantRow.Value.Id.Value))
                    (orphans ??= new List<PlantComponent>()).Add(plantRow.Value);
            if (orphans == null)
                return;
            foreach (var plant in orphans)
            {
                var soilId = new WorldComponentId(OrphanSoilBase + plant.Id.Value);
                if (Soils.Contains(soilId))
                    continue; // id already taken by an unrelated soil — leave the orphan alone
                Soils.Add(soilId, new SoilComponent(
                    soilId, plant.SiteId, plant.Position, fertility: 50, moisture: 50, plantId: plant.Id));
            }
        }

        // W-refactor 2026-07-26 DEAD-6: the five slice-era named role shims
        // (Player/Talker/Merchant/Guard/Enemy) retired — canonical accessor is
        // Actors.FirstByRole(role). See ReplaceActorView(role, record) for writes.
        public InventoryState PlayerInventory;
        public EquipmentState PlayerEquipment = new EquipmentState();
        public InventoryState MerchantInventory;
        public int PlayerLevel = 1;
        // F17: kill/quest experience; gates the level-up screen (PlayerLevelUpService.XpForNextLevel).
        public int PlayerXp;
        /// <summary>Chosen class display name; empty until character creation applies one.</summary>
        public string PlayerClassName = string.Empty;
        // F22: world quests PERSIST — generated contracts (F21) + the fixed bounty/pilgrimage pair's
        // runtime states live on the world root (the adapter-local stores died here). Keyed by raw
        // QuestId.Value because the kernel QuestStore stays catalog-only (the F2 lesson).
        public List<EmberCrpg.Domain.Quest.WorldQuestRecord> WorldContracts = new List<EmberCrpg.Domain.Quest.WorldQuestRecord>();
        public Dictionary<ulong, EmberCrpg.Domain.Quest.QuestState> WorldQuestStates = new Dictionary<ulong, EmberCrpg.Domain.Quest.QuestState>();
        // F31: the three-act MAIN QUEST spine (inscriptions → sage → final Warden) — world-root
        // state like the contracts, save-mapped, configured once at seed.
        public EmberCrpg.Domain.Quest.MainQuestState MainQuest = new EmberCrpg.Domain.Quest.MainQuestState();
        // F23: reputation (+1 per finished contract, −2 per crime; ≥5 buys a 10% market discount)
        // and the watch's BOUNTY on the player's head (>0 = guards hunt on sight).
        public int PlayerReputation;
        public int PlayerBountyGold;
        // F27: the communal LUNCH SPOT (the tavern) — realize-derived, deliberately NOT saved
        // (each realize republishes it); civilians route here over the midday window.
        public EmberCrpg.Domain.Actors.GridPosition? TavernCell;
        public List<string> PlayerKnownSpellIds = new List<string>();
        public List<RoomPickup> Pickups = new List<RoomPickup>();
        public List<DungeonRoomState> DungeonRoomStates = new List<DungeonRoomState>();
        public List<DungeonDoorState> DungeonDoorStates = new List<DungeonDoorState>();
        public List<AskAboutTopic> Topics = new List<AskAboutTopic>();
        public NpcMemoryStore NpcMemory = new NpcMemoryStore();

        // V3 YOLDAŞ: recruited companion actor ids. Membership only — the actors themselves
        // stay in Actors with their roles, sprites, and memories intact.
        // Typed ActorId (not raw ulong): callers stop hand-rolling `new ActorId(raw)` at every read.
        public List<EmberCrpg.Domain.Core.ActorId> CompanionIds = new List<EmberCrpg.Domain.Core.ActorId>();
        /// <summary>P0 pursuit: active guard chases (guard -> quarry, with an expiry).</summary>
        public List<PursuitRecord> GuardPursuits = new List<PursuitRecord>();
        /// <summary>W36 GUARD+COMBAT: active enemy hunts (hunter -> prey, with an expiry).
        /// Pursuit's mirror on the enemy side — the Decide phase arms, Advance reads.</summary>
        public List<HuntTargetRecord> HuntTargets = new List<HuntTargetRecord>();
        /// <summary>W32 EAT: count-based stockpile reservations — the "last bread" is claimed once.</summary>
        public ReservationLedger Reservations = new ReservationLedger();
        /// <summary>B10 §A3: sim-blocked cells (buildings projected from the presentation-side
        /// SettlementLayout). DERIVED — never serialized; rebuilt on load via HydrateBlockedCells
        /// on the same seam that runs EnsureInvariants (same pattern as Reservations.RebuildIndexes).</summary>
        public BlockedCellSet Blocked = new BlockedCellSet();
        /// <summary>W34 WORK: in-flight recipe work orders on the world root (docs/ruh/w34/02 §5.2) —
        /// the row outlives the action chain and the claimant, which IS the pause semantics.</summary>
        public WorkOrderLedger WorkOrders = new WorkOrderLedger();
        /// <summary>W32 EAT: bounded deterministic action phase trace (terminal outcomes go to Events).</summary>
        public EmberCrpg.Domain.Actors.Actions.ActionLogRing ActionLog = new EmberCrpg.Domain.Actors.Actions.ActionLogRing();
        /// <summary>P1 ambient life: rats and cats - cheap agents with real stock effects.</summary>
        public List<AmbientCritter> Critters = new List<AmbientCritter>();
        /// <summary>P1 RumorMill: town talk distilled from real events (cap 32, 3-day life).</summary>
        public List<RumorEntry> Rumors = new List<RumorEntry>();
        /// <summary>RumorMill's event cursor - persists so loads never re-mill old news.</summary>
        // B21: seq-based cursor (long) so trimming the event log does not re-mill or skip news.
        // Rename from RumorEventCursor (int, absolute index) — WorldStateCopyFromTests guards the copy.
        public long RumorEventCursorSeq;
        /// <summary>P2: per-settlement crime pressure - the sweep threshold lives on this.</summary>
        public List<SiteUnrestRecord> SiteUnrest = new List<SiteUnrestRecord>();
        public SpellCooldownState PlayerSpellCooldowns = new SpellCooldownState();
        public ShieldBuffState PlayerShieldBuffs = new ShieldBuffState();
        public bool DoorOpen;
        public bool GuardDoorAccessGranted;
        public int GuardWarningCount;
        public bool EncounterActive;
        public string LastNarrative;
        public int PlayerGold;
        public int MerchantGold;
        public bool MerchantStoreSeeded;

        /// <summary>
        /// ARCH-12: explicit, reflection-free state replace used by save/load restore. Mirrors every
        /// public field from <paramref name="other"/> onto this instance; callers run
        /// <see cref="EnsureInvariants"/> afterwards. Replaces a reflection field-walk that silently
        /// followed field type/visibility changes in the determinism-critical load path. A field added
        /// to this type MUST be added here too — WorldStateCopyFromTests guards that via reflection.
        /// </summary>
        public void CopyFrom(WorldState other)
        {
            if (other == null) return;
            Time = other.Time;
            RoomSeed = other.RoomSeed;
            Room = other.Room;
            Dungeon = other.Dungeon;
            CurrentRoomId = other.CurrentRoomId;
            PlayerRoomId = other.PlayerRoomId;
            TalkerRoomId = other.TalkerRoomId;
            MerchantRoomId = other.MerchantRoomId;
            GuardRoomId = other.GuardRoomId;
            EnemyRoomId = other.EnemyRoomId;
            PickupRoomId = other.PickupRoomId;
            Actors = other.Actors;
            Items = other.Items;
            Sites = other.Sites;
            Factions = other.Factions;
            Events = other.Events;
            Quests = other.Quests;
            Overland = other.Overland;
            Prices = other.Prices;
            Stockpiles = other.Stockpiles;
            TradeRoutes = other.TradeRoutes;
            Caravans = other.Caravans;
            ToolCallTrace = other.ToolCallTrace;
            LlmProposalLog = other.LlmProposalLog;
            NpcSeeds = other.NpcSeeds;
            WorldProfile = other.WorldProfile;
            Plants = other.Plants;
            Soils = other.Soils;
            Jobs = other.Jobs;
            Worksites = other.Worksites;
            PlayerInventory = other.PlayerInventory;
            PlayerEquipment = other.PlayerEquipment;
            MerchantInventory = other.MerchantInventory;
            PlayerLevel = other.PlayerLevel;
            PlayerXp = other.PlayerXp;
            PlayerClassName = other.PlayerClassName;
            WorldContracts = other.WorldContracts;
            WorldQuestStates = other.WorldQuestStates;
            MainQuest = other.MainQuest;
            PlayerReputation = other.PlayerReputation;
            PlayerBountyGold = other.PlayerBountyGold;
            TavernCell = other.TavernCell;
            PlayerKnownSpellIds = other.PlayerKnownSpellIds;
            Pickups = other.Pickups;
            DungeonRoomStates = other.DungeonRoomStates;
            DungeonDoorStates = other.DungeonDoorStates;
            Topics = other.Topics;
            NpcMemory = other.NpcMemory;
            CompanionIds = other.CompanionIds;
            GuardPursuits = other.GuardPursuits;
            HuntTargets = other.HuntTargets;
            Reservations = other.Reservations;
            Blocked = other.Blocked; // B10 §A3: derived, but WorldStateCopyFromTests's reflection lint requires every field.
            WorkOrders = other.WorkOrders;
            ActionLog = other.ActionLog;
            Critters = other.Critters;
            Rumors = other.Rumors;
            RumorEventCursorSeq = other.RumorEventCursorSeq;
            SiteUnrest = other.SiteUnrest;
            PlayerSpellCooldowns = other.PlayerSpellCooldowns;
            PlayerShieldBuffs = other.PlayerShieldBuffs;
            DoorOpen = other.DoorOpen;
            GuardDoorAccessGranted = other.GuardDoorAccessGranted;
            GuardWarningCount = other.GuardWarningCount;
            EncounterActive = other.EncounterActive;
            LastNarrative = other.LastNarrative;
            PlayerGold = other.PlayerGold;
            MerchantGold = other.MerchantGold;
            MerchantStoreSeeded = other.MerchantStoreSeeded;
        }

        /// <summary>
        /// Non-obsolete role-keyed write site for callers that previously assigned to
        /// the deprecated <c>Player</c>/<c>Talker</c>/<c>Merchant</c>/<c>Guard</c>/<c>Enemy</c>
        /// properties. New code should prefer <see cref="ActorStore.Add"/> /
        /// <see cref="ActorStore.Remove"/> on <see cref="Actors"/> directly; this helper
        /// exists to keep the slice-era assignment sites readable during the Phase 1 sweep.
        /// </summary>
        public void ReplaceActorView(ActorRole role, ActorRecord record)
        {
            SetActorView(role, record);
        }

        private void SetActorView(ActorRole expectedRole, ActorRecord record)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));
            if (record.Role != expectedRole)
                throw new ArgumentException($"Expected actor role {expectedRole}, got {record.Role}.", nameof(record));

            EnsureActorStore();

            var actorIdsToRemove = new List<ActorId>();
            foreach (var actor in Actors.Records)
            {
                if (actor.Role == expectedRole || actor.Id.Equals(record.Id))
                    actorIdsToRemove.Add(actor.Id);
            }

            foreach (var actorId in actorIdsToRemove)
                Actors.Remove(actorId);

            Actors.Add(record);
        }

        private void EnsureActorStore()
        {
            if (Actors == null)
                Actors = new ActorStore();
        }

        public StockpileComponent FindStockpile(SiteId siteId)
        {
            return Stockpiles?.FirstOrDefault(stockpile => stockpile != null && stockpile.SiteId.Equals(siteId));
        }

        public TradeRouteDef FindTradeRoute(TradeRouteId routeId)
        {
            return TradeRoutes?.FirstOrDefault(route => route != null && route.Id.Equals(routeId));
        }
    }
}
