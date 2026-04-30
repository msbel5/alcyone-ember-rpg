using System.Linq;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Inventory;

// Design note:
// InventoryEquipmentFormatter turns inventory and equipment identity into deterministic player-facing text.
// Inputs: current world inventory/equipment state.
// Outputs: compact HUD and inspect-screen strings with no Unity mutation.
// Bible reference: Sprint 4 Faz 4 player-facing inventory/equipment UI acceptance.
namespace EmberCrpg.Presentation.Slice
{
    /// <summary>Formats inventory and equipped gear for HUD and inspect commands.</summary>
    public static class InventoryEquipmentFormatter
    {
        public static string FormatEquipmentLine(SliceWorldState world)
        {
            var weapon = FindEquipped(world, EquipmentSlot.Weapon);
            if (weapon == null)
                return "Weapon: none";

            return $"Weapon: {weapon.DisplayName} (+{weapon.AccuracyBonus} ACC, +{weapon.DamageBonus} DMG)";
        }

        public static string FormatInspect(SliceWorldState world)
        {
            var itemLines = world.PlayerInventory.Items
                .Select((item, index) => FormatItemLine(world, item, index + 1))
                .ToArray();
            var items = itemLines.Length == 0 ? "  (empty)" : string.Join("\n", itemLines);
            return $@"Inventory {world.PlayerInventory.Items.Count}/{world.PlayerInventory.Capacity}
{items}
Equipped
  {FormatEquipmentLine(world)}";
        }

        private static string FormatItemLine(SliceWorldState world, InventoryItem item, int index)
        {
            var equipped = world.PlayerEquipment.IsEquipped(item.Id) ? " [equipped]" : string.Empty;
            var equipment = item.IsEquipment ? $" ({EquipmentService.GetSlotLabel(item.EquipmentSlot)}, +{item.AccuracyBonus} ACC, +{item.DamageBonus} DMG)" : string.Empty;
            return $"  {index}. {item.DisplayName} x{item.Quantity}{equipment}{equipped}";
        }

        private static InventoryItem FindEquipped(SliceWorldState world, EquipmentSlot slot)
        {
            return world.PlayerInventory.FindById(world.PlayerEquipment.GetEquippedItemId(slot));
        }
    }
}
