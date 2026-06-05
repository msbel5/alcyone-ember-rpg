using System;

// Design note:
// WorldSaveData is the Unity-serializable DTO tree for slice JSON persistence.
// Inputs: pure world-state values copied by the mapper.
// Outputs: JsonUtility-friendly fields with no behavior.
// Bible reference: MASTER_MECHANICS_BIBLE.md §48, PRD Sprint 1 FR-06, Sprint 2 FR-02 through FR-04.
//
// Codex audit (sixth pass D-P3 #D3): the playerRoomId/talkerRoomId/merchantRoomId/
// guardRoomId/enemyRoomId fields below mirror the deprecated WorldState
// named role views. They are kept for backward-compatible save migration —
// WorldSaveMapper writes BOTH the legacy named ids AND the new actors[]
// list so a player on an old save can load. New code that adds save data
// should write into actors[] / world stores only; do not expand the legacy
// named-id surface. Removal lines up with the WorldState role-view
// removal after the Phase 13 cleanup sprint.
namespace EmberCrpg.Data.Save
{
    [Serializable]
    public sealed partial class WorldSaveData
    {
        // EMB-012: explicit save schema version. Bump WorldSaveMapper.CurrentSchemaVersion when the
        // on-disk shape changes incompatibly and add a migration branch in ToWorld. Legacy saves
        // written before this field existed deserialize it to 0, which ToWorld treats as the v1 baseline.
        public int schemaVersion;
        public long totalMinutes;
        public long roomSeed;
        public long currentRoomId;
        public long dungeonStartRoomId;
        public long playerRoomId;
        public long talkerRoomId;
        public long merchantRoomId;
        public long guardRoomId;
        public long enemyRoomId;
        public long pickupRoomId;
public DungeonRoomSaveData[] dungeonRooms;
        public DungeonDoorSaveData[] dungeonDoors;
        public DungeonSpawnSaveData[] dungeonSpawns;
        public DungeonRoomStateSaveData[] dungeonRoomStates;
        public DungeonDoorStateSaveData[] dungeonDoorStates;
        public ActorSaveData player;
        public ActorSaveData talker;
        public ActorSaveData merchant;
        public ActorSaveData guard;
        public ActorSaveData enemy;
        public ActorSaveData[] actors;
        public ItemRecordSaveData[] itemRecords;
        public SiteRecordSaveData[] sites;
        public FactionRecordSaveData[] factions;
        public FactionReputationSaveData[] factionReputations;
        public PriceLedgerSaveData[] prices;
        public StockpileSaveData[] stockpiles;
        public TradeRouteSaveData[] tradeRoutes;
        public CaravanSaveData[] caravans;
        public WorldEventSaveData[] worldEvents;
        public ToolCallTraceSaveData[] toolCallTrace;
        public LlmProposalLogSaveData[] llmProposalLog;
        public NpcSeedSaveData[] npcSeeds;
        public WorldProfileSaveData worldProfile;
        public WorksiteSaveData[] worksites;
        public RecipeWorkOrderSaveData[] recipeWorkOrders;
        public JobRequestSaveData[] jobs;
        public SoilComponentSaveData[] soils;
        public PlantComponentSaveData[] plants;
        public InventorySaveData inventory;
        public EquipmentSaveData playerEquipment;
        public InventorySaveData merchantInventory;
        public int playerGold;
        public int merchantGold;
        public bool merchantStoreSeeded;
        public PickupSaveData[] pickups;
        public TopicSaveData[] topics;
        public NpcMemorySaveData[] npcMemories;
        public SpellCooldownSaveData playerSpellCooldowns;
        public ShieldBuffSaveData playerShieldBuffs;
        public bool doorOpen;
        public bool guardDoorAccessGranted;
        public int guardWarningCount;
        public bool encounterActive;
        public string lastNarrative;
    }

    [Serializable]
    public sealed class NpcSeedSaveData
    {
        public long id;
        public long home;
        public long faction;
        public string name;
        public int birthYear;
        public int role;
        public string portraitAssetPath;
    }

    [Serializable]
    public sealed class WorldProfileSaveData
    {
        public int style;
        public int genre;
        public long seed;
        public int targetPopulation;
public int regionCount;
        public int factionCount;
        public int historyYears;
        public string moodKeyword;
        public string playerCallingKeyword;
        public string startLocationKeyword;
    }
}
