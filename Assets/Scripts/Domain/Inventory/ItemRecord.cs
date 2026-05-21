using System;
using EmberCrpg.Domain.Core;

// Design note:
// ItemRecord is the Faz 1 pure-Domain payload for an item known to the world:
// MATTER-box material + crafting quality + (optional) equipment slot, keyed by ItemId.
// Inputs: ItemId handle, ItemMaterial substance, ItemQuality tier, EquipmentSlot slot.
// Outputs: immutable record consumed by ItemStore in the next Faz 1 PR; no Unity,
// no I/O, no serialization concerns. Mirrors SiteRecord's defensive constructor
// pattern so invariants are pinned at construction; EquipmentSlot.None remains
// the legal "non-equipment" value, just like InventoryItem.
// Atom-map ref: docs/sprint-faz-1-atom-map.md ItemStore sub-area.
namespace EmberCrpg.Domain.Inventory
{
    /// <summary>Pure record describing an item registry entry by id, material, quality, and slot.</summary>
    public sealed class ItemRecord
    {
        public ItemRecord(ItemId id, ItemMaterial material, ItemQuality quality, EquipmentSlot slot)
        {
            if (id.IsEmpty)
                throw new ArgumentException("ItemId.Empty cannot back an ItemRecord.", nameof(id));
            if (material == ItemMaterial.None)
                throw new ArgumentException("ItemMaterial.None is reserved as the empty sentinel.", nameof(material));
            if (quality == ItemQuality.None)
                throw new ArgumentException("ItemQuality.None is reserved as the empty sentinel.", nameof(quality));

            Id = id;
            Material = material;
            Quality = quality;
            Slot = slot;
        }

        public ItemId Id { get; }
        public ItemMaterial Material { get; }
        public ItemQuality Quality { get; }
        public EquipmentSlot Slot { get; }

        /// <summary>True when this record describes an equippable item (slot is not None).</summary>
        public bool IsEquipment
        {
            get { return Slot != EquipmentSlot.None; }
        }
    }
}
