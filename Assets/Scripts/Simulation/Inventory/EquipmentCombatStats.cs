// Design note:
// EquipmentCombatStats is the small additive combat surface gear can affect in Sprint 4.
// Inputs: equipped item definitions resolved from inventory identity.
// Outputs: deterministic accuracy and damage bonuses for combat services and HUD text.
// Bible reference: Sprint 4 Phase 4 acceptance that equipment changes a stat-testable mechanic.
namespace EmberCrpg.Simulation.Inventory
{
    /// <summary>Additive combat bonuses granted by equipped items.</summary>
    public readonly struct EquipmentCombatStats
    {
        public EquipmentCombatStats(int accuracyBonus, int damageBonus)
        {
            AccuracyBonus = accuracyBonus;
            DamageBonus = damageBonus;
        }

        public int AccuracyBonus { get; }
        public int DamageBonus { get; }
    }
}
