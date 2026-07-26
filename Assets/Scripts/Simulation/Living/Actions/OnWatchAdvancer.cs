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
// and the SAME PerTick ScheduleSystem routes the chase (existing pursuit lifecycle).
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
            var post = actor.DayAnchor;
            var dist = FarmOperations.Chebyshev(actor.Position, post);
            if (dist > PostReachCells)
            {
                actor.MoveTo(MovementService.StepToward(actor.Position, post, world?.NavView));
                TransitionTo(world, actor, state.Advanced(), ActionLogReason.ProgressTicked, stamp);
                return;
            }
            // On post: the terminal step. Succeeded => NextLink None => Idle => next Decide
            // tick re-opens OnWatch. The heartbeat is one Started/one Completed per beat,
            // not per tick (B21 spam lesson). PredationSystem's guard-first-strike still
            // triggers when a hunter enters StrikeReach — the beat writer is not the striker.
            TransitionTo(world, actor, state.Succeeded(), ActionLogReason.Arrived, stamp);
        }

        // Mirror of ActionAdvancer.IsPursuitQuarry keyed on GuardId — the pursuit's PURSUER
        // side. Kept local (not moved to the base) so the base template's contract stays
        // "quarry probe only"; the guard-side probe is guard-only ergonomics.
        private static bool HasLivePursuit(WorldState world, ActorRecord actor, GameTime stamp)
        {
            var pursuits = world.GuardPursuits;
            if (pursuits == null) return false;
            for (var i = 0; i < pursuits.Count; i++)
                if (pursuits[i].GuardId == actor.Id.Value && stamp.TotalMinutes <= pursuits[i].UntilMinutes)
                    return true;
            return false;
        }
    }
}
