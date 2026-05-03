using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Magic;

// Design note:
// SpellTargetValidationResult is the narrow response object for routing a spell to a concrete actor.
// Inputs: SpellTargetValidator decisions about spell/caster/target combination.
// Outputs: success flag, deterministic error code, the routed target ActorRecord, and player-facing text.
// Bible reference: EMBER_VISION_BIBLE.md §3 Layer 3, MASTER_MECHANICS_BIBLE.md §14 targetMultiplier.
namespace EmberCrpg.Simulation.Magic
{
    /// <summary>Result of one spell target validation/routing request.</summary>
    public sealed class SpellTargetValidationResult
    {
        private SpellTargetValidationResult(
            bool success,
            SpellTargetValidationError error,
            SpellDefinition spell,
            ActorRecord routedTarget,
            string message)
        {
            Success = success;
            Error = error;
            Spell = spell;
            RoutedTarget = routedTarget;
            Message = message;
        }

        public bool Success { get; }
        public SpellTargetValidationError Error { get; }
        public SpellDefinition Spell { get; }
        public ActorRecord RoutedTarget { get; }
        public string Message { get; }

        public static SpellTargetValidationResult Ok(SpellDefinition spell, ActorRecord routedTarget, string message)
        {
            return new SpellTargetValidationResult(true, SpellTargetValidationError.None, spell, routedTarget, message);
        }

        public static SpellTargetValidationResult Fail(SpellTargetValidationError error, SpellDefinition spell, string message)
        {
            return new SpellTargetValidationResult(false, error, spell, null, message);
        }
    }
}
