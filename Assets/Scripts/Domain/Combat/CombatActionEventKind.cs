// Design note:
// CombatActionEventKind names deterministic action-timeline milestones emitted by the RTWP scheduler.
// Inputs: queued action timing thresholds crossed by simulation ticks.
// Outputs: presentation/test events without Unity animation coupling.
// Bible reference: ARCHITECTURE.md RTWP combat lock, Sprint 4 Phase 2 real-time foundation.
namespace EmberCrpg.Domain.Combat
{
    /// <summary>Timeline milestone for a queued combat action.</summary>
    public enum CombatActionEventKind
    {
        Activated = 0,
        Completed = 1,
    }
}
