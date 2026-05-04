using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Magic;

// Design note:
// SpellExecutionResult captures the full deterministic Sprint 5 spell pipeline outcome.
// Inputs: cast-preparation/commit, target-routing, and effect-resolution stages.
// Outputs: success flag, failure stage, nested stage results, routed target, applied totals, and text.
// Bible reference: EMBER_VISION_BIBLE.md §3 Layer 3 and MASTER_MECHANICS_BIBLE.md §14-§15.
namespace EmberCrpg.Simulation.Magic
{
    /// <summary>Result of one end-to-end spell execution request.</summary>
    public sealed class SpellExecutionResult
    {
        private SpellExecutionResult(
            bool success,
            SpellExecutionError error,
            SpellDefinition spell,
            ActorRecord routedTarget,
            SpellCastResult castResult,
            SpellTargetValidationResult targetValidationResult,
            SpellEffectResolutionResult effectResolutionResult,
            string message)
        {
            Success = success;
            Error = error;
            Spell = spell;
            RoutedTarget = routedTarget;
            CastResult = castResult;
            TargetValidationResult = targetValidationResult;
            EffectResolutionResult = effectResolutionResult;
            Message = message;
        }

        public bool Success { get; }
        public SpellExecutionError Error { get; }
        public SpellDefinition Spell { get; }
        public ActorRecord RoutedTarget { get; }
        public SpellCastResult CastResult { get; }
        public SpellTargetValidationResult TargetValidationResult { get; }
        public SpellEffectResolutionResult EffectResolutionResult { get; }
        public string Message { get; }

        public int ManaSpent => CastResult == null ? 0 : CastResult.ManaSpent;
        public int AppliedEffectCount => EffectResolutionResult == null ? 0 : EffectResolutionResult.AppliedEffectCount;
        public int TotalDamage => EffectResolutionResult == null ? 0 : EffectResolutionResult.TotalDamage;
        public int TotalHealing => EffectResolutionResult == null ? 0 : EffectResolutionResult.TotalHealing;
        public int TotalRestoredFatigue => EffectResolutionResult == null ? 0 : EffectResolutionResult.TotalRestoredFatigue;

        public static SpellExecutionResult Ok(
            SpellCastResult castResult,
            SpellTargetValidationResult targetValidationResult,
            SpellEffectResolutionResult effectResolutionResult,
            string message)
        {
            return new SpellExecutionResult(
                true,
                SpellExecutionError.None,
                effectResolutionResult?.Spell ?? targetValidationResult?.Spell ?? castResult?.Spell,
                targetValidationResult?.RoutedTarget,
                castResult,
                targetValidationResult,
                effectResolutionResult,
                message);
        }

        public static SpellExecutionResult Fail(
            SpellExecutionError error,
            SpellDefinition spell,
            ActorRecord routedTarget,
            SpellCastResult castResult,
            SpellTargetValidationResult targetValidationResult,
            SpellEffectResolutionResult effectResolutionResult,
            string message)
        {
            return new SpellExecutionResult(
                false,
                error,
                spell ?? effectResolutionResult?.Spell ?? targetValidationResult?.Spell ?? castResult?.Spell,
                routedTarget,
                castResult,
                targetValidationResult,
                effectResolutionResult,
                message);
        }
    }
}
