using System;
using EmberCrpg.Domain.Magic;
using NUnit.Framework;

// Design note:
// Pins the deterministic Sprint 5 actor-keyed shield-buff registry: lazy per-actor
// ShieldBuffState ownership with no Unity dependency. Application, tick-down, save/load,
// and combat hookup land in follow-up slices.
namespace EmberCrpg.Tests.EditMode.Magic
{
    /// <summary>Verifies deterministic per-actor shield buff registry shape and lookups.</summary>
    public sealed class ShieldBuffStateRegistryTests
    {
        [Test]
        public void HasState_UntrackedActor_ReturnsFalse()
        {
            var registry = new ShieldBuffStateRegistry();

            Assert.That(registry.HasState("player"), Is.False);
        }

        [Test]
        public void HasState_NullOrWhitespaceActorId_ReturnsFalse()
        {
            var registry = new ShieldBuffStateRegistry();

            Assert.That(registry.HasState(null), Is.False);
            Assert.That(registry.HasState(string.Empty), Is.False);
            Assert.That(registry.HasState("   "), Is.False);
        }

        [Test]
        public void GetOrCreate_NullOrWhitespaceActorId_Throws()
        {
            var registry = new ShieldBuffStateRegistry();

            Assert.Throws<ArgumentException>(() => registry.GetOrCreate(null));
            Assert.Throws<ArgumentException>(() => registry.GetOrCreate(string.Empty));
            Assert.Throws<ArgumentException>(() => registry.GetOrCreate("   "));
        }

        [Test]
        public void GetOrCreate_NewActor_ReturnsFreshState()
        {
            var registry = new ShieldBuffStateRegistry();

            var state = registry.GetOrCreate("player");

            Assert.That(state, Is.Not.Null);
            Assert.That(state.GetTrackedSpellTemplateIds(), Is.Empty);
            Assert.That(registry.HasState("player"), Is.True);
        }

        [Test]
        public void GetOrCreate_SameActorTwice_ReturnsSameInstance()
        {
            var registry = new ShieldBuffStateRegistry();

            var first = registry.GetOrCreate("player");
            var second = registry.GetOrCreate("player");

            Assert.That(second, Is.SameAs(first));
        }

        [Test]
        public void GetOrCreate_DifferentActors_ReturnsDistinctInstances()
        {
            var registry = new ShieldBuffStateRegistry();

            var playerState = registry.GetOrCreate("player");
            var goblinState = registry.GetOrCreate("goblin_01");

            Assert.That(goblinState, Is.Not.SameAs(playerState));
            Assert.That(registry.HasState("player"), Is.True);
            Assert.That(registry.HasState("goblin_01"), Is.True);
        }

        [Test]
        public void GetOrCreate_PreservesPerActorBuffs_AcrossLookups()
        {
            var registry = new ShieldBuffStateRegistry();
            registry.GetOrCreate("player").SetActiveBuff("ember_ward", remainingTicks: 30, magnitude: 4);

            var roundTrip = registry.GetOrCreate("player");

            Assert.That(roundTrip.GetRemainingTicks("ember_ward"), Is.EqualTo(30));
            Assert.That(roundTrip.GetMagnitude("ember_ward"), Is.EqualTo(4));
        }

        [Test]
        public void GetOrCreate_DoesNotLeakStateBetweenActors()
        {
            var registry = new ShieldBuffStateRegistry();
            registry.GetOrCreate("player").SetActiveBuff("ember_ward", remainingTicks: 30, magnitude: 4);

            var goblinState = registry.GetOrCreate("goblin_01");

            Assert.That(goblinState.IsActive("ember_ward"), Is.False);
            Assert.That(goblinState.GetRemainingTicks("ember_ward"), Is.EqualTo(0));
            Assert.That(goblinState.GetMagnitude("ember_ward"), Is.EqualTo(0));
        }

        [Test]
        public void GetOrNull_UntrackedActor_ReturnsNull()
        {
            var registry = new ShieldBuffStateRegistry();

            Assert.That(registry.GetOrNull("player"), Is.Null);
        }

