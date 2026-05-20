using System;
using EmberCrpg.Simulation.Rng;

namespace EmberCrpg.Simulation.Combat
{
    /// <summary>
    /// Deterministic damage roll with armor mitigation. Faz 7 Atom 6.
    /// </summary>
    public sealed class CombatDamageService
    {
        public int Roll(int baseDamage, int damageBandWidth, int armor, IDeterministicRng rng)
        {
            if (baseDamage < 0) throw new ArgumentOutOfRangeException(nameof(baseDamage));
            if (damageBandWidth < 0) throw new ArgumentOutOfRangeException(nameof(damageBandWidth));
            if (armor < 0) throw new ArgumentOutOfRangeException(nameof(armor));

            var rolled = baseDamage;
            if (damageBandWidth > 0 && rng != null)
                rolled += rng.NextInt(damageBandWidth + 1);
            var mitigated = rolled - armor;
            return mitigated < 0 ? 0 : mitigated;
        }
    }
}
