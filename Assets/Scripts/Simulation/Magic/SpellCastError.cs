// Design note:
// SpellCastError gives deterministic refusal categories for the Sprint 5 casting service.
// Inputs: validation failures from SpellCastingService.
// Outputs: stable non-localized error ids to keep tests and future UI off brittle strings.
// Bible reference: EMBER_VISION_BIBLE.md §3 Layer 3, MASTER_MECHANICS_BIBLE.md §14 (mana <= cost guard).
namespace EmberCrpg.Simulation.Magic
{
    /// <summary>Stable outcome code for a spell cast attempt.</summary>
    public enum SpellCastError
    {
        None = 0,
        SpellNotFound = 1,
        SpellNotKnown = 2,
        InsufficientMana = 3,
        InvalidCaster = 4,
        SpellOnCooldown = 5,
    }
}
