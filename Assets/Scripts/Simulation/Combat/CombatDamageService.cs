using System;
using EmberCrpg.Simulation.Rng;

namespace EmberCrpg.Simulation.Combat
{
    /// <summary>
    /// Deterministic damage roll with armor mitigation. Phase 7 Atom 6.
    /// </summary>
    public sealed class CombatDamageService
    {
        public int Roll(int baseDamage, int damageBandWidth, int armor, IDeterministicRng rng)
        {
            if (baseDamage < 0) throw new ArgumentOutOfRangeException(nameof(baseDamage));
            if (damageBandWidth < 0) throw new ArgumentOutOfRangeException(nameof(damageBandWidth));
            if (armor < 0) throw new ArgumentOutOfRangeException(nameof(armor));

            // Codex audit (sixth pass A-P3 #12): the previous code silently
            // dropped damage variance when rng was null but bandWidth > 0,
            // returning baseDamage without the random band component the
            // caller expected. Make the contract explicit: if bandWidth > 0
            // the caller must supply a deterministic rng.
            if (damageBandWidth > 0 && rng == null)
                throw new ArgumentNullException(nameof(rng), "rng is required when damageBandWidth > 0");
            var rolled = baseDamage;
            if (damageBandWidth > 0)
                rolled += rng.NextInt(damageBandWidth + 1);
            var mitigated = rolled - armor;
            return mitigated < 0 ? 0 : mitigated;
        }
    }
}
