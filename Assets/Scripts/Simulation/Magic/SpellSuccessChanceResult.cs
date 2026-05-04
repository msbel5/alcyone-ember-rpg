using EmberCrpg.Domain.Magic;

// Design note:
// SpellSuccessChanceResult carries the deterministic Sprint 5 cast-probability breakdown.
// Inputs: SpellSuccessChanceService base chance, attribute bonuses, and difficulty penalties.
// Outputs: stable numeric breakdown plus text for UI, DM, or future seeded-roll callers.
// Bible reference: docs/mechanics/ARCHITECTURE.md §3.2 ComputeSpellSuccessChance,
// MASTER_MECHANICS_BIBLE.md §14 OpenMW casting note.
namespace EmberCrpg.Simulation.Magic
{
    /// <summary>Result of one spell success chance calculation.</summary>
    public sealed class SpellSuccessChanceResult
    {
        private SpellSuccessChanceResult(
            bool success,
            SpellSuccessChanceError error,
            SpellDefinition spell,
            int chancePercent,
            int baseChance,
            int primaryAttributeBonus,
            int secondaryAttributeBonus,
            int manaCostPenalty,
            int effectComplexityPenalty,
            int targetPenalty,
            int rangePenalty,
            string message)
        {
            Success = success;
            Error = error;
            Spell = spell;
            ChancePercent = chancePercent;
            BaseChance = baseChance;
            PrimaryAttributeBonus = primaryAttributeBonus;
            SecondaryAttributeBonus = secondaryAttributeBonus;
            ManaCostPenalty = manaCostPenalty;
            EffectComplexityPenalty = effectComplexityPenalty;
            TargetPenalty = targetPenalty;
            RangePenalty = rangePenalty;
            Message = message;
        }

        public bool Success { get; }
        public SpellSuccessChanceError Error { get; }
        public SpellDefinition Spell { get; }
        public int ChancePercent { get; }
        public int BaseChance { get; }
        public int PrimaryAttributeBonus { get; }
        public int SecondaryAttributeBonus { get; }
        public int ManaCostPenalty { get; }
        public int EffectComplexityPenalty { get; }
        public int TargetPenalty { get; }
        public int RangePenalty { get; }
        public string Message { get; }

        public static SpellSuccessChanceResult Ok(
            SpellDefinition spell,
            int chancePercent,
            int baseChance,
            int primaryAttributeBonus,
            int secondaryAttributeBonus,
            int manaCostPenalty,
            int effectComplexityPenalty,
            int targetPenalty,
            int rangePenalty,
            string message)
        {
            return new SpellSuccessChanceResult(
                true,
                SpellSuccessChanceError.None,
                spell,
                chancePercent,
                baseChance,
                primaryAttributeBonus,
                secondaryAttributeBonus,
                manaCostPenalty,
                effectComplexityPenalty,
                targetPenalty,
                rangePenalty,
                message);
        }

        public static SpellSuccessChanceResult Fail(SpellSuccessChanceError error, SpellDefinition spell, string message)
        {
            return new SpellSuccessChanceResult(false, error, spell, 0, 0, 0, 0, 0, 0, 0, 0, message);
        }
    }
}
