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
    /// <summary>Decides Eat/Plant/Harvest intents for idle civilians and Eat for off-pursuit
    /// guards, and advances active actions one phase-step per tick.</summary>
    public sealed class ActionLifecycleSystem
    {
        private readonly ActionAdvancerRegistry _registry;
        // W33: the composer's ONE plant catalog (same list PlantGrowthStep reads) — species
        // truth cannot fork. Null/empty disables the farm rules (bare EAT test worlds).
        private readonly System.Collections.Generic.IReadOnlyList<PlantSpeciesDef> _plantSpecies;

        public ActionLifecycleSystem(ActionLogManager log,
            System.Collections.Generic.IReadOnlyList<PlantSpeciesDef> plantSpecies = null)
        {
            log ??= new ActionLogManager();
            _plantSpecies = plantSpecies;
            _registry = new ActionAdvancerRegistry(
                new MoveToFoodAdvancer(log),
                new TakeFoodAdvancer(log),
                new ConsumeFoodAdvancer(log),
                new MoveToPlotAdvancer(log, plantSpecies),
                new PlantSeedAdvancer(log, plantSpecies),
                new HarvestCropAdvancer(log, plantSpecies),
                new HaulCropAdvancer(log));
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
                // guards-eat (B09 remainder): the watch eats only when no chase is live — pursuit
                // outranks lunch, mirroring the quarry-side probe (ActionAdvancer.IsPursuitQuarry).
                // READ-ONLY: living.decision is not a declared World.GuardPursuits writer
                // (FieldOwnershipRegistry keeps witness=arms, schedule=resolves/prunes), so
                // dead-quarry/lost-chase pruning stays in ScheduleSystem.TryResolvePursuit.
                if (actor.Role == ActorRole.Guard && HasLivePursuit(world, actor, stamp)) continue;
                if (actor.Needs.Hunger.Value >= NeedConsumptionSystem.HungerEatThreshold)
                {
                    if (species == null)
                    {
                        species = FoodPileCache.FoodTags(world);
                        cache = FoodPileCache.Build(world, species);
                    }
                    // No larder anywhere: eat is undecidable, but the FARM rules below may
                    // still act — a hungry town's harvest IS the way food comes back (W33).
                    if (cache.Count > 0)
                        TryDecideEat(world, actor, species, cache, stamp);
                    if (actor.ActionState.CurrentAction != ActorActionType.None) continue;
                }
                // W33-02 §5: the farm rules fire ONLY when eat produced no decision; rule
                // order is code order — a fixed, deterministic priority. Cheap gates first.
                if (_plantSpecies == null || _plantSpecies.Count == 0) continue;
                if (actor.Role == ActorRole.Guard) continue; // the watch does not farm
                if (!ScheduleSystem.IsWorkHour(stamp)) continue; // no fields at night
                if (!actor.ScheduleState.IsIdle)
                    TryDecidePlant(world, actor, stamp);
                else
                    TryDecideHarvest(world, actor, stamp);
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
                    var next = NextLink(state.CurrentIntent, state.CurrentAction);
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

        // Chains are fixed pipelines derived from the intent (W32-01 §8) — never saved.
        // W33-01 §2.1: MoveToPlot's successor forks on the intent, which is why Plant and
        // Harvest are two enum values instead of one "Farm" plus a saved sub-mode field.
        private static ActorActionType NextLink(ActorIntent intent, ActorActionType current)
            => (intent, current) switch
        {
            (ActorIntent.Eat, ActorActionType.MoveToFood) => ActorActionType.TakeFood,
            (ActorIntent.Eat, ActorActionType.TakeFood) => ActorActionType.ConsumeFood,
            (ActorIntent.Plant, ActorActionType.MoveToPlot) => ActorActionType.PlantSeed,
            (ActorIntent.Harvest, ActorActionType.MoveToPlot) => ActorActionType.HarvestCrop,
            (ActorIntent.Harvest, ActorActionType.HarvestCrop) => ActorActionType.HaulCrop,
            _ => ActorActionType.None,
        };

        // Shape mirrors ActionAdvancer.IsPursuitQuarry — same expiry predicate (<=), keyed on
        // GuardId instead of TargetId. Deliberately NO dead-quarry/40-cell checks: those need
        // pruning to stay cheap, and pruning is living.schedule's job under the single-writer
        // ledger. Worst case a stale-but-unexpired row defers lunch until UntilMinutes passes —
        // bounded and deterministic (the schedule prunes the row the same tick it routes him).
        private static bool HasLivePursuit(WorldState world, ActorRecord actor, GameTime stamp)
        {
            var pursuits = world.GuardPursuits;
            if (pursuits == null) return false;
            for (var i = 0; i < pursuits.Count; i++)
                if (pursuits[i].GuardId == actor.Id.Value && stamp.TotalMinutes <= pursuits[i].UntilMinutes)
                    return true;
            return false;
        }

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

        // W33-02 §5: the jobs→decision bridge — a claimed 5101 becomes a BODIED Plant chain.
        // The claim machine (priority/refusal/worksite) is reused untouched; this only reads
        // the claim it already wrote. Seedless/plotless sites WAIT claimed (the cascade's
        // HasPendingPlanting guard stays correctly quiet) instead of ghost-cancelling — B05's
        // root closes here.
        private void TryDecidePlant(WorldState world, ActorRecord actor, GameTime stamp)
        {
            var jobId = actor.ScheduleState.CurrentJobId;
            if (jobId.IsEmpty || world.Jobs == null || world.Reservations == null) return;
            if (!world.Jobs.TryGet(jobId, out var request)) return; // cancel race: next claim heals
            if (request.Kind != JobKind.Farmer) return;             // the recipe strip's business
            if (world.Jobs.GetClaimedBy(jobId) != actor.Id) return; // sweep race: defensive
            if (!request.RecipeId.Equals(EmberCrpg.Simulation.Process.FarmingJobRequestFactory.PlantCropRecipeId))
                return;
            var species = _plantSpecies[0]; // single-species slice; job→tag mapping is future work
            var pile = FoodOperations.FindPile(world, request.SiteId.Value);
            if (pile == null || pile.Get(species.SpeciesId) <= 0) return; // seed-corn rule: wait
            var soil = FarmOperations.FindFreeSoil(world, request.SiteId, request.WorksitePosition);
            if (soil == null) return; // fully planted: a harvest frees a plot, then this fires
            long walk = FarmOperations.Chebyshev(actor.Position, soil.Position);
            long until = stamp.TotalMinutes + walk + PlantSeedAdvancer.PlantDurationTicks + 60; // W32-02 §4.3
            if (!world.Reservations.TryReserve(soil.SiteId.Value, FarmOperations.PlotKey(soil.Id),
                    actor.Id.Value, until, 1, out var reservationId))
                return; // plot raced away this very tick; replan next tick
            var start = ActorActionState.ForIntent(ActorIntent.Plant).Start(
                ActorActionType.MoveToPlot, soil.SiteId, ItemId.Empty,
                new ReservationId(reservationId), stamp.TotalMinutes,
                ActionInterruptPolicy.Interruptible);
            _registry.For(ActorActionType.MoveToPlot).TransitionTo(world, actor, start,
                ActionLogReason.ReservationAcquired, stamp);
        }

        // W33: "ripe plant → Harvest intent" — the retired HarvestStep's proximity magic
        // replaced by a real decision: nearest unclaimed harvestable plot wins, first-wins
        // ties in Plants.Rows order (the W32 T2 determinism rule, moved to the soil).
        private void TryDecideHarvest(WorldState world, ActorRecord actor, GameTime stamp)
        {
            if (world.Plants == null || world.Soils == null || world.Reservations == null) return;
            SoilComponent bestSoil = null;
            long bestDist = long.MaxValue;
            foreach (var row in world.Plants.Rows)
            {
                var plant = row.Value;
                if (plant == null || !FarmOperations.IsHarvestable(_plantSpecies, plant)) continue;
                var soil = FarmOperations.FindSoilForPlant(world, plant.Id);
                if (soil == null) continue; // orphan plant: EnsureInvariants heals on load
                if (world.Reservations.ReservedCount(soil.SiteId.Value, FarmOperations.PlotKey(soil.Id)) > 0)
                    continue; // plot already claimed — the ledger IS the exclusivity
                long dist = FarmOperations.Chebyshev(actor.Position, plant.Position);
                if (dist < bestDist) { bestDist = dist; bestSoil = soil; }
            }
            if (bestSoil == null) return; // nothing ripe: the plot waits (M6 semantics, unteleported)
            long until = stamp.TotalMinutes + bestDist + HarvestCropAdvancer.HarvestDurationTicks + 60;
            if (!world.Reservations.TryReserve(bestSoil.SiteId.Value, FarmOperations.PlotKey(bestSoil.Id),
                    actor.Id.Value, until, 1, out var reservationId))
                return;
            var start = ActorActionState.ForIntent(ActorIntent.Harvest).Start(
                ActorActionType.MoveToPlot, bestSoil.SiteId, ItemId.Empty,
                new ReservationId(reservationId), stamp.TotalMinutes,
                ActionInterruptPolicy.Interruptible);
            _registry.For(ActorActionType.MoveToPlot).TransitionTo(world, actor, start,
                ActionLogReason.ReservationAcquired, stamp);
        }
    }
}
