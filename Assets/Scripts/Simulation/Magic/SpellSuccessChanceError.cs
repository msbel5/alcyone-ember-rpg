// SpellSuccessChanceError gives deterministic refusal categories for the Sprint 5 spell success
// probability service.
// Inputs: invalid caster or spell requests before any percentage breakdown is produced.
// Outputs: stable outcome code so callers do not parse text.
// Bible reference: docs/mechanics/ARCHITECTURE.md §3.2 ComputeSpellSuccessChance.
namespace EmberCrpg.Simulation.Magic
{
    /// <summary>Stable outcome code for a spell success chance calculation.</summary>
    public enum SpellSuccessChanceError
    {
        None = 0,
        InvalidCaster = 1,
        InvalidSpell = 2,
    }
}
