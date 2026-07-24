using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Actors.Actions;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;

// Design note:
// W32-03 §4 TakeFood (1 tick): the validated pile -> hand transfer. Reach and stock are
// re-checked INSIDE the step — a faster mouth (or vermin) may have emptied the pile between
// arrival and take. The claim row STAYS until the consume commit: it carries the taken unit's
// tag (ActorActionState stores ids only), so failure paths can return the unit to its pile.
namespace EmberCrpg.Simulation.Living.Actions
{
    /// <summary>Takes one reserved unit from the pile into the actor's hands.</summary>
    public sealed class TakeFoodAdvancer : ActionAdvancer
    {
        public TakeFoodAdvancer(ActionLogManager log) : base(log) { }

        public override ActorActionType Handles => ActorActionType.TakeFood;

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
            var pile = FoodOperations.FindPile(world, row.SiteId);
            if (pile == null || pile.Get(row.ItemTag) <= 0)
            {
                Fail(world, actor, ActionFailureReason.SourceDrained, stamp);
                return;
            }
            // Nudged out of the ring (witness shy-step): fail and replan — a re-walk, not a dup.
            if (!FoodOperations.WithinEatReach(world, actor, row.SiteId))
            {
                Fail(world, actor, ActionFailureReason.Unreachable, stamp);
                return;
            }
            pile.Remove(row.ItemTag, 1); // unit is physically in hand now (all-or-nothing)
            TransitionTo(world, actor, state.Succeeded(), ActionLogReason.ProgressTicked, stamp);
        }
    }
}
