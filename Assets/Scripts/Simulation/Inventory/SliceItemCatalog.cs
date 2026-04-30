using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;

// Design note:
// SliceItemCatalog centralizes the tiny deterministic item set used by the vertical slice.
// Inputs: an item-id sequence plus fixed templates chosen for slice interactions.
// Outputs: unique item instances for pickups, merchant stock, and trade flows.
// Bible reference: ARCHITECTURE.md inventory kernel, PRD Sprint 1 FR-05, Sprint 2 FR-03, Sprint 3 hardening.
namespace EmberCrpg.Simulation.Inventory
{
    /// <summary>Static item factories for the slice inventory and merchant loop.</summary>
    public static class SliceItemCatalog
    {
        public const string EmberShardTemplateId = "ember_shard";
        public const string GateWritTemplateId = "gate_writ";

        public static InventoryItem CreateEmberShard(ItemInstanceSequence itemIds)
        {
            return new InventoryItem(itemIds.TakeNext(), EmberShardTemplateId, "Ember Shard", 1);
        }

        public static InventoryItem CreateGateWrit(ItemInstanceSequence itemIds)
        {
            return new InventoryItem(itemIds.TakeNext(), GateWritTemplateId, "Gate Writ", 1);
        }
    }
}
