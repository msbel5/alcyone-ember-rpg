using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Actors.Actions;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;

// Design note:
// W34-01 §5.2: MoveToPlotAdvancer's skeleton with the actor's own Home cell as the target.
// The struct carries ids only (W32 rule): the destination is re-read from actor.Home every
// step, and the bed row must still NAME that cell — a midnight home change makes the row a
// lie and fails as ReservationLost, never as a silent walk to the wrong house.
namespace EmberCrpg.Simulation.Living.Actions
{
    /// <summary>Walks the actor one step per tick toward its reserved Home-cell bed.</summary>
    public sealed class MoveToBedAdvancer : ActionAdvancer
    {
        public MoveToBedAdvancer(ActionLogManager log) : base(log) { }

        public override ActorActionType Handles => ActorActionType.MoveToBed;

        protected override void Step(WorldState world, ActorRecord actor, GameTime stamp)
        {
            var state = actor.ActionState;
            if (world.Reservations == null
                || !world.Reservations.TryGetByActor(actor.Id.Value, out var row)
                || row.Id != state.ReservationId.Value
                || !SleepOperations.TryParseBedKey(row.ItemTag, out var bed)
                || !bed.Equals(actor.Home))
            {
                Fail(world, actor, ActionFailureReason.ReservationLost, stamp);
                return;
            }
            // Dawn caught the commute: sleeping is pointless now — the first live use of the
            // TimedOut value W32 minted (ToLogReason's default arm covers it, no new log row).
            // CONSTRAINT (§11 risk 5): the dawn predicate is SleepOperations.IsNightHour ONLY.
            if (!SleepOperations.IsNightHour(stamp.Hour))
            {
                Fail(world, actor, ActionFailureReason.TimedOut, stamp);
                return;
            }

            if (FarmOperations.Chebyshev(actor.Position, actor.Home) > SleepOperations.BedReachCells)
            {
                var movement = MovementService.RouteToward(
                    actor.Position, actor.Home, world?.NavView, SleepOperations.BedReachCells);
                if (!movement.Moved)
                {
                    Fail(world, actor, ActionFailureReason.Unreachable, stamp);
                    return;
                }
                actor.MoveTo(movement.Position);
            }
            // Arrival = within BedReachCells (NOT the exact cell): family members share the
            // Home cell, so they settle in the 3x3 bedroom instead of stacking on one tile.
            if (FarmOperations.Chebyshev(actor.Position, actor.Home) <= SleepOperations.BedReachCells)
                TransitionTo(world, actor, state.Succeeded(), ActionLogReason.Arrived, stamp);
            else
                TransitionTo(world, actor, state.Advanced(), ActionLogReason.ProgressTicked, stamp);
        }
    }
}
