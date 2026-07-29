// Why this file is intentionally long: each adapter mirrors one legacy WorldTickComposer statement so the registry refactor can be reviewed for zero behavior drift.
using System;
using System.Collections.Generic;
using EmberCrpg.Data.Recipes;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Configuration;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.Time;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Living;
using EmberCrpg.Simulation.Magic;
using EmberCrpg.Simulation.Process;
using EmberCrpg.Simulation.Quest;
using EmberCrpg.Simulation.Time;
using EmberCrpg.Simulation.World;

namespace EmberCrpg.Simulation.Composition
{
    public static class DefaultTickSystems
    {
        private static int LowStock => EmberRuntimeOptionsProvider.Current.Tick.LowStockThreshold;
        private static int HighStock => EmberRuntimeOptionsProvider.Current.Tick.HighStockThreshold;
        private static int PriceStep => EmberRuntimeOptionsProvider.Current.Tick.PriceStep;

        public static WorldTickRegistry Create(
            GameTimeAdvanceSystem timeAdvance,
            NeedsSystem needs,
            MagicTickDriver magic,
            CaravanSystem caravans,
            PlantGrowthSystem plantGrowth,
            JobAssignmentSystem jobAssignment,
            PriceUpdateSystem priceUpdate,
            ScheduleSystem schedule,
            FactionReputationDecaySystem factionDecay,
            FactionDecayConfig factionDecayConfig,
            SeasonCalendar seasonCalendar,
            IReadOnlyList<PlantSpeciesDef> plantSpecies)
        {
            // W32 EAT: decide (@18) and advance (@22) are two phases of ONE lifecycle system —
            // Actor.ActionState keeps a single writer while the registry shows both cadence slots.
            // W33: the SAME species list PlantGrowthStep reads feeds the farm rules — one catalog,
            // species truth cannot fork (W33-01 §7.1).
            // W34 WORK: the SAME recipe registry econ.jobs' ghost net resolves feeds the work
            // rules (null on unknown — the ghost net, not the action strip, owns that story).
            // W36 GUARD+COMBAT — LIVE (W39): the vertical slice is ON. enableGuardAndCombat=true
            // registers OnWatchAdvancer + HuntAdvancer + StrikeQuarryAdvancer with the lifecycle,
            // opens TryDecideWatch/TryDecideHunt on the decide slot, and the projection's Guard
            // "on watch" + Enemy "hunting" labels light up through ActionVerbTable rows (never
            // GUESS — the WorldProjection GUESS branches were retired W36-C). Predation stays
            // race-free: PredationSystem skips enemies with a non-None ActionState (single
            // damage writer per tick, declared in FieldOwnershipRegistry as
            // living.action_advance@PerTick:22 on Actor.Vitals). Digest golden re-baselines
            // legitimately when this flips (new HuntTarget events + guard OnWatch cadence enter
            // the event stream); chunking-invariance stays UNCHANGED. Reference: RUH_TESHIS §2.9.
            var actionLifecycle = new EmberCrpg.Simulation.Living.Actions.ActionLifecycleSystem(
                new EmberCrpg.Domain.Actors.Actions.ActionLogManager(
                    new EmberCrpg.Simulation.Living.Actions.ActionLogDebugSink()),
                plantSpecies,
                ResolveProductionRecipe,
                enableGuardAndCombat: true);
            return new WorldTickRegistry(new IWorldTickSystem[]
            {
                new TimeStep(timeAdvance),
                new MagicStep(magic),
                new JobAssignmentStep(jobAssignment),
                new QuestStep(new QuestSystem()),
                new DecisionStep(actionLifecycle),
                new ScheduleStep(schedule),
                new NeedsStep(needs),
                new ActionAdvancementStep(actionLifecycle),
                // W34: living.consumption@Hourly:35 is RETIRED — the positionless night fatigue
                // fiat died; recovery is now the action strip's MoveToBed→Sleep on PerTick:18/22.
                new AmbientLifeStep(),
                new RumorStep(), // CAN SUYU H1: needs finally COME BACK DOWN (eat/sleep)
                new WitnessStep(),      // CAN SUYU H3: attacks are seen, remembered, answered
                new CaravanStep(caravans),
                new PlantGrowthStep(plantGrowth, seasonCalendar, plantSpecies),
                // W33: world.harvest@Daily:25 is RETIRED — the fiat +2/self-replant teleport
                // died; harvesting is now the action strip's MoveToPlot→HarvestCrop→HaulCrop.
                new ShortageResponseStep(), // CAN SUYU H1+H3: shortage → planting job (first cascade)
                new RuntimeHistoryStep(),   // CAN SUYU H4: history keeps being written after worldgen
                new PriceStepSystem(priceUpdate),
                new FactionDecayStep(factionDecay, Normalize(factionDecayConfig)),
            });
        }

