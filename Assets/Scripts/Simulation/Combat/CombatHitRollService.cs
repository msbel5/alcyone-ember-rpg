using EmberCrpg.Simulation.Rng;

namespace EmberCrpg.Simulation.Combat
{
    /// <summary>
    /// Deterministic hit roll: returns true when (rng % 100) < accuracy - dodge.
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
            return roll < chance;
        }
    }
}
