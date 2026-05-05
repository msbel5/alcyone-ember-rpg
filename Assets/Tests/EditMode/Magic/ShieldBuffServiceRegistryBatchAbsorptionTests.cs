using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Magic;
using EmberCrpg.Simulation.Magic;
using NUnit.Framework;

// Design note:
// Pins the deterministic Sprint 5 batch actor-keyed shield-buff damage-absorption seam:
// ShieldBuffService.AbsorbDamageForActors routes a per-actor incoming-damage map through the
// existing single-actor AbsorbDamageForActor seam. Foundation-only: argument guards, empty-input
// no-op, untracked-actor passthrough, registry-read-only invariant, multi-actor isolation,
// and parity vs per-actor calls. No save/load, application, tick-down, or combat-pipeline
// coverage here.
namespace EmberCrpg.Tests.EditMode.Magic
{
    /// <summary>Verifies batch actor-keyed shield buff damage absorption against a registry.</summary>
    public sealed class ShieldBuffServiceRegistryBatchAbsorptionTests
    {
        [Test]
        public void AbsorbDamageForActors_NullRegistry_Throws()
        {
            var service = new ShieldBuffService();

            Assert.Throws<ArgumentNullException>(() => service.AbsorbDamageForActors(
                null,
                new Dictionary<string, int> { { "hero_a", 5 } }));
        }

        [Test]
        public void AbsorbDamageForActors_NullIncomingMap_Throws()
        {
            var service = new ShieldBuffService();
            var registry = new ShieldBuffStateRegistry();

            Assert.Throws<ArgumentNullException>(() => service.AbsorbDamageForActors(registry, null));
        }

        [Test]
        public void AbsorbDamageForActors_NullActorKey_Throws()
        {
            var service = new ShieldBuffService();
            var registry = new ShieldBuffStateRegistry();
            var input = new Dictionary<string, int>();
            // Dictionary<string,int> rejects null keys directly, so use whitespace to exercise the same guard.
            input["   "] = 5;

            Assert.Throws<ArgumentException>(() => service.AbsorbDamageForActors(registry, input));
        }

        [Test]
        public void AbsorbDamageForActors_EmptyActorKey_Throws()
        {
            var service = new ShieldBuffService();
            var registry = new ShieldBuffStateRegistry();
            var input = new Dictionary<string, int> { { string.Empty, 5 } };

            Assert.Throws<ArgumentException>(() => service.AbsorbDamageForActors(registry, input));
        }

        [Test]
        public void AbsorbDamageForActors_NegativeDamageValue_Throws()
        {
            var service = new ShieldBuffService();
            var registry = new ShieldBuffStateRegistry();
            var input = new Dictionary<string, int> { { "hero_a", -1 } };

            Assert.Throws<ArgumentOutOfRangeException>(() => service.AbsorbDamageForActors(registry, input));
        }

        [Test]
        public void AbsorbDamageForActors_EmptyMap_ReturnsEmptyResultAndDoesNotMutateRegistry()
        {
            var service = new ShieldBuffService();
            var registry = new ShieldBuffStateRegistry();
            var bag = registry.GetOrCreate("hero_a");
            bag.SetActiveBuff("ember_ward", remainingTicks: 30, magnitude: 4);

            var result = service.AbsorbDamageForActors(registry, new Dictionary<string, int>());

            Assert.That(result, Is.Empty);
            Assert.That(bag.GetMagnitude("ember_ward"), Is.EqualTo(4));
            Assert.That(bag.GetRemainingTicks("ember_ward"), Is.EqualTo(30));
            Assert.That(registry.GetTrackedActorIds().Count, Is.EqualTo(1));
        }

        [Test]
        public void AbsorbDamageForActors_ZeroDamageEntry_ProducesZeroResultAndPreservesBag()
        {
            var service = new ShieldBuffService();
            var registry = new ShieldBuffStateRegistry();
            var bag = registry.GetOrCreate("hero_a");
            bag.SetActiveBuff("ember_ward", remainingTicks: 30, magnitude: 4);

            var result = service.AbsorbDamageForActors(
                registry,
                new Dictionary<string, int> { { "hero_a", 0 } });

            Assert.That(result.Count, Is.EqualTo(1));
            var heroResult = result["hero_a"];
            Assert.That(heroResult.IncomingDamage, Is.EqualTo(0));
            Assert.That(heroResult.AbsorbedDamage, Is.EqualTo(0));
            Assert.That(heroResult.RemainingDamage, Is.EqualTo(0));
            Assert.That(heroResult.ConsumedSpellTemplateIds, Is.Empty);
            Assert.That(heroResult.ExpiredSpellTemplateIds, Is.Empty);
            Assert.That(bag.GetMagnitude("ember_ward"), Is.EqualTo(4));
            Assert.That(bag.GetRemainingTicks("ember_ward"), Is.EqualTo(30));
        }

