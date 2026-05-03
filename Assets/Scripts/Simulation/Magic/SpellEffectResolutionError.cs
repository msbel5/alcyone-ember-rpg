// Design note:
// SpellEffectResolutionError gives deterministic refusal categories for instantaneous spell effects.
// Inputs: SpellEffectResolutionService validation failures after a successful cast gate.
// Outputs: stable non-localized error ids to keep tests and future UI off brittle strings.
// Bible reference: EMBER_VISION_BIBLE.md §3 Layer 3, MASTER_MECHANICS_BIBLE.md §15 effect opcodes.
namespace EmberCrpg.Simulation.Magic
{
    /// <summary>Stable outcome code for spell effect resolution.</summary>
    public enum SpellEffectResolutionError
    {
        None = 0,
        InvalidCast = 1,
        InvalidTarget = 2,
        NonInstantaneousEffect = 3,
        UnsupportedEffect = 4,
    }
}
