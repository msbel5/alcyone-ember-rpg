using EmberCrpg.Domain.Magic;

// Design note:
// ShieldBuffApplicationResult is the narrow response object for writing timed shield buffs
// from a successful spell cast into a ShieldBuffState container.
// Inputs: SpellEffectResolutionService.ApplyShieldBuffs validation and buff entries written.
// Outputs: success flag, deterministic error code, count of buff effects written, applied
// magnitude/duration totals, and a textual message for trace/HUD use.
// Bible reference: EMBER_VISION_BIBLE.md §3 Layer 3, MASTER_MECHANICS_BIBLE.md §15 effect opcodes.
namespace EmberCrpg.Simulation.Magic
{
    /// <summary>Result of applying one successful cast's timed shield-buff effects.</summary>
    public sealed class ShieldBuffApplicationResult
    {
        private ShieldBuffApplicationResult(
            bool success,
            SpellEffectResolutionError error,
            SpellDefinition spell,
            int appliedBuffCount,
            int totalAppliedMagnitude,
            int totalAppliedDurationTicks,
            string message)
        {
            Success = success;
            Error = error;
            Spell = spell;
            AppliedBuffCount = appliedBuffCount;
            TotalAppliedMagnitude = totalAppliedMagnitude;
            TotalAppliedDurationTicks = totalAppliedDurationTicks;
            Message = message;
        }

        public bool Success { get; }
        public SpellEffectResolutionError Error { get; }
        public SpellDefinition Spell { get; }
        public int AppliedBuffCount { get; }
        public int TotalAppliedMagnitude { get; }
        public int TotalAppliedDurationTicks { get; }
        public string Message { get; }

        public static ShieldBuffApplicationResult Ok(
            SpellDefinition spell,
            int appliedBuffCount,
            int totalAppliedMagnitude,
            int totalAppliedDurationTicks,
            string message)
        {
            return new ShieldBuffApplicationResult(
                true,
                SpellEffectResolutionError.None,
                spell,
                appliedBuffCount,
                totalAppliedMagnitude,
                totalAppliedDurationTicks,
                message);
        }

        public static ShieldBuffApplicationResult Fail(
            SpellEffectResolutionError error,
            SpellDefinition spell,
            string message)
        {
            return new ShieldBuffApplicationResult(false, error, spell, 0, 0, 0, message);
        }
    }
}
