// Design note:
// SpellTargetValidationError gives deterministic refusal categories for the Sprint 5 target/route gate.
// Inputs: SpellTargetValidator validation failures before any effect resolution.
// Outputs: stable non-localized error ids to keep tests and future UI off brittle strings.
// Bible reference: EMBER_VISION_BIBLE.md §3 Layer 3, MASTER_MECHANICS_BIBLE.md §14 targetMultiplier.
namespace EmberCrpg.Simulation.Magic
{
    /// <summary>Stable outcome code for a spell target validation attempt.</summary>
    public enum SpellTargetValidationError
    {
        None = 0,
        InvalidSpell = 1,
        InvalidCaster = 2,
        InvalidTarget = 3,
        TargetNotAdjacent = 4,
        WrongTargetForSelfSpell = 5,
        UnsupportedTargetKind = 6,
        TargetOutOfRange = 7,
    }
}
