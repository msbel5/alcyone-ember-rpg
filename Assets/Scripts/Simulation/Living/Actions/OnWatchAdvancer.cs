using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Actors.Actions;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;

// Design note:
// W36 GUARD+COMBAT: MoveToBed's mould with the guard's OWN DayAnchor cell as the post.
// HaulCrop-style fused advancer — walk-to-post AND stand-post live in one advancer, one
// enum value, so the ledger cost is 1 row per guard (no reservation: two guards may share
// a post cell — the seat-ring stacking rule from FoodOperations does not apply, the beat
// is a location not a resource). Interruptibility for pursuit rides the base template's
// IsPursuitQuarry probe on the quarry SIDE only — a guard is the CHASER, so this file adds
// its own IsPursuingGuard probe: an armed guard fails OnWatch to Interrupted, Idle follows,
// and the canonical pursuit action chain takes over on the next decision.
// CONSTRAINT (single-writer): Actor.Position mutations here register under
// living.action_advance@PerTick:22; FieldOwnershipRegistry declares it.
namespace EmberCrpg.Simulation.Living.Actions
{
    /// <summary>Walks the guard one step per tick toward its DayAnchor beat, then stands post.</summary>
    public sealed class OnWatchAdvancer : ActionAdvancer
    {
        public OnWatchAdvancer(ActionLogManager log) : base(log) { }

        public override ActorActionType Handles => ActorActionType.OnWatch;

        /// <summary>Arrival tolerance — a guard within 1 cell of the beat is on-post
        /// (mirror of SleepOperations.BedReachCells: shared-cell semantics, no reservation).</summary>
        public const int PostReachCells = 1;

        protected override void Step(WorldState world, ActorRecord actor, GameTime stamp)
        {
            var state = actor.ActionState;
            // The chase outranks the beat: an armed pursuit for THIS guard interrupts OnWatch,
            // Idle follows, and living.schedule (PerTick:20) routes to the quarry on the next
            // tick under the existing pursuit lifecycle (TryResolvePursuit + StepToward).
            if (HasLivePursuit(world, actor, stamp))
            {
                Fail(world, actor, ActionFailureReason.Interrupted, stamp);
                return;
            }
            if (!ScheduleSystem.IsWorkHour(stamp))
            {
                TransitionTo(world, actor, state.Succeeded(), ActionLogReason.Completed, stamp);
                return;
            }
            var post = actor.DayAnchor;
            var dist = FarmOperations.Chebyshev(actor.Position, post);
            if (dist > PostReachCells)
            {
                var movement = MovementService.RouteToward(
                    actor.Position, post, world?.NavView, PostReachCells);
                if (!movement.Moved)
                {
                    Fail(world, actor, ActionFailureReason.Unreachable, stamp);
                    return;
                }
                actor.MoveTo(movement.Position);
                TransitionTo(world, actor, state.Advanced(), ActionLogReason.ProgressTicked, stamp);
                return;
            }
            // On post: hold the same Running state. Shift end or pursuit interruption are the
            // only terminal paths, so standing guard cannot mint Started/Completed heartbeats.
            TransitionTo(world, actor, state, ActionLogReason.ProgressTicked, stamp);
        }

        // ONE arithmetic home: Domain.World.PursuitLedgerQuery — the guard-side probe. The
        // base template stays "quarry probe only"; guard-side is guard-only ergonomics.
        private static bool HasLivePursuit(WorldState world, ActorRecord actor, GameTime stamp)
            => PursuitLedgerQuery.IsActivePursuer(world.GuardPursuits, actor.Id, stamp.TotalMinutes);
    }
}
