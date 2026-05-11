using System;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

// Design note:
// Pins Faz 1 migration rail: SliceWorldState's legacy named actor surfaces are
// now deprecated views over ActorStore role lookups. Consumers can keep running
// while new work writes through the store-backed path.
namespace EmberCrpg.Tests.EditMode.World
{
    public sealed class SliceWorldStateActorViewTests
    {
        [Test]
        public void SettingNamedActorView_RegistersRecordInActorStore()
        {
            var world = new SliceWorldState();
            var player = MakeRecord(1, "Warden", ActorRole.Player);

            world.Player = player;

            Assert.That(world.Actors.FirstByRole(ActorRole.Player), Is.SameAs(player));
            Assert.That(world.Player, Is.SameAs(player));
        }

        [Test]
        public void SettingNamedActorViewTwice_ReplacesPreviousRoleRecord()
        {
            var world = new SliceWorldState();
            var first = MakeRecord(1, "Warden", ActorRole.Player);
            var second = MakeRecord(11, "New Warden", ActorRole.Player);

            world.Player = first;
            world.Player = second;

            Assert.That(world.Player, Is.SameAs(second));
            Assert.That(world.Actors.Contains(first.Id), Is.False);
            Assert.That(world.Actors.Count, Is.EqualTo(1));
        }

        [Test]
        public void SettingNamedActorViewWithWrongRole_Throws()
        {
            var world = new SliceWorldState();
            var guard = MakeRecord(4, "Sentinel", ActorRole.Guard);

            Assert.Throws<ArgumentException>(() => world.Player = guard);
        }

        [Test]
        public void FactoryPopulatesStoreBackedNamedViews()
        {
            var world = new SliceWorldFactory().Create(17);

            Assert.That(world.Actors.Count, Is.EqualTo(5));
            Assert.That(world.Actors.FirstByRole(ActorRole.Player), Is.SameAs(world.Player));
            Assert.That(world.Actors.FirstByRole(ActorRole.Talker), Is.SameAs(world.Talker));
            Assert.That(world.Actors.FirstByRole(ActorRole.Merchant), Is.SameAs(world.Merchant));
            Assert.That(world.Actors.FirstByRole(ActorRole.Guard), Is.SameAs(world.Guard));
            Assert.That(world.Actors.FirstByRole(ActorRole.Enemy), Is.SameAs(world.Enemy));
        }

        private static ActorRecord MakeRecord(ulong id, string name, ActorRole role)
        {
            return new ActorRecord(
                new ActorId(id),
                name,
                role,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(
                    new VitalStat(10, 10),
                    new VitalStat(10, 10),
                    new VitalStat(10, 10)),
                new GridPosition(0, 0),
                accuracy: 50,
                dodge: 10,
                armor: 0,
                baseDamage: 1);
        }
    }
}
