using System;

// Design note:
// SliceSaveData is the Unity-serializable DTO tree for slice JSON persistence.
// Inputs: pure world-state values copied by the mapper.
// Outputs: JsonUtility-friendly fields with no behavior.
// Bible reference: MASTER_MECHANICS_BIBLE.md §48, PRD Sprint 1 FR-06, Sprint 2 FR-02 through FR-04.
namespace EmberCrpg.Data.Save
{
    [Serializable]
    public sealed class SliceSaveData
    {
        public long totalMinutes;
        public int roomSeed;
        public int currentRoomId;
        public int dungeonStartRoomId;
        public int playerRoomId;
        public int talkerRoomId;
        public int merchantRoomId;
        public int guardRoomId;
        public int enemyRoomId;
        public int pickupRoomId;
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
        public WorldEventSaveData[] worldEvents;
        public WorksiteSaveData[] worksites;
        public RecipeWorkOrderSaveData[] recipeWorkOrders;
        public JobRequestSaveData[] jobs;
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
    public sealed class ItemRecordSaveData
    {
        public ulong id;
        public int material;
        public int quality;
        public int slot;
    }

    [Serializable]
    public sealed class SiteRecordSaveData
    {
        public ulong id;
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
        public ulong siteId;
        public int positionX;
        public int positionY;
        public int kind;
        public bool isActive;
    }

    [Serializable]
    public sealed class RecipeWorkOrderSaveData
    {
        public ulong recipeId;
        public ulong siteId;
        public int positionX;
        public int positionY;
        public ulong actorId;
        public int progressTicks;
    }

    [Serializable]
    public sealed class FactionRecordSaveData
    {
        public ulong id;
        public string name;
        public string[] tags;
    }

    [Serializable]
    public sealed class WorldEventSaveData
    {
        public long tickMinutes;
        public int kind;
        public ulong actorId;
        public ulong siteId;
        public string reason;
        public string[] reasonTrace;
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
        public ulong itemId;
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
        public ulong id;
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
        public ulong currentJobId;
        public ulong targetSiteId;
        public int targetWorksitePositionX;
        public int targetWorksitePositionY;
    }

    [Serializable]
    public sealed class ActorJobPreferenceSaveData
    {
        public int kind;
        public int priority;
    }

    [Serializable]
    public sealed class JobRequestSaveData
    {
        public ulong id;
        public ulong recipeId;
        public ulong siteId;
        public int positionX;
        public int positionY;
        public int worksiteKind;
        public int kind;
        public int priority;
        public int quantity;
        public ulong requesterId;
        public ulong claimedByActorId;
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
        public ulong id;
        public string templateId;
        public string displayName;
        public int quantity;
        public int equipmentSlot;
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
        public ulong actorId;
        public InteractionEventSaveData[] events;
        public string[] dialogueSeen;
        public TransactionSaveData[] transactions;
    }

    [Serializable]
    public sealed class InteractionEventSaveData
    {
        public long timestampMinutes;
        public string eventType;
        public ulong actorSeen;
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
