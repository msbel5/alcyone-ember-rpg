// Design note:
// SliceWorldState groups every deterministic slice system into one saveable pure object graph.
// Inputs: room, actors, inventories, pickups, door, guard, and narrative shell state.
// Outputs: a single runtime snapshot for tests, presentation wrappers, and JSON mapping.
// Bible reference: PRD Sprint 1 FR-03 through FR-07, Sprint 2 FR-02 through FR-05.
using System;
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
        public ActorStore Actors = new ActorStore();

        [Obsolete("Use Actors.FirstByRole(ActorRole.Player) or ActorStore role-view helpers. This named slice view is deprecated in Faz 1.", false)]
        public ActorRecord Player
        {
            get { return GetActorView(ActorRole.Player); }
            set { SetActorView(ActorRole.Player, value); }
        }

        [Obsolete("Use Actors.FirstByRole(ActorRole.Talker) or ActorStore role-view helpers. This named slice view is deprecated in Faz 1.", false)]
        public ActorRecord Talker
        {
            get { return GetActorView(ActorRole.Talker); }
            set { SetActorView(ActorRole.Talker, value); }
        }

        [Obsolete("Use Actors.FirstByRole(ActorRole.Merchant) or ActorStore role-view helpers. This named slice view is deprecated in Faz 1.", false)]
        public ActorRecord Merchant
        {
            get { return GetActorView(ActorRole.Merchant); }
            set { SetActorView(ActorRole.Merchant, value); }
        }

        [Obsolete("Use Actors.FirstByRole(ActorRole.Guard) or ActorStore role-view helpers. This named slice view is deprecated in Faz 1.", false)]
        public ActorRecord Guard
        {
            get { return GetActorView(ActorRole.Guard); }
            set { SetActorView(ActorRole.Guard, value); }
        }

        [Obsolete("Use Actors.FirstByRole(ActorRole.Enemy) or ActorStore role-view helpers. This named slice view is deprecated in Faz 1.", false)]
        public ActorRecord Enemy
        {
            get { return GetActorView(ActorRole.Enemy); }
            set { SetActorView(ActorRole.Enemy, value); }
        }
        public InventoryState PlayerInventory;
        public EquipmentState PlayerEquipment = new EquipmentState();
        public InventoryState MerchantInventory;
        public List<RoomPickup> Pickups = new List<RoomPickup>();
        public List<DungeonRoomState> DungeonRoomStates = new List<DungeonRoomState>();
        public List<DungeonDoorState> DungeonDoorStates = new List<DungeonDoorState>();
        public List<AskAboutTopic> Topics = new List<AskAboutTopic>();
        public NpcMemoryStore NpcMemory = new NpcMemoryStore();
        public SpellCooldownState PlayerSpellCooldowns = new SpellCooldownState();
        public ShieldBuffState PlayerShieldBuffs = new ShieldBuffState();
        public bool DoorOpen;
        public bool GuardDoorAccessGranted;
        public int GuardWarningCount;
        public bool EncounterActive;
        public string LastNarrative;

        private ActorRecord GetActorView(ActorRole role)
        {
            EnsureActorStore();
            return Actors.FirstByRole(role);
        }

        private void SetActorView(ActorRole expectedRole, ActorRecord record)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));
            if (record.Role != expectedRole)
                throw new ArgumentException($"Expected actor role {expectedRole}, got {record.Role}.", nameof(record));

            EnsureActorStore();
            if (Actors.TryFirstByRole(expectedRole, out var previous))
                Actors.Remove(previous.Id);
            if (Actors.Contains(record.Id))
                Actors.Remove(record.Id);

            Actors.Add(record);
        }

        private void EnsureActorStore()
        {
            if (Actors == null)
                Actors = new ActorStore();
        }
    }
}
