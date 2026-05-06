using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Magic;
using EmberCrpg.Simulation.Magic;
using NUnit.Framework;

// Design note:
// Pins the deterministic Sprint 5 cross-batch shield-buff totals PartitionMany seam:
// ShieldBuffService.PartitionBatchTotalsMany(sequence, predicate) (and the underlying
// ShieldBuffAbsorptionBatchTotals.PartitionMany(sequence, predicate)) walks an
// arbitrary sequence of already-computed batch absorption totals snapshots exactly
// once and routes each snapshot to either the Included fold or the Excluded fold,
// returning a ShieldBuffAbsorptionBatchTotalsPartition whose two buckets are themselves
// complete batch totals snapshots. Foundation only: argument guards (null sequence,
// null predicate, null element with index in message even when the element would have
// been excluded), all-true behaviour matches MergeMany / Excluded is Empty, all-false
// behaviour mirrors it, empty sequence returns two Empty buckets, the
// Included-plus-Excluded sums equal the unfiltered MergeMany(sequence) total
// snapshot-by-snapshot, permutation invariance under per-snapshot predicates, and
// isolation from registry/buff state. No save/load, application, tick-down, or
// combat-pipeline coverage here.
namespace EmberCrpg.Tests.EditMode.Magic
{
    /// <summary>Verifies deterministic predicate partition fold over batch shield-buff absorption totals snapshots.</summary>
    public sealed class ShieldBuffServiceBatchAbsorptionTotalsPartitionManyTests
    {
        [Test]
        public void PartitionMany_NullSequence_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ShieldBuffAbsorptionBatchTotals.PartitionMany(
                    null,
                    includePredicate: _ => true));
        }

        [Test]
        public void PartitionBatchTotalsMany_NullSequence_Throws()
        {
            var service = new ShieldBuffService();

            Assert.Throws<ArgumentNullException>(() =>
                service.PartitionBatchTotalsMany(null, includePredicate: _ => true));
        }

        [Test]
        public void PartitionMany_NullPredicate_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ShieldBuffAbsorptionBatchTotals.PartitionMany(
                    new List<ShieldBuffAbsorptionBatchTotals>(),
                    includePredicate: null));
        }

        [Test]
        public void PartitionBatchTotalsMany_NullPredicate_Throws()
        {
            var service = new ShieldBuffService();

            Assert.Throws<ArgumentNullException>(() =>
                service.PartitionBatchTotalsMany(
                    new List<ShieldBuffAbsorptionBatchTotals>(),
                    includePredicate: null));
        }

        [Test]
        public void PartitionMany_NullElement_ThrowsBeforePredicate()
        {
            var totals = BuildTotalsFromBatch();
            var sequence = new List<ShieldBuffAbsorptionBatchTotals>
            {
                totals,
                null,
                ShieldBuffAbsorptionBatchTotals.Empty,
            };

            var ex = Assert.Throws<ArgumentException>(() =>
                ShieldBuffAbsorptionBatchTotals.PartitionMany(
                    sequence,
                    includePredicate: _ => false));
            Assert.That(ex.Message, Does.Contain("index 1"));
        }

        [Test]
        public void PartitionBatchTotalsMany_NullElement_ThrowsBeforePredicate()
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
                service.PartitionBatchTotalsMany(sequence, includePredicate: _ => false));
            Assert.That(ex.Message, Does.Contain("index 2"));
        }

        [Test]
        public void PartitionMany_EmptySequence_BothBucketsAreEmpty()
        {
            var partition = ShieldBuffAbsorptionBatchTotals.PartitionMany(
                new List<ShieldBuffAbsorptionBatchTotals>(),
                includePredicate: _ => true);

            AssertTotalsEqual(ShieldBuffAbsorptionBatchTotals.Empty, partition.Included);
            AssertTotalsEqual(ShieldBuffAbsorptionBatchTotals.Empty, partition.Excluded);
        }

        [Test]
        public void PartitionMany_PredicateAlwaysTrue_IncludedMatchesMergeMany_ExcludedIsEmpty()
        {
            var (a, b, c) = BuildThreeDistinctTotals();
            var sequence = new List<ShieldBuffAbsorptionBatchTotals> { a, b, c };

            var fullFold = ShieldBuffAbsorptionBatchTotals.MergeMany(sequence);
            var partition = ShieldBuffAbsorptionBatchTotals.PartitionMany(
                sequence,
                includePredicate: _ => true);

            AssertTotalsEqual(fullFold, partition.Included);
            AssertTotalsEqual(ShieldBuffAbsorptionBatchTotals.Empty, partition.Excluded);
        }

        [Test]
        public void PartitionMany_PredicateAlwaysFalse_ExcludedMatchesMergeMany_IncludedIsEmpty()
        {
            var (a, b, c) = BuildThreeDistinctTotals();
            var sequence = new List<ShieldBuffAbsorptionBatchTotals> { a, b, c };

            var fullFold = ShieldBuffAbsorptionBatchTotals.MergeMany(sequence);
            var partition = ShieldBuffAbsorptionBatchTotals.PartitionMany(
                sequence,
                includePredicate: _ => false);

            AssertTotalsEqual(ShieldBuffAbsorptionBatchTotals.Empty, partition.Included);
            AssertTotalsEqual(fullFold, partition.Excluded);
        }

        [Test]
        public void PartitionMany_BucketsSumToUnfilteredMergeMany()
        {
            var (a, b, c) = BuildThreeDistinctTotals();
            var sequence = new List<ShieldBuffAbsorptionBatchTotals>
            {
                a,
                ShieldBuffAbsorptionBatchTotals.Empty,
                b,
                c,
            };

            // Predicate splits snapshots that registered any absorption activity from
            // those that did not. The Included-plus-Excluded merge must equal the
            // unfiltered MergeMany fold over the same sequence.
            bool Include(ShieldBuffAbsorptionBatchTotals snapshot) =>
                snapshot.ActorsWithAbsorption > 0;

            var partition = ShieldBuffAbsorptionBatchTotals.PartitionMany(sequence, Include);
            var rejoined = ShieldBuffAbsorptionBatchTotals.Merge(
                partition.Included,
                partition.Excluded);
            var unfiltered = ShieldBuffAbsorptionBatchTotals.MergeMany(sequence);

            AssertTotalsEqual(unfiltered, rejoined);
        }

        [Test]
        public void PartitionMany_MatchesPreFilteredMergeManyOnEachBucket()
        {
            var (a, b, c) = BuildThreeDistinctTotals();
            var sequence = new List<ShieldBuffAbsorptionBatchTotals>
            {
                a,
                ShieldBuffAbsorptionBatchTotals.Empty,
                b,
                c,
            };

            bool Include(ShieldBuffAbsorptionBatchTotals snapshot) =>
                snapshot.ActorsWithAbsorption > 0;

            var includedByHand = new List<ShieldBuffAbsorptionBatchTotals>();
            var excludedByHand = new List<ShieldBuffAbsorptionBatchTotals>();
            foreach (var snapshot in sequence)
            {
                if (Include(snapshot))
                    includedByHand.Add(snapshot);
                else
                    excludedByHand.Add(snapshot);
            }

            var partition = ShieldBuffAbsorptionBatchTotals.PartitionMany(sequence, Include);

            AssertTotalsEqual(
                ShieldBuffAbsorptionBatchTotals.MergeMany(includedByHand),
                partition.Included);
            AssertTotalsEqual(
                ShieldBuffAbsorptionBatchTotals.MergeMany(excludedByHand),
                partition.Excluded);
        }

        [Test]
        public void PartitionMany_IsPermutationInvariant()
        {
            var (a, b, c) = BuildThreeDistinctTotals();

            bool Include(ShieldBuffAbsorptionBatchTotals snapshot) =>
                snapshot.ActorCount > 0;

            var forward = ShieldBuffAbsorptionBatchTotals.PartitionMany(
                new List<ShieldBuffAbsorptionBatchTotals> { a, b, c },
                Include);
            var reverse = ShieldBuffAbsorptionBatchTotals.PartitionMany(
                new List<ShieldBuffAbsorptionBatchTotals> { c, b, a },
                Include);
            var shuffled = ShieldBuffAbsorptionBatchTotals.PartitionMany(
                new List<ShieldBuffAbsorptionBatchTotals> { b, a, c },
                Include);

            AssertTotalsEqual(forward.Included, reverse.Included);
            AssertTotalsEqual(forward.Included, shuffled.Included);
            AssertTotalsEqual(forward.Excluded, reverse.Excluded);
            AssertTotalsEqual(forward.Excluded, shuffled.Excluded);
        }

        [Test]
        public void PartitionBatchTotalsMany_DoesNotMutateRegistry()
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

            service.PartitionBatchTotalsMany(
                new List<ShieldBuffAbsorptionBatchTotals>
                {
                    totals,
                    ShieldBuffAbsorptionBatchTotals.Empty,
                    totals,
                },
                snapshot => snapshot.ActorCount > 0);

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
