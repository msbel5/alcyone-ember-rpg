using System.Collections.Generic;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Actors.Actions;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;

// Design note:
// W32-01 §2 / W32-02 / W32-03: the SINGLE writer of Actor.ActionState. Decide and advance are
// two phases of ONE system (living.decision@PerTick:18 and living.action_advance@PerTick:22)
// so the diagnosis's multi-writer critique gains no new instance. Stateless between calls
// (chunking law); all writes flow through ActionAdvancer.TransitionTo -> ActionLogManager.
namespace EmberCrpg.Simulation.Living.Actions
{
    /// <summary>Decides EatIntent for idle hungry civilians and advances active actions one phase-step per tick.</summary>
    public sealed class ActionLifecycleSystem
    {
        private readonly ActionAdvancerRegistry _registry;

        public ActionLifecycleSystem(ActionLogManager log)
        {
            log ??= new ActionLogManager();
            _registry = new ActionAdvancerRegistry(
                new MoveToFoodAdvancer(log),
                new TakeFoodAdvancer(log),
                new ConsumeFoodAdvancer(log));
        }

        /// <summary>Decide phase (@PerTick:18): expiry sweep, then EatIntent + reservation +
        /// MoveToFood for idle hungry civilians. Cheap gates first; the pile cache is built
        /// LAZILY so a fed/busy town pays field reads only (W32-02 §3.2).</summary>
        public void Decide(WorldState world, GameTime stamp)
        {
            if (world?.Actors == null) return;
            // Safety net: rows the fail paths missed (dead actors, mis-sized TTLs) — W32-02 §4.4.
            world.Reservations?.SweepExpired(stamp.TotalMinutes, null);

            List<string> species = null;
            List<FoodPileCache.Entry> cache = null;
            foreach (var actor in world.Actors.Records)
            {
                if (actor == null || !actor.IsAlive) continue;
                if (actor.Role == ActorRole.Player || actor.Role == ActorRole.Enemy) continue;
                // One gate covers Running AND the one-advancement terminal handover states.
                if (actor.ActionState.CurrentAction != ActorActionType.None) continue;
                if (actor.Needs.Hunger.Value < NeedConsumptionSystem.HungerEatThreshold) continue;
                if (species == null)
                {
                    species = FoodPileCache.FoodTags(world);
                    cache = FoodPileCache.Build(world, species);
                }
                if (cache.Count == 0) return; // no larder anywhere this tick
                TryDecideEat(world, actor, species, cache, stamp);
            }
        }

        /// <summary>Advance phase (@PerTick:22): consume terminal handovers, then one phase-step.
        /// A link started by a handover takes its first step THIS tick, which keeps the W32-03 §4
        /// timeline: arrival T, take T+1, meal T+4.</summary>
        public void Advance(WorldState world, GameTime stamp)
        {
            if (world?.Actors == null) return;
            foreach (var actor in world.Actors.Records)
            {
                if (actor == null || !actor.IsAlive) continue;
                var state = actor.ActionState;
                if (state.CurrentAction == ActorActionType.None) continue;

                if (state.Phase == ActionPhase.Failed)
                {
                    // Reservation was released at the failure gate; replan is next tick's decision.
                    _registry.For(state.CurrentAction).TransitionTo(world, actor,
                        ActorActionState.Idle, ActionAdvancer.ToLogReason(state.FailureReason), stamp);
                    continue;
                }
                if (state.Phase == ActionPhase.Succeeded)
                {
                    var next = NextLink(state.CurrentAction);
                    if (next == ActorActionType.None)
                    {
                        _registry.For(state.CurrentAction).TransitionTo(world, actor,
                            ActorActionState.Idle, ActionLogReason.Completed, stamp);
                        continue;
                    }
                    var started = state.Start(next, state.TargetSiteId, state.TargetItemId,
                        state.ReservationId, state.StartedAtMinutes, state.InterruptPolicy);
                    _registry.For(next).TransitionTo(world, actor, started, ActionLogReason.ProgressTicked, stamp);
                    state = started;
                }
                _registry.For(state.CurrentAction).Advance(world, actor, stamp);
            }
        }

        // The EAT chain is a fixed pipeline derived from the intent (W32-01 §8) — not saved.
        private static ActorActionType NextLink(ActorActionType current) => current switch
        {
            ActorActionType.MoveToFood => ActorActionType.TakeFood,
            ActorActionType.TakeFood => ActorActionType.ConsumeFood,
            _ => ActorActionType.None,
        };

        private void TryDecideEat(WorldState world, ActorRecord actor,
            List<string> species, List<FoodPileCache.Entry> cache, GameTime stamp)
        {
            // Selection is the retired TryEatCached math verbatim: nearest food-bearing pile by
            // Chebyshev to its site centre, siteless piles sort first (dist 0), strict '<' keeps
            // first-wins tie-breaks in stockpile order — with stock measured as EFFECTIVE stock
            // (pile count minus active claims), so the LAST unit is never promised twice.
            StockpileComponent best = null;
            string bestTag = null;
            long bestDist = long.MaxValue;
            int bestCx = 0, bestCy = 0;
            for (var i = 0; i < cache.Count; i++)
            {
                var entry = cache[i];
                string tag = null;
                foreach (var candidate in species)
                    if (entry.Pile.Get(candidate)
                        - world.Reservations.ReservedCount(entry.Pile.SiteId.Value, candidate) > 0)
                    { tag = candidate; break; }
                if (tag == null) continue; // drained or fully claimed
                long dist = entry.HasSite
                    ? System.Math.Max(System.Math.Abs(actor.Position.X - entry.CentreX),
                                      System.Math.Abs(actor.Position.Y - entry.CentreY))
                    : 0L;
                if (dist < bestDist)
                { bestDist = dist; best = entry.Pile; bestTag = tag; bestCx = entry.CentreX; bestCy = entry.CentreY; }
            }
            // No known unit anywhere: the actor falls through to schedule routing, unlogged
            // (per-tick spam lesson — a starving town would write an event storm otherwise).
            if (best == null) return;

            var seat = CommunalSeat.For(new GridPosition(bestCx, bestCy),
                (int)(actor.Id.Value % (ulong)CommunalSeat.SeatCount));
            long walk = System.Math.Max(
                System.Math.Abs(actor.Position.X - seat.X),
                System.Math.Abs(actor.Position.Y - seat.Y));
            // Distance-scaled TTL (1 tick = 1 game minute): walk + chew + slack — W32-02 §4.3.
            long until = stamp.TotalMinutes + walk + ConsumeFoodAdvancer.ConsumeDurationTicks + 60;
            if (!world.Reservations.TryReserve(best.SiteId.Value, bestTag, actor.Id.Value,
                    until, best.Get(bestTag), out var reservationId))
                return; // only a pre-existing row can refuse here; the sweep will clear it

            var start = ActorActionState.ForIntent(ActorIntent.Eat).Start(
                ActorActionType.MoveToFood, best.SiteId, ItemId.Empty,
                new ReservationId(reservationId), stamp.TotalMinutes,
                ActionInterruptPolicy.Interruptible);
            _registry.For(ActorActionType.MoveToFood).TransitionTo(world, actor, start,
                ActionLogReason.ReservationAcquired, stamp);
        }
    }
}
