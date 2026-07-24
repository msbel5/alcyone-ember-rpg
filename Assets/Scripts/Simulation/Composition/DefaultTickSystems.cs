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
            return new WorldTickRegistry(new IWorldTickSystem[]
            {
                new TimeStep(timeAdvance),
                new MagicStep(magic),
                new JobAssignmentStep(jobAssignment),
                new QuestStep(new QuestSystem()),
                new ScheduleStep(schedule),
                new CompanionFollowStep(), // V3: companions heel-follow the player, sim-side
                new NeedsStep(needs),
                new EatOnArrivalStep(),
                new ConsumptionStep(),
                new AmbientLifeStep(),
                new RumorStep(), // CAN SUYU H1: needs finally COME BACK DOWN (eat/sleep)
                new PredationStep(),    // CAN SUYU H3: hunters hunt IN the simulation, NPC-vs-NPC
                new CompanionGuardStep(), // V3: companions strike hostiles beside the player
                new WitnessStep(),      // CAN SUYU H3: attacks are seen, remembered, answered
                new CaravanStep(caravans),
                new PlantGrowthStep(plantGrowth, seasonCalendar, plantSpecies),
                new HarvestStep(),
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

                if (world.PlayerInventory == null || world.Events == null)
                    return;

                foreach (var request in world.Jobs.Requests)
                {
                    if (!world.Jobs.IsClaimed(request.Id))
                        continue;

                    RecipeDef recipe;
                    try
                    {
                        recipe = ProductionRecipeRegistry.Resolve(request.RecipeId);
                    }
                    catch (KeyNotFoundException)
                    {
                        // Intentionally silent: content packs may claim jobs whose recipes land in
                        // a later authoring pass. The claim stays queued and resolves once the
                        // recipe registers; per-tick events here would spam the log hourly.
                        continue;
                    }

                    _jobAssignment.StartRecipeForClaim(
                        world.Actors,
                        world.Jobs,
                        world.Worksites,
                        recipe,
                        world.PlayerInventory,
                        request.Id,
                        out _);
                }

                var nextOutputItemId = NextInventoryItemId(world.PlayerInventory);
                _jobAssignment.TickAssignedJobs(
                    world.Actors,
                    world.Jobs,
                    world.Worksites,
                    world.PlayerInventory,
                    world.Events,
                    context.Stamp,
                    output => new InventoryItem(
                        new ItemId(nextOutputItemId++),
                        output.ItemTag,
                        ToDisplayName(output.ItemTag),
                        1));
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
                if (context.World.Actors != null)
                {
                    // OYNANABILIRLIK: every settlement's larder is a food spot; each actor walks
                    // to their NEAREST. (TavernCell routing predates real larders — retired.)
                    var foodSpots = EmberCrpg.Simulation.Living.NeedConsumptionSystem.FoodSpots(context.World);
                    _schedule.Advance(context.World.Actors, context.Stamp, foodSpots, context.World.GuardPursuits);
                }
            }
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

        // P0 arrival meals: the walk-eat-return rhythm resolves the tick the walker ARRIVES -
        // the hour-long standing crowd at the plaza table was the single biggest pile-up source.
        private sealed class EatOnArrivalStep : StepBase
        {
            private readonly EmberCrpg.Simulation.Living.NeedConsumptionSystem _consumption =
                new EmberCrpg.Simulation.Living.NeedConsumptionSystem();

            public EatOnArrivalStep() : base("living.eatOnArrival", TickCadence.PerTick, 22) { }

            public override void Run(in TickContext context)
                => _consumption.TickArrivals(context.World, context.Stamp);
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

        // CAN SUYU H1: the consumption half of the needs loop — hungry actors eat from real
        // stockpiles, tired actors sleep at night. Order 35: right after NeedsStep raises them.
        private sealed class ConsumptionStep : StepBase
        {
            private readonly EmberCrpg.Simulation.Living.NeedConsumptionSystem _consumption =
                new EmberCrpg.Simulation.Living.NeedConsumptionSystem();

            public ConsumptionStep() : base("living.consumption", TickCadence.Hourly, 35) { }

            public override void Run(in TickContext context)
            {
                var world = context.World;
                int hour = (int)((context.Stamp.TotalMinutes / 60) % 24);
                _consumption.Tick(world, hour, context.Stamp);
            }
        }

        // V3 YOLDAŞ: per-tick heel-follow — order 21 runs AFTER living.schedule (20) so the
        // follow step owns a lagging companion's tile for the tick (no schedule/follow jitter).
        private sealed class CompanionFollowStep : StepBase
        {
            private readonly CompanionSystem _companions = new CompanionSystem();
            public CompanionFollowStep() : base("living.companion_follow", TickCadence.PerTick, 21) { }
            public override void Run(in TickContext context) => _companions.TickFollow(context.World);
        }

        // V3 YOLDAŞ: hourly guard strike with predation's deterministic dice.
        private sealed class CompanionGuardStep : StepBase
        {
            private readonly CompanionSystem _companions = new CompanionSystem();
            public CompanionGuardStep() : base("living.companion_guard", TickCadence.Hourly, 42) { }
            public override void Run(in TickContext context) => _companions.TickGuard(context.World, context.Stamp);
        }

        // CAN SUYU H3: predation runs in the SIM (not the render pump) and hits NPCs.
        private sealed class PredationStep : StepBase
        {
            private readonly EmberCrpg.Simulation.Living.PredationSystem _predation =
                new EmberCrpg.Simulation.Living.PredationSystem();
            public PredationStep() : base("living.predation", TickCadence.Hourly, 40) { }
            public override void Run(in TickContext context) => _predation.Tick(context.World, context.Stamp);
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
                    if (ticked == 0) anchor = actor.Id; // deterministic representative for the summary event
                    actor.ApplyNeeds(_needs.TickNeeds(actor.Needs));
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

        // F7/economy-chain (shipcheck "FLAT" finding): plants ripened but NOTHING harvested them — stockpiles
        // and prices sat frozen forever. Daily harvest: every RIPE plant yields 2 units of its species into
        // its site's stockpile and is replanted at seed, closing the growth→stock→price loop.
        private sealed class HarvestStep : StepBase
        {
            public HarvestStep() : base("world.harvest", TickCadence.Daily, 25) { } // growth(20) → harvest(25) → prices(30): same-day chain

            public override void Run(in TickContext context)
            {
                var world = context.World;
                if (world.Plants == null || world.Stockpiles == null) return;

                // snapshot the ripe set first — Replace() during iteration would mutate Rows
                var ripe = new System.Collections.Generic.List<EmberCrpg.Domain.Process.PlantComponent>();
                foreach (var row in world.Plants.Rows)
                    if (row.Value != null && row.Value.StageId.Value == "ripe")
                        ripe.Add(row.Value);

                foreach (var p in ripe)
                {
                    // M6 ("kimse gelip toplamiyor"): no hands near = the plot WAITS ripe.
                    // Villagers pass their fields daily (planting jobs, homes by the belt),
                    // so yields shift by hours, not lost - and the event now names the picker.
                    var hands = EmberCrpg.Simulation.Process.HarvestHandsService.FindHarvester(world, p);
                    if (hands == null) continue;

                    EmberCrpg.Domain.Process.StockpileComponent pile = null;
                    for (int i = 0; i < world.Stockpiles.Count; i++)
                    {
                        var candidate = world.Stockpiles[i];
                        if (candidate != null && candidate.SiteId.Equals(p.SiteId)) { pile = candidate; break; }
                    }
                    if (pile == null)
                    {
                        pile = new EmberCrpg.Domain.Process.StockpileComponent(p.SiteId);
                        world.Stockpiles.Add(pile);
                    }

                    pile.Add(p.SpeciesId, 2); // a ripe plot yields two units
                    // Review fix: harvest mutated stock with ZERO audit trail — PlantHarvested
                    // existed but was never emitted from this step.
                    world.Events?.Append(new EmberCrpg.Domain.World.WorldEvent(
                        context.Stamp, EmberCrpg.Domain.World.WorldEventKind.PlantHarvested,
                        hands.Id, p.SiteId, $"harvested species:{p.SpeciesId} qty:2 by:{hands.Id.Value}"));
                    world.Plants.Replace(p.Id, new EmberCrpg.Domain.Process.PlantComponent(
                        p.Id, p.SiteId, p.Position, p.SpeciesId,
                        new EmberCrpg.Domain.Process.PlantStageId("seed"), 0)); // replant
                }
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
                        isSnowing: false);
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
                    foreach (var entry in stockpile.Entries)
                    {
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

        private static ulong NextInventoryItemId(InventoryState inventory)
        {
            ulong max = 0UL;
            foreach (var item in inventory.Items)
            {
                if (item.Id.Value > max)
                    max = item.Id.Value;
            }

            return max + 1UL;
        }

        private static string ToDisplayName(string itemTag)
        {
            if (string.IsNullOrWhiteSpace(itemTag))
                return "Crafted Item";

            var parts = itemTag.Split('_');
            for (var i = 0; i < parts.Length; i++)
            {
                if (string.IsNullOrEmpty(parts[i]))
                    continue;

                var part = parts[i];
                parts[i] = char.ToUpperInvariant(part[0]) + part.Substring(1);
            }

            return string.Join(" ", parts);
        }
    }
}
