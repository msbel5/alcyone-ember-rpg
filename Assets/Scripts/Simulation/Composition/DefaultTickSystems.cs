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
                new ScheduleStep(schedule),
                new NeedsStep(needs),
                new CaravanStep(caravans),
                new PlantGrowthStep(plantGrowth, seasonCalendar, plantSpecies),
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
                : base("living.schedule", TickCadence.Hourly, 20)
            {
                _schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));
            }

            public override void Run(in TickContext context)
            {
                if (context.World.Actors != null)
                    _schedule.Advance(context.World.Actors, context.Stamp);
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
                foreach (var actor in world.Actors.Records)
                {
                    if (actor != null)
                        _needs.TickActorNeeds(actor, world.Events, context.Stamp, ticks: 1);
                }
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
                        isSnowing: false);
                }
            }
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
