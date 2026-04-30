using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Rng;

// Design note:
// MultiRoomDungeonGenerator builds a deterministic connected graph of room nodes from one seed.
// Inputs: integer dungeon seed.
// Outputs: 5-10 connected rooms, door edges, and room-local archetype spawn points.
// Bible reference: MASTER_MECHANICS_BIBLE.md deterministic world lock-in, Sprint 4 Faz 3.
namespace EmberCrpg.Simulation.World
{
    /// <summary>Deterministic clean graph generator for Sprint 4's multi-room dungeon.</summary>
    public sealed class MultiRoomDungeonGenerator
    {
        private static readonly GridPosition[] Directions =
        {
            new GridPosition(1, 0),
            new GridPosition(0, 1),
            new GridPosition(-1, 0),
            new GridPosition(0, -1),
        };

        public GeneratedDungeonLayout Generate(int seed)
        {
            var rng = new XorShiftRng((uint)seed);
            var targetRooms = 5 + rng.NextInt(6);
            var layout = new GeneratedDungeonLayout { Seed = seed, StartRoomId = 0 };
            AddRoom(layout, 0, 0, 0, rng);

            while (layout.Rooms.Count < targetRooms)
                AddConnectedRoom(layout, rng);

            AddSpawnPoints(layout);
            return layout;
        }

        private static void AddConnectedRoom(GeneratedDungeonLayout layout, IDeterministicRng rng)
        {
            var anchor = layout.Rooms[rng.NextInt(layout.Rooms.Count)];
            var directions = Directions.OrderBy(_ => rng.NextInt(1000)).ToArray();
            foreach (var direction in directions)
            {
                if (TryAddAdjacent(layout, anchor, direction, rng))
                    return;
            }

            foreach (var room in layout.Rooms.OrderBy(room => room.Id))
                foreach (var direction in Directions)
                    if (TryAddAdjacent(layout, room, direction, rng))
                        return;
        }

        private static bool TryAddAdjacent(GeneratedDungeonLayout layout, DungeonRoom anchor, GridPosition direction, IDeterministicRng rng)
        {
            var nextX = anchor.GridX + direction.X;
            var nextY = anchor.GridY + direction.Y;
            if (layout.Rooms.Any(room => room.GridX == nextX && room.GridY == nextY))
                return false;

            var newRoom = AddRoom(layout, layout.Rooms.Count, nextX, nextY, rng);
            AddDoor(layout, anchor, newRoom, direction);
            return true;
        }

        private static DungeonRoom AddRoom(GeneratedDungeonLayout layout, int id, int gridX, int gridY, IDeterministicRng rng)
        {
            var room = new DungeonRoom
            {
                Id = id,
                GridX = gridX,
                GridY = gridY,
                Width = 8 + rng.NextInt(5),
                Height = 8 + rng.NextInt(5),
                TemplateId = SelectTemplate(id, rng),
            };
            layout.Rooms.Add(room);
            return room;
        }

        private static void AddDoor(GeneratedDungeonLayout layout, DungeonRoom from, DungeonRoom to, GridPosition direction)
        {
            var door = new DungeonDoor
            {
                Id = layout.Doors.Count + 1,
                FromRoomId = from.Id,
                ToRoomId = to.Id,
                FromCell = DoorCell(from, direction),
                ToCell = DoorCell(to, new GridPosition(-direction.X, -direction.Y)),
                StartsOpen = layout.Doors.Count > 0,
                RequiresGuardClearance = layout.Doors.Count == 0,
            };
            layout.Doors.Add(door);
            from.DoorIds.Add(door.Id);
            to.DoorIds.Add(door.Id);
        }

        private static GridPosition DoorCell(DungeonRoom room, GridPosition direction)
        {
            if (direction.X > 0)
                return new GridPosition(room.Width - 1, room.Height / 2);
            if (direction.X < 0)
                return new GridPosition(0, room.Height / 2);
            if (direction.Y > 0)
                return new GridPosition(room.Width / 2, room.Height - 1);
            return new GridPosition(room.Width / 2, 0);
        }

        private static string SelectTemplate(int id, IDeterministicRng rng)
        {
            var variant = rng.NextInt(3);
            return variant == 0 ? "ember-hall" : variant == 1 ? "ash-cell" : $"watch-node-{id % 3}";
        }

        private static void AddSpawnPoints(GeneratedDungeonLayout layout)
        {
            var rooms = layout.Rooms.OrderBy(room => room.Id).ToArray();
            AddSpawn(layout, rooms[0], DungeonSpawnKind.Player, 2, 2);
            AddSpawn(layout, rooms[0], DungeonSpawnKind.Guard, rooms[0].Width / 2, rooms[0].Height - 3);
            AddSpawn(layout, rooms[rooms.Length / 3], DungeonSpawnKind.Talker, 2, rooms[rooms.Length / 3].Height - 3);
            AddSpawn(layout, rooms[rooms.Length / 2], DungeonSpawnKind.Merchant, rooms[rooms.Length / 2].Width - 3, 2);
            AddSpawn(layout, rooms[rooms.Length - 1], DungeonSpawnKind.Enemy, rooms[rooms.Length - 1].Width - 3, rooms[rooms.Length - 1].Height - 3);
            AddSpawn(layout, rooms[(rooms.Length * 2) / 3], DungeonSpawnKind.Pickup, rooms[(rooms.Length * 2) / 3].Width / 2, rooms[(rooms.Length * 2) / 3].Height / 2);
        }

        private static void AddSpawn(GeneratedDungeonLayout layout, DungeonRoom room, DungeonSpawnKind kind, int x, int y)
        {
            layout.SpawnPoints.Add(new DungeonSpawnPoint(room.Id, kind, new GridPosition(x, y)));
        }
    }
}
