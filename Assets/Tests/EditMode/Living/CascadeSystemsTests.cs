using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Living;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Living
{
    /// <summary>
    /// Review-mandated coverage: the depth-4 report dedup and the guard-first strike order
    /// were pinned only by the two-day integration gate; these unit tests pin them per call.
    /// </summary>
    public sealed class CascadeSystemsTests
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
                new GridPosition(0, 0), new GridPosition(10, 10)));
            return world;
        }

        [Test]
        public void WitnessTick_SameAttackerTwice_FilesExactlyOneReport()
        {
            var world = World();
            var attacker = Actor(1, "Hound", ActorRole.Enemy, new GridPosition(5, 5));
            var witness = Actor(2, "Witness", ActorRole.Talker, new GridPosition(6, 5));
            var guard = Actor(3, "Watch", ActorRole.Guard, new GridPosition(6, 6)); // beside the witness
            world.Actors.Add(attacker);
            world.Actors.Add(witness);
            world.Actors.Add(guard);

            var system = new WitnessResponseSystem();
            var hour1 = new GameTime(60);
            world.Events.Append(new WorldEvent(hour1, WorldEventKind.CombatResolved, attacker.Id, new SiteId(1), "maul hits"));
            system.Tick(world, hour1);
            var hour2 = new GameTime(120);
            world.Events.Append(new WorldEvent(hour2, WorldEventKind.CombatResolved, attacker.Id, new SiteId(1), "maul hits"));
            system.Tick(world, hour2);

            var memory = world.NpcMemory.GetOrCreate(witness.Id);
            Assert.That(memory.Events.Count(e => e.EventType == "witnessed_attack"), Is.EqualTo(2),
                "each attack is separately witnessed");
            Assert.That(memory.Events.Count(e => e.EventType == "reported_attack"), Is.EqualTo(1),
                "the SAME attacker is reported to the watch exactly once");
        }

        [Test]
        public void PredationTick_GuardInReach_StrikesTheHunterFirst()
        {
            var world = World();
            world.Actors.Add(Actor(1, "Hound", ActorRole.Enemy, new GridPosition(5, 5)));
            world.Actors.Add(Actor(2, "Prey", ActorRole.Talker, new GridPosition(7, 5)));
            world.Actors.Add(Actor(3, "Watch", ActorRole.Guard, new GridPosition(6, 5))); // within strike reach

            new PredationSystem().Tick(world, new GameTime(60));

            Assert.That(world.Events.Events.Any(e => e.Kind == WorldEventKind.GuardResponded), Is.True,
                "a guard beside a hunter answers BEFORE the hunter feeds");
        }
    }
}
