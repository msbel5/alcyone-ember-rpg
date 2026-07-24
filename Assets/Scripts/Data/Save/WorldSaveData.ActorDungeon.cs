using System;

// REF-f (LEFT/244-LOC): ActorDungeon DTOs split out of WorldSaveData.cs (same namespace, zero behaviour change).
namespace EmberCrpg.Data.Save
{
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
        // PLAYTEST FIX: Home/DayAnchor were dropped by the mapper — a night save collapsed every
        // villager's Home onto the sleeping pile. hasHomeAnchor=false on old saves keeps the old
        // (position-default) behavior for them.
        public bool hasHomeAnchor;
        public int homeX;
        public int homeY;
        public int dayAnchorX;
        public int dayAnchorY;
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
        // Lowest mood (Value=0)" from "pre-Phase-4 save with no mood field"
        // (Unity default-deserializes both to 0). hasMood is set to true on
        // every new save so the load path can tell them apart and round-trip
        // genuinely-Lowest actors instead of forcing them back to Neutral.
        public bool hasMood;

        // W32 EAT slice — persisted mind/action state (docs/ruh/w32/01-actor-action-state.md).
        // CONSTRAINT: all-zero block == Idle == pre-W32 save. NO presence flag needed (contrast
        // hasMood, where 0 was a legitimate live value): StartedAtMinutes=0 is only meaningful
        // when currentAction != 0, so the zero block is unambiguous.
        public int currentIntent;          // ActorIntent
        public int currentAction;          // ActorActionType
        public int actionPhase;            // ActionPhase
        public long actionTargetItemId;    // 0 = ItemId.Empty
        public long actionTargetSiteId;    // 0 = SiteId.Empty
        public long actionReservationId;   // 0 = ReservationId.Empty
        public int actionProgressTicks;
        public long actionStartedAtMinutes;
        public int actionFailureReason;    // ActionFailureReason
        public int actionInterruptPolicy;  // ActionInterruptPolicy
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
