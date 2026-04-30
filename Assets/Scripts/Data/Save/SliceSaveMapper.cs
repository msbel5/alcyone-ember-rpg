// Design note:
// SliceSaveMapper translates between pure world state and Unity-serializable DTOs.
// Inputs: SliceWorldState or SliceSaveData snapshots.
// Outputs: round-trippable save objects with no UnityEngine in Domain/Simulation.
// Bible reference: PRD FR-06.
using System.Linq;
using EmberCrpg.Domain.Inventory;
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
                player = ActorSaveMapper.ToData(world.Player),
                talker = ActorSaveMapper.ToData(world.Talker),
                merchant = ActorSaveMapper.ToData(world.Merchant),
                guard = ActorSaveMapper.ToData(world.Guard),
                enemy = ActorSaveMapper.ToData(world.Enemy),
                inventory = new InventorySaveData
                {
                    capacity = world.PlayerInventory.Capacity,
                    items = world.PlayerInventory.Items.Select(ItemSaveMapper.ToData).ToArray(),
                },
                pickups = world.Pickups.Select(ItemSaveMapper.ToData).ToArray(),
                topics = world.Topics.Select(topic => new TopicSaveData { id = topic.Id, label = topic.Label, answer = topic.Answer }).ToArray(),
                encounterActive = world.EncounterActive,
                lastNarrative = world.LastNarrative,
            };
        }

        public static SliceWorldState ToWorld(SliceSaveData data)
        {
            var world = new SliceWorldFactory().Create(data.roomSeed);
            world.Time = new EmberCrpg.Domain.Core.GameTime(data.totalMinutes);
            world.Player = ActorSaveMapper.ToActor(data.player);
            world.Talker = ActorSaveMapper.ToActor(data.talker);
            world.Merchant = ActorSaveMapper.ToActor(data.merchant);
            world.Guard = ActorSaveMapper.ToActor(data.guard);
            world.Enemy = ActorSaveMapper.ToActor(data.enemy);
            world.PlayerInventory = new InventoryState(data.inventory.capacity);
            foreach (var item in data.inventory.items)
                world.PlayerInventory.TryAdd(ItemSaveMapper.ToItem(item));
            world.Pickups = data.pickups.Select(ItemSaveMapper.ToPickup).ToList();
            world.Topics = data.topics.Select(topic => new AskAboutTopic(topic.id, topic.label, topic.answer)).ToList();
            world.EncounterActive = data.encounterActive;
            world.LastNarrative = data.lastNarrative;
            return world;
        }
    }
}
