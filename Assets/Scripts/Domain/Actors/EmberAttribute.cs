// Design note:
// EmberAttribute defines Sprint 1's six-stat kernel only.
// Inputs: attribute selectors from pure Domain or Simulation code.
// Outputs: stable enum values for stat lookup.
// Bible reference: MASTER_MECHANICS_BIBLE.md §1 Ember recommendation.
namespace EmberCrpg.Domain.Actors
{
    /// <summary>Ember's six primary attributes for the deterministic slice.</summary>
    public enum EmberAttribute
    {
        Mig = 0,
        Agi = 1,
        End = 2,
        Mnd = 3,
        Ins = 4,
        Pre = 5,
    }
}
