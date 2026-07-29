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
            pile.Add("coin", 7);
            pile.Add("iron", 5);
            world.Critters.Add(new AmbientCritter
            { Id = 1, SiteId = new SiteId(1), Cell = new GridPosition(10, 10), Kind = "rat" });

            int before = pile.Get("wheat");
            new AmbientLifeSystem().Tick(world, new GameTime(60));

            Assert.That(pile.Get("wheat"), Is.EqualTo(before - 1), "the theft is REAL stock");
            Assert.That(pile.Get("coin"), Is.EqualTo(7));
            Assert.That(pile.Get("iron"), Is.EqualTo(5));
            var theft = world.Events.Events.Single(e => e.Kind == WorldEventKind.VerminTheft);
            Assert.That(theft.Reason, Does.Contain("item:wheat").And.Contain("qty:1").And.Contain("sink:vermin"));
            Assert.That(world.Events.Events.Any(e => e.Kind == WorldEventKind.NeedChanged), Is.False);
            Assert.That(before - pile.Get("wheat"), Is.EqualTo(1),
                "initial - explicit vermin loss = final matter");
        }

        [Test]
        public void Tick_RatAtLarder_WithOnlyNonFood_DoesNotMutateStock()
        {
            var world = World(out var pile);
            pile.Remove("wheat", 50);
            pile.Add("coin", 7);
            pile.Add("iron", 5);
            world.Critters.Add(new AmbientCritter
            { Id = 1, SiteId = new SiteId(1), Cell = new GridPosition(10, 10), Kind = "rat" });

            new AmbientLifeSystem().Tick(world, new GameTime(60));

            Assert.That(pile.Get("coin"), Is.EqualTo(7));
            Assert.That(pile.Get("iron"), Is.EqualTo(5));
            Assert.That(world.Events.Events.Any(e => e.Kind == WorldEventKind.VerminTheft), Is.False);
        }

        [Test]
        public void Tick_WithoutEventLog_FailsClosedBeforeMatterMutation()
        {
            var world = World(out var pile);
            world.Critters.Add(new AmbientCritter
            { Id = 1, SiteId = new SiteId(1), Cell = new GridPosition(10, 10), Kind = "rat" });
            world.Events = null;

            new AmbientLifeSystem().Tick(world, new GameTime(60));

            Assert.That(pile.Get("wheat"), Is.EqualTo(50));
        }

        [Test]
        public void Tick_CatBesideARat_EndsIt()
        {
            var world = World(out _);
            world.Critters.Add(new AmbientCritter
            { Id = 1, SiteId = new SiteId(1), Cell = new GridPosition(3, 3), Kind = "rat" });
            world.Critters.Add(new AmbientCritter
            { Id = 3, SiteId = new SiteId(1), Cell = new GridPosition(3, 4), Kind = "rat" });
            world.Critters.Add(new AmbientCritter
            { Id = 2, SiteId = new SiteId(1), Cell = new GridPosition(4, 3), Kind = "cat" });

            new AmbientLifeSystem().Tick(world, new GameTime(60));

            Assert.That(world.Critters.Count(c => c.Kind == "rat"), Is.EqualTo(1),
                "one cat catches at most one rat per tick");
            Assert.That(world.Events.Events.Count(e => e.Kind == WorldEventKind.CritterCaught), Is.EqualTo(1));
            Assert.That(world.Events.Events.Any(e => e.Kind == WorldEventKind.NeedChanged), Is.False);
        }

        [Test]
        public void Tick_RefillAfterRemoval_KeepsCritterIdsUnique()
        {
            var world = World(out _);
            var system = new AmbientLifeSystem();
            system.Tick(world, new GameTime(60));
            var removed = world.Critters.Where(c => c.Kind == "rat").OrderBy(c => c.Id).First();
            world.Critters.Remove(removed);

            system.Tick(world, new GameTime(120));

            Assert.That(world.Critters.Count(c => c.Kind == "rat"), Is.EqualTo(AmbientLifeSystem.MaxRatsPerSite));
            Assert.That(world.Critters.Select(c => c.Id).Distinct().Count(), Is.EqualTo(world.Critters.Count));
        }
    }
}
