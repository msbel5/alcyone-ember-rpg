using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Magic;
using EmberCrpg.Simulation.Magic;
using NUnit.Framework;

// Design note:
// Pins the deterministic Sprint 5 cross-batch shield-buff totals filtered GroupByMany seam:
// ShieldBuffService.GroupBatchTotalsByMany(sequence, keyExtractor, includePredicate) (and the
// underlying ShieldBuffAbsorptionBatchTotals.GroupByMany(sequence, keyExtractor,
// includePredicate)) walks an arbitrary sequence of already-computed batch absorption totals
// snapshots exactly once, applies a per-snapshot includePredicate, and routes each kept
// snapshot into a per-group bucket keyed by the caller-supplied keyExtractor. Foundation
// only: argument guards (null sequence, null keyExtractor, null element with index in message
// before either predicate or keyExtractor is consulted, whitespace-only group key on a kept
// snapshot but not on a filtered-out snapshot), null-predicate behaves as unfiltered overload,
// fully filtered-out and empty sequences return empty dictionaries, single-bucket equivalence
// to filtered MergeMany, multi-bucket sums equivalent to per-bucket pre-filtered MergeMany
// folds, union of all buckets equals filtered MergeMany over the whole sequence, permutation
// invariance, and isolation from registry/buff state. No save/load, application, tick-down,
// or combat-pipeline coverage here.
namespace EmberCrpg.Tests.EditMode.Magic
{
    /// <summary>Verifies deterministic predicate-filtered cross-batch group-by fold over batch shield-buff absorption totals snapshots.</summary>
    public sealed class ShieldBuffServiceBatchAbsorptionTotalsGroupByManyFilterTests
    {
        [Test]
        public void GroupByMany_NullSequence_ThrowsBeforePredicate()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ShieldBuffAbsorptionBatchTotals.GroupByMany(
                    null,
                    keyExtractor: _ => "k",
                    includePredicate: _ => true));
        }

        [Test]
        public void GroupBatchTotalsByMany_NullSequence_ThrowsBeforePredicate()
        {
            var service = new ShieldBuffService();

            Assert.Throws<ArgumentNullException>(() =>
                service.GroupBatchTotalsByMany(
                    null,
                    keyExtractor: _ => "k",
                    includePredicate: _ => true));
        }

        [Test]
        public void GroupByMany_NullKeyExtractor_ThrowsBeforePredicate()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ShieldBuffAbsorptionBatchTotals.GroupByMany(
                    new List<ShieldBuffAbsorptionBatchTotals>(),
                    keyExtractor: null,
                    includePredicate: _ => true));
        }

        [Test]
        public void GroupBatchTotalsByMany_NullKeyExtractor_ThrowsBeforePredicate()
        {
            var service = new ShieldBuffService();

            Assert.Throws<ArgumentNullException>(() =>
                service.GroupBatchTotalsByMany(
                    new List<ShieldBuffAbsorptionBatchTotals>(),
                    keyExtractor: null,
                    includePredicate: _ => true));
        }

        [Test]
        public void GroupByMany_NullElement_ThrowsBeforePredicate()
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
                    keyExtractor: _ => "k",
                    includePredicate: _ => true));
            Assert.That(ex.Message, Does.Contain("index 1"));
        }

        [Test]
        public void GroupBatchTotalsByMany_NullElement_ThrowsBeforePredicate()
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
                service.GroupBatchTotalsByMany(
                    sequence,
                    keyExtractor: _ => "k",
                    includePredicate: _ => true));
            Assert.That(ex.Message, Does.Contain("index 2"));
        }

        [Test]
        public void GroupByMany_WhitespaceGroupKeyOnKeptElement_Throws()
        {
            var (a, _, _) = BuildThreeDistinctTotals();

            Assert.Throws<ArgumentException>(() =>
                ShieldBuffAbsorptionBatchTotals.GroupByMany(
                    new List<ShieldBuffAbsorptionBatchTotals> { a },
                    keyExtractor: _ => "   ",
                    includePredicate: _ => true));
        }

        [Test]
        public void GroupByMany_WhitespaceGroupKeyOnFilteredOutElement_DoesNotThrow()
        {
            var (a, _, _) = BuildThreeDistinctTotals();

            // The whitespace key must never be consulted because includePredicate excludes
            // the only element. The fully filtered-out sequence returns an empty dictionary.
            var groups = ShieldBuffAbsorptionBatchTotals.GroupByMany(
                new List<ShieldBuffAbsorptionBatchTotals> { a },
                keyExtractor: _ => "   ",
                includePredicate: _ => false);

            Assert.That(groups, Is.Not.Null);
            Assert.That(groups.Count, Is.EqualTo(0));
        }

        [Test]
        public void GroupByMany_NullPredicate_BehavesAsUnfilteredOverload()
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

            var filtered = ShieldBuffAbsorptionBatchTotals.GroupByMany(
                sequence,
                KeyOf,
                includePredicate: null);
            var unfiltered = ShieldBuffAbsorptionBatchTotals.GroupByMany(sequence, KeyOf);

            Assert.That(filtered.Count, Is.EqualTo(unfiltered.Count));
            foreach (var pair in unfiltered)
                AssertTotalsEqual(pair.Value, filtered[pair.Key]);
        }

        [Test]
        public void GroupByMany_EmptySequence_ReturnsEmptyDictionary()
        {
            var groups = ShieldBuffAbsorptionBatchTotals.GroupByMany(
                new List<ShieldBuffAbsorptionBatchTotals>(),
                keyExtractor: _ => "k",
                includePredicate: _ => true);

            Assert.That(groups, Is.Not.Null);
            Assert.That(groups.Count, Is.EqualTo(0));
        }

        [Test]
        public void GroupByMany_FullyFilteredOut_ReturnsEmptyDictionary()
        {
            var (a, b, c) = BuildThreeDistinctTotals();

            var groups = ShieldBuffAbsorptionBatchTotals.GroupByMany(
                new List<ShieldBuffAbsorptionBatchTotals> { a, b, c },
                keyExtractor: _ => "k",
                includePredicate: _ => false);

            Assert.That(groups, Is.Not.Null);
            Assert.That(groups.Count, Is.EqualTo(0));
        }

        [Test]
        public void GroupByMany_SingleBucket_MatchesFilteredMergeMany()
        {
            var (a, b, c) = BuildThreeDistinctTotals();
            var sequence = new List<ShieldBuffAbsorptionBatchTotals>
            {
                a,
                ShieldBuffAbsorptionBatchTotals.Empty,
                b,
                c,
            };

            bool Keep(ShieldBuffAbsorptionBatchTotals snapshot) => snapshot.ActorCount > 0;

            var groups = ShieldBuffAbsorptionBatchTotals.GroupByMany(
                sequence,
                keyExtractor: _ => "all",
                includePredicate: Keep);
            var filteredFold = ShieldBuffAbsorptionBatchTotals.MergeMany(sequence, Keep);

            Assert.That(groups.Count, Is.EqualTo(1));
            Assert.That(groups.ContainsKey("all"), Is.True);
            AssertTotalsEqual(filteredFold, groups["all"]);
        }

        [Test]
        public void GroupByMany_TwoBuckets_MatchPerBucketPreFilteredMergeMany()
        {
            var (a, b, c) = BuildThreeDistinctTotals();
            var sequence = new List<ShieldBuffAbsorptionBatchTotals>
            {
                a,
                ShieldBuffAbsorptionBatchTotals.Empty,
                b,
                c,
            };

            // Filter out empty snapshots, then split kept ones by absorption activity.
            bool Keep(ShieldBuffAbsorptionBatchTotals snapshot) => snapshot.ActorCount > 0;
            string KeyOf(ShieldBuffAbsorptionBatchTotals snapshot) =>
                snapshot.ActorsWithAbsorption > 0 ? "active" : "idle";

            var activeByHand = new List<ShieldBuffAbsorptionBatchTotals>();
            var idleByHand = new List<ShieldBuffAbsorptionBatchTotals>();
            foreach (var snapshot in sequence)
            {
                if (!Keep(snapshot))
                    continue;
                if (KeyOf(snapshot) == "active")
                    activeByHand.Add(snapshot);
                else
                    idleByHand.Add(snapshot);
            }

            var groups = ShieldBuffAbsorptionBatchTotals.GroupByMany(sequence, KeyOf, Keep);

            var expectedGroupCount =
                (activeByHand.Count > 0 ? 1 : 0) + (idleByHand.Count > 0 ? 1 : 0);
            Assert.That(groups.Count, Is.EqualTo(expectedGroupCount));
            if (activeByHand.Count > 0)
                AssertTotalsEqual(
                    ShieldBuffAbsorptionBatchTotals.MergeMany(activeByHand),
                    groups["active"]);
            if (idleByHand.Count > 0)
                AssertTotalsEqual(
                    ShieldBuffAbsorptionBatchTotals.MergeMany(idleByHand),
                    groups["idle"]);
        }

        [Test]
        public void GroupByMany_BucketUnionEqualsFilteredMergeMany()
        {
            var (a, b, c) = BuildThreeDistinctTotals();
            var sequence = new List<ShieldBuffAbsorptionBatchTotals>
            {
                a,
                ShieldBuffAbsorptionBatchTotals.Empty,
                b,
                c,
            };

            bool Keep(ShieldBuffAbsorptionBatchTotals snapshot) => snapshot.ActorCount > 0;
            string KeyOf(ShieldBuffAbsorptionBatchTotals snapshot) =>
                snapshot.ActorsWithAbsorption > 0 ? "active" : "idle";

            var groups = ShieldBuffAbsorptionBatchTotals.GroupByMany(sequence, KeyOf, Keep);

            var union = ShieldBuffAbsorptionBatchTotals.Empty;
            foreach (var pair in groups)
                union = ShieldBuffAbsorptionBatchTotals.Merge(union, pair.Value);
            var filteredFold = ShieldBuffAbsorptionBatchTotals.MergeMany(sequence, Keep);

            AssertTotalsEqual(filteredFold, union);
        }

        [Test]
        public void GroupByMany_IsPermutationInvariant()
        {
            var (a, b, c) = BuildThreeDistinctTotals();

            bool Keep(ShieldBuffAbsorptionBatchTotals snapshot) => snapshot.ActorCount > 0;
            string KeyOf(ShieldBuffAbsorptionBatchTotals snapshot) =>
                snapshot.ActorsWithAbsorption > 0 ? "active" : "idle";

            var forward = ShieldBuffAbsorptionBatchTotals.GroupByMany(
                new List<ShieldBuffAbsorptionBatchTotals> { a, b, c },
                KeyOf,
                Keep);
            var reverse = ShieldBuffAbsorptionBatchTotals.GroupByMany(
                new List<ShieldBuffAbsorptionBatchTotals> { c, b, a },
                KeyOf,
                Keep);
            var shuffled = ShieldBuffAbsorptionBatchTotals.GroupByMany(
                new List<ShieldBuffAbsorptionBatchTotals> { b, a, c },
                KeyOf,
                Keep);

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
                keyExtractor: snapshot => snapshot.ActorCount > 0 ? "with" : "without",
                includePredicate: snapshot => snapshot.ActorCount > 0);

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
