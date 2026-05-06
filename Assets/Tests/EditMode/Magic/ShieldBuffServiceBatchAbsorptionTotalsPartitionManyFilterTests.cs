using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Magic;
using EmberCrpg.Simulation.Magic;
using NUnit.Framework;

// Design note:
// Pins the deterministic Sprint 5 cross-batch shield-buff totals filtered PartitionMany
// seam: ShieldBuffService.PartitionBatchTotalsMany(sequence, includePredicate,
// filterPredicate) (and the underlying ShieldBuffAbsorptionBatchTotals.PartitionMany
// (sequence, includePredicate, filterPredicate)) walks an arbitrary sequence of already
// computed batch absorption totals snapshots exactly once, drops elements for which
// filterPredicate returns false, and routes each surviving snapshot to either the
// Included fold (includePredicate returns true) or the Excluded fold (returns false),
// returning a ShieldBuffAbsorptionBatchTotalsPartition whose two buckets are themselves
// complete batch totals snapshots. Foundation only: argument guards (null sequence,
// null include predicate, null element with index in message even when filterPredicate
// would have rejected the element), filter-rejected snapshots contribute to neither
// bucket, null filterPredicate behaves like the binary PartitionMany overload, all-true
// filter agrees with the binary overload bucket-for-bucket, all-false filter empties
// both buckets, the Included-plus-Excluded sums equal the pre-filtered MergeMany over
// the same sequence, equivalence to running the binary PartitionMany over a hand
// pre-filtered sequence, permutation invariance under both predicates, and isolation
// from registry/buff state. No save/load, application, tick-down, or combat-pipeline
// coverage here.
namespace EmberCrpg.Tests.EditMode.Magic
{
    /// <summary>Verifies deterministic predicate-filtered partition fold over batch shield-buff absorption totals snapshots.</summary>
    public sealed class ShieldBuffServiceBatchAbsorptionTotalsPartitionManyFilterTests
    {
        [Test]
        public void PartitionMany_NullSequence_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ShieldBuffAbsorptionBatchTotals.PartitionMany(
                    null,
                    includePredicate: _ => true,
                    filterPredicate: _ => true));
        }

        [Test]
        public void PartitionBatchTotalsMany_NullSequence_Throws()
        {
            var service = new ShieldBuffService();

            Assert.Throws<ArgumentNullException>(() =>
                service.PartitionBatchTotalsMany(
                    null,
                    includePredicate: _ => true,
                    filterPredicate: _ => true));
        }

        [Test]
        public void PartitionMany_NullIncludePredicate_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ShieldBuffAbsorptionBatchTotals.PartitionMany(
                    new List<ShieldBuffAbsorptionBatchTotals>(),
                    includePredicate: null,
                    filterPredicate: _ => true));
        }

        [Test]
        public void PartitionBatchTotalsMany_NullIncludePredicate_Throws()
        {
            var service = new ShieldBuffService();

            Assert.Throws<ArgumentNullException>(() =>
                service.PartitionBatchTotalsMany(
                    new List<ShieldBuffAbsorptionBatchTotals>(),
                    includePredicate: null,
                    filterPredicate: _ => true));
        }

        [Test]
        public void PartitionMany_NullElement_ThrowsBeforePredicates()
        {
            var totals = BuildTotalsFromBatch();
            var sequence = new List<ShieldBuffAbsorptionBatchTotals>
            {
                totals,
                null,
                ShieldBuffAbsorptionBatchTotals.Empty,
            };

            // Even when filterPredicate would reject the null element, the indexed
            // null-element guard must trip first to keep the strict input contract.
            var ex = Assert.Throws<ArgumentException>(() =>
                ShieldBuffAbsorptionBatchTotals.PartitionMany(
                    sequence,
                    includePredicate: _ => false,
                    filterPredicate: _ => false));
            Assert.That(ex.Message, Does.Contain("index 1"));
        }

        [Test]
        public void PartitionBatchTotalsMany_NullElement_ThrowsBeforePredicates()
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
                service.PartitionBatchTotalsMany(
                    sequence,
                    includePredicate: _ => false,
                    filterPredicate: _ => false));
            Assert.That(ex.Message, Does.Contain("index 2"));
        }

        [Test]
        public void PartitionMany_NullFilterPredicate_BehavesLikeBinaryOverload()
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

            var binary = ShieldBuffAbsorptionBatchTotals.PartitionMany(sequence, Include);
            var filtered = ShieldBuffAbsorptionBatchTotals.PartitionMany(
                sequence,
                includePredicate: Include,
                filterPredicate: null);

            AssertTotalsEqual(binary.Included, filtered.Included);
            AssertTotalsEqual(binary.Excluded, filtered.Excluded);
        }

        [Test]
        public void PartitionMany_FilterAlwaysTrue_BehavesLikeBinaryOverload()
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

            var binary = ShieldBuffAbsorptionBatchTotals.PartitionMany(sequence, Include);
            var filtered = ShieldBuffAbsorptionBatchTotals.PartitionMany(
                sequence,
                includePredicate: Include,
                filterPredicate: _ => true);

            AssertTotalsEqual(binary.Included, filtered.Included);
            AssertTotalsEqual(binary.Excluded, filtered.Excluded);
        }

        [Test]
        public void PartitionMany_FilterAlwaysFalse_BothBucketsAreEmpty()
        {
            var (a, b, c) = BuildThreeDistinctTotals();
            var sequence = new List<ShieldBuffAbsorptionBatchTotals> { a, b, c };

            var partition = ShieldBuffAbsorptionBatchTotals.PartitionMany(
                sequence,
                includePredicate: _ => true,
                filterPredicate: _ => false);

            AssertTotalsEqual(ShieldBuffAbsorptionBatchTotals.Empty, partition.Included);
            AssertTotalsEqual(ShieldBuffAbsorptionBatchTotals.Empty, partition.Excluded);
        }

        [Test]
        public void PartitionMany_BucketsSumToPreFilteredMergeMany()
        {
            var (a, b, c) = BuildThreeDistinctTotals();
            var sequence = new List<ShieldBuffAbsorptionBatchTotals>
            {
                a,
                ShieldBuffAbsorptionBatchTotals.Empty,
                b,
                c,
            };

            // Filter keeps only snapshots that actually had any actors in the batch
            // (drops the Empty snapshot). Inside the kept subset the include predicate
            // splits snapshots that registered any absorption activity from those that
            // did not.
            bool Filter(ShieldBuffAbsorptionBatchTotals snapshot) =>
                snapshot.ActorCount > 0;
            bool Include(ShieldBuffAbsorptionBatchTotals snapshot) =>
                snapshot.ActorsWithAbsorption > 0;

            var partition = ShieldBuffAbsorptionBatchTotals.PartitionMany(
                sequence,
                includePredicate: Include,
                filterPredicate: Filter);
            var rejoined = ShieldBuffAbsorptionBatchTotals.Merge(
                partition.Included,
                partition.Excluded);
            var preFiltered = ShieldBuffAbsorptionBatchTotals.MergeMany(sequence, Filter);

            AssertTotalsEqual(preFiltered, rejoined);
        }

        [Test]
        public void PartitionMany_MatchesBinaryPartitionOverPreFilteredSequence()
        {
            var (a, b, c) = BuildThreeDistinctTotals();
            var sequence = new List<ShieldBuffAbsorptionBatchTotals>
            {
                a,
                ShieldBuffAbsorptionBatchTotals.Empty,
                b,
                c,
            };

            bool Filter(ShieldBuffAbsorptionBatchTotals snapshot) =>
                snapshot.ActorCount > 0;
            bool Include(ShieldBuffAbsorptionBatchTotals snapshot) =>
                snapshot.ActorsWithAbsorption > 0;

            var preFilteredByHand = new List<ShieldBuffAbsorptionBatchTotals>();
            foreach (var snapshot in sequence)
            {
                if (Filter(snapshot))
                    preFilteredByHand.Add(snapshot);
            }

            var filtered = ShieldBuffAbsorptionBatchTotals.PartitionMany(
                sequence,
                includePredicate: Include,
                filterPredicate: Filter);
            var hand = ShieldBuffAbsorptionBatchTotals.PartitionMany(
                preFilteredByHand,
                Include);

            AssertTotalsEqual(hand.Included, filtered.Included);
            AssertTotalsEqual(hand.Excluded, filtered.Excluded);
        }

        [Test]
        public void PartitionMany_IsPermutationInvariant()
        {
            var (a, b, c) = BuildThreeDistinctTotals();

            bool Filter(ShieldBuffAbsorptionBatchTotals snapshot) =>
                snapshot.ActorCount > 0;
            bool Include(ShieldBuffAbsorptionBatchTotals snapshot) =>
                snapshot.ActorsWithAbsorption > 0;

            var forward = ShieldBuffAbsorptionBatchTotals.PartitionMany(
                new List<ShieldBuffAbsorptionBatchTotals> { a, b, c },
                includePredicate: Include,
                filterPredicate: Filter);
            var reverse = ShieldBuffAbsorptionBatchTotals.PartitionMany(
                new List<ShieldBuffAbsorptionBatchTotals> { c, b, a },
                includePredicate: Include,
                filterPredicate: Filter);
            var shuffled = ShieldBuffAbsorptionBatchTotals.PartitionMany(
                new List<ShieldBuffAbsorptionBatchTotals> { b, a, c },
                includePredicate: Include,
                filterPredicate: Filter);

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
                includePredicate: snapshot => snapshot.ActorsWithAbsorption > 0,
                filterPredicate: snapshot => snapshot.ActorCount > 0);

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
