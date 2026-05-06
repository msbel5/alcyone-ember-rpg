using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Magic;
using EmberCrpg.Simulation.Magic;
using NUnit.Framework;

// Design note:
// Pins the deterministic Sprint 5 cross-batch shield-buff totals GroupByMany seam:
// ShieldBuffService.GroupBatchTotalsByMany(sequence, keyExtractor) (and the underlying
// ShieldBuffAbsorptionBatchTotals.GroupByMany(sequence, keyExtractor)) walks an arbitrary
// sequence of already-computed batch absorption totals snapshots exactly once and routes
// each snapshot into a per-group bucket keyed by the caller-supplied keyExtractor,
// returning an IReadOnlyDictionary<string, ShieldBuffAbsorptionBatchTotals> whose values
// are themselves complete batch totals snapshots. Foundation only: argument guards (null
// sequence, null keyExtractor, null element with index in message before the keyExtractor
// is consulted, whitespace-only group key), empty-sequence behaviour, single-bucket
// equivalence to MergeMany, multi-bucket sums equivalent to per-bucket pre-filtered
// MergeMany folds, union of all buckets equals MergeMany over the whole sequence,
// permutation invariance, and isolation from registry/buff state. No save/load,
// application, tick-down, or combat-pipeline coverage here.
namespace EmberCrpg.Tests.EditMode.Magic
{
    /// <summary>Verifies deterministic cross-batch group-by fold over batch shield-buff absorption totals snapshots.</summary>
    public sealed class ShieldBuffServiceBatchAbsorptionTotalsGroupByManyTests
    {
        [Test]
        public void GroupByMany_NullSequence_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ShieldBuffAbsorptionBatchTotals.GroupByMany(
                    null,
                    keyExtractor: _ => "k"));
        }

        [Test]
        public void GroupBatchTotalsByMany_NullSequence_Throws()
        {
            var service = new ShieldBuffService();

            Assert.Throws<ArgumentNullException>(() =>
                service.GroupBatchTotalsByMany(null, keyExtractor: _ => "k"));
        }

        [Test]
        public void GroupByMany_NullKeyExtractor_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ShieldBuffAbsorptionBatchTotals.GroupByMany(
                    new List<ShieldBuffAbsorptionBatchTotals>(),
                    keyExtractor: null));
        }

        [Test]
        public void GroupBatchTotalsByMany_NullKeyExtractor_Throws()
        {
            var service = new ShieldBuffService();

            Assert.Throws<ArgumentNullException>(() =>
                service.GroupBatchTotalsByMany(
                    new List<ShieldBuffAbsorptionBatchTotals>(),
                    keyExtractor: null));
        }

        [Test]
        public void GroupByMany_NullElement_ThrowsBeforeKeyExtractor()
        {
            var totals = BuildTotalsFromBatch();
            var sequence = new List<ShieldBuffAbsorptionBatchTotals>
            {
                totals,
                null,
                ShieldBuffAbsorptionBatchTotals.Empty,
            };

            var ex = Assert.Throws<ArgumentException>(() =>
                ShieldBuffAbsorptionBatchTotals.GroupByMany(
                    sequence,
                    keyExtractor: _ => "k"));
            Assert.That(ex.Message, Does.Contain("index 1"));
        }

        [Test]
        public void GroupBatchTotalsByMany_NullElement_ThrowsBeforeKeyExtractor()
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
                service.GroupBatchTotalsByMany(sequence, keyExtractor: _ => "k"));
            Assert.That(ex.Message, Does.Contain("index 2"));
        }

        [Test]
        public void GroupByMany_WhitespaceGroupKey_Throws()
        {
            var (a, _, _) = BuildThreeDistinctTotals();

            Assert.Throws<ArgumentException>(() =>
                ShieldBuffAbsorptionBatchTotals.GroupByMany(
                    new List<ShieldBuffAbsorptionBatchTotals> { a },
                    keyExtractor: _ => "   "));
        }

        [Test]
        public void GroupByMany_EmptySequence_ReturnsEmptyDictionary()
        {
            var groups = ShieldBuffAbsorptionBatchTotals.GroupByMany(
                new List<ShieldBuffAbsorptionBatchTotals>(),
                keyExtractor: _ => "k");

            Assert.That(groups, Is.Not.Null);
            Assert.That(groups.Count, Is.EqualTo(0));
        }

        [Test]
        public void GroupByMany_SingleBucket_MatchesMergeMany()
        {
            var (a, b, c) = BuildThreeDistinctTotals();
            var sequence = new List<ShieldBuffAbsorptionBatchTotals> { a, b, c };

            var groups = ShieldBuffAbsorptionBatchTotals.GroupByMany(
                sequence,
                keyExtractor: _ => "all");
            var unfiltered = ShieldBuffAbsorptionBatchTotals.MergeMany(sequence);

            Assert.That(groups.Count, Is.EqualTo(1));
            Assert.That(groups.ContainsKey("all"), Is.True);
            AssertTotalsEqual(unfiltered, groups["all"]);
        }

        [Test]
        public void GroupByMany_TwoBuckets_MatchPreFilteredMergeManyOnEachBucket()
        {
            var (a, b, c) = BuildThreeDistinctTotals();
            var sequence = new List<ShieldBuffAbsorptionBatchTotals>
            {
                a,
                ShieldBuffAbsorptionBatchTotals.Empty,
                b,
                c,
            };

            string KeyOf(ShieldBuffAbsorptionBatchTotals snapshot) =>
                snapshot.ActorsWithAbsorption > 0 ? "active" : "idle";

            var activeByHand = new List<ShieldBuffAbsorptionBatchTotals>();
            var idleByHand = new List<ShieldBuffAbsorptionBatchTotals>();
            foreach (var snapshot in sequence)
            {
                if (KeyOf(snapshot) == "active")
                    activeByHand.Add(snapshot);
                else
                    idleByHand.Add(snapshot);
            }

            var groups = ShieldBuffAbsorptionBatchTotals.GroupByMany(sequence, KeyOf);

            Assert.That(groups.Count, Is.EqualTo(2));
            AssertTotalsEqual(
                ShieldBuffAbsorptionBatchTotals.MergeMany(activeByHand),
                groups["active"]);
            AssertTotalsEqual(
                ShieldBuffAbsorptionBatchTotals.MergeMany(idleByHand),
                groups["idle"]);
        }

        [Test]
        public void GroupByMany_BucketUnionEqualsUnfilteredMergeMany()
        {
            var (a, b, c) = BuildThreeDistinctTotals();
            var sequence = new List<ShieldBuffAbsorptionBatchTotals>
            {
                a,
                ShieldBuffAbsorptionBatchTotals.Empty,
                b,
                c,
            };

            string KeyOf(ShieldBuffAbsorptionBatchTotals snapshot) =>
                snapshot.ActorCount > 0 ? "with-actors" : "no-actors";

            var groups = ShieldBuffAbsorptionBatchTotals.GroupByMany(sequence, KeyOf);

            var union = ShieldBuffAbsorptionBatchTotals.Empty;
            foreach (var pair in groups)
                union = ShieldBuffAbsorptionBatchTotals.Merge(union, pair.Value);
            var unfiltered = ShieldBuffAbsorptionBatchTotals.MergeMany(sequence);

            AssertTotalsEqual(unfiltered, union);
        }

        [Test]
        public void GroupByMany_IsPermutationInvariant()
        {
            var (a, b, c) = BuildThreeDistinctTotals();

            string KeyOf(ShieldBuffAbsorptionBatchTotals snapshot) =>
                snapshot.ActorCount > 0 ? "with-actors" : "no-actors";

            var forward = ShieldBuffAbsorptionBatchTotals.GroupByMany(
                new List<ShieldBuffAbsorptionBatchTotals> { a, b, c },
                KeyOf);
            var reverse = ShieldBuffAbsorptionBatchTotals.GroupByMany(
                new List<ShieldBuffAbsorptionBatchTotals> { c, b, a },
                KeyOf);
            var shuffled = ShieldBuffAbsorptionBatchTotals.GroupByMany(
                new List<ShieldBuffAbsorptionBatchTotals> { b, a, c },
                KeyOf);

            Assert.That(reverse.Count, Is.EqualTo(forward.Count));
            Assert.That(shuffled.Count, Is.EqualTo(forward.Count));
            foreach (var pair in forward)
            {
                AssertTotalsEqual(pair.Value, reverse[pair.Key]);
                AssertTotalsEqual(pair.Value, shuffled[pair.Key]);
            }
        }

        [Test]
        public void GroupBatchTotalsByMany_DoesNotMutateRegistry()
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

            service.GroupBatchTotalsByMany(
                new List<ShieldBuffAbsorptionBatchTotals>
                {
                    totals,
                    ShieldBuffAbsorptionBatchTotals.Empty,
                    totals,
                },
                snapshot => snapshot.ActorCount > 0 ? "with" : "without");

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
