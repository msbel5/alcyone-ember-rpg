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
    public sealed class WorldState
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
        }

        // Codex audit (sixth pass D-P3 #D2): the five named role views below
        // (Player/Talker/Merchant/Guard/Enemy) are deprecated since Phase 1 but
        // 71 call sites across Simulation/Presentation/Data still read or
        // write them. Removal is scheduled for after the Phase 13 cleanup
        // sprint — until then, the [Obsolete] attribute fires warning-only
        // (error: false) so existing callers compile while new code is
        // guided to ActorStore.FirstByRole(...).
        [Obsolete("Slice-era role shim, scheduled for removal after Phase 13. Use Actors.FirstByRole(ActorRole.Player) or ActorStore role-view helpers.", false)]
        public ActorRecord Player
        {
            get { return GetActorView(ActorRole.Player); }
            set { SetActorView(ActorRole.Player, value); }
        }

        [Obsolete("Slice-era role shim, scheduled for removal after Phase 13. Use Actors.FirstByRole(ActorRole.Talker) or ActorStore role-view helpers.", false)]
        public ActorRecord Talker
        {
            get { return GetActorView(ActorRole.Talker); }
            set { SetActorView(ActorRole.Talker, value); }
        }

        [Obsolete("Slice-era role shim, scheduled for removal after Phase 13. Use Actors.FirstByRole(ActorRole.Merchant) or ActorStore role-view helpers.", false)]
        public ActorRecord Merchant
        {
            get { return GetActorView(ActorRole.Merchant); }
            set { SetActorView(ActorRole.Merchant, value); }
        }

        [Obsolete("Slice-era role shim, scheduled for removal after Phase 13. Use Actors.FirstByRole(ActorRole.Guard) or ActorStore role-view helpers.", false)]
        public ActorRecord Guard
        {
            get { return GetActorView(ActorRole.Guard); }
            set { SetActorView(ActorRole.Guard, value); }
        }

        [Obsolete("Slice-era role shim, scheduled for removal after Phase 13. Use Actors.FirstByRole(ActorRole.Enemy) or ActorStore role-view helpers.", false)]
        public ActorRecord Enemy
        {
            get { return GetActorView(ActorRole.Enemy); }
            set { SetActorView(ActorRole.Enemy, value); }
        }
        public InventoryState PlayerInventory;
        public EquipmentState PlayerEquipment = new EquipmentState();
        public InventoryState MerchantInventory;
        public List<RoomPickup> Pickups = new List<RoomPickup>();
        public List<DungeonRoomState> DungeonRoomStates = new List<DungeonRoomState>();
        public List<DungeonDoorState> DungeonDoorStates = new List<DungeonDoorState>();
        public List<AskAboutTopic> Topics = new List<AskAboutTopic>();
        public NpcMemoryStore NpcMemory = new NpcMemoryStore();
        public SpellCooldownState PlayerSpellCooldowns = new SpellCooldownState();
        public ShieldBuffState PlayerShieldBuffs = new ShieldBuffState();
        public bool DoorOpen;
        public bool GuardDoorAccessGranted;
        public int GuardWarningCount;
        public bool EncounterActive;
        public string LastNarrative;

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
            Pickups = other.Pickups;
            DungeonRoomStates = other.DungeonRoomStates;
            DungeonDoorStates = other.DungeonDoorStates;
            Topics = other.Topics;
            NpcMemory = other.NpcMemory;
            PlayerSpellCooldowns = other.PlayerSpellCooldowns;
            PlayerShieldBuffs = other.PlayerShieldBuffs;
            DoorOpen = other.DoorOpen;
            GuardDoorAccessGranted = other.GuardDoorAccessGranted;
            GuardWarningCount = other.GuardWarningCount;
            EncounterActive = other.EncounterActive;
            LastNarrative = other.LastNarrative;
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

        private ActorRecord GetActorView(ActorRole role)
        {
            EnsureActorStore();
            return Actors.FirstByRole(role);
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
