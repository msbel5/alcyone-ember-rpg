using System;
using EmberCrpg.Domain.Magic;
using EmberCrpg.Simulation.Magic;
using NUnit.Framework;

// Design note:
// Pins the deterministic Sprint 5 actor-keyed shield-buff tick-down sweep:
// ShieldBuffService.AdvanceTicksForAllActors forwards each tracked actor's bag in a
// ShieldBuffStateRegistry to the single-bag AdvanceTicks. Foundation-only: tests cover
// argument guards, no-op paths, multi-actor independent decay, and per-actor parity vs
// stand-alone AdvanceTicks. No save/load, application, or damage-absorption coverage here.
namespace EmberCrpg.Tests.EditMode.Magic
{
    /// <summary>Verifies actor-keyed shield buff tick-down sweep across a registry.</summary>
    public sealed class ShieldBuffServiceRegistrySweepTests
    {
        [Test]
        public void AdvanceTicksForAllActors_NullRegistry_Throws()
        {
            var service = new ShieldBuffService();

            Assert.Throws<ArgumentNullException>(() => service.AdvanceTicksForAllActors(null, 5));
        }

        [Test]
        public void AdvanceTicksForAllActors_NegativeElapsed_Throws()
        {
            var service = new ShieldBuffService();
            var registry = new ShieldBuffStateRegistry();

            Assert.Throws<ArgumentOutOfRangeException>(() => service.AdvanceTicksForAllActors(registry, -1));
        }

        [Test]
        public void AdvanceTicksForAllActors_ZeroElapsed_IsNoOp()
        {
            var service = new ShieldBuffService();
            var registry = new ShieldBuffStateRegistry();
            var bag = registry.GetOrCreate("hero_a");
            bag.SetActiveBuff("ember_ward", remainingTicks: 30, magnitude: 4);

            service.AdvanceTicksForAllActors(registry, 0);

            Assert.That(bag.GetRemainingTicks("ember_ward"), Is.EqualTo(30));
            Assert.That(bag.GetMagnitude("ember_ward"), Is.EqualTo(4));
        }

        [Test]
        public void AdvanceTicksForAllActors_EmptyRegistry_IsNoOp()
        {
            var service = new ShieldBuffService();
            var registry = new ShieldBuffStateRegistry();

            Assert.DoesNotThrow(() => service.AdvanceTicksForAllActors(registry, 5));
            Assert.That(registry.GetTrackedActorIds(), Is.Empty);
        }

        [Test]
        public void AdvanceTicksForAllActors_DecaysEachActorIndependently()
        {
            var service = new ShieldBuffService();
            var registry = new ShieldBuffStateRegistry();
            var heroBag = registry.GetOrCreate("hero_a");
            heroBag.SetActiveBuff("ember_ward", remainingTicks: 30, magnitude: 4);
            var rivalBag = registry.GetOrCreate("rival_b");
            rivalBag.SetActiveBuff("aegis_pulse", remainingTicks: 12, magnitude: 2);

            service.AdvanceTicksForAllActors(registry, 7);

            Assert.That(heroBag.GetRemainingTicks("ember_ward"), Is.EqualTo(23));
            Assert.That(heroBag.GetMagnitude("ember_ward"), Is.EqualTo(4));
            Assert.That(rivalBag.GetRemainingTicks("aegis_pulse"), Is.EqualTo(5));
            Assert.That(rivalBag.GetMagnitude("aegis_pulse"), Is.EqualTo(2));
        }

        [Test]
        public void AdvanceTicksForAllActors_ExpiresOnlyTheActorsBuffsThatHitZero()
        {
            var service = new ShieldBuffService();
            var registry = new ShieldBuffStateRegistry();
            var heroBag = registry.GetOrCreate("hero_a");
            heroBag.SetActiveBuff("ember_ward", remainingTicks: 4, magnitude: 4);
            var rivalBag = registry.GetOrCreate("rival_b");
            rivalBag.SetActiveBuff("aegis_pulse", remainingTicks: 9, magnitude: 2);

            service.AdvanceTicksForAllActors(registry, 4);

            Assert.That(heroBag.IsActive("ember_ward"), Is.False);
            Assert.That(heroBag.GetTrackedSpellTemplateIds(), Is.Empty);
            Assert.That(rivalBag.IsActive("aegis_pulse"), Is.True);
            Assert.That(rivalBag.GetRemainingTicks("aegis_pulse"), Is.EqualTo(5));
            Assert.That(rivalBag.GetMagnitude("aegis_pulse"), Is.EqualTo(2));
        }

