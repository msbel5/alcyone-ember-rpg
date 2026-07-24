using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Living;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Living
{
    /// <summary>
    /// P0 pin (ARCHITECTURE_GAPS #2): the watch CHASES. The witness report arms a pursuit,
    /// the PerTick schedule runs it at full speed, and expiry hands the guard back to its post.
    /// </summary>
    public sealed class GuardPursuitTests
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

        [Test]
        public void WitnessReport_ArmsAPursuit_ForGuardsInEarshot()
        {
            var world = World();
            var attacker = Actor(1, "Hound", ActorRole.Enemy, new GridPosition(5, 5));
            world.Actors.Add(attacker);
            world.Actors.Add(Actor(2, "Witness", ActorRole.Talker, new GridPosition(6, 5)));
            world.Actors.Add(Actor(3, "Watch", ActorRole.Guard, new GridPosition(12, 5)));

            var hour = new GameTime(60);
            world.Events.Append(new WorldEvent(hour, WorldEventKind.CombatResolved, attacker.Id, new SiteId(1), "maul hits"));
            new WitnessResponseSystem().Tick(world, hour);

            Assert.That(world.GuardPursuits.Any(p => p.GuardId == 3UL && p.TargetId == 1UL), Is.True,
                "a guard within earshot must arm a chase, not just nudge one tile an hour");
        }

        [Test]
        public void Advance_PursuingGuard_ClosesEveryTick_InsteadOfRubberBanding()
        {
            var world = World();
            var guard = Actor(3, "Watch", ActorRole.Guard, new GridPosition(0, 0))
                .WithHomeAndAnchor(new GridPosition(0, 0), new GridPosition(0, 0));
            var quarry = Actor(1, "Hound", ActorRole.Enemy, new GridPosition(8, 0))
                .WithHomeAndAnchor(new GridPosition(8, 0), new GridPosition(20, 0));
            world.Actors.Add(guard);
            world.Actors.Add(quarry);
            world.GuardPursuits.Add(new PursuitRecord { GuardId = 3UL, TargetId = 1UL, UntilMinutes = 600 });

            var schedule = new ScheduleSystem();
            for (int tick = 1; tick <= 7; tick++)
                schedule.Advance(world.Actors, new GameTime(60 + tick), world.GuardPursuits);

            int dist = System.Math.Max(
                System.Math.Abs(world.Actors.Get(new ActorId(3)).Position.X - world.Actors.Get(new ActorId(1)).Position.X),
                System.Math.Abs(world.Actors.Get(new ActorId(3)).Position.Y - world.Actors.Get(new ActorId(1)).Position.Y));
            Assert.That(dist, Is.LessThanOrEqualTo(2),
                "seven ticks must close an 8-cell gap - the post writer may no longer erase the chase");
        }

        [Test]
        public void Advance_ExpiredPursuit_IsPruned_AndTheWatchGoesHome()
        {
            var world = World();
            var guard = Actor(3, "Watch", ActorRole.Guard, new GridPosition(5, 5))
                .WithHomeAndAnchor(new GridPosition(0, 0), new GridPosition(0, 0));
            var quarry = Actor(1, "Hound", ActorRole.Enemy, new GridPosition(9, 5))
                .WithHomeAndAnchor(new GridPosition(9, 5), new GridPosition(20, 0));
            world.Actors.Add(guard);
            world.Actors.Add(quarry);
            world.GuardPursuits.Add(new PursuitRecord { GuardId = 3UL, TargetId = 1UL, UntilMinutes = 100 });

            new ScheduleSystem().Advance(world.Actors, new GameTime(23 * 60), world.GuardPursuits);

            Assert.That(world.GuardPursuits, Is.Empty, "an expired chase is pruned");
            Assert.That(world.Actors.Get(new ActorId(3)).Position, Is.EqualTo(new GridPosition(4, 4)),
                "off-shift after the chase, the guard steps toward home");
        }
    }
}
