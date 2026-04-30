using EmberCrpg.Domain.Inventory;

// Design note:
// PickupService bridges a room pickup source into the deterministic backpack.
// Inputs: room pickup state and player inventory.
// Outputs: success/failure plus collected pickup state when space exists.
// Bible reference: PRD FR-05.
namespace EmberCrpg.Simulation.Inventory
{
    /// <summary>Pure pickup-to-inventory transfer helper.</summary>
    public sealed class PickupService
    {
        public bool TryCollect(RoomPickup pickup, InventoryState inventory)
        {
            if (pickup.IsCollected)
                return false;
            if (!inventory.TryAdd(pickup.Item))
                return false;

            pickup.Collect();
            return true;
        }
    }
}
