using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;

// Design note:
// ScheduleSystem (SOUL-03) is the first LIVING mover that reads ActorScheduleState.
// Pure Domain/Simulation: no Unity, no I/O, fully deterministic. For each actor whose
// schedule is Assigned, it steps the actor one grid tile toward its target worksite
// during work hours. It does not pathfind around obstacles, claim/complete jobs, tick
// recipes, mutate needs, or emit EventLog rows — those belong to JobAssignmentSystem
// and the per-tick composer. One Advance call == one game-hour of travel progress.
namespace EmberCrpg.Simulation.Living
{
    /// <summary>Steps job-assigned actors one tile toward their worksite per game-hour.</summary>
    public sealed class ScheduleSystem
    {
        /// <summary>First game-hour (inclusive) of the working day.</summary>
        public const int WorkStartHour = 6;

        /// <summary>End game-hour (exclusive) of the working day.</summary>
        public const int WorkEndHour = 20;

        /// <summary>
        /// Advances one game-hour of schedule movement. During work hours every Assigned actor moves
        /// one Chebyshev step toward its <see cref="ActorScheduleState.TargetWorksitePosition"/>;
        /// actors already on the cell, idle actors, and dead actors are left untouched. Outside work
        /// hours actors hold position (a home coordinate is not modelled on ActorRecord, so night-home
        /// routing is intentionally a no-op here rather than fabricating a destination).
        /// </summary>
        public void Advance(ActorStore actors, GameTime time)
        {
            if (actors == null)
                return;

            if (!IsWorkHour(time))
                return;

            foreach (var actor in actors.Records)
            {
                if (actor == null || !actor.IsAlive)
                    continue;

                var schedule = actor.ScheduleState;
                if (schedule.IsIdle)
                    continue;

                var next = StepToward(actor.Position, schedule.TargetWorksitePosition);
                if (!next.Equals(actor.Position))
                    actor.MoveTo(next);
            }
        }

        /// <summary>True when the supplied timestamp falls within the working day.</summary>
        public static bool IsWorkHour(GameTime time)
        {
            var hour = time.Hour;
            return hour >= WorkStartHour && hour < WorkEndHour;
        }

        // Deterministic one-tile move toward the target on the integer grid. Each axis advances by at
        // most one, so movement is a Chebyshev (8-direction) step that converges monotonically and
        // never overshoots the destination cell.
        private static GridPosition StepToward(GridPosition from, GridPosition to)
        {
            return new GridPosition(
                from.X + System.Math.Sign(to.X - from.X),
                from.Y + System.Math.Sign(to.Y - from.Y));
        }
    }
}
