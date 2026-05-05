using System;
using EmberCrpg.Domain.Magic;
using EmberCrpg.Simulation.Magic;
using NUnit.Framework;

// Design note:
// Pins the deterministic Sprint 5 actor-keyed shield-buff damage-absorption seam:
// ShieldBuffService.AbsorbDamageForActor routes incoming damage through one actor's
// ShieldBuffState bag in a ShieldBuffStateRegistry by delegating to the single-bag
// AbsorbDamage. Foundation-only: argument guards, no-op paths, untracked-actor passthrough,
// per-actor parity vs the single-bag AbsorbDamage, and registry-read-only invariant. No
// save/load, application, tick-down, or combat-pipeline coverage here.
namespace EmberCrpg.Tests.EditMode.Magic
{
    /// <summary>Verifies actor-keyed shield buff damage absorption against a registry.</summary>
    public sealed class ShieldBuffServiceRegistryAbsorptionTests
    {
        [Test]
        public void AbsorbDamageForActor_NullRegistry_Throws()
        {
            var service = new ShieldBuffService();

            Assert.Throws<ArgumentNullException>(() => service.AbsorbDamageForActor(null, "hero_a", 5));
        }

        [Test]
        public void AbsorbDamageForActor_NullActorId_Throws()
        {
            var service = new ShieldBuffService();
            var registry = new ShieldBuffStateRegistry();

            Assert.Throws<ArgumentException>(() => service.AbsorbDamageForActor(registry, null, 5));
        }

        [Test]
        public void AbsorbDamageForActor_WhitespaceActorId_Throws()
        {
            var service = new ShieldBuffService();
            var registry = new ShieldBuffStateRegistry();

            Assert.Throws<ArgumentException>(() => service.AbsorbDamageForActor(registry, "   ", 5));
        }

        [Test]
        public void AbsorbDamageForActor_NegativeDamage_Throws()
        {
            var service = new ShieldBuffService();
            var registry = new ShieldBuffStateRegistry();

            Assert.Throws<ArgumentOutOfRangeException>(() => service.AbsorbDamageForActor(registry, "hero_a", -1));
        }

        [Test]
        public void AbsorbDamageForActor_ZeroDamage_ReturnsEmptyTrace()
        {
            var service = new ShieldBuffService();
            var registry = new ShieldBuffStateRegistry();
            var bag = registry.GetOrCreate("hero_a");
            bag.SetActiveBuff("ember_ward", remainingTicks: 30, magnitude: 4);

            var result = service.AbsorbDamageForActor(registry, "hero_a", 0);

            Assert.That(result.IncomingDamage, Is.EqualTo(0));
            Assert.That(result.AbsorbedDamage, Is.EqualTo(0));
            Assert.That(result.RemainingDamage, Is.EqualTo(0));
            Assert.That(result.ConsumedSpellTemplateIds, Is.Empty);
            Assert.That(result.ExpiredSpellTemplateIds, Is.Empty);
            Assert.That(bag.GetMagnitude("ember_ward"), Is.EqualTo(4));
            Assert.That(bag.GetRemainingTicks("ember_ward"), Is.EqualTo(30));
        }

        [Test]
        public void AbsorbDamageForActor_ZeroDamage_DoesNotAddActorToRegistry()
        {
            var service = new ShieldBuffService();
            var registry = new ShieldBuffStateRegistry();

            service.AbsorbDamageForActor(registry, "hero_a", 0);

            Assert.That(registry.HasState("hero_a"), Is.False);
            Assert.That(registry.GetTrackedActorIds(), Is.Empty);
        }

        [Test]
        public void AbsorbDamageForActor_UntrackedActor_ReturnsFullRemainingAndEmptyTrace()
        {
            var service = new ShieldBuffService();
            var registry = new ShieldBuffStateRegistry();

            var result = service.AbsorbDamageForActor(registry, "hero_a", 7);

            Assert.That(result.IncomingDamage, Is.EqualTo(7));
            Assert.That(result.AbsorbedDamage, Is.EqualTo(0));
            Assert.That(result.RemainingDamage, Is.EqualTo(7));
            Assert.That(result.ConsumedSpellTemplateIds, Is.Empty);
            Assert.That(result.ExpiredSpellTemplateIds, Is.Empty);
        }

        [Test]
        public void AbsorbDamageForActor_UntrackedActor_DoesNotAddActorToRegistry()
        {
            var service = new ShieldBuffService();
            var registry = new ShieldBuffStateRegistry();

            service.AbsorbDamageForActor(registry, "hero_a", 12);

            Assert.That(registry.HasState("hero_a"), Is.False);
            Assert.That(registry.GetTrackedActorIds(), Is.Empty);
        }

