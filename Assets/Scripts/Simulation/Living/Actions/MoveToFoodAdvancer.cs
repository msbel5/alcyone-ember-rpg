using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Actors.Actions;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;

// Design note:
// W32-03 §4 MoveToFood: one grid step per tick toward the reserved site's seat. Arrival is a
// TRANSITION — TakeFood executes NEXT tick (transitions consume the tick, uniformly).
namespace EmberCrpg.Simulation.Living.Actions
{
    /// <summary>Walks the actor one step per tick toward its reserved seat.</summary>
    public sealed class MoveToFoodAdvancer : ActionAdvancer
    {
        public MoveToFoodAdvancer(ActionLogManager log) : base(log) { }

        public override ActorActionType Handles => ActorActionType.MoveToFood;

        protected override void Step(WorldState world, ActorRecord actor, GameTime stamp)
        {
            var state = actor.ActionState;
            if (world.Reservations == null
                || !world.Reservations.TryGetByActor(actor.Id.Value, out var row)
                || row.Id != state.ReservationId.Value)
            {
                Fail(world, actor, ActionFailureReason.ReservationLost, stamp);
                return;
            }
            // Mid-route drain: the claim is soft — vermin/hourly writers still empty piles.
            var pile = FoodOperations.FindPile(world, row.SiteId);
            if (pile == null || pile.Get(row.ItemTag) <= 0)
            {
                Fail(world, actor, ActionFailureReason.SourceDrained, stamp);
                return;
            }
            // Arrival means STANDING ON the seat cell (ring-2, distinct per ordinal) — stopping at
            // first ring contact would stack approach-side diners on one cell (Gate8).
            var seat = FoodOperations.SeatFor(world, row.SiteId, actor);
            if (!actor.Position.Equals(seat))
                actor.MoveTo(MovementService.StepToward(actor.Position, seat));
            if (actor.Position.Equals(seat))
                TransitionTo(world, actor, state.Succeeded(), ActionLogReason.Arrived, stamp);
            else
                TransitionTo(world, actor, state.Advanced(), ActionLogReason.ProgressTicked, stamp);
        }
    }
}
