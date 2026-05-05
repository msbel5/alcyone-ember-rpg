// Design note:
// SliceSaveMapper translates between pure world state and Unity-serializable DTOs.
// Inputs: SliceWorldState or SliceSaveData snapshots.
// Outputs: round-trippable save objects with no UnityEngine in Domain/Simulation.
// Bible reference: PRD Sprint 1 FR-06, Sprint 2 FR-02 through FR-04.
using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Memory;
using EmberCrpg.Domain.Narrative;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.World;

namespace EmberCrpg.Data.Save
{
    /// <summary>Pure mapping layer between aggregate world state and JSON DTOs.</summary>
    public static class SliceSaveMapper
    {
        public static SliceSaveData ToData(SliceWorldState world)
        {
            return new SliceSaveData
            {
                totalMinutes = world.Time.TotalMinutes,
                roomSeed = world.RoomSeed,
                currentRoomId = world.CurrentRoomId,
                dungeonStartRoomId = world.Dungeon?.StartRoomId ?? 0,
                playerRoomId = world.PlayerRoomId,
                talkerRoomId = world.TalkerRoomId,
                merchantRoomId = world.MerchantRoomId,
                guardRoomId = world.GuardRoomId,
                enemyRoomId = world.EnemyRoomId,
                pickupRoomId = world.PickupRoomId,
                dungeonRooms = DungeonSaveMapper.ToRoomData(world.Dungeon),
                dungeonDoors = DungeonSaveMapper.ToDoorData(world.Dungeon),
                dungeonSpawns = DungeonSaveMapper.ToSpawnData(world.Dungeon),
                dungeonRoomStates = DungeonSaveMapper.ToRoomStateData(world.DungeonRoomStates),
                dungeonDoorStates = DungeonSaveMapper.ToDoorStateData(world.DungeonDoorStates),
                player = ActorSaveMapper.ToData(world.Player),
                talker = ActorSaveMapper.ToData(world.Talker),
                merchant = ActorSaveMapper.ToData(world.Merchant),
                guard = ActorSaveMapper.ToData(world.Guard),
                enemy = ActorSaveMapper.ToData(world.Enemy),
                inventory = ToInventoryData(world.PlayerInventory),
                playerEquipment = ToEquipmentData(world.PlayerEquipment),
                merchantInventory = ToInventoryData(world.MerchantInventory),
                pickups = world.Pickups.Select(ItemSaveMapper.ToData).ToArray(),
                topics = world.Topics.Select(topic => new TopicSaveData { id = topic.Id, label = topic.Label, answer = topic.Answer }).ToArray(),
                npcMemories = ToNpcMemoryData(world.NpcMemory),
                playerSpellCooldowns = SpellCooldownSaveMapper.ToData(world.PlayerSpellCooldowns),
                playerShieldBuffs = ShieldBuffSaveMapper.ToData(world.PlayerShieldBuffs),
                doorOpen = world.DoorOpen,
                guardDoorAccessGranted = world.GuardDoorAccessGranted,
                guardWarningCount = world.GuardWarningCount,
                encounterActive = world.EncounterActive,
                lastNarrative = world.LastNarrative,
            };
        }

        public static SliceWorldState ToWorld(SliceSaveData data)
        {
            var world = new SliceWorldFactory().Create(data.roomSeed);
            world.Time = new EmberCrpg.Domain.Core.GameTime(data.totalMinutes);
            if (data.dungeonRooms != null && data.dungeonRooms.Length > 0)
                world.Dungeon = DungeonSaveMapper.ToLayout(data.roomSeed, data.dungeonStartRoomId, data.dungeonRooms, data.dungeonDoors, data.dungeonSpawns);
            world.CurrentRoomId = data.currentRoomId;
            world.PlayerRoomId = data.playerRoomId;
            world.TalkerRoomId = data.talkerRoomId;
            world.MerchantRoomId = data.merchantRoomId;
            world.GuardRoomId = data.guardRoomId;
            world.EnemyRoomId = data.enemyRoomId;
            world.PickupRoomId = data.pickupRoomId;
            if (data.dungeonRoomStates != null && data.dungeonRoomStates.Length > 0)
                world.DungeonRoomStates = DungeonSaveMapper.ToRoomStates(data.dungeonRoomStates);
            if (data.dungeonDoorStates != null && data.dungeonDoorStates.Length > 0)
                world.DungeonDoorStates = DungeonSaveMapper.ToDoorStates(data.dungeonDoorStates);
            world.Player = ActorSaveMapper.ToActor(data.player);
            world.Talker = ActorSaveMapper.ToActor(data.talker);
            world.Merchant = ActorSaveMapper.ToActor(data.merchant);
            world.Guard = ActorSaveMapper.ToActor(data.guard);
            world.Enemy = ActorSaveMapper.ToActor(data.enemy);
            world.PlayerInventory = ToInventoryState(data.inventory, world.PlayerInventory.Capacity);
            world.PlayerEquipment = ToEquipmentState(data.playerEquipment);
            world.MerchantInventory = ToInventoryState(data.merchantInventory, world.MerchantInventory.Capacity);
            world.Pickups = (data.pickups ?? new PickupSaveData[0]).Select(ItemSaveMapper.ToPickup).ToList();
            world.Topics = (data.topics ?? new TopicSaveData[0]).Select(topic => new AskAboutTopic(topic.id, topic.label, topic.answer)).ToList();
            world.NpcMemory = ToNpcMemoryStore(data.npcMemories);
            world.PlayerSpellCooldowns = SpellCooldownSaveMapper.ToState(data.playerSpellCooldowns);
            world.PlayerShieldBuffs = ShieldBuffSaveMapper.ToState(data.playerShieldBuffs);
            world.DoorOpen = data.doorOpen;
            world.GuardDoorAccessGranted = data.guardDoorAccessGranted;
            world.GuardWarningCount = data.guardWarningCount;
            world.EncounterActive = data.encounterActive;
            world.LastNarrative = data.lastNarrative;
            return world;
        }

