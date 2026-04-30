using EmberCrpg.Domain.Actors;

// Design note:
// RoomPickup ties a deterministic item source to one room grid cell.
// Inputs: one item instance and one procedural room position.
// Outputs: pure pickup state that can be collected and serialized.
// Bible reference: PRD FR-03/FR-05.
namespace EmberCrpg.Domain.Inventory
{
    /// <summary>Single collectable world item for the vertical slice room.</summary>
    public sealed class RoomPickup
    {
        public RoomPickup(InventoryItem item, GridPosition position)
        {
            Item = item;
            Position = position;
        }

        public InventoryItem Item { get; }
        public GridPosition Position { get; }
        public bool IsCollected { get; private set; }

        public void Collect()
        {
            IsCollected = true;
        }
    }
}
