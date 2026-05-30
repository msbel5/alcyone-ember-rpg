// Design note:
// CombatDefenseIntent compresses active defensive actions into a pure damage-pipeline input.
// Inputs: scheduler-active block/dodge windows or tests.
// Outputs: armor-class and mitigation adjustments without presentation dependencies.
// Bible reference: MASTER_MECHANICS_BIBLE.md §8-§10, Sprint 4 Phase 2 RTWP foundation.
namespace EmberCrpg.Domain.Combat
{
    /// <summary>Defensive posture applied while resolving an incoming weapon hit.</summary>
    public enum CombatDefenseIntent
    {
        None = 0,
        Blocking = 1,
        Dodging = 2,
    }
}
