using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.World;

// Design note:
// ProceduralRoomGenerator builds Sprint 1's one-room slice from a seed with Sprint 3 template variation.
// Inputs: integer room seed.
// Outputs: deterministic room dimensions, layout id, door cell, and spawn positions.
// Bible reference: MASTER_MECHANICS_BIBLE.md §40/§41, PRD FR-03, Sprint 3 richer room templates.
namespace EmberCrpg.Simulation.World
{
    /// <summary>Deterministic generator for the vertical-slice room layouts.</summary>
    public sealed class ProceduralRoomGenerator
    {
        public ProceduralRoom Generate(int seed)
        {
            var width = 10 + (seed % 4);
            var height = 9 + ((seed / 10) % 4);
            var centerX = width / 2;
            var layoutIndex = ((seed % 3) + 3) % 3;

            return layoutIndex switch
            {
                0 => new ProceduralRoom
                {
                    Seed = seed,
                    Width = width,
                    Height = height,
                    LayoutId = RoomLayoutId.CheckpointAxis,
                    DoorCell = new GridPosition(centerX, 0),
                    PlayerSpawn = new GridPosition(2, 2),
                    TalkerSpawn = new GridPosition(width - 3, 2),
                    MerchantSpawn = new GridPosition(2, height - 3),
                    GuardSpawn = new GridPosition(centerX, height - 3),
                    EnemySpawn = new GridPosition(width - 3, height - 3),
                    PickupSpawn = new GridPosition(centerX, height / 2),
                },
                1 => new ProceduralRoom
                {
                    Seed = seed,
                    Width = width,
                    Height = height,
                    LayoutId = RoomLayoutId.OffsetWatch,
                    DoorCell = new GridPosition(width - 3, 0),
                    PlayerSpawn = new GridPosition(2, height / 2),
                    TalkerSpawn = new GridPosition(centerX, 2),
                    MerchantSpawn = new GridPosition(2, height - 3),
                    GuardSpawn = new GridPosition(width - 3, 2),
                    EnemySpawn = new GridPosition(centerX, height - 3),
                    PickupSpawn = new GridPosition(centerX, height / 2),
                },
                _ => new ProceduralRoom
                {
                    Seed = seed,
                    Width = width,
                    Height = height,
                    LayoutId = RoomLayoutId.SplitHall,
                    DoorCell = new GridPosition(2, 0),
                    PlayerSpawn = new GridPosition(centerX, 2),
                    TalkerSpawn = new GridPosition(width - 3, height / 2),
                    MerchantSpawn = new GridPosition(2, 2),
                    GuardSpawn = new GridPosition(2, height - 3),
                    EnemySpawn = new GridPosition(width - 3, height - 3),
                    PickupSpawn = new GridPosition(centerX, height / 2),
                },
            };
        }
    }
}
