using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Magic;
using EmberCrpg.Simulation.Magic;
using NUnit.Framework;

// Design note:
// Pins the deterministic Sprint 5 batch shield-buff absorption totals seam:
// ShieldBuffService.ComputeBatchTotals (and the underlying ShieldBuffAbsorptionBatchTotals.From)
// aggregates the per-actor result map returned by AbsorbDamageForActors into a single
// snapshot. Foundation-only: argument guards, empty-input zeros, single-actor passthrough,
// multi-actor sums, mixed absorbed/non-absorbed counts, and parity vs walking the map
// manually. No save/load, application, tick-down, or combat-pipeline coverage here.
namespace EmberCrpg.Tests.EditMode.Magic
{
    /// <summary>Verifies deterministic totals over batch shield-buff absorption results.</summary>
    public sealed class ShieldBuffServiceBatchAbsorptionTotalsTests
    {
        [Test]
        public void ComputeBatchTotals_NullResultMap_Throws()
        {
            var service = new ShieldBuffService();

            Assert.Throws<ArgumentNullException>(() => service.ComputeBatchTotals(null));
        }

        [Test]
        public void From_NullResultMap_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => ShieldBuffAbsorptionBatchTotals.From(null));
        }

        [Test]
        public void From_WhitespaceActorKey_Throws()
        {
            var map = new Dictionary<string, ShieldBuffAbsorptionResult>
            {
                {
                    "  ", ShieldBuffAbsorptionResult.Create(
                        incomingDamage: 4,
                        absorbedDamage: 4,
                        remainingDamage: 0,
                        consumedSpellTemplateIds: new[] { "spellA" },
                        expiredSpellTemplateIds: Array.Empty<string>())
                }
            };

            Assert.Throws<ArgumentException>(() => ShieldBuffAbsorptionBatchTotals.From(map));
        }

        [Test]
        public void From_NullActorResult_Throws()
        {
            var map = new Dictionary<string, ShieldBuffAbsorptionResult>
            {
                { "hero_a", null }
            };

            Assert.Throws<ArgumentException>(() => ShieldBuffAbsorptionBatchTotals.From(map));
        }

        [Test]
        public void ComputeBatchTotals_EmptyMap_AllZero()
        {
            var service = new ShieldBuffService();
            var totals = service.ComputeBatchTotals(
                new Dictionary<string, ShieldBuffAbsorptionResult>());

            Assert.That(totals.TotalIncomingDamage, Is.EqualTo(0));
            Assert.That(totals.TotalAbsorbedDamage, Is.EqualTo(0));
            Assert.That(totals.TotalRemainingDamage, Is.EqualTo(0));
            Assert.That(totals.ActorCount, Is.EqualTo(0));
            Assert.That(totals.ActorsWithAbsorption, Is.EqualTo(0));
            Assert.That(totals.TotalConsumedBuffEntries, Is.EqualTo(0));
            Assert.That(totals.TotalExpiredBuffEntries, Is.EqualTo(0));
        }

        [Test]
        public void ComputeBatchTotals_SingleActorFullAbsorption_ReportsSingleActorTotals()
        {
            var registry = new ShieldBuffStateRegistry();
            registry.GetOrCreate("hero_a").SetActiveBuff("spellA", remainingTicks: 4, magnitude: 7);

            var service = new ShieldBuffService();
            var perActor = service.AbsorbDamageForActors(
                registry,
                new Dictionary<string, int> { { "hero_a", 5 } });

            var totals = service.ComputeBatchTotals(perActor);

            Assert.That(totals.ActorCount, Is.EqualTo(1));
            Assert.That(totals.TotalIncomingDamage, Is.EqualTo(5));
            Assert.That(totals.TotalAbsorbedDamage, Is.EqualTo(5));
            Assert.That(totals.TotalRemainingDamage, Is.EqualTo(0));
            Assert.That(totals.ActorsWithAbsorption, Is.EqualTo(1));
            Assert.That(totals.TotalConsumedBuffEntries, Is.EqualTo(1));
            Assert.That(totals.TotalExpiredBuffEntries, Is.EqualTo(0));
        }

        [Test]
        public void ComputeBatchTotals_UntrackedActor_TreatedAsRemainingOnly()
        {
            var registry = new ShieldBuffStateRegistry();

            var service = new ShieldBuffService();
            var perActor = service.AbsorbDamageForActors(
                registry,
                new Dictionary<string, int> { { "hero_b", 9 } });

            var totals = service.ComputeBatchTotals(perActor);

            Assert.That(totals.ActorCount, Is.EqualTo(1));
            Assert.That(totals.TotalIncomingDamage, Is.EqualTo(9));
            Assert.That(totals.TotalAbsorbedDamage, Is.EqualTo(0));
            Assert.That(totals.TotalRemainingDamage, Is.EqualTo(9));
            Assert.That(totals.ActorsWithAbsorption, Is.EqualTo(0));
            Assert.That(totals.TotalConsumedBuffEntries, Is.EqualTo(0));
            Assert.That(totals.TotalExpiredBuffEntries, Is.EqualTo(0));
        }

        [Test]
        public void ComputeBatchTotals_MultiActorMixedAbsorption_AggregatesAcrossActors()
        {
            var registry = new ShieldBuffStateRegistry();
            // hero_a: shield expires this absorption (magnitude 4, damage 4) -> 1 consumed, 1 expired
            registry.GetOrCreate("hero_a").SetActiveBuff("spellA", remainingTicks: 3, magnitude: 4);
            // hero_b: shield consumed but not expired (magnitude 10, damage 4) -> 1 consumed
            registry.GetOrCreate("hero_b").SetActiveBuff("spellB", remainingTicks: 5, magnitude: 10);
            // hero_c: untracked actor -> 0 absorbed, 7 remaining
            // hero_d: tracked but no incoming damage in input map -> not in result

            var service = new ShieldBuffService();
            var input = new Dictionary<string, int>
            {
                { "hero_a", 4 },
                { "hero_b", 4 },
                { "hero_c", 7 }
            };
            var perActor = service.AbsorbDamageForActors(registry, input);

            var totals = service.ComputeBatchTotals(perActor);

            Assert.That(totals.ActorCount, Is.EqualTo(3));
            Assert.That(totals.TotalIncomingDamage, Is.EqualTo(4 + 4 + 7));
            Assert.That(totals.TotalAbsorbedDamage, Is.EqualTo(4 + 4 + 0));
            Assert.That(totals.TotalRemainingDamage, Is.EqualTo(0 + 0 + 7));
            Assert.That(totals.ActorsWithAbsorption, Is.EqualTo(2));
            Assert.That(totals.TotalConsumedBuffEntries, Is.EqualTo(2));
            Assert.That(totals.TotalExpiredBuffEntries, Is.EqualTo(1));
        }

        [Test]
        public void ComputeBatchTotals_ParityWithManualWalk()
        {
            var registry = new ShieldBuffStateRegistry();
            registry.GetOrCreate("hero_a").SetActiveBuff("spellA", remainingTicks: 4, magnitude: 6);
            registry.GetOrCreate("hero_b").SetActiveBuff("spellB", remainingTicks: 2, magnitude: 3);

            var service = new ShieldBuffService();
            var perActor = service.AbsorbDamageForActors(
                registry,
                new Dictionary<string, int>
                {
                    { "hero_a", 4 },
                    { "hero_b", 5 }
                });

            var manualIncoming = 0;
            var manualAbsorbed = 0;
            var manualRemaining = 0;
            var manualWithAbsorption = 0;
            var manualConsumed = 0;
            var manualExpired = 0;
            foreach (var pair in perActor)
            {
                manualIncoming += pair.Value.IncomingDamage;
                manualAbsorbed += pair.Value.AbsorbedDamage;
                manualRemaining += pair.Value.RemainingDamage;
                if (pair.Value.AbsorbedDamage > 0)
                    manualWithAbsorption++;
                manualConsumed += pair.Value.ConsumedSpellTemplateIds.Count;
                manualExpired += pair.Value.ExpiredSpellTemplateIds.Count;
            }

            var totals = service.ComputeBatchTotals(perActor);

            Assert.That(totals.TotalIncomingDamage, Is.EqualTo(manualIncoming));
            Assert.That(totals.TotalAbsorbedDamage, Is.EqualTo(manualAbsorbed));
            Assert.That(totals.TotalRemainingDamage, Is.EqualTo(manualRemaining));
            Assert.That(totals.ActorsWithAbsorption, Is.EqualTo(manualWithAbsorption));
            Assert.That(totals.TotalConsumedBuffEntries, Is.EqualTo(manualConsumed));
            Assert.That(totals.TotalExpiredBuffEntries, Is.EqualTo(manualExpired));
            Assert.That(totals.ActorCount, Is.EqualTo(perActor.Count));
        }

        [Test]
        public void ComputeBatchTotals_DoesNotMutateRegistry()
        {
            var registry = new ShieldBuffStateRegistry();
            registry.GetOrCreate("hero_a").SetActiveBuff("spellA", remainingTicks: 4, magnitude: 6);

            var service = new ShieldBuffService();
            var perActor = service.AbsorbDamageForActors(
                registry,
                new Dictionary<string, int> { { "hero_a", 0 } });

            var trackedBefore = new List<string>(registry.GetTrackedActorIds());
            var ticksBefore = registry.GetOrNull("hero_a").GetRemainingTicks("spellA");
            var magBefore = registry.GetOrNull("hero_a").GetMagnitude("spellA");

            service.ComputeBatchTotals(perActor);

            var trackedAfter = new List<string>(registry.GetTrackedActorIds());
            var ticksAfter = registry.GetOrNull("hero_a").GetRemainingTicks("spellA");
            var magAfter = registry.GetOrNull("hero_a").GetMagnitude("spellA");

            Assert.That(trackedAfter, Is.EquivalentTo(trackedBefore));
            Assert.That(ticksAfter, Is.EqualTo(ticksBefore));
            Assert.That(magAfter, Is.EqualTo(magBefore));
        }
    }
}
