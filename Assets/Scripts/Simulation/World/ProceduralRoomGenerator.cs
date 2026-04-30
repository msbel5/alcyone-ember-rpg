using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.World;

// Design note:
// ProceduralRoomGenerator builds Sprint 1's one-room slice from a seed with fixed spawn semantics.
// Inputs: integer room seed.
// Outputs: deterministic room dimensions, door cell, and spawn positions.
// Bible reference: MASTER_MECHANICS_BIBLE.md §40/§41, PRD FR-03.
namespace EmberCrpg.Simulation.World
{
    /// <summary>Deterministic generator for the single vertical-slice room layout.</summary>
    public sealed class ProceduralRoomGenerator
    {
        public ProceduralRoom Generate(int seed)
        {
            var width = 10 + (seed % 4);
            var height = 9 + ((seed / 10) % 4);
            var centerX = width / 2;

            return new ProceduralRoom
            {
                Seed = seed,
                Width = width,
                Height = height,
                DoorCell = new GridPosition(centerX, 0),
                PlayerSpawn = new GridPosition(2, 2),
                TalkerSpawn = new GridPosition(width - 3, 2),
                MerchantSpawn = new GridPosition(2, height - 3),
                GuardSpawn = new GridPosition(centerX, height - 3),
                EnemySpawn = new GridPosition(width - 3, height - 3),
                PickupSpawn = new GridPosition(centerX, height / 2),
            };
        }
    }
}
