// Design note:
// EquipmentSlot keeps Sprint 3's first weapon/armor separation tiny and deterministic.
// Inputs: equip-capable inventory items and pure simulation services.
// Outputs: stable slot keys for save/load, DM queries, and swap rules.
// Bible reference: ARCHITECTURE.md equipment tier, Sprint 3 expanded item state.
namespace EmberCrpg.Domain.Inventory
{
    /// <summary>Minimal equipment slots needed by the slice.</summary>
    public enum EquipmentSlot
    {
        None = 0,
        Weapon = 1,
        Armor = 2,
    }
}
