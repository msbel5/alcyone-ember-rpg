using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Actors.Actions;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Living;
using EmberCrpg.Simulation.Living.Actions;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Living
{
    /// <summary>
    /// Review-mandated coverage: the depth-4 report dedup and the guard-first strike order
    /// were pinned only by the two-day integration gate; these unit tests pin them per call.
    /// </summary>
    public sealed class CascadeSystemsTests
    {
        private static ActorRecord Actor(
            ulong id,
            string name,
            ActorRole role,
            GridPosition position,
            int health = 30,
            int accuracy = 50,
            int baseDamage = 2)
        {
            return new ActorRecord(
                new ActorId(id), name, role,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(health, health), new VitalStat(10, 10), new VitalStat(10, 10)),
                position, accuracy: accuracy, dodge: 10, armor: 0, baseDamage: baseDamage);
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
            var lifecycle = new ActionLifecycleSystem(
                new ActionLogManager(), enableGuardAndCombat: true);
            var hour1 = new GameTime(60);
            world.Events.Append(new WorldEvent(hour1, WorldEventKind.CombatResolved, attacker.Id, new SiteId(1), "maul hits"));
            system.Tick(world, hour1);
            lifecycle.Decide(world, hour1);
            Assert.That(witness.ActionState.CurrentAction, Is.EqualTo(ActorActionType.ReportCrime));
            Assert.That(witness.ActionState.TargetActorId, Is.EqualTo(attacker.Id),
                "the report carries its durable actor target before movement starts");
            lifecycle.Advance(world, hour1);
            lifecycle.Advance(world, new GameTime(61)); // consume ReportCrime success
            var hour2 = new GameTime(120);
            world.Events.Append(new WorldEvent(hour2, WorldEventKind.CombatResolved, attacker.Id, new SiteId(1), "maul hits"));
            system.Tick(world, hour2);
            lifecycle.Decide(world, hour2);
            lifecycle.Advance(world, hour2);

            var memory = world.NpcMemory.GetOrCreate(witness.Id);
            Assert.That(memory.Events.Count(e => e.EventType == "witnessed_attack"), Is.EqualTo(2),
                "each attack is separately witnessed");
            Assert.That(memory.Events.Count(e => e.EventType == "reported_attack"), Is.EqualTo(1),
                "the SAME attacker is reported to the watch exactly once");
        }

        [Test]
        public void WitnessEventVolume_CannotMoveReporterMoreThanOneActionStep()
        {
            var world = World();
            var attacker = Actor(1, "Hound", ActorRole.Enemy, new GridPosition(1, 0));
            var witness = Actor(2, "Witness", ActorRole.Talker, new GridPosition(0, 0));
            var guard = Actor(3, "Watch", ActorRole.Guard, new GridPosition(10, 0));
            world.Actors.Add(attacker);
            world.Actors.Add(witness);
            world.Actors.Add(guard);
            var stamp = new GameTime(60);
            world.Events.Append(new WorldEvent(
                stamp, WorldEventKind.CombatResolved, attacker.Id, new SiteId(1), "hit one"));
            world.Events.Append(new WorldEvent(
                stamp, WorldEventKind.CombatResolved, attacker.Id, new SiteId(1), "hit two"));

            new WitnessResponseSystem().Tick(world, stamp);
            var lifecycle = new ActionLifecycleSystem(
                new ActionLogManager(), enableGuardAndCombat: true);
            lifecycle.Decide(world, stamp);
            var before = witness.Position;
            lifecycle.Advance(world, stamp);

            global::EmberCrpg.Tests.EditMode.TestAssert.Multiple(() =>
            {
                Assert.That(witness.Position.ChebyshevDistanceTo(before), Is.EqualTo(1));
                Assert.That(witness.ActionState.CurrentAction, Is.EqualTo(ActorActionType.ReportCrime));
                Assert.That(witness.ActionState.ProgressTicks, Is.EqualTo(1));
            });
        }

        [Test]
        public void ActionPredation_CivilianCanNeverDie_OnlyMauled()
        {
            // PLAYTEST FIX ('vardigimda kimse yoktu'): 58 travel days of predation depopulated
            // whole towns. Wolves maul; they do not erase settlements. 24 hours of a strong
            // hunter vs a 2-HP civilian must leave the civilian ALIVE with mauled marks.
            var world = World();
            world.Actors.Add(Actor(
                1, "Hound", ActorRole.Enemy, new GridPosition(5, 5),
                accuracy: 100, baseDamage: 20));
            var prey = Actor(2, "Frail", ActorRole.Talker, new GridPosition(6, 5), health: 2);
            world.Actors.Add(prey);
            var lifecycle = new ActionLifecycleSystem(
                new ActionLogManager(), enableGuardAndCombat: true);

            lifecycle.Decide(world, new GameTime(60));
            lifecycle.Advance(world, new GameTime(60));
            lifecycle.Advance(world, new GameTime(61));
            lifecycle.Advance(world, new GameTime(120));

            Assert.That(prey.IsAlive, Is.True, "predation must maul, never kill, a civilian");
            Assert.That(world.Events.Events.Any(e => e.Kind == WorldEventKind.CombatResolved), Is.True,
                "strikes must actually have landed for this test to mean anything");
        }

        [Test]
        public void ActionPredation_ProducesOneAuthoritativeCombatEventPerStrike()
        {
            var world = World();
            var hunter = Actor(1, "Hound", ActorRole.Enemy, new GridPosition(5, 5));
            world.Actors.Add(hunter);
            world.Actors.Add(Actor(2, "Prey", ActorRole.Talker, new GridPosition(6, 5)));
            var lifecycle = new ActionLifecycleSystem(
                new ActionLogManager(), enableGuardAndCombat: true);

            lifecycle.Decide(world, new GameTime(60));
            lifecycle.Advance(world, new GameTime(60));
            lifecycle.Advance(world, new GameTime(61));
            lifecycle.Advance(world, new GameTime(120));
            lifecycle.Advance(world, new GameTime(120));

            Assert.That(world.Events.Events.Count(e =>
                    e.Kind == WorldEventKind.CombatResolved
                    && e.ActorId.Equals(hunter.Id)), Is.EqualTo(1),
                "one autonomous strike action emits one CombatResolved event");
        }
    }
}
