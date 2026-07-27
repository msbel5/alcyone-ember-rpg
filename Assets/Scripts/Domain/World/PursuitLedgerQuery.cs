using System.Collections.Generic;
using EmberCrpg.Domain.Core;

// Design note:
// PursuitLedgerQuery consolidates the two-sided expiry probe over GuardPursuits — pre-W38 tail,
// three near-identical loops lived in ActionAdvancer / ActionLifecycleSystem / OnWatchAdvancer.
// Same expiry predicate (<=), one keyed on TargetId (quarry side), one on GuardId (pursuer side).
namespace EmberCrpg.Domain.World
{
    /// <summary>Two-sided liveness probes over the guard pursuit ledger.
    /// Null list = no active pursuits (avoids the null-check ceremony at every call site).</summary>
    public static class PursuitLedgerQuery
    {
        public static bool IsActiveQuarry(List<PursuitRecord> pursuits, ActorId actor, long nowMinutes)
        {
            if (pursuits == null) return false;
            for (var i = 0; i < pursuits.Count; i++)
                if (pursuits[i].TargetId == actor.Value && nowMinutes <= pursuits[i].UntilMinutes)
                    return true;
            return false;
        }

        public static bool IsActivePursuer(List<PursuitRecord> pursuits, ActorId actor, long nowMinutes)
        {
            if (pursuits == null) return false;
            for (var i = 0; i < pursuits.Count; i++)
                if (pursuits[i].GuardId == actor.Value && nowMinutes <= pursuits[i].UntilMinutes)
                    return true;
            return false;
        }

        // Newest-wins per source. Both RegisterPursuit (WitnessResponseSystem, PursuitRecord/GuardId)
        // and RegisterHunt (ActionLifecycleSystem, HuntTargetRecord/HunterId) used to hand-roll this
        // find-or-append loop identically — ONE arithmetic home lives here.
        public static void UpsertPursuit(List<PursuitRecord> pursuits, ulong guardId, ulong targetId, long untilMinutes)
        {
            for (var i = 0; i < pursuits.Count; i++)
            {
                var row = pursuits[i];
                if (row.GuardId != guardId) continue;
                row.TargetId = targetId;
                row.UntilMinutes = untilMinutes;
                return;
            }
            pursuits.Add(new PursuitRecord { GuardId = guardId, TargetId = targetId, UntilMinutes = untilMinutes });
        }

        public static void UpsertHunt(List<HuntTargetRecord> hunts, ulong hunterId, ulong targetId, long untilMinutes)
        {
            for (var i = 0; i < hunts.Count; i++)
            {
                var row = hunts[i];
                if (row.HunterId != hunterId) continue;
                row.TargetId = targetId;
                row.UntilMinutes = untilMinutes;
                return;
            }
            hunts.Add(new HuntTargetRecord { HunterId = hunterId, TargetId = targetId, UntilMinutes = untilMinutes });
        }
    }
}
