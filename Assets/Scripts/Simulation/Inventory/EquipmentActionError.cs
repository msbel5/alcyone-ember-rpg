// Design note:
// EquipmentActionError gives deterministic refusal categories for UI, tests, and future logs.
// Inputs: equip/unequip validation failures.
// Outputs: stable non-localized error ids that avoid brittle string matching in core tests.
// Bible reference: Sprint 4 Faz 4 equipment constraints acceptance.
namespace EmberCrpg.Simulation.Inventory
{
    /// <summary>Stable outcome code for equipment actions.</summary>
    public enum EquipmentActionError
    {
        None = 0,
        ItemNotFound = 1,
        ItemNotEquippable = 2,
        SlotOccupied = 3,
        AlreadyEquipped = 4,
        SlotEmpty = 5,
        InvalidSlot = 6,
    }
}
