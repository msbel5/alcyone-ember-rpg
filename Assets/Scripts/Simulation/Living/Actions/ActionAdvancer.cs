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
            if (actor.ActionState.InterruptPolicy == ActionInterruptPolicy.Interruptible
                && IsPursuitQuarry(world, actor, stamp))
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
            var state = RecoverMatterAndReleaseReservation(world, actor, actor.ActionState, stamp);
            // PRD-03: HuntTargets is the target-identity ledger for BOTH Hunt and
            // StrikeQuarry. Every terminal failure path (expired/dead/missing prey,
            // interruption, unreachable) must retire the relationship exactly here.
            if (state.CurrentIntent == ActorIntent.Hunt
                || state.CurrentIntent == ActorIntent.GuardCompanion
                || state.CurrentIntent == ActorIntent.Pursue)
                ClearHuntTarget(world, actor.Id.Value);
            if (state.CurrentIntent == ActorIntent.Pursue)
                ClearGuardPursuit(world, actor.Id.Value);
            TransitionTo(world, actor, state.Failed(reason), ToLogReason(reason), stamp);
        }

        /// <summary>
        /// Shared interruption cleanup for both Running failures and a target killed while its
        /// action is already terminal. Matter and the reservation close before the caller idles it.
        /// </summary>
        protected ActorActionState RecoverMatterAndReleaseReservation(
            WorldState world,
            ActorRecord actor,
            ActorActionState state,
            GameTime stamp)
        {
            var carriedMatterRecovered = state.CarriedUnits == 0;
            if (world.Reservations != null
                && world.Reservations.TryGetByActor(actor.Id.Value, out var row)
                && row.Id == state.ReservationId.Value)
            {
                // ConsumeFood means the unit left the pile at TakeFood — matter conservation
                // returns it before the claim dies (no dup, no loss; W32-06 T5 contract).
                if (state.CurrentAction == ActorActionType.ConsumeFood || (state.CurrentAction == ActorActionType.TakeFood && state.Phase == ActionPhase.Succeeded))
                    FoodOperations.FindPile(world, row.SiteId)?.Add(row.ItemTag, 1);
                // W33-01 §6: a failed haul's load sweeps into the destination pile the carry
                // row names — conservation over realism (same class as the ConsumeFood return).
                else if (state.CarriedUnits > 0)
                {
                    var cropTag = state.CarriedMatterTag;
                    if (string.IsNullOrWhiteSpace(cropTag)
                        && FarmOperations.TryParseCarryKey(row.ItemTag, out var rowCropTag))
                        cropTag = rowCropTag;
                    RecoverCarriedMatter(world, actor, state, cropTag, stamp, missingReservation: false);
                    carriedMatterRecovered = true;
                }
                world.Reservations.Release(row.Id);
            }
            if (!carriedMatterRecovered)
                RecoverCarriedMatter(
                    world, actor, state, state.CarriedMatterTag, stamp, missingReservation: true);
            // Hands zero only after the carried quantity has been returned to an authoritative pile.
            if (state.CarriedUnits > 0)
                state = state.WithCarriedUnits(0);
            return state;
        }

        private static void RecoverCarriedMatter(
            WorldState world,
            ActorRecord actor,
            ActorActionState state,
            string itemTag,
            GameTime stamp,
            bool missingReservation)
        {
            if (string.IsNullOrWhiteSpace(itemTag))
                throw new System.InvalidOperationException(
                    "Carried matter cannot be recovered without an item tag.");
            if (missingReservation && world.Events == null)
                throw new System.InvalidOperationException(
                    "Carried matter recovery requires the authoritative event log.");
            var pile = FarmOperations.FindOrCreatePile(world, state.TargetSiteId);
            if (pile == null)
                throw new System.InvalidOperationException(
                    "Carried matter cannot be recovered without a target-site stockpile.");

            pile.Add(itemTag, state.CarriedUnits);
            if (missingReservation)
                world.Events.Append(new WorldEvent(
                    stamp, WorldEventKind.MatterRecovered, actor.Id, state.TargetSiteId,
                    $"carried_matter_recovered item:{itemTag} qty:{state.CarriedUnits} path:missing_reservation"));
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
            ActionFailureReason.TargetGone => ActionLogReason.TargetGone,
            _ => ActionLogReason.InterruptPreempted,
        };

        private static void ClearHuntTarget(WorldState world, ulong hunterId)
        {
            var rows = world.HuntTargets;
            if (rows == null) return;
            for (var i = rows.Count - 1; i >= 0; i--)
                if (rows[i].HunterId == hunterId)
                    rows.RemoveAt(i);
        }

        private static void ClearGuardPursuit(WorldState world, ulong guardId)
        {
            var rows = world.GuardPursuits;
            if (rows == null) return;
            for (var i = rows.Count - 1; i >= 0; i--)
                if (rows[i].GuardId == guardId)
                    rows.RemoveAt(i);
        }

        // ONE arithmetic home: Domain.World.PursuitLedgerQuery — same expiry predicate on
        // both sides (quarry here, pursuer at ActionLifecycleSystem / OnWatchAdvancer).
        private static bool IsPursuitQuarry(WorldState world, ActorRecord actor, GameTime stamp)
            => PursuitLedgerQuery.IsActiveQuarry(world.GuardPursuits, actor.Id, stamp.TotalMinutes);
    }
}
