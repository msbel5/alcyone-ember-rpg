using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;

// Design note:
// WorldItemCatalog centralizes the tiny deterministic item set used by the vertical slice.
// Inputs: none beyond fixed ids/templates chosen for Sprint 1 and Sprint 2 interactions.
// Outputs: reusable item factories for pickups, merchant stock, and trade costs.
// Bible reference: ARCHITECTURE.md inventory kernel, PRD Sprint 1 FR-05, Sprint 2 FR-03.
namespace EmberCrpg.Simulation.Inventory
{
    /// <summary>Static item factories for the slice inventory and merchant loop.</summary>
    public static class WorldItemCatalog
    {
        public const string EmberShardTemplateId = "ember_shard";
        public const string GateWritTemplateId = "gate_writ";
        public const string AshTrainingBladeTemplateId = "ash_training_blade";

        public static InventoryItem CreateEmberShard()
        {
            return new InventoryItem(new ItemId(1001), EmberShardTemplateId, "Ember Shard", 1);
        }

        public static InventoryItem CreateGateWrit()
        {
            return new InventoryItem(new ItemId(2001), GateWritTemplateId, "Gate Writ", 1);
        }

        public static InventoryItem CreateAshTrainingBlade()
        {
            return new InventoryItem(new ItemId(3001), AshTrainingBladeTemplateId, "Ash Training Blade", 1, EquipmentSlot.Weapon, 5, 2);
        }
    }
}
