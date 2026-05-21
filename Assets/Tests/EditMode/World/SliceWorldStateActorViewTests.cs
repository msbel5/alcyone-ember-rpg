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

            world.ReplaceActorView(ActorRole.Player, player);

            Assert.That(world.Actors.FirstByRole(ActorRole.Player), Is.SameAs(player));
            Assert.That(world.Actors.FirstByRole(ActorRole.Player), Is.SameAs(player));
        }

        [Test]
        public void SettingNamedActorViewTwice_ReplacesPreviousRoleRecord()
        {
            var world = new SliceWorldState();
            var first = MakeRecord(1, "Warden", ActorRole.Player);
            var second = MakeRecord(11, "New Warden", ActorRole.Player);

            world.ReplaceActorView(ActorRole.Player, first);
            world.ReplaceActorView(ActorRole.Player, second);

            Assert.That(world.Actors.FirstByRole(ActorRole.Player), Is.SameAs(second));
            Assert.That(world.Actors.Contains(first.Id), Is.False);
            Assert.That(world.Actors.Count, Is.EqualTo(1));
        }

        [Test]
        public void SettingNamedActorViewWithWrongRole_Throws()
        {
            var world = new SliceWorldState();
            var guard = MakeRecord(4, "Sentinel", ActorRole.Guard);

            Assert.Throws<ArgumentException>(() => world.ReplaceActorView(ActorRole.Player, guard));
        }

        [Test]
        public void SettingNamedActorView_RemovesAllExistingRoleRecordsBeforeAddingNew()
        {
            var world = new SliceWorldState();
            var firstGuard = MakeRecord(4, "Sentinel", ActorRole.Guard);
            var secondGuard = MakeRecord(5, "Patrol", ActorRole.Guard);
            var replacementGuard = MakeRecord(6, "Captain", ActorRole.Guard);
            world.Actors.Add(firstGuard);
            world.Actors.Add(secondGuard);

            world.ReplaceActorView(ActorRole.Guard, replacementGuard);

            Assert.That(world.Actors.FirstByRole(ActorRole.Guard), Is.SameAs(replacementGuard));
            Assert.That(world.Actors.Contains(firstGuard.Id), Is.False);
            Assert.That(world.Actors.Contains(secondGuard.Id), Is.False);
            Assert.That(world.Actors.Count, Is.EqualTo(1));
        }

        [Test]
        public void NewWorldStateStartsWithEmptyCoreStoreRoots()
        {
            var world = new SliceWorldState();

            Assert.That(world.Actors, Is.Not.Null);
            Assert.That(world.Items, Is.Not.Null);
            Assert.That(world.Sites, Is.Not.Null);
            Assert.That(world.Factions, Is.Not.Null);
            Assert.That(world.Events, Is.Not.Null);
            Assert.That(world.Actors.Count, Is.EqualTo(0));
            Assert.That(world.Items.Count, Is.EqualTo(0));
            Assert.That(world.Sites.Count, Is.EqualTo(0));
            Assert.That(world.Factions.Count, Is.EqualTo(0));
            Assert.That(world.Events.IsEmpty, Is.True);
        }

        [Test]
        public void FactoryPopulatesStoreBackedNamedViewsAndKeepsOtherStoreRootsReady()
        {
            var world = new SliceWorldFactory().Create(17);

            Assert.That(world.Actors.Count, Is.EqualTo(5));
            Assert.That(world.Items, Is.Not.Null);
            Assert.That(world.Sites, Is.Not.Null);
            Assert.That(world.Factions, Is.Not.Null);
            Assert.That(world.Events, Is.Not.Null);
            Assert.That(world.Items.Count, Is.EqualTo(0));
            Assert.That(world.Sites.Count, Is.EqualTo(0));
            Assert.That(world.Factions.Count, Is.EqualTo(0));
            Assert.That(world.Events.IsEmpty, Is.True);
            // Codex review on PR #184 (P2): the Batch 1 sweep rewrote both
            // sides of these assertions to the new API, leaving them
            // tautological. The intent is to pin the compatibility contract —
            // the deprecated named property MUST resolve to the same record
            // ActorStore.FirstByRole returns during the obsolescence window.
            // Keep the LEFT side on the deprecated property (warning silenced)
            // and the RIGHT side on the canonical API.
#pragma warning disable CS0618 // intentional cross-API equivalence check
            Assert.That(world.Player, Is.SameAs(world.Actors.FirstByRole(ActorRole.Player)));
            Assert.That(world.Talker, Is.SameAs(world.Actors.FirstByRole(ActorRole.Talker)));
            Assert.That(world.Merchant, Is.SameAs(world.Actors.FirstByRole(ActorRole.Merchant)));
            Assert.That(world.Guard, Is.SameAs(world.Actors.FirstByRole(ActorRole.Guard)));
            Assert.That(world.Enemy, Is.SameAs(world.Actors.FirstByRole(ActorRole.Enemy)));
#pragma warning restore CS0618
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
