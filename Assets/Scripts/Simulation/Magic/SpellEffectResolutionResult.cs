using EmberCrpg.Domain.Magic;

// Design note:
// SpellEffectResolutionResult is the narrow response object for applying immediate spell effects.
// Inputs: SpellEffectResolutionService validation and target vitality mutations.
// Outputs: success flag, deterministic error code, applied counts, vitality delta totals, and text.
// Bible reference: EMBER_VISION_BIBLE.md §3 Layer 3, MASTER_MECHANICS_BIBLE.md §15 effect opcodes.
namespace EmberCrpg.Simulation.Magic
{
    /// <summary>Result of resolving one successful cast's instantaneous effects against a target.</summary>
    public sealed class SpellEffectResolutionResult
    {
        private SpellEffectResolutionResult(
            bool success,
            SpellEffectResolutionError error,
            SpellDefinition spell,
            int appliedEffectCount,
            int totalDamage,
            int totalHealing,
            int totalRestoredFatigue,
            string message)
        {
            Success = success;
            Error = error;
            Spell = spell;
            AppliedEffectCount = appliedEffectCount;
            TotalDamage = totalDamage;
            TotalHealing = totalHealing;
            TotalRestoredFatigue = totalRestoredFatigue;
            Message = message;
        }

        public bool Success { get; }
        public SpellEffectResolutionError Error { get; }
        public SpellDefinition Spell { get; }
        public int AppliedEffectCount { get; }
        public int TotalDamage { get; }
        public int TotalHealing { get; }
        public int TotalRestoredFatigue { get; }
        public string Message { get; }

        public static SpellEffectResolutionResult Ok(
            SpellDefinition spell,
            int appliedEffectCount,
            int totalDamage,
            int totalHealing,
            string message)
        {
            return Ok(spell, appliedEffectCount, totalDamage, totalHealing, 0, message);
        }

        public static SpellEffectResolutionResult Ok(
            SpellDefinition spell,
            int appliedEffectCount,
            int totalDamage,
            int totalHealing,
            int totalRestoredFatigue,
            string message)
        {
            return new SpellEffectResolutionResult(
                true,
                SpellEffectResolutionError.None,
                spell,
                appliedEffectCount,
                totalDamage,
                totalHealing,
                totalRestoredFatigue,
                message);
        }

        public static SpellEffectResolutionResult Fail(SpellEffectResolutionError error, SpellDefinition spell, string message)
        {
            return new SpellEffectResolutionResult(false, error, spell, 0, 0, 0, 0, message);
        }
    }
}
