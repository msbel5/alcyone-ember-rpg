using System;
using System.Linq;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.Time;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Composition;
using EmberCrpg.Simulation.Living;
using EmberCrpg.Simulation.Magic;
using EmberCrpg.Simulation.Process;
using EmberCrpg.Simulation.Time;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Composition
{
    public sealed class WorldTickRegistryTests
    {
        [Test]
        public void Constructor_SortsByCadenceOrderThenId()
        {
            var registry = new WorldTickRegistry(new IWorldTickSystem[]
            {
                new StubStep("b", TickCadence.Hourly, 20),
                new StubStep("a", TickCadence.PerTick, 30),
                new StubStep("c", TickCadence.PerTick, 30),
                new StubStep("d", TickCadence.Daily, 10),
            });

            var ids = registry.Ordered.Select(s => s.Id).ToArray();
            Assert.That(ids, Is.EqualTo(new[] { "a", "c", "b", "d" }));
        }

        [Test]
        public void Constructor_RejectsDuplicateIds()
        {
            Assert.Throws<InvalidOperationException>(() => new WorldTickRegistry(new IWorldTickSystem[]
            {
                new StubStep("dup", TickCadence.PerTick, 10),
                new StubStep("dup", TickCadence.Daily, 10),
            }));
        }

        [Test]
        public void DefaultRegistry_DeclaresCanonicalOrder()
        {
            var registry = DefaultTickSystems.Create(
                new GameTimeAdvanceSystem(DefaultCalendar()),
                new NeedsSystem(),
                new MagicTickDriver(new SpellCooldownService(), new ShieldBuffService()),
                new CaravanSystem(),
                new PlantGrowthSystem(),
                new JobAssignmentSystem(),
                new PriceUpdateSystem(),
                new ScheduleSystem(),
                new FactionReputationDecaySystem(),
                FactionDecayConfig.Default,
                DefaultCalendar(),
                DefaultPlantSpecies());

            var triples = registry.Ordered
                .Select(s => $"{s.Cadence}:{s.Order}:{s.Id}")
                .ToArray();

            Assert.That(triples, Is.EqualTo(new[]
            {
                "PerTick:10:core.time",
                "PerTick:20:core.magic",
                "PerTick:20:living.schedule",
                "PerTick:21:living.companion_follow", // V3: heel-follow after schedule
                "PerTick:22:living.eatOnArrival", // P0: reaching the table IS the meal
                "Hourly:10:econ.jobs",
                "Hourly:15:quest.tick",
                "Hourly:30:living.needs",
                "Hourly:35:living.consumption", // CAN SUYU H1: needs come back DOWN (eat/sleep)
                "Hourly:40:living.predation",   // CAN SUYU H3: NPC-vs-NPC in the sim
                "Hourly:42:living.companion_guard", // V3: companions strike beside the player
                "Hourly:45:living.witness",     // CAN SUYU H3: seen, remembered, answered
                "Daily:10:world.caravans",
                "Daily:20:econ.plantgrowth",
                "Daily:25:world.harvest", // v0.2 F7: same-day grow→harvest→price chain (shipcheck FLAT finding)
                "Daily:27:econ.shortage_response", // CAN SUYU H1+H3: shortage → planting job (first cascade)
                "Daily:28:world.runtime_history", // CAN SUYU H4: history keeps being written
                "Daily:30:econ.prices",
                "Daily:40:politics.faction_decay",
            }));
        }

        private static SeasonCalendar DefaultCalendar()
        {
            return new SeasonCalendar(new[]
            {
                new SeasonDefinition(Season.Spring, 1, 90),
                new SeasonDefinition(Season.Summer, 91, 180),
                new SeasonDefinition(Season.Autumn, 181, 270),
                new SeasonDefinition(Season.Winter, 271, 360),
            });
        }

        private static PlantSpeciesDef[] DefaultPlantSpecies()
        {
            return new[]
            {
                new PlantSpeciesDef(
                    "wheat",
                    "wheat_seed",
                    "wheat_grain",
                    new[]
                    {
                        new PlantGrowthStageDef(new PlantStageId("seed"), "Seed", 1, false),
                        new PlantGrowthStageDef(new PlantStageId("sprout"), "Sprout", 1, false),
                        new PlantGrowthStageDef(new PlantStageId("ripe"), "Ripe", 0, true),
                    },
                    new[]
                    {
                        new PlantGrowthRule(Season.None, true, false),
                    }),
            };
        }

        private sealed class StubStep : IWorldTickSystem
        {
            public StubStep(string id, TickCadence cadence, int order)
            {
                Id = id;
                Cadence = cadence;
                Order = order;
            }

            public string Id { get; }
            public TickCadence Cadence { get; }
            public int Order { get; }
            public void Run(in TickContext context) { }
        }
    }
}
