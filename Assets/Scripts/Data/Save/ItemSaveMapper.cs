using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;

// Design note:
// ItemSaveMapper isolates item and pickup save/load field translation for Sprint 1 JSON persistence.
// Inputs: pure item or pickup records and matching DTOs.
// Outputs: round-trippable item and pickup save snapshots.
// Bible reference: PRD FR-05/FR-06.
namespace EmberCrpg.Data.Save
{
    /// <summary>Field mapper between item-domain records and save DTOs.</summary>
    public static class ItemSaveMapper
    {
        public static ItemSaveData ToData(InventoryItem item)
        {
            return new ItemSaveData
            {
                id = (long)item.Id.Value,
                templateId = item.TemplateId,
                displayName = item.DisplayName,
                quantity = item.Quantity,
                equipmentSlot = (int)item.EquipmentSlot,
                equipmentSlotCode = item.EquipmentSlot.Code,
                accuracyBonus = item.AccuracyBonus,
                damageBonus = item.DamageBonus,
            };
        }

        public static InventoryItem ToItem(ItemSaveData item)
        {
            var slot = string.IsNullOrWhiteSpace(item.equipmentSlotCode)
                ? EquipmentSlot.FromLegacyValue(item.equipmentSlot)
                : EquipmentSlot.FromCode(item.equipmentSlotCode);
            return new InventoryItem(new ItemId((ulong)item.id), item.templateId, item.displayName, item.quantity, slot, item.accuracyBonus, item.damageBonus);
        }

        public static PickupSaveData ToData(RoomPickup pickup)
        {
            return new PickupSaveData
            {
                item = ToData(pickup.Item),
                positionX = pickup.Position.X,
                positionY = pickup.Position.Y,
                collected = pickup.IsCollected,
            };
        }

        public static RoomPickup ToPickup(PickupSaveData pickup)
        {
            var roomPickup = new RoomPickup(ToItem(pickup.item), new GridPosition(pickup.positionX, pickup.positionY));
            if (pickup.collected)
                roomPickup.Collect();
            return roomPickup;
        }
    }
}