        [Test]
        public void AbsorbDamageForActor_TrackedActor_PartialConsumeReducesMagnitudeAndPreservesTicks()
        {
            var service = new ShieldBuffService();
            var registry = new ShieldBuffStateRegistry();
            var bag = registry.GetOrCreate("hero_a");
            bag.SetActiveBuff("ember_ward", remainingTicks: 30, magnitude: 4);

            var result = service.AbsorbDamageForActor(registry, "hero_a", 1);

            Assert.That(result.IncomingDamage, Is.EqualTo(1));
            Assert.That(result.AbsorbedDamage, Is.EqualTo(1));
            Assert.That(result.RemainingDamage, Is.EqualTo(0));
            Assert.That(result.ConsumedSpellTemplateIds, Is.EqualTo(new[] { "ember_ward" }));
            Assert.That(result.ExpiredSpellTemplateIds, Is.Empty);
            Assert.That(bag.GetMagnitude("ember_ward"), Is.EqualTo(3));
            Assert.That(bag.GetRemainingTicks("ember_ward"), Is.EqualTo(30));
        }

        [Test]
        public void AbsorbDamageForActor_TrackedActor_ExactConsumeClearsBuffEvenWithTicksLeft()
        {
            var service = new ShieldBuffService();
            var registry = new ShieldBuffStateRegistry();
            var bag = registry.GetOrCreate("hero_a");
            bag.SetActiveBuff("ember_ward", remainingTicks: 30, magnitude: 4);

            var result = service.AbsorbDamageForActor(registry, "hero_a", 4);

            Assert.That(result.AbsorbedDamage, Is.EqualTo(4));
            Assert.That(result.RemainingDamage, Is.EqualTo(0));
            Assert.That(result.ConsumedSpellTemplateIds, Is.EqualTo(new[] { "ember_ward" }));
            Assert.That(result.ExpiredSpellTemplateIds, Is.EqualTo(new[] { "ember_ward" }));
            Assert.That(bag.GetMagnitude("ember_ward"), Is.EqualTo(0));
            Assert.That(bag.GetRemainingTicks("ember_ward"), Is.EqualTo(0));
        }

        [Test]
        public void AbsorbDamageForActor_TrackedActor_OverConsumeReturnsLeftover()
        {
            var service = new ShieldBuffService();
            var registry = new ShieldBuffStateRegistry();
            var bag = registry.GetOrCreate("hero_a");
            bag.SetActiveBuff("ember_ward", remainingTicks: 30, magnitude: 4);

            var result = service.AbsorbDamageForActor(registry, "hero_a", 7);

            Assert.That(result.AbsorbedDamage, Is.EqualTo(4));
            Assert.That(result.RemainingDamage, Is.EqualTo(3));
            Assert.That(result.ConsumedSpellTemplateIds, Is.EqualTo(new[] { "ember_ward" }));
            Assert.That(result.ExpiredSpellTemplateIds, Is.EqualTo(new[] { "ember_ward" }));
            Assert.That(bag.GetMagnitude("ember_ward"), Is.EqualTo(0));
        }

        [Test]
        public void AbsorbDamageForActor_TrackedActor_MultiBuffOrdinalOrderingMirrorsSingleBag()
        {
            var service = new ShieldBuffService();
            var registry = new ShieldBuffStateRegistry();
            var bag = registry.GetOrCreate("hero_a");
            bag.SetActiveBuff("ember_ward", remainingTicks: 30, magnitude: 3);
            bag.SetActiveBuff("aegis_pulse", remainingTicks: 10, magnitude: 2);

            var result = service.AbsorbDamageForActor(registry, "hero_a", 4);

            Assert.That(result.AbsorbedDamage, Is.EqualTo(4));
            Assert.That(result.RemainingDamage, Is.EqualTo(0));
            Assert.That(result.ConsumedSpellTemplateIds, Is.EqualTo(new[] { "aegis_pulse", "ember_ward" }));
            Assert.That(result.ExpiredSpellTemplateIds, Is.EqualTo(new[] { "aegis_pulse" }));
            Assert.That(bag.GetMagnitude("aegis_pulse"), Is.EqualTo(0));
            Assert.That(bag.GetMagnitude("ember_ward"), Is.EqualTo(1));
            Assert.That(bag.GetRemainingTicks("ember_ward"), Is.EqualTo(30));
        }

        [Test]
        public void AbsorbDamageForActor_OnlyAffectsTargetActor_LeavesOtherActorsUntouched()
        {
            var service = new ShieldBuffService();
            var registry = new ShieldBuffStateRegistry();
            var heroBag = registry.GetOrCreate("hero_a");
            heroBag.SetActiveBuff("ember_ward", remainingTicks: 30, magnitude: 4);
            var rivalBag = registry.GetOrCreate("rival_b");
            rivalBag.SetActiveBuff("aegis_pulse", remainingTicks: 12, magnitude: 2);

            var result = service.AbsorbDamageForActor(registry, "hero_a", 3);

            Assert.That(result.AbsorbedDamage, Is.EqualTo(3));
            Assert.That(heroBag.GetMagnitude("ember_ward"), Is.EqualTo(1));
            Assert.That(heroBag.GetRemainingTicks("ember_ward"), Is.EqualTo(30));
            Assert.That(rivalBag.GetMagnitude("aegis_pulse"), Is.EqualTo(2));
            Assert.That(rivalBag.GetRemainingTicks("aegis_pulse"), Is.EqualTo(12));
        }

