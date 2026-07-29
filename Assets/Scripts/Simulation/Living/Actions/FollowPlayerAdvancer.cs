using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Actors.Actions;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;

namespace EmberCrpg.Simulation.Living.Actions
{
    /// <summary>
    /// Advances the persistent companion-follow action by at most one route step.
    /// Player position is deliberately re-read each tick; the action stores no stale cell.
    /// </summary>
    public sealed class FollowPlayerAdvancer : ActionAdvancer
    {
        public FollowPlayerAdvancer(ActionLogManager log) : base(log) { }

        public override ActorActionType Handles => ActorActionType.FollowPlayer;

        protected override void Step(WorldState world, ActorRecord actor, GameTime stamp)
        {
            var state = actor.ActionState;
            if (!CompanionService.IsCompanion(world, actor.Id))
            {
                Fail(world, actor, ActionFailureReason.Interrupted, stamp);
                return;
            }

            var player = CompanionService.FindPlayer(world);
            if (player == null)
            {
                Fail(world, actor, ActionFailureReason.Interrupted, stamp);
                return;
            }

            if (actor.Position.ChebyshevDistanceTo(player.Position) <= CompanionSystem.HeelCells)
            {
                TransitionTo(world, actor, state.Succeeded(), ActionLogReason.Arrived, stamp);
                return;
            }

            var movement = MovementService.RouteToward(
                actor.Position, player.Position, world.NavView, CompanionSystem.HeelCells);
            if (!movement.Moved)
            {
                Fail(world, actor, ActionFailureReason.Unreachable, stamp);
                return;
            }

            actor.MoveTo(movement.Position);
            TransitionTo(world, actor, state.Advanced(), ActionLogReason.ProgressTicked, stamp);
        }
    }
}