        [Test]
        public void AbsorbDamageForActors_UntrackedActor_ReturnsFullRemainingAndEmptyTrace()
        {
            var service = new ShieldBuffService();
            var registry = new ShieldBuffStateRegistry();

            var result = service.AbsorbDamageForActors(
                registry,
                new Dictionary<string, int> { { "hero_a", 7 } });

            Assert.That(result.Count, Is.EqualTo(1));
            var heroResult = result["hero_a"];
            Assert.That(heroResult.IncomingDamage, Is.EqualTo(7));
            Assert.That(heroResult.AbsorbedDamage, Is.EqualTo(0));
            Assert.That(heroResult.RemainingDamage, Is.EqualTo(7));
            Assert.That(heroResult.ConsumedSpellTemplateIds, Is.Empty);
            Assert.That(heroResult.ExpiredSpellTemplateIds, Is.Empty);
        }

        [Test]
        public void AbsorbDamageForActors_UntrackedActor_DoesNotAddActorToRegistry()
        {
            var service = new ShieldBuffService();
            var registry = new ShieldBuffStateRegistry();

            service.AbsorbDamageForActors(
                registry,
                new Dictionary<string, int>
                {
                    { "hero_a", 12 },
                    { "rival_b", 0 },
                });

            Assert.That(registry.HasState("hero_a"), Is.False);
            Assert.That(registry.HasState("rival_b"), Is.False);
            Assert.That(registry.GetTrackedActorIds(), Is.Empty);
        }

        [Test]
        public void AbsorbDamageForActors_MixedTrackedAndUntracked_DispatchesPerActor()
        {
            var service = new ShieldBuffService();
            var registry = new ShieldBuffStateRegistry();
            var heroBag = registry.GetOrCreate("hero_a");
            heroBag.SetActiveBuff("ember_ward", remainingTicks: 20, magnitude: 5);
            var rivalBag = registry.GetOrCreate("rival_b");
            rivalBag.SetActiveBuff("aegis_pulse", remainingTicks: 8, magnitude: 3);

            var result = service.AbsorbDamageForActors(
                registry,
                new Dictionary<string, int>
                {
                    { "hero_a", 6 },
                    { "rival_b", 1 },
                    { "stranger_c", 4 },
                });

            Assert.That(result.Count, Is.EqualTo(3));

            var heroResult = result["hero_a"];
            Assert.That(heroResult.AbsorbedDamage, Is.EqualTo(5));
            Assert.That(heroResult.RemainingDamage, Is.EqualTo(1));
            Assert.That(heroResult.ConsumedSpellTemplateIds, Is.EqualTo(new[] { "ember_ward" }));
            Assert.That(heroResult.ExpiredSpellTemplateIds, Is.EqualTo(new[] { "ember_ward" }));
            Assert.That(heroBag.GetMagnitude("ember_ward"), Is.EqualTo(0));

            var rivalResult = result["rival_b"];
            Assert.That(rivalResult.AbsorbedDamage, Is.EqualTo(1));
            Assert.That(rivalResult.RemainingDamage, Is.EqualTo(0));
            Assert.That(rivalResult.ConsumedSpellTemplateIds, Is.EqualTo(new[] { "aegis_pulse" }));
            Assert.That(rivalResult.ExpiredSpellTemplateIds, Is.Empty);
            Assert.That(rivalBag.GetMagnitude("aegis_pulse"), Is.EqualTo(2));
            Assert.That(rivalBag.GetRemainingTicks("aegis_pulse"), Is.EqualTo(8));

            var strangerResult = result["stranger_c"];
            Assert.That(strangerResult.IncomingDamage, Is.EqualTo(4));
            Assert.That(strangerResult.AbsorbedDamage, Is.EqualTo(0));
            Assert.That(strangerResult.RemainingDamage, Is.EqualTo(4));
            Assert.That(strangerResult.ConsumedSpellTemplateIds, Is.Empty);
            Assert.That(strangerResult.ExpiredSpellTemplateIds, Is.Empty);
            Assert.That(registry.HasState("stranger_c"), Is.False);
        }