        private static EquipmentSaveData ToEquipmentData(EquipmentState equipment)
        {
            return new EquipmentSaveData
            {
                slots = new[] { EquipmentSlot.Weapon }
                    .Select(slot => new EquippedItemSaveData { slot = (int)slot, itemId = equipment.GetEquippedItemId(slot).Value })
                    .Where(slot => slot.itemId != 0UL)
                    .ToArray(),
            };
        }

        private static EquipmentState ToEquipmentState(EquipmentSaveData data)
        {
            var equipment = new EquipmentState();
            foreach (var slot in data?.slots ?? new EquippedItemSaveData[0])
                equipment.Equip((EquipmentSlot)slot.slot, new ItemId(slot.itemId));
            return equipment;
        }

        private static NpcMemorySaveData[] ToNpcMemoryData(NpcMemoryStore store)
        {
            return (store ?? new NpcMemoryStore()).GetAllSorted().Select(memory => new NpcMemorySaveData
            {
                actorId = memory.ActorId.Value,
                events = memory.Events.Select(ToInteractionEventData).ToArray(),
                dialogueSeen = memory.DialogueSeen.OrderBy(topicId => topicId).ToArray(),
                transactions = memory.Transactions.Select(ToTransactionData).ToArray(),
            }).ToArray();
        }

        private static NpcMemoryStore ToNpcMemoryStore(NpcMemorySaveData[] data)
        {
            var store = new NpcMemoryStore();
            store.ReplaceAll((data ?? new NpcMemorySaveData[0]).Select(ToActorMemory));
            return store;
        }

        private static ActorMemory ToActorMemory(NpcMemorySaveData data)
        {
            var memory = new ActorMemory(new ActorId(data.actorId));
            memory.ReplaceEvents((data.events ?? new InteractionEventSaveData[0]).Select(ToInteractionEvent));
            memory.ReplaceDialogueSeen(data.dialogueSeen);
            memory.ReplaceTransactions((data.transactions ?? new TransactionSaveData[0]).Select(ToTransaction));
            return memory;
        }

        private static InteractionEventSaveData ToInteractionEventData(InteractionEvent interactionEvent)
        {
            return new InteractionEventSaveData
            {
                timestampMinutes = interactionEvent.Timestamp.TotalMinutes,
                eventType = interactionEvent.EventType,
                actorSeen = interactionEvent.ActorSeen.Value,
                subjectId = interactionEvent.SubjectId,
                itemTemplateId = interactionEvent.ItemTemplateId,
                amount = interactionEvent.Amount,
                locationX = interactionEvent.Location.X,
                locationY = interactionEvent.Location.Y,
            };
        }

        private static InteractionEvent ToInteractionEvent(InteractionEventSaveData data)
        {
            return new InteractionEvent(
                new GameTime(data.timestampMinutes),
                data.eventType,
                new ActorId(data.actorSeen),
                data.subjectId,
                data.itemTemplateId,
                data.amount,
                new GridPosition(data.locationX, data.locationY));
        }

        private static TransactionSaveData ToTransactionData(TransactionRecord transaction)
        {
            return new TransactionSaveData
            {
                timestampMinutes = transaction.Timestamp.TotalMinutes,
                transactionType = transaction.TransactionType,
                itemTemplateId = transaction.ItemTemplateId,
                count = transaction.Count,
                goldDelta = transaction.GoldDelta,
            };
        }

        private static TransactionRecord ToTransaction(TransactionSaveData data)
        {
            return new TransactionRecord(
                new GameTime(data.timestampMinutes),
                data.transactionType,
                data.itemTemplateId,
                data.count,
                data.goldDelta);
        }

        private static InventorySaveData ToInventoryData(InventoryState inventory)
        {
            return new InventorySaveData
            {
                capacity = inventory.Capacity,
                items = inventory.Items.Select(ItemSaveMapper.ToData).ToArray(),
            };
        }

        private static InventoryState ToInventoryState(InventorySaveData inventory, int fallbackCapacity)
        {
            var state = new InventoryState(inventory?.capacity ?? fallbackCapacity);
            foreach (var item in inventory?.items ?? new ItemSaveData[0])
                state.TryAdd(ItemSaveMapper.ToItem(item));
            return state;
        }
    }
}