        [Test]
        public void GetOrNull_NullOrWhitespaceActorId_ReturnsNull()
        {
            var registry = new ShieldBuffStateRegistry();

            Assert.That(registry.GetOrNull(null), Is.Null);
            Assert.That(registry.GetOrNull(string.Empty), Is.Null);
            Assert.That(registry.GetOrNull("   "), Is.Null);
        }

        [Test]
        public void GetOrNull_TrackedActor_ReturnsExistingInstance()
        {
            var registry = new ShieldBuffStateRegistry();
            var created = registry.GetOrCreate("player");

            var fetched = registry.GetOrNull("player");

            Assert.That(fetched, Is.SameAs(created));
        }

        [Test]
        public void GetOrNull_DoesNotCreateState()
        {
            var registry = new ShieldBuffStateRegistry();

            registry.GetOrNull("player");

            Assert.That(registry.HasState("player"), Is.False);
            Assert.That(registry.GetTrackedActorIds(), Is.Empty);
        }

        [Test]
        public void GetTrackedActorIds_EmptyRegistry_ReturnsEmpty()
        {
            var registry = new ShieldBuffStateRegistry();

            Assert.That(registry.GetTrackedActorIds(), Is.Empty);
        }

        [Test]
        public void GetTrackedActorIds_ListsAllActorsWithState()
        {
            var registry = new ShieldBuffStateRegistry();
            registry.GetOrCreate("player");
            registry.GetOrCreate("goblin_01");
            registry.GetOrCreate("goblin_02");

            var trackedActorIds = registry.GetTrackedActorIds();

            Assert.That(trackedActorIds, Is.EquivalentTo(new[] { "player", "goblin_01", "goblin_02" }));
        }

        [Test]
        public void Remove_TrackedActor_DropsState()
        {
            var registry = new ShieldBuffStateRegistry();
            registry.GetOrCreate("player").SetActiveBuff("ember_ward", remainingTicks: 30, magnitude: 4);

            registry.Remove("player");

            Assert.That(registry.HasState("player"), Is.False);
            Assert.That(registry.GetOrNull("player"), Is.Null);
            Assert.That(registry.GetTrackedActorIds(), Does.Not.Contain("player"));
        }

        [Test]
        public void Remove_UntrackedActor_IsNoOp()
        {
            var registry = new ShieldBuffStateRegistry();

            Assert.DoesNotThrow(() => registry.Remove("player"));
            Assert.DoesNotThrow(() => registry.Remove(null));
            Assert.DoesNotThrow(() => registry.Remove(string.Empty));
        }

        [Test]
        public void Remove_OneActor_LeavesOthersIntact()
        {
            var registry = new ShieldBuffStateRegistry();
            registry.GetOrCreate("player").SetActiveBuff("ember_ward", remainingTicks: 30, magnitude: 4);
            registry.GetOrCreate("goblin_01").SetActiveBuff("ember_ward", remainingTicks: 12, magnitude: 2);

            registry.Remove("player");

            Assert.That(registry.HasState("goblin_01"), Is.True);
            Assert.That(registry.GetOrNull("goblin_01").GetRemainingTicks("ember_ward"), Is.EqualTo(12));
            Assert.That(registry.GetOrNull("goblin_01").GetMagnitude("ember_ward"), Is.EqualTo(2));
        }

        [Test]
        public void GetOrCreate_AfterRemove_CreatesFreshState()
        {
            var registry = new ShieldBuffStateRegistry();
            registry.GetOrCreate("player").SetActiveBuff("ember_ward", remainingTicks: 30, magnitude: 4);
            registry.Remove("player");

            var rebuilt = registry.GetOrCreate("player");

            Assert.That(rebuilt.IsActive("ember_ward"), Is.False);
            Assert.That(rebuilt.GetTrackedSpellTemplateIds(), Is.Empty);
        }
    }
}
