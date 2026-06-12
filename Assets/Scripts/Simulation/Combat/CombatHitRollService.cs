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
            // v0.3 PLAYTEST ("nadiren vuruyorum", 104 swings per kill): the raw accuracy-dodge difference
            // pinned a fresh player to the 5% floor against any dodgy enemy — statistically unplayable.
            // Daggerfall's curve is BASE 50% shifted by the stat diff: evenly matched fighters land half
            // their swings, mismatches shift the odds without erasing them. Floor 15 keeps weak attackers
            // dangerous, ceiling 95 keeps misses possible. (Supersedes the looptest3 5% graze floor.)
            var chance = 50 + attackerAccuracy - defenderDodge;
            if (chance < 15) chance = 15;
            else if (chance > 95) chance = 95;
            return roll <= chance;
        }
    }
}