        private static FactionDecayConfig Normalize(FactionDecayConfig config)
        {
            return config.DaysPerDecayStep < 1 ? FactionDecayConfig.Default : config;
        }

        // W34 WORK (docs/ruh/w34/02 §6): ProductionRecipeRegistry.Resolve behind a null-on-
        // unknown wrapper for the action strip — the DomainSimulationAdapter delegate precedent.
        // Unknown ids stay econ.jobs' ghost-cancel business; the decide gate just passes them by.
        private static RecipeDef ResolveProductionRecipe(RecipeId id)
        {
            try
            {
                return ProductionRecipeRegistry.Resolve(id);
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }

        private abstract class StepBase : IWorldTickSystem
        {
            protected StepBase(string id, TickCadence cadence, int order)
            {
                Id = id;
                Cadence = cadence;
                Order = order;
            }

            public string Id { get; }
            public TickCadence Cadence { get; }
            public int Order { get; }
            public abstract void Run(in TickContext context);
        }

        private sealed class TimeStep : StepBase
        {
            private readonly GameTimeAdvanceSystem _timeAdvance;

            public TimeStep(GameTimeAdvanceSystem timeAdvance)
                : base("core.time", TickCadence.PerTick, 10)
            {
                _timeAdvance = timeAdvance ?? throw new ArgumentNullException(nameof(timeAdvance));
            }

            public override void Run(in TickContext context)
            {
                context.World.Time = _timeAdvance.Advance(
                    context.World.Time,
                    context.Delta * WorldTickComposer.MinutesPerTick);
            }
        }

        private sealed class MagicStep : StepBase
        {
            private readonly MagicTickDriver _magic;

            public MagicStep(MagicTickDriver magic)
                : base("core.magic", TickCadence.PerTick, 20)
            {
                _magic = magic ?? throw new ArgumentNullException(nameof(magic));
            }

            public override void Run(in TickContext context)
            {
                var world = context.World;
                if (world.PlayerSpellCooldowns != null && world.PlayerShieldBuffs != null)
                    _magic.AdvanceTicks(world.PlayerSpellCooldowns, world.PlayerShieldBuffs, context.Delta);
            }
        }

        private sealed class JobAssignmentStep : StepBase
        {
            private readonly JobAssignmentSystem _jobAssignment;

            public JobAssignmentStep(JobAssignmentSystem jobAssignment)
                : base("econ.jobs", TickCadence.Hourly, 10)
            {
                _jobAssignment = jobAssignment ?? throw new ArgumentNullException(nameof(jobAssignment));
            }

            public override void Run(in TickContext context)
            {
                var world = context.World;
                if (world.Actors == null || world.Jobs == null || world.Worksites == null)
                    return;

                // W33: dead-claimant sweep (deterministic, Requests order). A chain failure
                // leaves the claim with a LIVE actor (who retries); a DEAD claimant would hold
                // the job forever and HasPendingPlanting would refreeze the cascade — the
                // exact B05 resurrection this sweep forbids.
                List<JobId> released = null;
                foreach (var request in world.Jobs.Requests)
                {
                    var claimant = world.Jobs.GetClaimedBy(request.Id);
                    if (claimant.IsEmpty) continue;
                    if (world.Actors.TryGet(claimant, out var holder) && holder != null && holder.IsAlive)
                        continue;
                    (released ??= new List<JobId>()).Add(request.Id);
                }
                if (released != null)
                    foreach (var jobId in released)
                        if (world.Jobs.ReleaseClaim(jobId) && world.Jobs.TryGet(jobId, out var freed))
                            world.Events?.Append(new WorldEvent(context.Stamp, WorldEventKind.ChronicleEvent,
                                default, freed.SiteId, "job_claim_released reason:claimant_dead"));

                while (_jobAssignment.TryAssignNext(world.Actors, world.Jobs, world.Worksites, out var result))
                {
                    if (world.Actors.TryGet(result.ActorId, out var actor) && actor != null)
                    {
                        actor.ApplyScheduleState(ActorScheduleState.Assigned(
                            result.JobId,
                            result.SiteId,
                            result.WorksitePosition));
                    }

                    world.Events?.Append(new WorldEvent(
                        context.Stamp,
                        WorldEventKind.JobAssigned,
                        result.ActorId,
                        result.SiteId,
                        $"job_assigned:{result.JobId.Value}",
                        new ReasonTrace(new[]
                        {
                            $"job:{result.JobId.Value}",
                            $"actor:{result.ActorId.Value}",
                            $"site:{result.SiteId.Value}",
                            $"worksite:{result.WorksitePosition.X},{result.WorksitePosition.Y}",
                        })));
                }

                if (world.Events == null)
                    return;

                List<JobId> ghostJobs = null;
                foreach (var request in world.Jobs.Requests)
                {
                    if (!world.Jobs.IsClaimed(request.Id))
                        continue;

                    // W33: Farmer jobs work EMBODIED — the action strip (decide@18 +
                    // advance@22) walks, plants and Completes them; the remote-progress
                    // recipe strip may not touch them, so 5101/5102 can never reach the
                    // ghost-cancel below again (B05's root closes).
                    if (request.Kind == JobKind.Farmer)
                        continue;

                    try
                    {
                        // W34: resolve-only probe. StartRecipeForClaim is RETIRED from this
                        // step — order birth AND input consumption moved to the bench
                        // (PerformWork's progress==0 step); this step starts NOTHING.
                        ProductionRecipeRegistry.Resolve(request.RecipeId);
                    }
                    catch (KeyNotFoundException)
                    {
                        // Safety net for genuinely unknown recipe ids (kept deliberately):
                        // an unresolvable CLAIMED job would freeze its claimant forever.
                        (ghostJobs ??= new List<JobId>()).Add(request.Id);
                        world.Events.Append(new WorldEvent(context.Stamp, WorldEventKind.ChronicleEvent,
                            default, request.SiteId,
                            $"job_dropped recipe:{request.RecipeId.Value} unregistered"));
                    }
                }

                if (ghostJobs != null)
                    foreach (var ghost in ghostJobs)
                        world.Jobs.Cancel(ghost);

                // W34: TickAssignedJobs is RETIRED from this step — its free-running counter
                // (progress as a function of the CALENDAR, not of labour) died with the WORK
                // slice; the only mover of order progress is living.action_advance@PerTick:22.
                // The SiteRecipeInventory helper retired with it (WorkOperations.SiteIo is the
                // one home now). docs/ruh/w34/02 §8.
            }
        }

        private sealed class ScheduleStep : StepBase
        {
            private readonly ScheduleSystem _schedule;

            public ScheduleStep(ScheduleSystem schedule)
                // PerTick (not Hourly): ScheduleSystem.Advance walks each NPC ONE tile per call, so it must run
                // every tick to read as continuous walking (~1.2 m/s at the 0.83 s tick). Hourly crawled one
                // tile per game-hour — NPCs never reached work/home. Job assignment + needs stay Hourly.
                : base("living.schedule", TickCadence.PerTick, 20)
            {
                _schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));
            }

