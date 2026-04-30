using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

// Design note:
// These tests pin deterministic Sprint 4 multi-room dungeon generation.
// They cover graph connectivity, seed repeatability, orphan prevention, and archetype spawns.
namespace EmberCrpg.Tests.EditMode.World
{
    /// <summary>Verifies deterministic connected generated dungeon topology.</summary>
    public sealed class MultiRoomDungeonGeneratorTests
    {
        [Test]
        public void Generate_CreatesFiveToTenConnectedRoomsWithoutOrphans()
        {
            var dungeon = new MultiRoomDungeonGenerator().Generate(1337);
            var reached = ReachableRoomIds(dungeon);
            Assert.That(dungeon.Rooms.Count, Is.InRange(5, 10));
            Assert.That(reached, Is.EquivalentTo(dungeon.Rooms.Select(room => room.Id)));
            Assert.That(dungeon.Rooms.All(room => room.Id == dungeon.StartRoomId || room.DoorIds.Count > 0), Is.True);
        }

        [Test]
        public void Generate_SameSeed_RepeatsTopologyDoorsAndSpawns()
        {
            var left = new MultiRoomDungeonGenerator().Generate(20260430);
            var right = new MultiRoomDungeonGenerator().Generate(20260430);
            Assert.That(left.Rooms.Select(room => $"{room.Id}:{room.GridX},{room.GridY}:{room.Width}x{room.Height}:{room.TemplateId}"), Is.EqualTo(right.Rooms.Select(room => $"{room.Id}:{room.GridX},{room.GridY}:{room.Width}x{room.Height}:{room.TemplateId}")));
            Assert.That(left.Doors.Select(door => $"{door.Id}:{door.FromRoomId}>{door.ToRoomId}:{door.StartsOpen}:{door.RequiresGuardClearance}"), Is.EqualTo(right.Doors.Select(door => $"{door.Id}:{door.FromRoomId}>{door.ToRoomId}:{door.StartsOpen}:{door.RequiresGuardClearance}")));
            Assert.That(left.SpawnPoints.Select(spawn => $"{spawn.Kind}:{spawn.RoomId}:{spawn.Position}"), Is.EqualTo(right.SpawnPoints.Select(spawn => $"{spawn.Kind}:{spawn.RoomId}:{spawn.Position}")));
        }

        [Test]
        public void Generate_PlacesRequiredArchetypesInWalkableCells()
        {
            var dungeon = new MultiRoomDungeonGenerator().Generate(9876);
            foreach (var kind in new[] { DungeonSpawnKind.Talker, DungeonSpawnKind.Merchant, DungeonSpawnKind.Enemy })
            {
                var spawn = dungeon.FindSpawn(kind);
                Assert.That(dungeon.FindRoom(spawn.RoomId).IsWalkable(spawn.Position), Is.True, kind.ToString());
            }
        }

        [Test]
        public void Traverse_ClosedGuardedDoorBlocksUntilSprint2DoorOpens()
        {
            var world = new SliceWorldFactory().Create(1337);
            var guardedDoor = world.Dungeon.Doors.First(door => door.RequiresGuardClearance);
            var traversal = new DungeonTraversalService();
            Assert.That(traversal.Traverse(world, guardedDoor.Id), Does.Contain("closed"));

            world.GuardDoorAccessGranted = true;
            world.Player.MoveTo(new EmberCrpg.Domain.Actors.GridPosition(world.Room.DoorCell.X, 1));
            new DoorInteractionService().Toggle(world);
            Assert.That(traversal.Traverse(world, guardedDoor.Id), Does.Contain($"room {world.CurrentRoomId}"));
            Assert.That(world.DungeonRoomStates.First(state => state.RoomId == world.CurrentRoomId).Visited, Is.True);
        }

        private static HashSet<int> ReachableRoomIds(GeneratedDungeonLayout dungeon)
        {
            var reached = new HashSet<int> { dungeon.StartRoomId };
            var frontier = new Queue<int>();
            frontier.Enqueue(dungeon.StartRoomId);
            while (frontier.Count > 0)
            {
                var roomId = frontier.Dequeue();
                foreach (var door in dungeon.Doors.Where(door => door.FromRoomId == roomId || door.ToRoomId == roomId))
                {
                    var other = door.OtherRoom(roomId);
                    if (reached.Add(other))
                        frontier.Enqueue(other);
                }
            }
            return reached;
        }
    }
}
