// Design note:
// SliceWorldState groups every deterministic slice system into one saveable pure object graph.
// Inputs: room, actors, inventories, equipment, reputations, pickups, item-id cursor, npc memories, door, and narrative state.
// Outputs: a single runtime snapshot for tests, presentation wrappers, and JSON mapping.
// Bible reference: PRD Sprint 1 FR-03 through FR-07, Sprint 2 FR-02 through FR-05, Sprint 3 simulation depth.
using System.Collections.Generic;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Memory;
using EmberCrpg.Domain.Narrative;

namespace EmberCrpg.Domain.World
{
    /// <summary>Pure aggregate state for the playable vertical slice.</summary>
    public sealed class SliceWorldState
    {
        public GameTime Time;
        public int RoomSeed;
        public ProceduralRoom Room;
        public ActorRecord Player;
        public ActorRecord Talker;
        public ActorRecord Merchant;
        public ActorRecord Guard;
        public ActorRecord Enemy;
        public InventoryState PlayerInventory;
        public EquipmentState PlayerEquipment;
        public InventoryState MerchantInventory;
        public ItemInstanceSequence ItemIds;
        public FactionReputationLedger Reputations;
        public NpcMemoryStore NpcMemories;
        public List<RoomPickup> Pickups = new List<RoomPickup>();
        public List<AskAboutTopic> Topics = new List<AskAboutTopic>();
        public bool DoorOpen;
        public bool GuardDoorAccessGranted;
        public int GuardWarningCount;
        public bool EncounterActive;
        public string LastNarrative;
    }
}
