using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Actors.Actions;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;

// Design note:
// W34 WORK slice (docs/ruh/w34/02 §7.1): MoveToPlotAdvancer's skeleton with the claimed
// bench as the target. The lock is the JobBoard CLAIM, not a reservation row (§3) — the
// struct carries ReservationId.Empty, so ActionAdvancer.Fail's refund arms never engage.
// The bench cell is re-read from ScheduleState.TargetWorksitePosition every step (the claim
// wrote it; the save already carries it) — the struct carries ids only, the W32 rule.
namespace EmberCrpg.Simulation.Living.Actions
{
    /// <summary>Walks the actor one step per tick toward its claimed job's bench cell.</summary>
    public sealed class MoveToWorksiteAdvancer : ActionAdvancer
    {
        public MoveToWorksiteAdvancer(ActionLogManager log) : base(log) { }

        public override ActorActionType Handles => ActorActionType.MoveToWorksite;

        protected override void Step(WorldState world, ActorRecord actor, GameTime stamp)
        {
            var state = actor.ActionState;
            // 1) The claim died (sweep / cancel / another claimant): the walk is pointless.
            if (!WorkOperations.TryGetClaim(world, actor, out var request))
            {
                Fail(world, actor, ActionFailureReason.JobLost, stamp);
                return;
            }
            // 2) The forge went cold mid-commute (record gone / inactive / kind drift).
            if (!WorkOperations.TryGetWorksite(world, request, out _))
            {
                Fail(world, actor, ActionFailureReason.WorksiteGone, stamp);
                return;
            }
            // 3) Quitting time interrupts the commute; the order row (if any) waits frozen and
            //    the morning decision rebuilds the chain — "pause" is claim + row + Idle (§2).
            if (!ScheduleSystem.IsWorkHour(stamp))
            {
                Fail(world, actor, ActionFailureReason.Interrupted, stamp);
                return;
            }

            var bench = actor.ScheduleState.TargetWorksitePosition;
            if (FarmOperations.Chebyshev(actor.Position, bench) > WorkOperations.WorkReachCells)
                actor.MoveTo(MovementService.StepToward(actor.Position, bench));
            // Arrival is ADJACENCY (≤ WorkReachCells): the bench cell is occupied furniture.
            if (FarmOperations.Chebyshev(actor.Position, bench) <= WorkOperations.WorkReachCells)
                TransitionTo(world, actor, state.Succeeded(), ActionLogReason.Arrived, stamp);
            else
                TransitionTo(world, actor, state.Advanced(), ActionLogReason.ProgressTicked, stamp);
        }
    }
}
