using System.Linq;
using System.Text;
using EmberCrpg.Domain.Actors.Actions;
using EmberCrpg.Domain.World;

namespace EmberCrpg.Tests.EditMode.Actions.Support
{
    /// <summary>
    /// W32 DOC6 §0: the action history a story test may cite. CONSTRAINT: reads ONLY
    /// WorldState-owned evidence — the ActionLog ring and the WorldEventLog. Render or
    /// diagnostic log output is NEVER proof (the verify-at-render-layer rule guards the UI
    /// side; domain claims read the domain trace).
    /// </summary>
    internal static class ActionTrace
    {
        /// <summary>Every retained phase transition plus every terminal action event, one line each.</summary>
        public static string Of(WorldState world)
        {
            var sb = new StringBuilder();
            sb.Append("pushed=").Append(world.ActionLog.TotalPushed).Append('\n');
            for (var i = 0; i < world.ActionLog.Count; i++)
            {
                var e = world.ActionLog.At(i);
                sb.Append(e.TickMinutes).Append(':').Append(e.ActorId).Append(':').Append(e.Intent)
                  .Append(':').Append(e.FromAction).Append('/').Append(e.FromPhase)
                  .Append("->").Append(e.ToAction).Append('/').Append(e.ToPhase)
                  .Append(':').Append(e.TargetId).Append(':').Append(e.Reason).Append('\n');
            }
            foreach (var e in world.Events.Events.Where(e =>
                e.Kind == WorldEventKind.ActionCompleted || e.Kind == WorldEventKind.ActionFailed))
                sb.Append(e.Tick.TotalMinutes).Append(':').Append(e.Kind).Append(':')
                  .Append(e.ActorId.Value).Append(':').Append(e.Reason).Append('\n');
            return sb.ToString();
        }

        /// <summary>Final mind state per actor — the (id, intent, action, phase, progress, reservation, started) rows.</summary>
        public static string StateDigest(WorldState world)
        {
            return string.Join("\n", world.Actors.Records
                .Where(a => a != null)
                .OrderBy(a => a.Id.Value)
                .Select(a => $"{a.Id.Value}:{a.ActionState.CurrentIntent}:{a.ActionState.CurrentAction}" +
                             $":{a.ActionState.Phase}:{a.ActionState.ProgressTicks}" +
                             $":{a.ActionState.ReservationId.Value}:{a.ActionState.StartedAtMinutes}"));
        }
    }
}
