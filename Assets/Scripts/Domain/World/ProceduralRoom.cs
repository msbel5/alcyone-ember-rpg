using EmberCrpg.Domain.Actors;

// Design note:
// ProceduralRoom is the deterministic one-room world snapshot for Sprint 1.
// Inputs: room seed, dimensions, door cell, and spawn positions.
// Outputs: pure room geometry metadata for movement, bootstrap, and saves.
// Bible reference: MASTER_MECHANICS_BIBLE.md §40/§41, PRD FR-03.
namespace EmberCrpg.Domain.World
{
    /// <summary>Pure description of the single generated room used by the vertical slice.</summary>
    public sealed class ProceduralRoom
    {
        public int Seed;
        public int Width;
        public int Height;
        public GridPosition DoorCell;
        public GridPosition PlayerSpawn;
        public GridPosition TalkerSpawn;
        public GridPosition MerchantSpawn;
        public GridPosition GuardSpawn;
        public GridPosition EnemySpawn;
        public GridPosition PickupSpawn;

        public bool IsWalkable(GridPosition position)
        {
            return position.X > 0 && position.X < Width - 1 && position.Y > 0 && position.Y < Height - 1;
        }
    }
}