        [Test]
        public void AdvanceTicksForAllActors_IsParityWithPerActorAdvanceTicks()
        {
            var service = new ShieldBuffService();

            var sweepRegistry = new ShieldBuffStateRegistry();
            var sweepHero = sweepRegistry.GetOrCreate("hero_a");
            sweepHero.SetActiveBuff("ember_ward", remainingTicks: 18, magnitude: 4);
            var sweepRival = sweepRegistry.GetOrCreate("rival_b");
            sweepRival.SetActiveBuff("aegis_pulse", remainingTicks: 9, magnitude: 2);
            sweepRival.SetActiveBuff("ember_ward", remainingTicks: 25, magnitude: 1);

            var directHero = new ShieldBuffState();
            directHero.SetActiveBuff("ember_ward", remainingTicks: 18, magnitude: 4);
            var directRival = new ShieldBuffState();
            directRival.SetActiveBuff("aegis_pulse", remainingTicks: 9, magnitude: 2);
            directRival.SetActiveBuff("ember_ward", remainingTicks: 25, magnitude: 1);

            service.AdvanceTicksForAllActors(sweepRegistry, 6);
            service.AdvanceTicks(directHero, 6);
            service.AdvanceTicks(directRival, 6);

            Assert.That(sweepHero.GetRemainingTicks("ember_ward"), Is.EqualTo(directHero.GetRemainingTicks("ember_ward")));
            Assert.That(sweepHero.GetMagnitude("ember_ward"), Is.EqualTo(directHero.GetMagnitude("ember_ward")));
            Assert.That(sweepRival.GetRemainingTicks("aegis_pulse"), Is.EqualTo(directRival.GetRemainingTicks("aegis_pulse")));
            Assert.That(sweepRival.GetMagnitude("aegis_pulse"), Is.EqualTo(directRival.GetMagnitude("aegis_pulse")));
            Assert.That(sweepRival.GetRemainingTicks("ember_ward"), Is.EqualTo(directRival.GetRemainingTicks("ember_ward")));
            Assert.That(sweepRival.GetMagnitude("ember_ward"), Is.EqualTo(directRival.GetMagnitude("ember_ward")));
        }

        [Test]
        public void AdvanceTicksForAllActors_RepeatedCallsAccumulateDecayPerActor()
        {
            var service = new ShieldBuffService();
            var registry = new ShieldBuffStateRegistry();
            var heroBag = registry.GetOrCreate("hero_a");
            heroBag.SetActiveBuff("ember_ward", remainingTicks: 30, magnitude: 4);
            var rivalBag = registry.GetOrCreate("rival_b");
            rivalBag.SetActiveBuff("aegis_pulse", remainingTicks: 30, magnitude: 2);

            service.AdvanceTicksForAllActors(registry, 5);
            service.AdvanceTicksForAllActors(registry, 7);
            service.AdvanceTicksForAllActors(registry, 3);

            Assert.That(heroBag.GetRemainingTicks("ember_ward"), Is.EqualTo(15));
            Assert.That(heroBag.GetMagnitude("ember_ward"), Is.EqualTo(4));
            Assert.That(rivalBag.GetRemainingTicks("aegis_pulse"), Is.EqualTo(15));
            Assert.That(rivalBag.GetMagnitude("aegis_pulse"), Is.EqualTo(2));
        }

        [Test]
        public void AdvanceTicksForAllActors_ActorWithEmptyBag_StaysEmpty()
        {
            var service = new ShieldBuffService();
            var registry = new ShieldBuffStateRegistry();
            var emptyBag = registry.GetOrCreate("hero_a");
            var rivalBag = registry.GetOrCreate("rival_b");
            rivalBag.SetActiveBuff("aegis_pulse", remainingTicks: 12, magnitude: 2);

            service.AdvanceTicksForAllActors(registry, 4);

            Assert.That(emptyBag.GetTrackedSpellTemplateIds(), Is.Empty);
            Assert.That(rivalBag.GetRemainingTicks("aegis_pulse"), Is.EqualTo(8));
        }
    }
}
