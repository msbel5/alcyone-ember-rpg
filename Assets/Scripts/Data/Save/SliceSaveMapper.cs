// Design note:
// SliceSaveMapper translates between pure world state and Unity-serializable DTOs.
// Inputs: SliceWorldState or SliceSaveData snapshots.
// Outputs: round-trippable save objects with no UnityEngine in Domain/Simulation.
// Bible reference: PRD Sprint 1 FR-06, Sprint 2 FR-02 through FR-04, Sprint 3 persistence hardening.
using System.Linq;
using EmberCrpg.Domain.Core;
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
                nextItemIdValue = world.ItemIds.NextValue,
                player = ActorSaveMapper.ToData(world.Player),
                talker = ActorSaveMapper.ToData(world.Talker),
                merchant = ActorSaveMapper.ToData(world.Merchant),
                guard = ActorSaveMapper.ToData(world.Guard),
                enemy = ActorSaveMapper.ToData(world.Enemy),
                inventory = ToInventoryData(world.PlayerInventory),
                merchantInventory = ToInventoryData(world.MerchantInventory),
                pickups = world.Pickups.Select(ItemSaveMapper.ToData).ToArray(),
                topics = world.Topics.Select(topic => new TopicSaveData { id = topic.Id, label = topic.Label, answer = topic.Answer }).ToArray(),
                npcMemories = (world.NpcMemories == null ? new ActorMemory[0] : world.NpcMemories.Entries.ToArray()).Select(MemorySaveMapper.ToData).ToArray(),
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
            world.Time = new GameTime(data.totalMinutes);
            world.ItemIds = new ItemInstanceSequence(data.roomSeed, data.nextItemIdValue);
            world.Player = ActorSaveMapper.ToActor(data.player);
            world.Talker = ActorSaveMapper.ToActor(data.talker);
            world.Merchant = ActorSaveMapper.ToActor(data.merchant);
            world.Guard = ActorSaveMapper.ToActor(data.guard);
            world.Enemy = ActorSaveMapper.ToActor(data.enemy);
            world.PlayerInventory = ToInventoryState(data.inventory, world.PlayerInventory.Capacity);
            world.MerchantInventory = ToInventoryState(data.merchantInventory, world.MerchantInventory.Capacity);
            world.Pickups = (data.pickups ?? new PickupSaveData[0]).Select(ItemSaveMapper.ToPickup).ToList();
            world.Topics = (data.topics ?? new TopicSaveData[0]).Select(topic => new AskAboutTopic(topic.id, topic.label, topic.answer)).ToList();
            var memories = new NpcMemoryStore(new[] { world.Talker.Id, world.Merchant.Id, world.Guard.Id });
            memories.Replace((data.npcMemories ?? new NpcMemorySaveData[0]).Select(MemorySaveMapper.ToMemory));
            memories.RegisterRange(new[] { world.Talker.Id, world.Merchant.Id, world.Guard.Id });
            world.NpcMemories = memories;
            world.DoorOpen = data.doorOpen;
            world.GuardDoorAccessGranted = data.guardDoorAccessGranted;
            world.GuardWarningCount = data.guardWarningCount;
            world.EncounterActive = data.encounterActive;
            world.LastNarrative = data.lastNarrative;
            return world;
        }

        private static InventorySaveData ToInventoryData(EmberCrpg.Domain.Inventory.InventoryState inventory)
        {
            return new InventorySaveData
            {
                capacity = inventory.Capacity,
                items = inventory.Items.Select(ItemSaveMapper.ToData).ToArray(),
            };
        }

        private static EmberCrpg.Domain.Inventory.InventoryState ToInventoryState(InventorySaveData inventory, int fallbackCapacity)
        {
            var state = new EmberCrpg.Domain.Inventory.InventoryState(inventory?.capacity ?? fallbackCapacity);
            foreach (var item in inventory?.items ?? new ItemSaveData[0])
                state.TryAdd(ItemSaveMapper.ToItem(item));
            return state;
        }
    }
}
