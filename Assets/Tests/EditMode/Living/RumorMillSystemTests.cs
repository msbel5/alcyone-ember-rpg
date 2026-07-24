using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Living;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Living
{
    /// <summary>P1 pin: the mill turns real events into talk, prunes the stale, never re-mills.</summary>
    public sealed class RumorMillSystemTests
    {
        private static WorldState World()
        {
            var world = new WorldState();
            world.EnsureInvariants();
            world.Sites.Add(new SiteRecord(new SiteId(1), SiteKind.Settlement, "Town",
                new GridPosition(0, 0), new GridPosition(10, 10)));
            return world;
        }

        [Test]
        public void Tick_DistillsEventsOnce_AndCursorNeverReMills()
        {
            var world = World();
            world.Events.Append(new WorldEvent(new GameTime(60), WorldEventKind.NeedChanged,
                default, new SiteId(1), "vermin_theft item:wheat critter:9"));

            var mill = new RumorMillSystem();
            Assert.That(mill.Tick(world, new GameTime(61)), Is.EqualTo(1), "one event, one rumor");
            Assert.That(mill.Tick(world, new GameTime(121)), Is.EqualTo(0), "the cursor never re-mills");
            Assert.That(world.Rumors[0].Text, Does.Contain("Rats"));
        }

        [Test]
        public void Tick_StaleRumors_ArePruned()
        {
            var world = World();
            world.Rumors.Add(new RumorEntry { BornMinutes = 0, SiteId = new SiteId(1), Text = "old news" });
            new RumorMillSystem().Tick(world, new GameTime(RumorMillSystem.LifeMinutes + 61));
            Assert.That(world.Rumors, Is.Empty, "three-day-old talk dies");
        }

        [Test]
        public void PickFor_IsDeterministic_AndPrefersLocalTalk()
        {
            var world = World();
            world.Rumors.Add(new RumorEntry { BornMinutes = 0, SiteId = new SiteId(1), Text = "local tale" });
            world.Rumors.Add(new RumorEntry { BornMinutes = 0, SiteId = new SiteId(2), Text = "far tale" });

            var pick1 = RumorMillSystem.PickFor(world, 42UL, new SiteId(1), new GameTime(600));
            var pick2 = RumorMillSystem.PickFor(world, 42UL, new SiteId(1), new GameTime(700));
            Assert.That(pick1, Is.EqualTo("local tale"), "site-local talk wins");
            Assert.That(pick2, Is.EqualTo(pick1), "same asker, same day, same tale");
        }
    }
}
