// Design note:
// W32 EAT slice (docs/ruh/w32/04-action-log.md §2.3): observer seam for the phase trace.
// CONSTRAINT: a sink is an OBSERVER, never a mutator — it must not touch world state, must
// not throw, and must not participate in determinism. Implementations live outside Domain
// (Presentation: Debug.Log mirror; tests: capture list; headless harness: none).
namespace EmberCrpg.Domain.Actors.Actions
{
    /// <summary>Read-only observer of action phase transitions.</summary>
    public interface IActionLogSink
    {
        void OnPhase(in ActionLogEntry entry);
    }
}