            public override void Run(in TickContext context)
            {
                // W32 EAT: no food-spot feed — eating is the decision layer's business; the
                // schedule now routes only actionless actors (rest/work/idle + pursuits).
                if (context.World.Actors != null)
                    // B10 §A5: pass the world too so StepToward can consult world.NavView (blocker probe).
                    _schedule.Advance(context.World.Actors, context.Stamp, context.World);
            }
        }

        // W32 EAT decide phase: intent + reservation + MoveToFood for idle hungry civilians.
        // Order 18 < schedule(20): a decided actor is skipped by the router the SAME tick.
        private sealed class DecisionStep : StepBase
        {
            private readonly EmberCrpg.Simulation.Living.Actions.ActionLifecycleSystem _lifecycle;

            public DecisionStep(EmberCrpg.Simulation.Living.Actions.ActionLifecycleSystem lifecycle)
                : base("living.decision", TickCadence.PerTick, 18)
            {
                _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
            }

            public override void Run(in TickContext context)
                => _lifecycle.Decide(context.World, context.Stamp);
        }

        // W32 EAT advance phase: inherits the retired EatOnArrivalStep's PerTick:22 slot so the
        // Needs/Stockpiles write point stays fixed within the tick. CONSTRAINT: stamp is the
        // cadence-BOUNDARY stamp (the catchup contract) — never post-advance world time.
        private sealed class ActionAdvancementStep : StepBase
        {
            private readonly EmberCrpg.Simulation.Living.Actions.ActionLifecycleSystem _lifecycle;

