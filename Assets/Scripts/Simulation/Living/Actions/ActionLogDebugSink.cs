using EmberCrpg.Domain.Actors.Actions;
using EmberCrpg.Simulation.Diagnostics;

// Design note:
// W32-04 §5.b: the greppable one-line phase mirror. Observer ONLY — no world access, no
// determinism stake. String work is guarded behind the EmberLog sink so headless runs pay
// nothing; the Presentation layer binds the sink once (proof harness greps "[Action]").
namespace EmberCrpg.Simulation.Living.Actions
{
    /// <summary>Mirrors action phase transitions to the EmberLog seam, one fixed-format line each.</summary>
    public sealed class ActionLogDebugSink : IActionLogSink
    {
        /// <summary>W32-04 §5.b proof gate: the composition root (DomainSimulationAdapter) turns the
        /// mirror on only under --ember-proof-screenshots / --ember-action-log; default off so
        /// normal play pays zero per-transition string cost. Observer-only — never determinism.</summary>
        public static bool Enabled;

        private static readonly EmberLogger Log = EmberLog.For("Action");

        public void OnPhase(in ActionLogEntry entry)
        {
            if (!Enabled || EmberLog.Sink == null) return; // ungated or headless: zero formatting cost
            Log.Info($"t={entry.TickMinutes} actor={entry.ActorId} intent={entry.Intent} " +
                     $"ph={entry.FromAction}/{entry.FromPhase}->{entry.ToAction}/{entry.ToPhase} " +
                     $"tgt=site:{entry.TargetId} why={entry.Reason}");
        }
    }
}
