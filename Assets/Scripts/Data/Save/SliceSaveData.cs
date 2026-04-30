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
        public ActorSaveData player;
        public ActorSaveData talker;
        public ActorSaveData merchant;
        public ActorSaveData guard;
        public ActorSaveData enemy;
        public InventorySaveData inventory;
        public InventorySaveData merchantInventory;
        public PickupSaveData[] pickups;
        public TopicSaveData[] topics;
        public NpcMemorySaveData[] npcMemories;
        public bool doorOpen;
        public bool guardDoorAccessGranted;
        public int guardWarningCount;
        public bool encounterActive;
        public string lastNarrative;
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
}
