using System.Collections.Generic;
using EmberCrpg.Domain.Combat;

// Design note:
// RealtimeCombatTickResult collects timeline events emitted by one unpaused simulation step.
// Inputs: scheduler tick delta and queued actions.
// Outputs: ordered activation/completion events for tests and presentation adapters.
// Bible reference: ARCHITECTURE.md RTWP combat lock, Sprint 4 Phase 2 action queue.
namespace EmberCrpg.Simulation.Combat
{
    /// <summary>Pure result object for one real-time combat scheduler tick.</summary>
    public sealed class RealtimeCombatTickResult
    {
        private readonly List<CombatActionEvent> _events = new List<CombatActionEvent>();

        public IReadOnlyList<CombatActionEvent> Events => _events;

        internal void Add(CombatActionEvent combatEvent)
        {
            _events.Add(combatEvent);
        }
    }
}
