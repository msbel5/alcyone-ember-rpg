using System;
using System.Linq;
using EmberCrpg.Simulation.Composition;
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
            // B03: construction lives in DefaultRegistryFixture — the SAME registry the
            // ownership lint derives its known-id set from, so the two can never fork.
            var registry = DefaultRegistryFixture.CreateDefault();

            var triples = registry.Ordered
                .Select(s => $"{s.Cadence}:{s.Order}:{s.Id}")
                .ToArray();

            Assert.That(triples, Is.EqualTo(new[]
            {
                "PerTick:10:core.time",
                "PerTick:18:living.decision", // W32: intent + reservation BEFORE the router runs
                "PerTick:20:core.magic",
                "PerTick:20:living.schedule",
                "PerTick:22:living.action_advance", // W32/PRD-04: all active action movement, including follow
                "Hourly:10:econ.jobs",
                "Hourly:15:quest.tick",
                "Hourly:30:living.needs",
                // W34: Hourly:35:living.consumption RETIRED — the night fatigue fiat died; sleep
                // recovery is now the action strip's MoveToBed→Sleep on PerTick:18/22.
                "Hourly:45:living.witness",     // CAN SUYU H3: seen, remembered, answered
                "Hourly:50:living.ambient",     // P1: rats raid, cats hunt - cheap agents, real stock
                "Hourly:55:living.rumors",      // P1: new events become one-line town talk
                "Daily:10:world.caravans",
                "Daily:20:econ.plantgrowth",
                // W33: Daily:25:world.harvest RETIRED — the fiat teleport died; harvest is now
                // the action strip's MoveToPlot→HarvestCrop→HaulCrop on PerTick:18/22.
                "Daily:27:econ.shortage_response", // CAN SUYU H1+H3: shortage → planting job (first cascade)
                "Daily:28:world.runtime_history", // CAN SUYU H4: history keeps being written
                "Daily:30:econ.prices",
                "Daily:40:politics.faction_decay",
            }));
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
