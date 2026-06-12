using System;
using EmberCrpg.Domain.Magic;

// Design note:
// SpellCostCalculator estimates deterministic mana cost from spell effects and target shape.
// Inputs: Unity-free SpellDefinition / SpellEffectSpec data.
// Outputs: integer mana estimate using a simple Σ(magnitude + duration component) * target multiplier formula.
// Bible reference: MASTER_MECHANICS_BIBLE.md §14 corrected cost shape: sum per-effect components, then apply target multiplier.
namespace EmberCrpg.Simulation.Magic
{
    /// <summary>Pure deterministic estimator for spell mana cost.</summary>
    public sealed class SpellCostCalculator
    {
        private const int MultiplierDenominator = 2;
        private const int DurationTicksPerCostPoint = 10;

        public int EstimateTotalManaCost(SpellDefinition spell)
        {
            if (spell == null)
                throw new ArgumentNullException(nameof(spell));

            var effectCostTotal = 0;
            for (var i = 0; i < spell.Effects.Count; i++)
            {
                effectCostTotal += EstimateEffectCost(spell.Effects[i]);
            }

            return ApplyTargetMultiplier(effectCostTotal, spell.TargetKind);
        }

        public int EstimateEffectCost(SpellEffectSpec effect)
        {
            // F28: open-set world codes ("light"/"haste"/"recall") measure magnitude in WORLD
            // units (minutes of glow, seconds of haste) — not vitals points. Their mana price is
            // authored in the catalog, not derived, so the estimator prices them at zero.
            if (!effect.Kind.IsCanonical)
                return 0;
            return effect.Magnitude + CalculateDurationComponent(effect.DurationTicks);
        }

        public int ApplyTargetMultiplier(int effectCostTotal, SpellTargetKind targetKind)
        {
            if (effectCostTotal < 0)
                throw new ArgumentOutOfRangeException(nameof(effectCostTotal), effectCostTotal, "Effect cost total must be zero or positive.");

            var numerator = GetTargetMultiplierNumerator(targetKind);
            return DivideRoundedUp(effectCostTotal * numerator, MultiplierDenominator);
        }

        public int GetTargetMultiplierNumerator(SpellTargetKind targetKind)
        {
            if (targetKind == SpellTargetKind.CasterSelf || targetKind == SpellTargetKind.Touch)
                return 2;
            if (targetKind == SpellTargetKind.SingleTarget)
                return 3;
            if (targetKind == SpellTargetKind.AreaAroundCaster)
                return 4;
            if (targetKind == SpellTargetKind.AreaAtRange)
                return 5;

            throw new ArgumentOutOfRangeException(nameof(targetKind), targetKind, "Spell target kind must be a real target shape.");
        }

        private static int CalculateDurationComponent(int durationTicks)
        {
            return DivideRoundedUp(durationTicks, DurationTicksPerCostPoint);
        }

        private static int DivideRoundedUp(int numerator, int denominator)
        {
            if (numerator == 0)
                return 0;
            return (numerator + denominator - 1) / denominator;
        }
    }
}
