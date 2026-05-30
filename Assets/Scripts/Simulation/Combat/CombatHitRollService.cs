using EmberCrpg.Simulation.Rng;

namespace EmberCrpg.Simulation.Combat
{
    /// <summary>
    /// Deterministic hit roll: returns true when roll &lt;= accuracy - dodge.
    /// Pure, seeded. Phase 7 Atom 5.
    /// </summary>
    public sealed class CombatHitRollService
    {
        public bool Roll(int attackerAccuracy, int defenderDodge, IDeterministicRng rng)
        {
            if (rng == null) return false;
            // Codex audit (sixth pass A-P3 #11): always consume one
            // RollPercent so the RNG sequence does not depend on the chance
            // value. Previously toggling a single stat from 99→100 or 1→0
            // skipped the roll and reshuffled every subsequent action's
            // outcome — authoring tools that diffed a save's combat log
            // could see noise. Roll first, decide after.
            var roll = rng.RollPercent();
            var chance = attackerAccuracy - defenderDodge;
            if (chance >= 100) return true;
            if (chance <= 0) return false;
            return roll <= chance;
        }
    }
}
