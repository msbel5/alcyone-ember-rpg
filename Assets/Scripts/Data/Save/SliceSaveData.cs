using System;

// Design note:
// SliceSaveData is the Unity-serializable DTO tree for slice JSON persistence.
// Inputs: pure world-state values copied by the mapper.
// Outputs: JsonUtility-friendly fields with no behavior.
// Bible reference: MASTER_MECHANICS_BIBLE.md §48, PRD Sprint 1 FR-06, Sprint 2 FR-02 through FR-04.
//
// Codex audit (sixth pass D-P3 #D3): the playerRoomId/talkerRoomId/merchantRoomId/
// guardRoomId/enemyRoomId fields below mirror the deprecated SliceWorldState
// named role views. They are kept for backward-compatible save migration —
// SliceSaveMapper writes BOTH the legacy named ids AND the new actors[]
// list so a player on an old save can load. New code that adds save data
// should write into actors[] / world stores only; do not expand the legacy
// named-id surface. Removal lines up with the SliceWorldState role-view
// removal after the Faz 13 cleanup sprint.
namespace EmberCrpg.Data.Save
{
    [Serializable]
    public sealed class SliceSaveData
    {
        // EMB-012: explicit save schema version. Bump SliceSaveMapper.CurrentSchemaVersion when the
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

    [Serializable]
    public sealed class ItemRecordSaveData
    {
        public long id;
        public int material;
        public int quality;
        public int slot;
        public string slotCode;
    }

    [Serializable]
    public sealed class SiteRecordSaveData
    {
        public long id;
        public int kind;
        public string name;
        public int minX;
        public int minY;
        public int maxX;
        public int maxY;
    }

    [Serializable]
    public sealed class WorksiteSaveData
    {
        public long siteId;
        public int positionX;
        public int positionY;
        public int kind;
        public bool isActive;
    }

    [Serializable]
    public sealed class RecipeWorkOrderSaveData
    {
        public long recipeId;
        public long siteId;
        public int positionX;
        public int positionY;
        public long actorId;
        public int progressTicks;
    }

    [Serializable]
    public sealed class SoilComponentSaveData
    {
        public long id;
        public long siteId;
        public int positionX;
        public int positionY;
        public int fertility;
        public int moisture;
        public long plantId;
    }

    [Serializable]
    public sealed class PlantComponentSaveData
    {
        public long id;
        public long siteId;
        public int positionX;
        public int positionY;
        public string speciesId;
        public string stageId;
        public int daysInStage;
    }

    [Serializable]
    public sealed class FactionRecordSaveData
    {
        public long id;
        public string name;
        public string[] tags;
    }

    [Serializable]
    public sealed class FactionReputationSaveData
    {
        public long a;
        public long b;
        public int reputation;
    }

    [Serializable]
    public sealed class PriceLedgerSaveData
    {
        public long siteId;
        public string itemTag;
        public int price;
    }

    [Serializable]
    public sealed class StockpileSaveData
    {
        public long siteId;
        public StockpileEntrySaveData[] entries;
    }

    [Serializable]
    public sealed class StockpileEntrySaveData
    {
        public string itemTag;
        public int count;
    }

    [Serializable]
    public sealed class TradeRouteSaveData
    {
        public long id;
        public long originSiteId;
        public long destinationSiteId;
        public string itemTag;
        public int quantityPerCaravan;
        public int cadenceDays;
    }

    [Serializable]
    public sealed class CaravanSaveData
    {
        public long id;
        public long routeId;
        public long currentSiteId;
        public int payloadRemaining;
        public int stepsSinceDeparture;
        public string stateCode;
    }

    [Serializable]
    public sealed class WorldEventSaveData
    {
        public long tickMinutes;
        public int kind;
        public long actorId;
        public long siteId;
        public string reason;
        public string[] reasonTrace;
    }

    [Serializable]
    public sealed class ToolCallTraceSaveData
    {
        public long tickMinutes;
        public long siteId;
        public string surfaceCode;
        public string toolCode;
        public ToolCallParameterSaveData[] parameters;
        public bool accepted;
        public string payload;
        public string rejectionReason;
    }

    [Serializable]
    public sealed class ToolCallParameterSaveData
    {
        public string name;
        public string value;
    }

    [Serializable]
    public sealed class LlmProposalLogSaveData
    {
        public long tickMinutes;
        public string providerCode;
        public string conversationId;
        public string responseText;
        public ToolCallTraceSaveData[] acceptedToolCalls;
        public LlmRejectedToolCallSaveData[] rejectedToolCalls;
    }

    [Serializable]
    public sealed class LlmRejectedToolCallSaveData
    {
        public ToolCallTraceSaveData request;
        public string reason;
    }

    [Serializable]
    public sealed class EquipmentSaveData
    {
        public EquippedItemSaveData[] slots;
    }

    [Serializable]
    public sealed class EquippedItemSaveData
    {
        public int slot;
        public string slotCode;
        public long itemId;
    }