        [Test]
        public void AbsorbDamageForActor_RepeatedCalls_AccumulateMagnitudeReductionWithoutTickChange()
        {
            var service = new ShieldBuffService();
            var registry = new ShieldBuffStateRegistry();
            var bag = registry.GetOrCreate("hero_a");
            bag.SetActiveBuff("ember_ward", remainingTicks: 30, magnitude: 4);

            service.AbsorbDamageForActor(registry, "hero_a", 1);
            service.AbsorbDamageForActor(registry, "hero_a", 1);

            Assert.That(bag.GetMagnitude("ember_ward"), Is.EqualTo(2));
            Assert.That(bag.GetRemainingTicks("ember_ward"), Is.EqualTo(30));
        }

        [Test]
        public void AbsorbDamageForActor_AfterTickSweepExpiresBuff_NoLongerAbsorbs()
        {
            var service = new ShieldBuffService();
            var registry = new ShieldBuffStateRegistry();
            var bag = registry.GetOrCreate("hero_a");
            bag.SetActiveBuff("ember_ward", remainingTicks: 5, magnitude: 4);

            service.AdvanceTicksForAllActors(registry, 5);
            var result = service.AbsorbDamageForActor(registry, "hero_a", 3);

            Assert.That(result.AbsorbedDamage, Is.EqualTo(0));
            Assert.That(result.RemainingDamage, Is.EqualTo(3));
            Assert.That(result.ConsumedSpellTemplateIds, Is.Empty);
            Assert.That(result.ExpiredSpellTemplateIds, Is.Empty);
        }

        [Test]
        public void AbsorbDamageForActor_TrackedActorEmptyBag_ReturnsFullRemainingAndEmptyTrace()
        {
            var service = new ShieldBuffService();
            var registry = new ShieldBuffStateRegistry();
            registry.GetOrCreate("hero_a");

            var result = service.AbsorbDamageForActor(registry, "hero_a", 9);

            Assert.That(result.IncomingDamage, Is.EqualTo(9));
            Assert.That(result.AbsorbedDamage, Is.EqualTo(0));
            Assert.That(result.RemainingDamage, Is.EqualTo(9));
            Assert.That(result.ConsumedSpellTemplateIds, Is.Empty);
            Assert.That(result.ExpiredSpellTemplateIds, Is.Empty);
        }

        [Test]
        public void AbsorbDamageForActor_TrackedActor_ParityWithDirectAbsorbDamage()
        {
            var serviceA = new ShieldBuffService();
            var registry = new ShieldBuffStateRegistry();
            var registryBag = registry.GetOrCreate("hero_a");
            registryBag.SetActiveBuff("ember_ward", remainingTicks: 20, magnitude: 5);
            registryBag.SetActiveBuff("aegis_pulse", remainingTicks: 8, magnitude: 3);

            var serviceB = new ShieldBuffService();
            var directBag = new ShieldBuffState();
            directBag.SetActiveBuff("ember_ward", remainingTicks: 20, magnitude: 5);
            directBag.SetActiveBuff("aegis_pulse", remainingTicks: 8, magnitude: 3);

            var registryResult = serviceA.AbsorbDamageForActor(registry, "hero_a", 6);
            var directResult = serviceB.AbsorbDamage(directBag, 6);

            Assert.That(registryResult.IncomingDamage, Is.EqualTo(directResult.IncomingDamage));
            Assert.That(registryResult.AbsorbedDamage, Is.EqualTo(directResult.AbsorbedDamage));
            Assert.That(registryResult.RemainingDamage, Is.EqualTo(directResult.RemainingDamage));
            Assert.That(registryResult.ConsumedSpellTemplateIds, Is.EqualTo(directResult.ConsumedSpellTemplateIds));
            Assert.That(registryResult.ExpiredSpellTemplateIds, Is.EqualTo(directResult.ExpiredSpellTemplateIds));
            Assert.That(registryBag.GetMagnitude("ember_ward"), Is.EqualTo(directBag.GetMagnitude("ember_ward")));
            Assert.That(registryBag.GetMagnitude("aegis_pulse"), Is.EqualTo(directBag.GetMagnitude("aegis_pulse")));
            Assert.That(registryBag.GetRemainingTicks("ember_ward"), Is.EqualTo(directBag.GetRemainingTicks("ember_ward")));
        }
    }
}