            public ActionAdvancementStep(EmberCrpg.Simulation.Living.Actions.ActionLifecycleSystem lifecycle)
                : base("living.action_advance", TickCadence.PerTick, 22)
            {
                _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
            }

            public override void Run(in TickContext context)
                => _lifecycle.Advance(context.World, context.Stamp);
        }

        private sealed class QuestStep : StepBase
        {
            private readonly QuestSystem _questSystem;

            public QuestStep(QuestSystem questSystem)
                : base("quest.tick", TickCadence.Hourly, 15)
            {
                _questSystem = questSystem ?? throw new ArgumentNullException(nameof(questSystem));
            }

            public override void Run(in TickContext context)
            {
                _questSystem.Tick(context.World);
            }
        }

        // P1 ambient life: rats raid the larder, cats hunt the rats - cheap agents, real stock.
        private sealed class AmbientLifeStep : StepBase
        {
            private readonly EmberCrpg.Simulation.Living.AmbientLifeSystem _life =
                new EmberCrpg.Simulation.Living.AmbientLifeSystem();

            public AmbientLifeStep() : base("living.ambient", TickCadence.Hourly, 50) { }

            public override void Run(in TickContext context)
                => _life.Tick(context.World, context.Stamp);
        }

        // P1 RumorMill: new events become one-line town talk (Hourly:55, after ambient life).
        private sealed class RumorStep : StepBase
        {
            private readonly EmberCrpg.Simulation.Living.RumorMillSystem _mill =
                new EmberCrpg.Simulation.Living.RumorMillSystem();

            public RumorStep() : base("living.rumors", TickCadence.Hourly, 55) { }

            public override void Run(in TickContext context)
                => _mill.Tick(context.World, context.Stamp);
        }

        // CAN SUYU H3: witnesses write REAL memory and the watch converges.
        private sealed class WitnessStep : StepBase
        {
            private readonly EmberCrpg.Simulation.Living.WitnessResponseSystem _witness =
                new EmberCrpg.Simulation.Living.WitnessResponseSystem();
            public WitnessStep() : base("living.witness", TickCadence.Hourly, 45) { }
            public override void Run(in TickContext context) => _witness.Tick(context.World, context.Stamp);
        }

