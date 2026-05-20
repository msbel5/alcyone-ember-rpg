using EmberCrpg.Simulation.Rng;

namespace EmberCrpg.Simulation.Combat
{
    /// <summary>
    /// Deterministic hit roll: returns true when roll &lt;= accuracy - dodge.
    /// Pure, seeded. Faz 7 Atom 5.
    /// </summary>
    public sealed class CombatHitRollService
    {
        public bool Roll(int attackerAccuracy, int defenderDodge, IDeterministicRng rng)
        {
            if (rng == null) return false;
            var chance = attackerAccuracy - defenderDodge;
            if (chance >= 100) return true;
            if (chance <= 0) return false;
            var roll = rng.RollPercent();
            // PR#163 bot review fix: RollPercent returns 1..100, so an inclusive
            // comparison is required for chance=N to yield exactly N% hit rate.
            // With the old `<` operator, chance=50 only hit on rolls 1..49 (49%).
            return roll <= chance;
        }
    }
}
