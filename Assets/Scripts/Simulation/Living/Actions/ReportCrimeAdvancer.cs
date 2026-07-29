using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Actors.Actions;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Memory;
using EmberCrpg.Domain.World;

namespace EmberCrpg.Simulation.Living.Actions
{
    /// <summary>Moves one witness toward the watch and files one durable report.</summary>
    public sealed class ReportCrimeAdvancer : ActionAdvancer
    {
        public const int ReportReach = 2;
        public const int GuardSearchRadius = 16;

        public ReportCrimeAdvancer(ActionLogManager log) : base(log) { }

        public override ActorActionType Handles => ActorActionType.ReportCrime;

        protected override void Step(WorldState world, ActorRecord actor, GameTime stamp)
        {
            var state = actor.ActionState;
            if (state.TargetActorId.IsEmpty
                || !world.Actors.TryGet(state.TargetActorId, out var attacker)
                || attacker == null
                || !attacker.IsAlive)
            {
                ClosePendingReport(world, actor, state.TargetActorId, stamp, "target_gone");
                Fail(world, actor, ActionFailureReason.TargetGone, stamp);
                return;
            }

            var guard = CombatOperations.Nearest(
                world, actor.Position, GuardSearchRadius,
                candidate => candidate.Role == ActorRole.Guard);
            if (guard == null)
            {
                ClosePendingReport(world, actor, state.TargetActorId, stamp, "no_guard");
                Fail(world, actor, ActionFailureReason.TargetGone, stamp);
                return;
            }

            if (actor.Position.ChebyshevDistanceTo(guard.Position) <= ReportReach)
            {
                var memory = world.NpcMemory.GetOrCreate(actor.Id);
                var alreadyReported = false;
                foreach (var known in memory.Events)
                    if (known.EventType == "reported_attack"
                        && known.ActorSeen.Equals(state.TargetActorId))
                    {
                        alreadyReported = true;
                        break;
                    }
                if (!alreadyReported)
                {
                    memory.RecordEvent(new InteractionEvent(
                        stamp, "reported_attack", state.TargetActorId,
                        "watch_report", string.Empty, 0, actor.Position));
                    world.Events?.Append(new WorldEvent(
                        stamp, WorldEventKind.WitnessRecorded, actor.Id, state.TargetSiteId,
                        $"reported attacker:{state.TargetActorId.Value} guard:{guard.Id.Value}"));
                }
                WitnessResponseSystem.RegisterPursuit(
                    world, guard.Id.Value, state.TargetActorId.Value, stamp);
                TransitionTo(world, actor, state.Succeeded(), ActionLogReason.Completed, stamp);
                return;
            }

            var movement = MovementService.RouteToward(
                actor.Position, guard.Position, world.NavView, ReportReach);
            if (!movement.Moved)
            {
                ClosePendingReport(world, actor, state.TargetActorId, stamp, "unreachable");
                Fail(world, actor, ActionFailureReason.Unreachable, stamp);
                return;
            }
            actor.MoveTo(movement.Position);
            TransitionTo(world, actor, state.Advanced(), ActionLogReason.ProgressTicked, stamp);
        }

        private static void ClosePendingReport(
            WorldState world,
            ActorRecord witness,
            ActorId target,
            GameTime stamp,
            string reason)
        {
            if (target.IsEmpty || world.NpcMemory == null) return;
            world.NpcMemory.GetOrCreate(witness.Id).RecordEvent(new InteractionEvent(
                stamp, "report_closed", target, reason, string.Empty, 0, witness.Position));
        }
    }
}
