// Design note:
// CombatActionEvent is the scheduler's pure output, suitable for logs, tests, and thin Unity adapters.
// Inputs: timeline thresholds crossed during a simulation tick.
// Outputs: stable action sequence plus event kind and timestamp.
// Bible reference: ARCHITECTURE.md RTWP combat lock, Sprint 4 Phase 2 action queue.
namespace EmberCrpg.Domain.Combat
{
    /// <summary>One deterministic emitted milestone for a queued combat action.</summary>
    public sealed class CombatActionEvent
    {
        public CombatActionEvent(QueuedCombatAction action, CombatActionEventKind eventKind, double elapsedSeconds)
        {
            Action = action;
            EventKind = eventKind;
            ElapsedSeconds = elapsedSeconds;
        }

        public QueuedCombatAction Action { get; }
        public CombatActionEventKind EventKind { get; }
        public double ElapsedSeconds { get; }
    }
}
