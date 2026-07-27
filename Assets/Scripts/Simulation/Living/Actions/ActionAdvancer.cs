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
                // W33-01 §6: a failed haul's load sweeps into the destination pile the carry
                // row names — conservation over realism (same class as the ConsumeFood return).
                else if (state.CarriedUnits > 0
                    && FarmOperations.TryParseCarryKey(row.ItemTag, out var cropTag))
                    FarmOperations.FindOrCreatePile(world, new EmberCrpg.Domain.Core.SiteId(row.SiteId))
                        ?.Add(cropTag, state.CarriedUnits);
                world.Reservations.Release(row.Id);
            }
            // W33: hands zero on EVERY failure — the load was swept above, or (rowless: the
            // mis-TTL/death class) is buried with its carrier; a Failed state still carrying
            // units would double-count against the pile in the conservation ledger.
            if (state.CarriedUnits > 0)
                state = state.WithCarriedUnits(0);
            TransitionTo(world, actor, state.Failed(reason), ToLogReason(reason), stamp);
        }

        internal static ActionLogReason ToLogReason(ActionFailureReason reason) => reason switch
        {
            ActionFailureReason.ReservationLost => ActionLogReason.ReservationLost,
            ActionFailureReason.Unreachable => ActionLogReason.PathBlocked,
            ActionFailureReason.Interrupted => ActionLogReason.InterruptPreempted,
            ActionFailureReason.NoFoodFound => ActionLogReason.TargetGone,
            ActionFailureReason.SourceDrained => ActionLogReason.TargetGone,
            ActionFailureReason.PlotTaken => ActionLogReason.PlotTaken,
            ActionFailureReason.CropGone => ActionLogReason.CropGone,
            _ => ActionLogReason.InterruptPreempted,
        };

        // ONE arithmetic home: Domain.World.PursuitLedgerQuery — same expiry predicate on
        // both sides (quarry here, pursuer at ActionLifecycleSystem / OnWatchAdvancer).
        private static bool IsPursuitQuarry(WorldState world, ActorRecord actor, GameTime stamp)
            => PursuitLedgerQuery.IsActiveQuarry(world.GuardPursuits, actor.Id, stamp.TotalMinutes);
    }
}
