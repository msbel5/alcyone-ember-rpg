using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Actors.Actions;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;

namespace EmberCrpg.Simulation.Living.Actions
{
    /// <summary>Moves a guard one bounded action step toward its durable actor target.</summary>
    public sealed class PursueAdvancer : ActionAdvancer
    {
        public const int MaxDistance = 40;

        public PursueAdvancer(ActionLogManager log) : base(log) { }

        public override ActorActionType Handles => ActorActionType.Pursue;

        protected override void Step(WorldState world, ActorRecord actor, GameTime stamp)
        {
            var state = actor.ActionState;
            if (!TryResolveLivePursuit(
                    world, actor, stamp, out var pursuit, out var target, out var failure))
            {
                Fail(world, actor, failure, stamp);
                return;
            }

            if (pursuit.TargetId != state.TargetActorId.Value)
            {
                world.HuntTargets ??= new System.Collections.Generic.List<HuntTargetRecord>();
                PursuitLedgerQuery.UpsertHunt(
                    world.HuntTargets, actor.Id.Value, target.Id.Value, pursuit.UntilMinutes);
                var retargeted = ActorActionState.ForIntent(ActorIntent.Pursue).Start(
                    ActorActionType.Pursue,
                    PredationSystem.FallbackSite(world, target.Position),
                    ItemId.Empty,
                    ReservationId.Empty,
                    stamp.TotalMinutes,
                    ActionInterruptPolicy.Interruptible,
                    target.Id);
                TransitionTo(world, actor, retargeted, ActionLogReason.TargetSelected, stamp);
                return;
            }

            if (actor.Position.ChebyshevDistanceTo(target.Position) <= CombatOperations.StrikeReach)
            {
                TransitionTo(world, actor, state.Succeeded(), ActionLogReason.Arrived, stamp);
                return;
            }

            var movement = MovementService.RouteToward(
                actor.Position, target.Position, world.NavView, CombatOperations.StrikeReach);
            if (!movement.Moved)
            {
                Fail(world, actor, ActionFailureReason.Unreachable, stamp);
                return;
            }
            actor.MoveTo(movement.Position);
            TransitionTo(world, actor, state.Advanced(), ActionLogReason.ProgressTicked, stamp);
        }

        internal static bool TryResolveLivePursuit(
            WorldState world,
            ActorRecord guard,
            GameTime stamp,
            out PursuitRecord pursuit,
            out ActorRecord target,
            out ActionFailureReason failure)
        {
            target = null;
            failure = ActionFailureReason.TargetGone;
            if (!PursuitLedgerQuery.TryGetPursuit(world.GuardPursuits, guard.Id, out pursuit))
                return false;
            if (stamp.TotalMinutes > pursuit.UntilMinutes)
            {
                failure = ActionFailureReason.TimedOut;
                return false;
            }
            if (!world.Actors.TryGet(new ActorId(pursuit.TargetId), out target)
                || target == null
                || !target.IsAlive
                || guard.Position.ChebyshevDistanceTo(target.Position) > MaxDistance)
                return false;
            return true;
        }
    }
}
