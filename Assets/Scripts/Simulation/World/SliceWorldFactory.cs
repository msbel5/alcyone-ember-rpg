// Design note:
// SliceWorldFactory builds the smallest fully wired world state for the playable slice.
// Inputs: room seed.
// Outputs: deterministic room, actors, inventories, topics, and interaction state.
// Bible reference: PRD Sprint 1 FR-01 through FR-07, Sprint 2 FR-02 through FR-05.
using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Narrative;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Inventory;

namespace EmberCrpg.Simulation.World
{
    /// <summary>Creates the initial deterministic world snapshot for the vertical slice.</summary>
    public sealed class SliceWorldFactory
    {
        private readonly ProceduralRoomGenerator _rooms = new ProceduralRoomGenerator();
        private readonly MultiRoomDungeonGenerator _dungeons = new MultiRoomDungeonGenerator();
        private readonly SliceActorLoadoutFactory _actors = new SliceActorLoadoutFactory();

        public SliceWorldState Create(int roomSeed)
        {
            var room = _rooms.Generate(roomSeed);
            var dungeon = _dungeons.Generate(roomSeed);
            var playerSpawn = dungeon.FindSpawn(DungeonSpawnKind.Player);
            var talkerSpawn = dungeon.FindSpawn(DungeonSpawnKind.Talker);
            var merchantSpawn = dungeon.FindSpawn(DungeonSpawnKind.Merchant);
            var guardSpawn = dungeon.FindSpawn(DungeonSpawnKind.Guard);
            var enemySpawn = dungeon.FindSpawn(DungeonSpawnKind.Enemy);
            var pickupSpawn = dungeon.FindSpawn(DungeonSpawnKind.Pickup);
            var talkTopics = new[] { "embers", "gate", "watch" };
            var world = new SliceWorldState();
            world.Time = new GameTime(8 * GameTime.MinutesPerHour);
            world.RoomSeed = roomSeed;
            world.Room = room;
            world.Dungeon = dungeon;
            world.CurrentRoomId = dungeon.StartRoomId;
            world.PlayerRoomId = playerSpawn.RoomId;
            world.TalkerRoomId = talkerSpawn.RoomId;
            world.MerchantRoomId = merchantSpawn.RoomId;
            world.GuardRoomId = guardSpawn.RoomId;
            world.EnemyRoomId = enemySpawn.RoomId;
            world.PickupRoomId = pickupSpawn.RoomId;
            world.DungeonRoomStates = dungeon.Rooms.Select(roomNode => new DungeonRoomState(roomNode.Id, roomNode.Id == dungeon.StartRoomId, false)).ToList();
            world.DungeonDoorStates = dungeon.Doors.Select(door => new DungeonDoorState(door.Id, door.StartsOpen)).ToList();
            world.Player = _actors.Create(new ActorId(1), "Warden", ActorRole.Player, playerSpawn.Position);
            world.Talker = _actors.Create(new ActorId(2), "Sage Nera", ActorRole.Talker, talkerSpawn.Position, talkTopics);
            world.Merchant = _actors.Create(new ActorId(3), "Quartermaster Ivo", ActorRole.Merchant, merchantSpawn.Position);
            world.Guard = _actors.Create(new ActorId(4), "Sentinel Rook", ActorRole.Guard, guardSpawn.Position);
            world.Enemy = _actors.Create(new ActorId(5), "Ash Rat", ActorRole.Enemy, enemySpawn.Position);
            world.PlayerInventory = new InventoryState(10);
            world.MerchantInventory = new InventoryState(4);
            world.MerchantInventory.TryAdd(SliceItemCatalog.CreateGateWrit());
            world.Pickups = new List<RoomPickup>
            {
                new RoomPickup(SliceItemCatalog.CreateEmberShard(), pickupSpawn.Position),
            };
            world.Topics = new List<AskAboutTopic>
            {
                new AskAboutTopic("embers", "Embers", "The embers in this room never fully die; they mark old warding lines."),
                new AskAboutTopic("gate", "Gate", "Quartermaster Ivo still issues writs for the south door, but Sentinel Rook honors only sealed paper."),
                new AskAboutTopic("watch", "Watch", "Sentinel Rook keeps count of every footstep, including yours."),
            };
            world.DoorOpen = false;
            world.GuardDoorAccessGranted = false;
            world.GuardWarningCount = 0;
            world.LastNarrative = "Pick up the Ember Shard, trade for a gate writ, speak to Sentinel Rook, then work the south door.";
            return world;
        }
    }
}
