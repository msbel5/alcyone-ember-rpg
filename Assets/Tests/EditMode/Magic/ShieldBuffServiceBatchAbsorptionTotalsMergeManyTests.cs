using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Magic;
using EmberCrpg.Simulation.Magic;
using NUnit.Framework;

// Design note:
// Pins the deterministic Sprint 5 cross-batch shield-buff totals MergeMany seam:
// ShieldBuffService.MergeBatchTotalsMany (and the underlying
// ShieldBuffAbsorptionBatchTotals.MergeMany) folds an arbitrary sequence of already-
// computed batch absorption totals snapshots into a single field-wise sum. Foundation
// only: argument guards (null sequence, null element with index in message), Empty
// identity (empty sequence and singleton sequences), parity with chained pairwise
// Merge, parity with computing totals over the union of every source batch result map,
// permutation invariance, and isolation from registry/buff state. No save/load,
// application, tick-down, or combat-pipeline coverage here.
namespace EmberCrpg.Tests.EditMode.Magic
{
    /// <summary>Verifies deterministic MergeMany fold over batch shield-buff absorption totals snapshots.</summary>
    public sealed class ShieldBuffServiceBatchAbsorptionTotalsMergeManyTests
    {
        [Test]
        public void MergeMany_NullSequence_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ShieldBuffAbsorptionBatchTotals.MergeMany(null));
        }

        [Test]
        public void MergeBatchTotalsMany_NullSequence_Throws()
        {
            var service = new ShieldBuffService();

            Assert.Throws<ArgumentNullException>(() => service.MergeBatchTotalsMany(null));
        }

        [Test]
        public void MergeMany_NullElement_Throws()
        {
            var totals = BuildTotalsFromBatch();
            var sequence = new List<ShieldBuffAbsorptionBatchTotals>
            {
                totals,
                null,
                ShieldBuffAbsorptionBatchTotals.Empty,
            };

            var ex = Assert.Throws<ArgumentException>(() =>
                ShieldBuffAbsorptionBatchTotals.MergeMany(sequence));
            Assert.That(ex.Message, Does.Contain("index 1"));
        }

        [Test]
        public void MergeBatchTotalsMany_NullElement_Throws()
        {
            var service = new ShieldBuffService();
            var totals = BuildTotalsFromBatch();
            var sequence = new List<ShieldBuffAbsorptionBatchTotals>
            {
                totals,
                ShieldBuffAbsorptionBatchTotals.Empty,
                null,
            };

            var ex = Assert.Throws<ArgumentException>(() =>
                service.MergeBatchTotalsMany(sequence));
            Assert.That(ex.Message, Does.Contain("index 2"));
        }

        [Test]
        public void MergeMany_EmptySequence_ReturnsEmpty()
        {
            var merged = ShieldBuffAbsorptionBatchTotals.MergeMany(
                new List<ShieldBuffAbsorptionBatchTotals>());

            AssertTotalsEqual(ShieldBuffAbsorptionBatchTotals.Empty, merged);
        }

        [Test]
        public void MergeBatchTotalsMany_EmptySequence_ReturnsEmpty()
        {
            var service = new ShieldBuffService();

            var merged = service.MergeBatchTotalsMany(
                new List<ShieldBuffAbsorptionBatchTotals>());

            AssertTotalsEqual(ShieldBuffAbsorptionBatchTotals.Empty, merged);
        }

        [Test]
        public void MergeMany_SingletonSequence_EqualsSingleSnapshot()
        {
            var totals = BuildTotalsFromBatch();

            var merged = ShieldBuffAbsorptionBatchTotals.MergeMany(
                new List<ShieldBuffAbsorptionBatchTotals> { totals });

            AssertTotalsEqual(totals, merged);
        }

        [Test]
        public void MergeMany_MatchesChainedPairwiseMerge()
        {
            var (a, b, c) = BuildThreeDistinctTotals();

            var pairwise = ShieldBuffAbsorptionBatchTotals.Merge(
                ShieldBuffAbsorptionBatchTotals.Merge(a, b), c);
            var merged = ShieldBuffAbsorptionBatchTotals.MergeMany(
                new List<ShieldBuffAbsorptionBatchTotals> { a, b, c });

            AssertTotalsEqual(pairwise, merged);
        }

        [Test]
        public void MergeMany_MatchesFromOverUnionOfAllBatches()
        {
            var registryA = new ShieldBuffStateRegistry();
            registryA.GetOrCreate("ally_a").SetActiveBuff("spellA", remainingTicks: 3, magnitude: 4);

            var registryB = new ShieldBuffStateRegistry();
            registryB.GetOrCreate("ally_b").SetActiveBuff("spellB", remainingTicks: 5, magnitude: 10);

            var registryC = new ShieldBuffStateRegistry();
            registryC.GetOrCreate("enemy_a").SetActiveBuff("spellC", remainingTicks: 2, magnitude: 3);

            var service = new ShieldBuffService();
            var batchA = service.AbsorbDamageForActors(
                registryA, new Dictionary<string, int> { { "ally_a", 4 } });
            var batchB = service.AbsorbDamageForActors(
                registryB, new Dictionary<string, int> { { "ally_b", 4 } });
            var batchC = service.AbsorbDamageForActors(
                registryC, new Dictionary<string, int> { { "enemy_a", 5 } });

            var union = new Dictionary<string, ShieldBuffAbsorptionResult>(StringComparer.Ordinal);
            foreach (var pair in batchA) union[pair.Key] = pair.Value;
            foreach (var pair in batchB) union[pair.Key] = pair.Value;
            foreach (var pair in batchC) union[pair.Key] = pair.Value;

            var merged = ShieldBuffAbsorptionBatchTotals.MergeMany(new List<ShieldBuffAbsorptionBatchTotals>
            {
                service.ComputeBatchTotals(batchA),
                service.ComputeBatchTotals(batchB),
                service.ComputeBatchTotals(batchC),
            });

            var unionTotals = service.ComputeBatchTotals(union);

            AssertTotalsEqual(unionTotals, merged);
        }

        [Test]
        public void MergeMany_IsPermutationInvariant()
        {
            var (a, b, c) = BuildThreeDistinctTotals();

            var forward = ShieldBuffAbsorptionBatchTotals.MergeMany(
                new List<ShieldBuffAbsorptionBatchTotals> { a, b, c });
            var reverse = ShieldBuffAbsorptionBatchTotals.MergeMany(
                new List<ShieldBuffAbsorptionBatchTotals> { c, b, a });
            var shuffled = ShieldBuffAbsorptionBatchTotals.MergeMany(
                new List<ShieldBuffAbsorptionBatchTotals> { b, a, c });

            AssertTotalsEqual(forward, reverse);
            AssertTotalsEqual(forward, shuffled);
        }

        [Test]
        public void MergeMany_EmptyEntriesAreIdentities()
        {
            var totals = BuildTotalsFromBatch();

            var withEmpties = ShieldBuffAbsorptionBatchTotals.MergeMany(new List<ShieldBuffAbsorptionBatchTotals>
            {
                ShieldBuffAbsorptionBatchTotals.Empty,
                totals,
                ShieldBuffAbsorptionBatchTotals.Empty,
                ShieldBuffAbsorptionBatchTotals.Empty,
            });

            AssertTotalsEqual(totals, withEmpties);
        }

        [Test]
        public void MergeBatchTotalsMany_DoesNotMutateRegistry()
        {
            var registry = new ShieldBuffStateRegistry();
            registry.GetOrCreate("hero_a").SetActiveBuff("spellA", remainingTicks: 4, magnitude: 6);

            var service = new ShieldBuffService();
            var perActor = service.AbsorbDamageForActors(
                registry,
                new Dictionary<string, int> { { "hero_a", 0 } });
            var totals = service.ComputeBatchTotals(perActor);

            var trackedBefore = new List<string>(registry.GetTrackedActorIds());
            var ticksBefore = registry.GetOrNull("hero_a").GetRemainingTicks("spellA");
            var magBefore = registry.GetOrNull("hero_a").GetMagnitude("spellA");

            service.MergeBatchTotalsMany(new List<ShieldBuffAbsorptionBatchTotals>
            {
                totals,
                ShieldBuffAbsorptionBatchTotals.Empty,
                totals,
            });

            var trackedAfter = new List<string>(registry.GetTrackedActorIds());
            var ticksAfter = registry.GetOrNull("hero_a").GetRemainingTicks("spellA");
            var magAfter = registry.GetOrNull("hero_a").GetMagnitude("spellA");

            Assert.That(trackedAfter, Is.EquivalentTo(trackedBefore));
            Assert.That(ticksAfter, Is.EqualTo(ticksBefore));
            Assert.That(magAfter, Is.EqualTo(magBefore));
        }

        private static ShieldBuffAbsorptionBatchTotals BuildTotalsFromBatch()
        {
            var registry = new ShieldBuffStateRegistry();
            registry.GetOrCreate("ally_a").SetActiveBuff("spellA", remainingTicks: 3, magnitude: 4);
            registry.GetOrCreate("ally_b").SetActiveBuff("spellB", remainingTicks: 5, magnitude: 10);

            var service = new ShieldBuffService();
            var perActor = service.AbsorbDamageForActors(
                registry,
                new Dictionary<string, int> { { "ally_a", 4 }, { "ally_b", 4 } });

            return service.ComputeBatchTotals(perActor);
        }

        private static (ShieldBuffAbsorptionBatchTotals a,
                        ShieldBuffAbsorptionBatchTotals b,
                        ShieldBuffAbsorptionBatchTotals c) BuildThreeDistinctTotals()
        {
            var registryA = new ShieldBuffStateRegistry();
            registryA.GetOrCreate("hero_a").SetActiveBuff("spellA", remainingTicks: 3, magnitude: 4);

            var registryB = new ShieldBuffStateRegistry();
            registryB.GetOrCreate("hero_b").SetActiveBuff("spellB", remainingTicks: 5, magnitude: 10);

            var registryC = new ShieldBuffStateRegistry();
            registryC.GetOrCreate("hero_c").SetActiveBuff("spellC", remainingTicks: 2, magnitude: 3);

            var service = new ShieldBuffService();
            var a = service.ComputeBatchTotals(service.AbsorbDamageForActors(
                registryA, new Dictionary<string, int> { { "hero_a", 4 } }));
            var b = service.ComputeBatchTotals(service.AbsorbDamageForActors(
                registryB, new Dictionary<string, int> { { "hero_b", 4 } }));
            var c = service.ComputeBatchTotals(service.AbsorbDamageForActors(
                registryC, new Dictionary<string, int> { { "hero_c", 5 } }));

            return (a, b, c);
        }

        private static void AssertTotalsEqual(
            ShieldBuffAbsorptionBatchTotals expected,
            ShieldBuffAbsorptionBatchTotals actual)
        {
            Assert.That(actual.TotalIncomingDamage, Is.EqualTo(expected.TotalIncomingDamage));
            Assert.That(actual.TotalAbsorbedDamage, Is.EqualTo(expected.TotalAbsorbedDamage));
            Assert.That(actual.TotalRemainingDamage, Is.EqualTo(expected.TotalRemainingDamage));
            Assert.That(actual.ActorCount, Is.EqualTo(expected.ActorCount));
            Assert.That(actual.ActorsWithAbsorption, Is.EqualTo(expected.ActorsWithAbsorption));
            Assert.That(actual.TotalConsumedBuffEntries, Is.EqualTo(expected.TotalConsumedBuffEntries));
            Assert.That(actual.TotalExpiredBuffEntries, Is.EqualTo(expected.TotalExpiredBuffEntries));
        }
    }
}
