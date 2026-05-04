// Design note:
// SpellCastRollError gives deterministic refusal categories for the Sprint 5 spell-cast roll seam.
// Inputs: invalid caster, invalid spell, missing seeded RNG, or upstream chance failure.
// Outputs: stable outcome code so callers do not parse text.
// Bible reference: docs/mechanics/ARCHITECTURE.md §3.3 Tier 3 seeded rolls (RollResult shape).
namespace EmberCrpg.Simulation.Magic
{
    /// <summary>Stable outcome code for one deterministic spell-cast roll.</summary>
    public enum SpellCastRollError
    {
        None = 0,
        InvalidCaster = 1,
        InvalidSpell = 2,
        InvalidRng = 3,
        ChanceCalculationFailed = 4,
    }
}