    [Serializable]
    public sealed class DungeonRoomSaveData
    {
        public int id;
        public int gridX;
        public int gridY;
        public int width;
        public int height;
        public string templateId;
        public int[] doorIds;
    }

    [Serializable]
    public sealed class DungeonDoorSaveData
    {
        public int id;
        public int fromRoomId;
        public int toRoomId;
        public int fromX;
        public int fromY;
        public int toX;
        public int toY;
        public bool startsOpen;
        public bool requiresGuardClearance;
    }

    [Serializable]
    public sealed class DungeonSpawnSaveData
    {
        public int roomId;
        public int kind;
        public int positionX;
        public int positionY;
    }

    [Serializable]
    public sealed class DungeonRoomStateSaveData
    {
        public int roomId;
        public bool visited;
        public bool cleared;
    }

    [Serializable]
    public sealed class DungeonDoorStateSaveData
    {
        public int doorId;
        public bool open;
    }

    [Serializable]
    public sealed class ActorSaveData
    {
        public long id;
        public string name;
        public int role;
        public int positionX;
        public int positionY;
        public int mig;
        public int agi;
        public int end;
        public int mnd;
        public int ins;
        public int pre;
        public int healthCurrent;
        public int healthMax;
        public int fatigueCurrent;
        public int fatigueMax;
        public int manaCurrent;
        public int manaMax;
        public int accuracy;
        public int dodge;
        public int armor;
        public int baseDamage;
        public string[] topicIds;
        public string[] askedTopicIds;
        public ActorJobPreferenceSaveData[] jobPreferences;
        public MemoryFactSaveData[] memoryFacts;
        // Persisted schedule targets (0/empty means idle)
        public long currentJobId;
        public long targetSiteId;
        public int targetWorksitePositionX;
        public int targetWorksitePositionY;
        // Persisted needs and mood (integers 0-100)
        public int hunger;
        public int fatigue;
        public int thirst;
        public int mood;

        // Codex audit (A/P3): `mood` cannot distinguish "actor saved at the
        // Lowest mood (Value=0)" from "pre-Faz-4 save with no mood field"
        // (Unity default-deserializes both to 0). hasMood is set to true on
        // every new save so the load path can tell them apart and round-trip
        // genuinely-Lowest actors instead of forcing them back to Neutral.
        public bool hasMood;
    }

    [Serializable]
    public sealed class ActorJobPreferenceSaveData
    {
        public int kind;
        public int priority;
    }

    [Serializable]
    public sealed class MemoryFactSaveData
    {
        public long remembererId;
        public string topicCode;
        public long aboutActorId;
        public long recordedAtMinutes;
        public string detail;
    }

    [Serializable]
    public sealed class JobRequestSaveData
    {
        public long id;
        public long recipeId;
        public long siteId;
        public int positionX;
        public int positionY;
        public int worksiteKind;
        public int kind;
        public int priority;
        public int quantity;
        public long requesterId;
        public long claimedByActorId;
        // PR#138 bot review fix: persist the original ClaimSequence so the
        // load path can restore the same queue order (otherwise re-claiming in
        // insertion-order assigns fresh sequences that no longer match the
        // pre-save queue index used by GetQueueIndex).
        public int claimSequence;
    }

    [Serializable]
    public sealed class InventorySaveData
    {
        public int capacity;
        public ItemSaveData[] items;
    }

    [Serializable]
    public sealed class ItemSaveData
    {
        public long id;
        public string templateId;
        public string displayName;
        public int quantity;
        public int equipmentSlot;
        public string equipmentSlotCode;
        public int accuracyBonus;
        public int damageBonus;
    }

    [Serializable]
    public sealed class PickupSaveData
    {
        public ItemSaveData item;
        public int positionX;
        public int positionY;
        public bool collected;
    }

    [Serializable]
    public sealed class TopicSaveData
    {
        public string id;
        public string label;
        public string answer;
    }

    [Serializable]
    public sealed class NpcMemorySaveData
    {
        public long actorId;
        public InteractionEventSaveData[] events;
        public string[] dialogueSeen;
        public TransactionSaveData[] transactions;
    }

    [Serializable]
    public sealed class InteractionEventSaveData
    {
        public long timestampMinutes;
        public string eventType;
        public long actorSeen;
        public string subjectId;
        public string itemTemplateId;
        public int amount;
        public int locationX;
        public int locationY;
    }

    [Serializable]
    public sealed class TransactionSaveData
    {
        public long timestampMinutes;
        public string transactionType;
        public string itemTemplateId;
        public int count;
        public int goldDelta;
    }

    [Serializable]
    public sealed class SpellCooldownSaveData
    {
        public SpellCooldownEntrySaveData[] entries;
    }

    [Serializable]
    public sealed class SpellCooldownEntrySaveData
    {
        public string spellTemplateId;
        public int remainingTicks;
    }

    [Serializable]
    public sealed class ShieldBuffSaveData
    {
        public ShieldBuffEntrySaveData[] entries;
    }

    [Serializable]
    public sealed class ShieldBuffEntrySaveData
    {
        public string spellTemplateId;
        public int remainingTicks;
        public int magnitude;
    }
}
