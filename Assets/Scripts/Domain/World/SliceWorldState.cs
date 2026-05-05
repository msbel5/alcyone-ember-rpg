// Design note:
// SliceWorldState groups every deterministic slice system into one saveable pure object graph.
// Inputs: room, actors, inventories, pickups, door, guard, and narrative shell state.
// Outputs: a single runtime snapshot for tests, presentation wrappers, and JSON mapping.
// Bible reference: PRD Sprint 1 FR-03 through FR-07, Sprint 2 FR-02 through FR-05.
using System.Collections.Generic;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Magic;
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
        public GeneratedDungeonLayout Dungeon;
        public int CurrentRoomId;
        public int PlayerRoomId;
        public int TalkerRoomId;
        public int MerchantRoomId;
        public int GuardRoomId;
        public int EnemyRoomId;
        public int PickupRoomId;
        public ActorRecord Player;
        public ActorRecord Talker;
        public ActorRecord Merchant;
        public ActorRecord Guard;
        public ActorRecord Enemy;
        public InventoryState PlayerInventory;
        public EquipmentState PlayerEquipment = new EquipmentState();
        public InventoryState MerchantInventory;
        public List<RoomPickup> Pickups = new List<RoomPickup>();
        public List<DungeonRoomState> DungeonRoomStates = new List<DungeonRoomState>();
        public List<DungeonDoorState> DungeonDoorStates = new List<DungeonDoorState>();
        public List<AskAboutTopic> Topics = new List<AskAboutTopic>();
        public NpcMemoryStore NpcMemory = new NpcMemoryStore();
        public SpellCooldownState PlayerSpellCooldowns = new SpellCooldownState();
        public bool DoorOpen;
        public bool GuardDoorAccessGranted;
        public int GuardWarningCount;
        public bool EncounterActive;
        public string LastNarrative;
    }
}