        // CAN SUYU H1+H3: shortage detector sweep + the planting-job response. Order 27 sits
        // between harvest (25) and prices (30) so the sweep sees post-harvest truth.
        private sealed class ShortageResponseStep : StepBase
        {
            private readonly EmberCrpg.Simulation.World.ShortageResponseSystem _response =
                new EmberCrpg.Simulation.World.ShortageResponseSystem();

            public ShortageResponseStep() : base("econ.shortage_response", TickCadence.Daily, 27) { }

            public override void Run(in TickContext context)
            {
                _response.Tick(context.World, context.Stamp);
            }
        }

        private sealed class NeedsStep : StepBase
        {
            private readonly NeedsSystem _needs;

            public NeedsStep(NeedsSystem needs)
                : base("living.needs", TickCadence.Hourly, 30)
            {
                _needs = needs ?? throw new ArgumentNullException(nameof(needs));
            }

            public override void Run(in TickContext context)
            {
                var world = context.World;
                int ticked = 0;
                EmberCrpg.Domain.Core.ActorId anchor = default;
                foreach (var actor in world.Actors.Records)
                {
                    if (actor == null || !actor.IsAlive) continue; // corpses do not hunger (review fix)
                    if (!NeedsSystem.AppliesPressure(actor.Role)) continue;
                    if (ticked == 0) anchor = actor.Id; // deterministic representative for the summary event
                    actor.ApplyNeeds(_needs.TickNeeds(actor.Role, actor.Needs));
                    _needs.RecomputeMood(actor);
                    ticked++;
                }

                // ONE summary event per hourly crossing instead of one per actor: per-actor NeedChanged spam
                // grew the unbounded event log by ~900 entries every game hour (~2M events / ~1GB heap by day
                // 90), and the resulting Gen2 GC pauses were the 1.4-second "slow tick" spikes the profiler
                // pinned on NeedsStep. Needs/mood stay fully deterministic per actor — only the audit trail
                // is summarized. (TickActorNeeds keeps its per-actor event for callers that want the trace.)
                if (ticked > 0 && world.Events != null)
                    world.Events.Append(new EmberCrpg.Domain.World.WorldEvent(
                        context.Stamp,
                        EmberCrpg.Domain.World.WorldEventKind.NeedChanged,
                        anchor,
                        default,
                        "needs_tick_summary",
                        new EmberCrpg.Domain.World.ReasonTrace(new[]
                        {
                            "needs_tick",
                            "actors:" + ticked,
                            "time:" + context.Stamp.TotalMinutes,
                        })));
            }
        }

        private sealed class CaravanStep : StepBase
        {
            private readonly CaravanSystem _caravans;

            public CaravanStep(CaravanSystem caravans)
                : base("world.caravans", TickCadence.Daily, 10)
            {
                _caravans = caravans ?? throw new ArgumentNullException(nameof(caravans));
            }

            public override void Run(in TickContext context)
            {
                var world = context.World;
                if (world.Caravans != null)
                    _caravans.Tick(world.Caravans, world.FindTradeRoute, world.FindStockpile, context.Stamp, world.Events);
            }
        }

        private sealed class PlantGrowthStep : StepBase
        {
            private readonly PlantGrowthSystem _plantGrowth;
            private readonly SeasonCalendar _seasonCalendar;
            private readonly IReadOnlyList<PlantSpeciesDef> _plantSpecies;

            public PlantGrowthStep(
                PlantGrowthSystem plantGrowth,
                SeasonCalendar seasonCalendar,
                IReadOnlyList<PlantSpeciesDef> plantSpecies)
                : base("econ.plantgrowth", TickCadence.Daily, 20)
            {
                _plantGrowth = plantGrowth ?? throw new ArgumentNullException(nameof(plantGrowth));
                _seasonCalendar = seasonCalendar ?? throw new ArgumentNullException(nameof(seasonCalendar));
                _plantSpecies = plantSpecies ?? throw new ArgumentNullException(nameof(plantSpecies));
            }