        [Test]
        public void AbsorbDamageForActors_OnlyAffectsActorsInInputMap()
        {
            var service = new ShieldBuffService();
            var registry = new ShieldBuffStateRegistry();
            var heroBag = registry.GetOrCreate("hero_a");
            heroBag.SetActiveBuff("ember_ward", remainingTicks: 30, magnitude: 4);
            var rivalBag = registry.GetOrCreate("rival_b");
            rivalBag.SetActiveBuff("aegis_pulse", remainingTicks: 12, magnitude: 2);

            var result = service.AbsorbDamageForActors(
                registry,
                new Dictionary<string, int> { { "hero_a", 3 } });

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.ContainsKey("rival_b"), Is.False);
            Assert.That(rivalBag.GetMagnitude("aegis_pulse"), Is.EqualTo(2));
            Assert.That(rivalBag.GetRemainingTicks("aegis_pulse"), Is.EqualTo(12));
            Assert.That(heroBag.GetMagnitude("ember_ward"), Is.EqualTo(1));
            Assert.That(heroBag.GetRemainingTicks("ember_ward"), Is.EqualTo(30));
        }

        [Test]
        public void AbsorbDamageForActors_ParityWithIndividualAbsorbDamageForActorCalls()
        {
            var service = new ShieldBuffService();
            var registry = new ShieldBuffStateRegistry();
            var heroBag = registry.GetOrCreate("hero_a");
            heroBag.SetActiveBuff("ember_ward", remainingTicks: 20, magnitude: 5);
            heroBag.SetActiveBuff("aegis_pulse", remainingTicks: 8, magnitude: 3);
            var rivalBag = registry.GetOrCreate("rival_b");
            rivalBag.SetActiveBuff("ember_ward", remainingTicks: 6, magnitude: 4);

            var perActorService = new ShieldBuffService();
            var perActorRegistry = new ShieldBuffStateRegistry();
            var perActorHeroBag = perActorRegistry.GetOrCreate("hero_a");
            perActorHeroBag.SetActiveBuff("ember_ward", remainingTicks: 20, magnitude: 5);
            perActorHeroBag.SetActiveBuff("aegis_pulse", remainingTicks: 8, magnitude: 3);
            var perActorRivalBag = perActorRegistry.GetOrCreate("rival_b");
            perActorRivalBag.SetActiveBuff("ember_ward", remainingTicks: 6, magnitude: 4);

            var batch = service.AbsorbDamageForActors(
                registry,
                new Dictionary<string, int>
                {
                    { "hero_a", 6 },
                    { "rival_b", 2 },
                });

            var heroDirect = perActorService.AbsorbDamageForActor(perActorRegistry, "hero_a", 6);
            var rivalDirect = perActorService.AbsorbDamageForActor(perActorRegistry, "rival_b", 2);

            Assert.That(batch["hero_a"].AbsorbedDamage, Is.EqualTo(heroDirect.AbsorbedDamage));
            Assert.That(batch["hero_a"].RemainingDamage, Is.EqualTo(heroDirect.RemainingDamage));
            Assert.That(batch["hero_a"].ConsumedSpellTemplateIds, Is.EqualTo(heroDirect.ConsumedSpellTemplateIds));
            Assert.That(batch["hero_a"].ExpiredSpellTemplateIds, Is.EqualTo(heroDirect.ExpiredSpellTemplateIds));

            Assert.That(batch["rival_b"].AbsorbedDamage, Is.EqualTo(rivalDirect.AbsorbedDamage));
            Assert.That(batch["rival_b"].RemainingDamage, Is.EqualTo(rivalDirect.RemainingDamage));
            Assert.That(batch["rival_b"].ConsumedSpellTemplateIds, Is.EqualTo(rivalDirect.ConsumedSpellTemplateIds));
            Assert.That(batch["rival_b"].ExpiredSpellTemplateIds, Is.EqualTo(rivalDirect.ExpiredSpellTemplateIds));

            Assert.That(heroBag.GetMagnitude("ember_ward"), Is.EqualTo(perActorHeroBag.GetMagnitude("ember_ward")));
            Assert.That(heroBag.GetMagnitude("aegis_pulse"), Is.EqualTo(perActorHeroBag.GetMagnitude("aegis_pulse")));
            Assert.That(rivalBag.GetMagnitude("ember_ward"), Is.EqualTo(perActorRivalBag.GetMagnitude("ember_ward")));
            Assert.That(rivalBag.GetRemainingTicks("ember_ward"), Is.EqualTo(perActorRivalBag.GetRemainingTicks("ember_ward")));
        }

