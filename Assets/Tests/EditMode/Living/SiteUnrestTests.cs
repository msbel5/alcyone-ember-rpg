using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Living;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Living
{
    /// <summary>P2 pin: crime pressure accumulates, days grind it down, and past the
    /// threshold the WHOLE watch sweeps at once.</summary>
    public sealed class SiteUnrestTests
    {
        private static ActorRecord Actor(ulong id, string name, ActorRole role, GridPosition position)
        {
            return new ActorRecord(
                new ActorId(id), name, role,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(30, 30), new VitalStat(10, 10), new VitalStat(10, 10)),
                position, accuracy: 50, dodge: 10, armor: 0, baseDamage: 2);
        }

        private static WorldState World()
        {
            var world = new WorldState();
            world.EnsureInvariants();
            world.Sites.Add(new SiteRecord(new SiteId(1), SiteKind.Settlement, "Town",
                new GridPosition(0, 0), new GridPosition(30, 30)));
            return world;
        }

        private static void Report(WorldState world, ActorRecord attacker, int hour)
        {
            var stamp = new GameTime(hour * 60);
            world.Events.Append(new WorldEvent(stamp, WorldEventKind.CombatResolved,
                attacker.Id, new SiteId(1), "maul hits"));
            new WitnessResponseSystem().Tick(world, stamp);
        }

        [Test]
        public void RepeatedReports_CrossTheThreshold_AndTheWholeWatchSweeps()
        {
            var world = World();
            var attacker = Actor(1, "Hound", ActorRole.Enemy, new GridPosition(5, 5));
            world.Actors.Add(attacker);
            world.Actors.Add(Actor(2, "Witness", ActorRole.Talker, new GridPosition(6, 5)));
            world.Actors.Add(Actor(3, "NearWatch", ActorRole.Guard, new GridPosition(9, 5)));
            world.Actors.Add(Actor(4, "FarWatch", ActorRole.Guard, new GridPosition(28, 28)));

            for (int hour = 1; hour <= 3; hour++) Report(world, attacker, hour);

            Assert.That(world.Events.Events.Any(e =>
                e.Kind == WorldEventKind.ChronicleEvent
                && e.Reason != null && e.Reason.StartsWith("watch_sweep")), Is.True,
                "three reported attacks in a day must trip the sweep");
            Assert.That(world.GuardPursuits.Any(p => p.GuardId == 4UL && p.TargetId == 1UL), Is.True,
                "the sweep arms even the guard on the FAR side of town");
        }

        [Test]
        public void ContinuousTrouble_SweepsOncePerDay_AndReArmsAfterTheCooldown()
        {
            var world = World();
            var attacker = Actor(1, "Hound", ActorRole.Enemy, new GridPosition(5, 5));
            world.Actors.Add(attacker);
            world.Actors.Add(Actor(2, "Witness", ActorRole.Talker, new GridPosition(6, 5)));
            world.Actors.Add(Actor(3, "Watch", ActorRole.Guard, new GridPosition(9, 5)));
            world.Actors.Add(Actor(4, "Watch2", ActorRole.Guard, new GridPosition(11, 5)));

            for (int hour = 1; hour <= 12; hour++) Report(world, attacker, hour);

            int Sweeps() => world.Events.Events.Count(e =>
                e.Kind == WorldEventKind.ChronicleEvent
                && e.Reason != null && e.Reason.StartsWith("watch_sweep"));
            Assert.That(Sweeps(), Is.EqualTo(1),
                "a day of continuous trouble is ONE sweep, not a chronicle flood");

            Report(world, attacker, 26); // past the one-day cooldown
            Assert.That(Sweeps(), Is.EqualTo(2),
                "the watch re-arms once the cooldown lapses");
        }

        [Test]
        public void Unrest_DecaysWithTheDays()
        {
            var world = World();
            var attacker = Actor(1, "Hound", ActorRole.Enemy, new GridPosition(5, 5));
            world.Actors.Add(attacker);
            world.Actors.Add(Actor(2, "Witness", ActorRole.Talker, new GridPosition(6, 5)));
            world.Actors.Add(Actor(3, "Watch", ActorRole.Guard, new GridPosition(9, 5)));

            Report(world, attacker, 1);
            int after = world.SiteUnrest.First(u => u.SiteId.Equals(new SiteId(1))).Unrest;

            Report(world, attacker, 1 + 24 * 3); // three days later
            int later = world.SiteUnrest.First(u => u.SiteId.Equals(new SiteId(1))).Unrest;
            Assert.That(later, Is.LessThanOrEqualTo(after + 2 - 2),
                "three quiet days must have ground pressure down before the new report added its 2");
        }
    }
}
