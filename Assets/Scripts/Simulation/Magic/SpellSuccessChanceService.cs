using System;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Magic;

// Design note:
// SpellSuccessChanceService exposes the deterministic Sprint 5 cast-probability seam described in
// docs/mechanics/ARCHITECTURE.md before any seeded spell roll exists.
// Inputs: caster mental attributes plus immutable spell cost/shape data.
// Outputs: clamped success percentage and explicit breakdown fields without mutating game state.
// Bible reference: docs/mechanics/ARCHITECTURE.md §3.2 ComputeSpellSuccessChance,
// MASTER_MECHANICS_BIBLE.md §14 OpenMW casting note.
namespace EmberCrpg.Simulation.Magic
{
    /// <summary>Pure deterministic probability service for spell-cast success chance.</summary>
    public sealed class SpellSuccessChanceService
    {
        private const int BaseChance = 40;

        public SpellSuccessChanceResult Calculate(ActorRecord caster, SpellDefinition spell)
        {
            if (caster == null)
                return SpellSuccessChanceResult.Fail(SpellSuccessChanceError.InvalidCaster, spell, "A living caster record is required to calculate spell success chance.");
            if (!caster.IsAlive)
                return SpellSuccessChanceResult.Fail(SpellSuccessChanceError.InvalidCaster, spell, $"{caster.Name} cannot calculate spell success chance while incapacitated.");
            if (spell == null)
                return SpellSuccessChanceResult.Fail(SpellSuccessChanceError.InvalidSpell, null, "A spell definition is required to calculate spell success chance.");

            var primaryAttributeBonus = GetPrimaryAttribute(caster.Stats, spell.School) / 2;
            var secondaryAttributeBonus = GetSecondaryAttribute(caster.Stats, spell.School) / 4;
            var manaCostPenalty = spell.ManaCost / 2;
            var effectComplexityPenalty = spell.Effects.Count * 3;
            var targetPenalty = GetTargetPenalty(spell.TargetKind);
            var rangePenalty = spell.RangeInTiles <= 0 ? 0 : spell.RangeInTiles / 2;

            var rawChance = BaseChance
                + primaryAttributeBonus
                + secondaryAttributeBonus
                - manaCostPenalty
                - effectComplexityPenalty
                - targetPenalty
                - rangePenalty;

            var finalChance = ClampPercent(rawChance);
            return SpellSuccessChanceResult.Ok(
                spell,
                finalChance,
                BaseChance,
                primaryAttributeBonus,
                secondaryAttributeBonus,
                manaCostPenalty,
                effectComplexityPenalty,
                targetPenalty,
                rangePenalty,
                $"{caster.Name} has a {finalChance}% deterministic cast chance for {spell.DisplayName}.");
        }

        private static int GetPrimaryAttribute(EmberStatBlock stats, MagicSchool school)
        {
            switch (school)
            {
                case MagicSchool.Restoration:
                case MagicSchool.Illusion:
                case MagicSchool.Mysticism:
                    return stats.Ins;
                case MagicSchool.Destruction:
                case MagicSchool.Alteration:
                case MagicSchool.Conjuration:
                default:
                    return stats.Mnd;
            }
        }

        private static int GetSecondaryAttribute(EmberStatBlock stats, MagicSchool school)
        {
            switch (school)
            {
                case MagicSchool.Restoration:
                case MagicSchool.Illusion:
                case MagicSchool.Mysticism:
                    return stats.Mnd;
                case MagicSchool.Destruction:
                case MagicSchool.Alteration:
                case MagicSchool.Conjuration:
                default:
                    return stats.Ins;
            }
        }

        private static int GetTargetPenalty(SpellTargetKind targetKind)
        {
            switch (targetKind)
            {
                case SpellTargetKind.CasterSelf: return 0;
                case SpellTargetKind.Touch: return 4;
                case SpellTargetKind.SingleTarget: return 7;
                case SpellTargetKind.AreaAroundCaster: return 10;
                case SpellTargetKind.AreaAtRange: return 14;
                default: return 14;
            }
        }

        private static int ClampPercent(int value)
        {
            return Math.Max(5, Math.Min(95, value));
        }
    }
}
