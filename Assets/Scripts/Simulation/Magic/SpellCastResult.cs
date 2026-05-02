using EmberCrpg.Domain.Magic;

// Design note:
// SpellCastResult is the narrow response object for one spell cast attempt.
// Inputs: SpellCastingService validation and mana-spend decisions.
// Outputs: success flag, deterministic error code, mana spent, and player-facing text.
// Bible reference: EMBER_VISION_BIBLE.md §3 Layer 3, MASTER_MECHANICS_BIBLE.md §14 cost gating.
namespace EmberCrpg.Simulation.Magic
{
    /// <summary>Result of one cast spell request.</summary>
    public sealed class SpellCastResult
    {
        private SpellCastResult(bool success, SpellCastError error, SpellDefinition spell, int manaSpent, string message)
        {
            Success = success;
            Error = error;
            Spell = spell;
            ManaSpent = manaSpent;
            Message = message;
        }

        public bool Success { get; }
        public SpellCastError Error { get; }
        public SpellDefinition Spell { get; }
        public int ManaSpent { get; }
        public string Message { get; }

        public static SpellCastResult Ok(SpellDefinition spell, int manaSpent, string message)
        {
            return new SpellCastResult(true, SpellCastError.None, spell, manaSpent, message);
        }

        public static SpellCastResult Fail(SpellCastError error, SpellDefinition spell, string message)
        {
            return new SpellCastResult(false, error, spell, 0, message);
        }
    }
}
