// Design note:
// EquipmentSlot names the tiny Sprint 4 equipment surface without leaking UI concerns into rules.
// Inputs: equipment-capable item definitions and equip/unequip requests.
// Outputs: deterministic slot ids for domain state, save/load, and presentation labels.
// Bible reference: ARCHITECTURE.md inventory/equipment kernel direction, Sprint 4 Faz 4 roadmap.
namespace EmberCrpg.Domain.Inventory
{
    /// <summary>Supported equipment slots for the current vertical slice.</summary>
    public enum EquipmentSlot
    {
        None = 0,
        Weapon = 1,
    }
}
