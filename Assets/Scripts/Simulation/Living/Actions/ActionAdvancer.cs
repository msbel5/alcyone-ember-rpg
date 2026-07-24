using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Actors.Actions;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;

// Design note:
// W32-01 §6 / W32-03 §3: Strategy + Template Method for action advancement. Advancers are
// STATELESS — every unit of progress lives on ActorActionState inside WorldState, or chunked
// replay diverges (CadenceChunkingInvarianceTests is the enforcement). The template fixes the
// probe -> step -> log order so a subclass cannot skip the interruption check or the log seam.
namespace EmberCrpg.Simulation.Living.Actions
{
    /// <summary>Advances one action type by exactly one phase-step per tick.</summary>
    public interface IActionAdvancer
    {
        ActorActionType Handles { get; }
        void Advance(WorldState world, ActorRecord actor, GameTime stamp);
    }

    /// <summary>Template base: interruption probe, then one step; ALL state writes go through TransitionTo.</summary>
    public abstract class ActionAdvancer : IActionAdvancer
    {
        private readonly ActionLogManager _log;

        protected ActionAdvancer(ActionLogManager log)
        {
            _log = log ?? new ActionLogManager();
        }

        public abstract ActorActionType Handles { get; }

        public void Advance(WorldState world, ActorRecord actor, GameTime stamp)
        {
            // Pull-based probe at every step start (W32-03 §7): being hunted outranks lunch.
            // Deterministic and order-independent; no push/callback machinery.
            if (IsPursuitQuarry(world, actor, stamp))
            {
                Fail(world, actor, ActionFailureReason.Interrupted, stamp);
                return;
            }
            Step(world, actor, stamp);
        }

        protected abstract void Step(WorldState world, ActorRecord actor, GameTime stamp);

        // CONSTRAINT (single seam, W32-04 §1): the ONLY writer of Actor.ActionState in simulation
        // code, and the ONLY caller of ActionLogManager.Record. Callers: advancer Steps and
        // ActionLifecycleSystem (decision start + chain handover). In-phase progress is applied
        // but NOT logged — phase BOUNDARIES are the log grammar (B21 spam lesson).
        internal void TransitionTo(WorldState world, ActorRecord actor, in ActorActionState next,
            ActionLogReason reason, GameTime stamp)
        {
            var before = actor.ActionState;
            actor.ApplyActionState(next);
            if (before.CurrentAction == next.CurrentAction && before.Phase == next.Phase)
                return;
            _log.Record(world, new ActionLogEntry(
                stamp.TotalMinutes, actor.Id.Value, next.CurrentIntent,
                before.CurrentAction, before.Phase, next.CurrentAction, next.Phase,
                next.TargetSiteId.IsEmpty ? before.TargetSiteId.Value : next.TargetSiteId.Value,
                reason));
        }

        /// <summary>Uniform failure gate: conserve the carried unit, release the claim ONCE, mark Failed.</summary>
        protected void Fail(WorldState world, ActorRecord actor, ActionFailureReason reason, GameTime stamp)
        {
            var state = actor.ActionState;
            if (world.Reservations != null
                && world.Reservations.TryGetByActor(actor.Id.Value, out var row)
                && row.Id == state.ReservationId.Value)
            {
                // ConsumeFood means the unit left the pile at TakeFood — matter conservation
                // returns it before the claim dies (no dup, no loss; W32-06 T5 contract).
                if (state.CurrentAction == ActorActionType.ConsumeFood)
                    FoodOperations.FindPile(world, row.SiteId)?.Add(row.ItemTag, 1);
                world.Reservations.Release(row.Id);
            }
            TransitionTo(world, actor, state.Failed(reason), ToLogReason(reason), stamp);
        }

        internal static ActionLogReason ToLogReason(ActionFailureReason reason) => reason switch
        {
            ActionFailureReason.ReservationLost => ActionLogReason.ReservationLost,
            ActionFailureReason.Unreachable => ActionLogReason.PathBlocked,
            ActionFailureReason.Interrupted => ActionLogReason.InterruptPreempted,
            ActionFailureReason.NoFoodFound => ActionLogReason.TargetGone,
            ActionFailureReason.SourceDrained => ActionLogReason.TargetGone,
            _ => ActionLogReason.InterruptPreempted,
        };

        private static bool IsPursuitQuarry(WorldState world, ActorRecord actor, GameTime stamp)
        {
            var pursuits = world.GuardPursuits;
            if (pursuits == null) return false;
            for (var i = 0; i < pursuits.Count; i++)
                if (pursuits[i].TargetId == actor.Id.Value && stamp.TotalMinutes <= pursuits[i].UntilMinutes)
                    return true;
            return false;
        }
    }
}
