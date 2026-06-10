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
            // LOOP-PROOF finding (looptest3): the raw difference made any pairing with dodge >= accuracy a
            // PERMANENT 0% — a fresh player literally could not hit an outlaw (60/60 misses in the full-loop
            // proof). Classic floor/ceiling keeps every swing a gamble: 5% graze floor, 95% whiff ceiling.
            if (chance < 5) chance = 5;
            else if (chance > 95) chance = 95;
            return roll <= chance;
        }
    }
}
