using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Living;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Living
{
    /// <summary>P1 pin: ambient life has REAL consequences - stolen stock, hunted rats.</summary>
    public sealed class AmbientLifeSystemTests
    {
        private static WorldState World(out StockpileComponent pile)
        {
            var world = new WorldState();
            world.EnsureInvariants();
            world.Sites.Add(new SiteRecord(new SiteId(1), SiteKind.Settlement, "Town",
                new GridPosition(0, 0), new GridPosition(20, 20)));
            pile = new StockpileComponent(new SiteId(1));
            pile.Add("wheat", 50);
            world.Stockpiles.Add(pile);
            return world;
        }

        [Test]
        public void Tick_SpawnsToCaps_Deterministically()
        {
            var world = World(out _);
            new AmbientLifeSystem().Tick(world, new GameTime(60));
            Assert.That(world.Critters.Count(c => c.Kind == "rat"), Is.EqualTo(AmbientLifeSystem.MaxRatsPerSite));
            Assert.That(world.Critters.Count(c => c.Kind == "cat"), Is.EqualTo(AmbientLifeSystem.MaxCatsPerSite));

            var again = World(out _);
            new AmbientLifeSystem().Tick(again, new GameTime(60));
            Assert.That(again.Critters.Select(c => c.Cell).ToArray(),
                Is.EqualTo(world.Critters.Select(c => c.Cell).ToArray()),
                "same seed world, same spawn cells - determinism holds");
        }

        [Test]
        public void Tick_RatAtTheLarder_StealsRealStock()
        {
            var world = World(out var pile);
            world.Critters.Add(new AmbientCritter
            { Id = 1, SiteId = new SiteId(1), Cell = new GridPosition(10, 10), Kind = "rat" });

            int before = pile.Get("wheat");
            new AmbientLifeSystem().Tick(world, new GameTime(60));

            Assert.That(pile.Get("wheat"), Is.EqualTo(before - 1), "the theft is REAL stock");
            Assert.That(world.Events.Events.Any(e => e.Reason != null && e.Reason.StartsWith("vermin_theft")),
                Is.True, "the theft is on the record");
        }

        [Test]
        public void Tick_CatBesideARat_EndsIt()
        {
            var world = World(out _);
            world.Critters.Add(new AmbientCritter
            { Id = 1, SiteId = new SiteId(1), Cell = new GridPosition(3, 3), Kind = "rat" });
            world.Critters.Add(new AmbientCritter
            { Id = 2, SiteId = new SiteId(1), Cell = new GridPosition(4, 3), Kind = "cat" });

            new AmbientLifeSystem().Tick(world, new GameTime(60));

            Assert.That(world.Critters.Any(c => c.Id == 1UL), Is.False, "the cat earned its keep");
            Assert.That(world.Events.Events.Any(e => e.Reason != null && e.Reason.StartsWith("cat_catch")),
                Is.True, "the catch is on the record");
        }
    }
}