            public override void Run(in TickContext context)
            {
                var world = context.World;
                if (world.Plants == null || world.Events == null)
                    return;

                var season = _seasonCalendar.TryGetSeason(context.Stamp, out var resolved)
                    ? resolved
                    : Season.Spring;

                for (var i = 0; i < _plantSpecies.Count; i++)
                {
                    _plantGrowth.AdvanceOneDay(
                        _plantSpecies[i],
                        world.Plants,
                        world.Events,
                        context.Stamp,
                        season,
                        // B27 wound-close: coarse "growth pauses in winter" gate — see Slice 2 spec for a real weather roll.
                        isSnowing: season == Season.Winter);
                }
            }
        }

        // CAN SUYU H4: daily event→relation drift + monthly seeded chronicle.
        private sealed class RuntimeHistoryStep : StepBase
        {
            private readonly RuntimeHistorySystem _history = new RuntimeHistorySystem();
            public RuntimeHistoryStep() : base("world.runtime_history", TickCadence.Daily, 28) { }
            public override void Run(in TickContext context) => _history.Tick(context.World, context.Stamp);
        }

        private sealed class PriceStepSystem : StepBase
        {
            private readonly PriceUpdateSystem _priceUpdate;

            public PriceStepSystem(PriceUpdateSystem priceUpdate)
                : base("econ.prices", TickCadence.Daily, 30)
            {
                _priceUpdate = priceUpdate ?? throw new ArgumentNullException(nameof(priceUpdate));
            }

            public override void Run(in TickContext context)
            {
                var world = context.World;
                if (world.Prices == null || world.Stockpiles == null || world.Events == null)
                    return;

                foreach (var stockpile in world.Stockpiles)
                {
                    if (stockpile == null) continue;
                    // B08 ('fiyat kitlikta donuyor'): Entries drops zero-count items, so the
                    // price froze at PEAK scarcity. Reprice the ledger's known pairs for this
                    // site too - a drained item keeps walking up until stock returns.
                    var repriced = new HashSet<string>();
                    foreach (var entry in stockpile.Entries)
                    {
                        repriced.Add(entry.Key);
                        _priceUpdate.Recompute(
                            world.Prices,
                            stockpile,
                            entry.Key,
                            LowStock,
                            HighStock,
                            PriceStep,
                            context.Stamp,
                            world.Events);
                    }
                    List<string> drained = null;
                    foreach (var known in world.Prices.Entries)
                    {
                        if (!known.SiteId.Equals(stockpile.SiteId) || repriced.Contains(known.ItemTag))
                            continue;
                        (drained ??= new List<string>()).Add(known.ItemTag);
                    }
                    if (drained != null)
                        foreach (var tag in drained)
                            _priceUpdate.Recompute(
                                world.Prices,
                                stockpile,
                                tag,
                                LowStock,
                                HighStock,
                                PriceStep,
                                context.Stamp,
                                world.Events);
                }
            }
        }

        private sealed class FactionDecayStep : StepBase
        {
            private readonly FactionReputationDecaySystem _factionDecay;
            private readonly FactionDecayConfig _config;

            public FactionDecayStep(FactionReputationDecaySystem factionDecay, FactionDecayConfig config)
                : base("politics.faction_decay", TickCadence.Daily, 40)
            {
                _factionDecay = factionDecay ?? throw new ArgumentNullException(nameof(factionDecay));
                _config = config;
            }

            public override void Run(in TickContext context)
            {
                var world = context.World;
                if (world.Factions == null || world.Events == null || !ShouldApply(context.Stamp))
                    return;

                _factionDecay.Apply(world.Factions, _config, context.Stamp, world.Events);
            }

            private bool ShouldApply(GameTime stamp)
            {
                var composerDay = stamp.TotalMinutes /
                                  (WorldTickComposer.TicksPerGameDay * WorldTickComposer.MinutesPerTick);
                return composerDay % _config.DaysPerDecayStep == 0;
            }
        }

        // W33 (B06): NextInventoryItemId/ToDisplayName retired with the player-bag output
        // mint — village production is tag-count now; item identity is the player lane's need.
    }
}