        [Test]
        public void AbsorbDamageForActors_ResultKeysMirrorInputKeys()
        {
            var service = new ShieldBuffService();
            var registry = new ShieldBuffStateRegistry();
            registry.GetOrCreate("hero_a").SetActiveBuff("ember_ward", remainingTicks: 10, magnitude: 5);

            var input = new Dictionary<string, int>
            {
                { "hero_a", 1 },
                { "rival_b", 0 },
                { "stranger_c", 3 },
            };

            var result = service.AbsorbDamageForActors(registry, input);

            Assert.That(result.Count, Is.EqualTo(input.Count));
            foreach (var actorId in input.Keys)
            {
                Assert.That(result.ContainsKey(actorId), Is.True, $"missing actor key: {actorId}");
            }
        }

        [Test]
        public void AbsorbDamageForActors_AfterTickSweepExpiresBuffs_NoLongerAbsorbs()
        {
            var service = new ShieldBuffService();
            var registry = new ShieldBuffStateRegistry();
            var heroBag = registry.GetOrCreate("hero_a");
            heroBag.SetActiveBuff("ember_ward", remainingTicks: 5, magnitude: 4);
            var rivalBag = registry.GetOrCreate("rival_b");
            rivalBag.SetActiveBuff("aegis_pulse", remainingTicks: 5, magnitude: 4);

            service.AdvanceTicksForAllActors(registry, 5);
            var result = service.AbsorbDamageForActors(
                registry,
                new Dictionary<string, int>
                {
                    { "hero_a", 3 },
                    { "rival_b", 2 },
                });

            Assert.That(result["hero_a"].AbsorbedDamage, Is.EqualTo(0));
            Assert.That(result["hero_a"].RemainingDamage, Is.EqualTo(3));
            Assert.That(result["hero_a"].ConsumedSpellTemplateIds, Is.Empty);
            Assert.That(result["rival_b"].AbsorbedDamage, Is.EqualTo(0));
            Assert.That(result["rival_b"].RemainingDamage, Is.EqualTo(2));
            Assert.That(result["rival_b"].ConsumedSpellTemplateIds, Is.Empty);
        }

        [Test]
        public void AbsorbDamageForActors_TrackedActorEmptyBag_ReturnsFullRemainingAndEmptyTrace()
        {
            var service = new ShieldBuffService();
            var registry = new ShieldBuffStateRegistry();
            registry.GetOrCreate("hero_a");

            var result = service.AbsorbDamageForActors(
                registry,
                new Dictionary<string, int> { { "hero_a", 9 } });

            Assert.That(result.Count, Is.EqualTo(1));
            var heroResult = result["hero_a"];
            Assert.That(heroResult.IncomingDamage, Is.EqualTo(9));
            Assert.That(heroResult.AbsorbedDamage, Is.EqualTo(0));
            Assert.That(heroResult.RemainingDamage, Is.EqualTo(9));
            Assert.That(heroResult.ConsumedSpellTemplateIds, Is.Empty);
            Assert.That(heroResult.ExpiredSpellTemplateIds, Is.Empty);
        }

        [Test]
        public void AbsorbDamageForActors_DoesNotEnumerateUnreferencedRegistryActors()
        {
            var service = new ShieldBuffService();
            var registry = new ShieldBuffStateRegistry();
            registry.GetOrCreate("hero_a").SetActiveBuff("ember_ward", remainingTicks: 30, magnitude: 4);
            registry.GetOrCreate("rival_b").SetActiveBuff("aegis_pulse", remainingTicks: 12, magnitude: 2);
            registry.GetOrCreate("ally_c").SetActiveBuff("ember_ward", remainingTicks: 10, magnitude: 6);

            var result = service.AbsorbDamageForActors(
                registry,
                new Dictionary<string, int> { { "hero_a", 1 } });

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(registry.GetTrackedActorIds().Count, Is.EqualTo(3));
            Assert.That(registry.GetOrNull("rival_b").GetMagnitude("aegis_pulse"), Is.EqualTo(2));
            Assert.That(registry.GetOrNull("rival_b").GetRemainingTicks("aegis_pulse"), Is.EqualTo(12));
            Assert.That(registry.GetOrNull("ally_c").GetMagnitude("ember_ward"), Is.EqualTo(6));
            Assert.That(registry.GetOrNull("ally_c").GetRemainingTicks("ember_ward"), Is.EqualTo(10));
        }
    }
}
