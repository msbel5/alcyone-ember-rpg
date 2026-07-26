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
        // W34 WORK: the composer's recipe resolver (ProductionRecipeRegistry behind a null-on-
        // unknown wrapper). Null disables the WORK rules (bare EAT/FARM test worlds) — the
        // _plantSpecies null contract's mirror (docs/ruh/w34/02 §6).
        private readonly System.Func<RecipeId, RecipeDef> _resolveRecipe;
        // W36 GUARD+COMBAT feature flag: DEFAULT OFF preserves the W35 tick surface so pre-W36
        // fixtures (test worlds that do NOT expect enemies/guards to acquire action state) stay
        // green. Composer sets it TRUE to enable the vertical slice; story tests do the same.
        // When OFF, TryDecideWatch/TryDecideHunt are no-ops and the three W36 advancer slots
        // stay unregistered (never dispatched — Decide never opens their intents).
        private readonly bool _enableGuardAndCombat;

        public ActionLifecycleSystem(ActionLogManager log,
            System.Collections.Generic.IReadOnlyList<PlantSpeciesDef> plantSpecies = null,
            System.Func<RecipeId, RecipeDef> resolveRecipe = null,
            bool enableGuardAndCombat = false)
        {
            log ??= new ActionLogManager();
            _plantSpecies = plantSpecies;
            _resolveRecipe = resolveRecipe;
            _enableGuardAndCombat = enableGuardAndCombat;
            var advancers = new System.Collections.Generic.List<ActionAdvancer>
            {
                new MoveToFoodAdvancer(log),
                new TakeFoodAdvancer(log),
                new ConsumeFoodAdvancer(log),
                new MoveToPlotAdvancer(log, plantSpecies),
                new PlantSeedAdvancer(log, plantSpecies),
                new HarvestCropAdvancer(log, plantSpecies),
                new HaulCropAdvancer(log),
                // W34 SLEEP: night commute + in-bed recovery (docs/ruh/w34/01 §5).
                new MoveToBedAdvancer(log),
                new SleepAdvancer(log),
                // W34 WORK: bench commute + embodied production (docs/ruh/w34/02 §7).
                new MoveToWorksiteAdvancer(log),
                new PerformWorkAdvancer(log, resolveRecipe),
            };
            if (enableGuardAndCombat)
            {
                // W36 GUARD+COMBAT: guard beat + enemy approach->strike loop. Registered ONLY
                // when the flag is on so a pre-W36 test that constructs the lifecycle without
                // an ActorRole.Enemy hunter never dispatches the new strategies. Registry stays
                // sized for the enum tail; unused slots stay null (safe, gated at Decide).
                advancers.Add(new OnWatchAdvancer(log));
                advancers.Add(new HuntAdvancer(log));
                advancers.Add(new StrikeQuarryAdvancer(log));
            }
            _registry = new ActionAdvancerRegistry(advancers.ToArray());
        }

        /// <summary>Decide phase (@PerTick:18): expiry sweep, then EatIntent + reservation +
        /// MoveToFood for idle hungry civilians. Cheap gates first; the pile cache is built
        /// LAZILY so a fed/busy town pays field reads only (W32-02 §3.2).</summary>
        public void Decide(WorldState world, GameTime stamp)
        {
            if (world?.Actors == null) return;
            // Safety net: rows the fail paths missed (dead actors, mis-sized TTLs) — W32-02 §4.4.
            world.Reservations?.SweepExpired(stamp.TotalMinutes, null);
            // W34 WORK (docs/ruh/w34/02 §6.3): orphan order rows (job cancelled / externally
            // removed) refund their consumed inputs and leave — matter conservation's safety net.
            SweepOrphanWorkOrders(world, stamp);

            List<string> species = null;
            List<FoodPileCache.Entry> cache = null;
            foreach (var actor in world.Actors.Records)
            {
                if (actor == null || !actor.IsAlive) continue;
                if (actor.Role == ActorRole.Player) continue;
                // W36 GUARD+COMBAT: when the flag is ON, Enemy passes the door and drops into
                // TryDecideHunt below. When OFF (legacy default), the blanket skip stays so
                // pre-W36 test worlds see identical behaviour.
                if (actor.Role == ActorRole.Enemy && !_enableGuardAndCombat) continue;
                // One gate covers Running AND the one-advancement terminal handover states.
                if (actor.ActionState.CurrentAction != ActorActionType.None) continue;
                // W36 GUARD+COMBAT: Enemy's Decide short-circuits into Hunt — enemies eat, farm,
                // work, sleep NONE of these; the only decision they take is the next prey. Guard
                // + Watch is handled at the end of this loop after the eat carve-out.
                if (actor.Role == ActorRole.Enemy)
                {
                    TryDecideHunt(world, actor, stamp);
                    continue;
                }
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
                // W34 SLEEP (docs/ruh/w34/01 §4): sleep loses to hunger — code order IS priority
                // order (W33-02 §5 doctrine); a hungry sleeper eats first and beds down on the
                // next decision tick. Threshold >= 1 keeps the fiat's "Fatigue > 0" gate verbatim;
                // the guard live-pursuit gate above already covers this rule (chase outranks bed).
                if (SleepOperations.IsNightHour(stamp.Hour)
                    && actor.Needs.Fatigue.Value >= SleepOperations.FatigueSleepThreshold)
                {
                    TryDecideRest(world, actor, stamp);
                    if (actor.ActionState.CurrentAction != ActorActionType.None) continue;
                }
                // W33-02 §5: the farm/work rules fire ONLY when eat produced no decision; rule
                // order is code order — a fixed, deterministic priority. Cheap gates first.
                if (actor.Role == ActorRole.Guard)
                {
                    // W36 GUARD+COMBAT: idle guard with no pursuit and no eat need walks the
                    // beat. Live pursuit was already gated at the top (HasLivePursuit), so this
                    // path only fires when the watch has NOTHING better to do than stand post.
                    if (_enableGuardAndCombat)
                        TryDecideWatch(world, actor, stamp);
                    continue; // the watch does not farm or work
                }
                if (!ScheduleSystem.IsWorkHour(stamp)) continue; // no fields or benches at night
                if (!actor.ScheduleState.IsIdle)
                {
                    // W34 (docs/ruh/w34/02 §6): a claimed actor's JOB KIND routes the chain —
                    // Farmer stays on the W33 plant branch; every other kind is EMBODIED work.
                    if (JobKindOf(world, actor) == JobKind.Farmer)
                    {
                        if (_plantSpecies != null && _plantSpecies.Count > 0)
                            TryDecidePlant(world, actor, stamp);
                    }
                    else
                    {
                        TryDecideWork(world, actor, stamp);
                    }
                }
                else if (_plantSpecies != null && _plantSpecies.Count > 0)
                {
                    TryDecideHarvest(world, actor, stamp);
                }
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
            // W34: (Rest, Sleep) -> None via the default arm: dawn completes, Idle follows,
            // and the morning decision starts clean (docs/ruh/w34/01 §4).
            (ActorIntent.Rest, ActorActionType.MoveToBed) => ActorActionType.Sleep,
            // W34 WORK: bench commute hands over to embodied production; (Work, PerformWork)
            // -> None via the default arm — the commit frees the actor (docs/ruh/w34/02 §4).
            (ActorIntent.Work, ActorActionType.MoveToWorksite) => ActorActionType.PerformWork,
            // W36 GUARD+COMBAT: the FIRST cyclic NextLink — Hunt ⇄ StrikeQuarry loops while
            // the HuntTargets row survives; StrikeQuarry clears its own row on kill/clamp so
            // the next Advance's Hunt fails NoFoodFound → Idle (the terminating condition).
            // Watch chain is a single-link OnWatch → None: chain re-arms on the next Decide.
            (ActorIntent.Hunt, ActorActionType.Hunt) => ActorActionType.StrikeQuarry,
            (ActorIntent.Hunt, ActorActionType.StrikeQuarry) => ActorActionType.Hunt,
            _ => ActorActionType.None,
        };

        /// <summary>The kind of the actor's claimed job; None when the claim is gone (the
        /// cancel/sweep race — TryDecideWork's own gates handle that honestly).</summary>
        private static JobKind JobKindOf(WorldState world, ActorRecord actor)
        {
            var jobId = actor.ScheduleState.CurrentJobId;
            if (jobId.IsEmpty || world.Jobs == null) return JobKind.None;
            return world.Jobs.TryGet(jobId, out var request) ? request.Kind : JobKind.None;
        }

        // W34 WORK (docs/ruh/w34/02 §6.3): order rows whose job no longer exists (ghost-cancel,
        // external removal). ProgressTicks > 0 means the current execution's inputs were consumed
        // (the §5.2 funding invariant) — they return to the SITE pile before the row drops, the
        // ConsumeFood-return conservation class. An unresolvable recipe id drops without refund
        // (practically unreachable: rows are only born from resolvable ids).
        private void SweepOrphanWorkOrders(WorldState world, GameTime stamp)
        {
            var orders = world.WorkOrders;
            if (orders == null || orders.Rows == null || orders.Rows.Count == 0 || world.Jobs == null)
                return;
            System.Collections.Generic.List<WorkOrderRecord> orphans = null;
            foreach (var row in orders.Rows)
            {
                if (row == null || world.Jobs.Contains(new JobId(row.JobId))) continue;
                (orphans ??= new System.Collections.Generic.List<WorkOrderRecord>()).Add(row);
            }
            if (orphans == null) return;
            foreach (var row in orphans)
            {
                if (row.ProgressTicks > 0)
                {
                    var recipe = _resolveRecipe?.Invoke(new RecipeId(row.RecipeId));
                    var pile = FarmOperations.FindOrCreatePile(world, new SiteId(row.SiteId));
                    if (recipe != null && pile != null)
                        foreach (var input in recipe.Inputs)
                            pile.Add(input.ItemTag, input.Quantity);
                }
                orders.Remove(row.JobId);
                world.Events?.Append(new WorldEvent(stamp, WorldEventKind.ChronicleEvent,
                    default, new SiteId(row.SiteId),
                    $"work_order_refunded job:{row.JobId} recipe:{row.RecipeId}"));
            }
        }

        // W34 WORK (docs/ruh/w34/02 §6): the jobs→decision bridge for NON-farm claims — the
        // smelt/bake counterpart of TryDecidePlant. The lock is the CLAIM itself; no reservation
        // row is opened (§3: the ledger is 1-row-per-actor and smelt needs two input tags).
        // Gates cheap-to-expensive; every "return" leaves the job CLAIMED and waiting — a
        // benchless/dry site never freezes or ghost-cancels a registered id (seedless-site rule).
        private void TryDecideWork(WorldState world, ActorRecord actor, GameTime stamp)
        {
            if (_resolveRecipe == null) return; // null resolver: WORK rules off (bare test worlds)
            var jobId = actor.ScheduleState.CurrentJobId;
            if (jobId.IsEmpty || world.Jobs == null || world.Worksites == null || world.WorkOrders == null)
                return;
            if (!world.Jobs.TryGet(jobId, out var request)) return; // cancel race: next claim heals
            if (request.Kind == JobKind.Farmer) return;             // defensive; routing already forked
            if (world.Jobs.GetClaimedBy(jobId) != actor.Id) return; // sweep race: defensive
            var recipe = _resolveRecipe(request.RecipeId);
            if (recipe == null) return; // unknown id: econ.jobs' ghost net owns that story
            if (!world.Worksites.TryGet(request.SiteId, request.WorksitePosition, out var worksite)
                || !worksite.IsActive || worksite.Kind != request.WorksiteKind)
                return; // job waits claimed — a cold forge is a pause, not a cancel
            if (!world.WorkOrders.TryGetByJob(jobId.Value, out _))
            {
                // Fresh order: can the pile fund ONE execution? Read-only counts (no clone) —
                // the REAL consumption happens at the bench (PerformWork's progress==0 step).
                var pile = FoodOperations.FindPile(world, request.SiteId.Value);
                foreach (var input in recipe.Inputs)
                    if (pile == null || pile.Get(input.ItemTag) < input.Quantity)
                        return; // job waits claimed; the caravan's restock starts the chain
            }
            // A resume row exists: funding is NOT asked — the inputs are either baked into the
            // row (ProgressTicks > 0) or the bench will ask at progress==0 (docs/ruh/w34/02 §6.5).
            var start = ActorActionState.ForIntent(ActorIntent.Work).Start(
                ActorActionType.MoveToWorksite, request.SiteId, ItemId.Empty,
                ReservationId.Empty, stamp.TotalMinutes, ActionInterruptPolicy.Interruptible);
            _registry.For(ActorActionType.MoveToWorksite).TransitionTo(world, actor, start,
                ActionLogReason.TargetSelected, stamp); // no reservation → ReservationAcquired would lie
        }

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
                    ? actor.Position.ChebyshevDistanceTo(new GridPosition(entry.CentreX, entry.CentreY))
                    : 0L;
                if (dist < bestDist)
                { bestDist = dist; best = entry.Pile; bestTag = tag; bestCx = entry.CentreX; bestCy = entry.CentreY; }
            }
            // No known unit anywhere: the actor falls through to schedule routing, unlogged
            // (per-tick spam lesson — a starving town would write an event storm otherwise).
            if (best == null) return;

            var seat = CommunalSeat.For(new GridPosition(bestCx, bestCy),
                (int)(actor.Id.Value % (ulong)CommunalSeat.SeatCount));
            long walk = actor.Position.ChebyshevDistanceTo(seat);
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

        // W34 SLEEP (docs/ruh/w34/01 §3-§4): the TryDecideEat/TryDecidePlant mould with the
        // actor's OWN Home cell as the bed — a stranger structurally CANNOT reserve a foreign
        // bed because no code path ever targets one. Capacity = living residents of that cell
        // (the residence rule: worldgen's house assignment IS the family definition). The row
        // is the chain's per-step validation gate + the 1-row-per-actor chain-exclusion + the
        // TTL cleanup + the future furniture-bed capacity hook, not a mere formality.
        private void TryDecideRest(WorldState world, ActorRecord actor, GameTime stamp)
        {
            if (world.Reservations == null) return;
            var home = actor.Home;
            // Distance-scaled TTL (1 tick = 1 game minute): walk + sleep-until-dawn + slack.
            long walk = FarmOperations.Chebyshev(actor.Position, home);
            long until = stamp.TotalMinutes + walk + SleepOperations.MinutesUntilDawn(stamp) + 60;
            if (!world.Reservations.TryReserve(0UL, SleepOperations.BedKey(home), actor.Id.Value,
                    until, SleepOperations.ResidentCount(world, home), out var reservationId))
                return; // bed full (family capacity) or a pre-existing row — not tonight, silent
            var start = ActorActionState.ForIntent(ActorIntent.Rest).Start(
                ActorActionType.MoveToBed, default(SiteId), ItemId.Empty,
                new ReservationId(reservationId), stamp.TotalMinutes,
                ActionInterruptPolicy.Interruptible);
            _registry.For(ActorActionType.MoveToBed).TransitionTo(world, actor, start,
                ActionLogReason.ReservationAcquired, stamp);
        }

        // W36 GUARD+COMBAT: the SIMPLEST decide method — no ledger, no reservation. A guard's
        // beat is the actor's OWN DayAnchor cell; two guards may share it (posts are locations,
        // not resources — the FoodPileCache seat-ring exclusivity does NOT apply). The chain
        // re-arms on the next Decide tick after OnWatch's Succeeded returns Idle (one live row
        // per guard via the CurrentAction == None gate above — chain-exclusion is structural).
        // CONSTRAINT (Gate8 personal-space parity): fires ONLY during work hours — off-hours,
        // the guard stays Idle and ScheduleSystem routes them Home (ClassicTarget's night rule).
        // Without this, multiple guards sharing a DayAnchor stack there at night instead of
        // scattering to their homes, and the 2-day soak gate breaks with "the crowd stacks".
        private void TryDecideWatch(WorldState world, ActorRecord actor, GameTime stamp)
        {
            if (!ScheduleSystem.IsWorkHour(stamp)) return; // night: silent, ScheduleSystem routes home
            // Live pursuit outranks the beat AND blocks re-arming it. Without this the guard
            // ping-pongs OnWatch/Failed/Idle each pair of ticks while ScheduleSystem — which
            // could route the chase — sees the non-None action state and skips the actor,
            // trapping the pursuit indefinitely. Idle here lets the schedule take the wheel.
            if (HasLivePursuit(world, actor, stamp)) return;
            var start = ActorActionState.ForIntent(ActorIntent.Watch).Start(
                ActorActionType.OnWatch, default(SiteId), ItemId.Empty,
                ReservationId.Empty, stamp.TotalMinutes,
                ActionInterruptPolicy.Interruptible);
            _registry.For(ActorActionType.OnWatch).TransitionTo(world, actor, start,
                ActionLogReason.TargetSelected, stamp); // no reservation → ReservationAcquired would lie
        }

        // W36 GUARD+COMBAT: enemy's TryDecidePlant equivalent — pick nearest prey within
        // HuntRadius (Chebyshev, first-wins in ActorStore order), open a HuntTargets row
        // (RegisterHunt: PursuitRecord's mirror; newest prey per hunter wins — same overwrite
        // semantics), then start Hunt. Empty scan = no decision this tick (the hunter waits,
        // never spam-logs). This is the ONLY writer of World.HuntTargets on the Decide slot.
        private void TryDecideHunt(WorldState world, ActorRecord actor, GameTime stamp)
        {
            var prey = CombatOperations.Nearest(world, actor.Position,
                CombatOperations.HuntRadius, CombatOperations.IsPrey);
            if (prey == null) return; // no prey in range: quiet — a starving town's soundtrack
            RegisterHunt(world, actor.Id.Value, prey.Id.Value, stamp);
            var start = ActorActionState.ForIntent(ActorIntent.Hunt).Start(
                ActorActionType.Hunt, default(SiteId), ItemId.Empty,
                ReservationId.Empty, stamp.TotalMinutes,
                ActionInterruptPolicy.Interruptible); // interruptible: a guard-strike stops the hunt
            _registry.For(ActorActionType.Hunt).TransitionTo(world, actor, start,
                ActionLogReason.TargetSelected, stamp); // no reservation row → TargetSelected reason
        }

        // W36 GUARD+COMBAT: newest prey wins per hunter (mirror of WitnessResponseSystem's
        // RegisterPursuit overwrite semantics). Row lifetime is bounded (HuntMinutes); an
        // unresolved row is pruned by the advancer's TTL check next tick.
        private static void RegisterHunt(WorldState world, ulong hunterId, ulong targetId, GameTime stamp)
        {
            world.HuntTargets ??= new System.Collections.Generic.List<HuntTargetRecord>();
            foreach (var row in world.HuntTargets)
                if (row.HunterId == hunterId)
                {
                    row.TargetId = targetId;
                    row.UntilMinutes = stamp.TotalMinutes + CombatOperations.HuntMinutes;
                    return;
                }
            world.HuntTargets.Add(new HuntTargetRecord
            {
                HunterId = hunterId,
                TargetId = targetId,
                UntilMinutes = stamp.TotalMinutes + CombatOperations.HuntMinutes,
            });
        }
    }
}
