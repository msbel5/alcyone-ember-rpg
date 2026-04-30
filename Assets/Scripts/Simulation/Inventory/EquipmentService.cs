using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.World;

// Design note:
// EquipmentService owns deterministic equip/unequip swaps between backpack and equipped slots.
// Inputs: exact item ids plus pure world state.
// Outputs: stable slot assignment with reversible inventory mutations and no Unity dependency.
// Bible reference: ARCHITECTURE.md equipment tier, Sprint 3 expanded item state.
namespace EmberCrpg.Simulation.Inventory
{
    /// <summary>Rules-based weapon and armor swapping for the slice player.</summary>
    public sealed class EquipmentService
    {
        public string Equip(SliceWorldState world, ItemId itemId)
        {
            InventoryItem item;
            if (!world.PlayerInventory.TryTakeById(itemId, out item))
                return "That item is not available to equip.";
            if (!item.CanEquip)
            {
                world.PlayerInventory.TryAdd(item);
                return item.DisplayName + " cannot be equipped.";
            }

            var displaced = world.PlayerEquipment == null ? null : world.PlayerEquipment.Get(item.EquipSlot);
            if (world.PlayerEquipment == null)
                world.PlayerEquipment = new EquipmentState();

            world.PlayerEquipment.Set(item);
            if (displaced != null && !world.PlayerInventory.TryAdd(displaced))
            {
                world.PlayerEquipment.Set(displaced);
                world.PlayerInventory.TryAdd(item);
                return "Your inventory is too full to swap equipment right now.";
            }

            return displaced == null
                ? "You equip " + item.DisplayName + "."
                : "You equip " + item.DisplayName + " and stow " + displaced.DisplayName + ".";
        }

        public string Unequip(SliceWorldState world, EquipmentSlot slot)
        {
            if (world.PlayerEquipment == null || world.PlayerEquipment.Get(slot) == null)
                return "Nothing is equipped in that slot.";

            var item = world.PlayerEquipment.Get(slot);
            if (!world.PlayerInventory.TryAdd(item))
                return "Your inventory is too full to stow " + item.DisplayName + ".";

            world.PlayerEquipment.Clear(slot);
            return "You stow " + item.DisplayName + ".";
        }
    }
}
