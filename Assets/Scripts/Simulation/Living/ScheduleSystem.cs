using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;

// Design note:
// ScheduleSystem (SOUL-03) is the first LIVING mover that reads ActorScheduleState.
// Pure Domain/Simulation: no Unity, no I/O, fully deterministic. It picks one
// per-hour destination for each living actor: assigned worksites during work hours,
// day anchors for idle daytime actors, and home outside work hours. It does not
// pathfind around obstacles, claim/complete jobs, tick recipes, mutate needs, or emit
// EventLog rows — those belong to JobAssignmentSystem and the per-tick composer.
namespace EmberCrpg.Simulation.Living
{
    /// <summary>Steps living actors one tile toward their current daily-rhythm target.</summary>
    public sealed class ScheduleSystem
    {
        /// <summary>First game-hour (inclusive) of the working day.</summary>
        public const int WorkStartHour = 6;

        /// <summary>End game-hour (exclusive) of the working day.</summary>
        public const int WorkEndHour = 20;

        /// <summary>F27: the midday meal — 12:00-13:59. Civilians route to the communal lunch spot
        /// (the tavern, when the realize step has published one).</summary>
        public const int LunchStartHour = 12;
        public const int LunchEndHour = 14;

        /// <summary>
        /// Advances one game-hour of schedule movement. Assigned actors route to their worksite
        /// during work hours, idle daytime actors route to their day anchor, and all living actors
        /// route home outside work hours.
        /// </summary>
        public void Advance(ActorStore actors, GameTime time)
        {
            Advance(actors, time, lunchSpot: null);
        }

        /// <summary>F27 overload: when the realize step published a communal LUNCH SPOT (the tavern),
        /// civilians route there over the midday window instead of their work/anchor target.</summary>
        public void Advance(ActorStore actors, GameTime time, GridPosition? lunchSpot)
        {
            if (actors == null)
                return;

            foreach (var actor in actors.Records)
            {
                if (actor == null || !actor.IsAlive)
                    continue;

                // F18 lair guards: a pinned Enemy (home == dayAnchor, the F10 dungeon-dweller contract)
                // has no daily rhythm — its only mover is the hostile-pursuit AI. Stepping it back home
                // every tick rubber-bands an active chase (proof: 1.8m closed instead of ~5.7m over
                // 2.6s). Commuting enemies (street outlaws, home != dayAnchor) keep the F6 curfew walk.
                if (actor.Role == ActorRole.Enemy && actor.Home.Equals(actor.DayAnchor))
                    continue;

                // F27: midday — civilians (never enemies or the watch) head for the lunch spot.
                var target = lunchSpot.HasValue && IsLunchHour(time)
                             && actor.Role != ActorRole.Enemy && actor.Role != ActorRole.Guard
                    ? lunchSpot.Value
                    : ResolveTarget(actor, time);

                var next = StepToward(actor.Position, target);
                if (!next.Equals(actor.Position))
                    actor.MoveTo(next);
            }
        }

        /// <summary>True within the midday meal window (12:00-13:59).</summary>
        public static bool IsLunchHour(GameTime time)
        {
            var hour = time.Hour;
            return hour >= LunchStartHour && hour < LunchEndHour;
        }

        /// <summary>True when the supplied timestamp falls within the working day.</summary>
        public static bool IsWorkHour(GameTime time)
        {
            var hour = time.Hour;
            return hour >= WorkStartHour && hour < WorkEndHour;
        }

        private static GridPosition ResolveTarget(ActorRecord actor, GameTime time)
        {
            if (!IsWorkHour(time))
                return actor.Home;

            var schedule = actor.ScheduleState;
            return schedule.IsIdle ? actor.DayAnchor : schedule.TargetWorksitePosition;
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
