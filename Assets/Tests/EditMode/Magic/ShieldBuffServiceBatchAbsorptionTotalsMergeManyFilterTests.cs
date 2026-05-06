using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Magic;
using EmberCrpg.Simulation.Magic;
using NUnit.Framework;

// Design note:
// Pins the deterministic Sprint 5 cross-batch shield-buff totals predicate-filtered
// MergeMany seam: ShieldBuffService.MergeBatchTotalsMany(sequence, predicate) (and the
// underlying ShieldBuffAbsorptionBatchTotals.MergeMany(sequence, predicate)) folds an
// arbitrary sequence of already-computed batch absorption totals snapshots into a
// single field-wise sum, skipping any element for which includePredicate returns false.
// Foundation only: argument guards (null sequence, null element with index in message
// even when the element would have been excluded), null-predicate parity with the
// unfiltered MergeMany overload, all-included parity, all-excluded returning Empty,
// parity with a pre-filtered MergeMany call, permutation invariance, and isolation
// from registry/buff state. No save/load, application, tick-down, or combat-pipeline
// coverage here.
namespace EmberCrpg.Tests.EditMode.Magic
{
    /// <summary>Verifies deterministic predicate-filtered MergeMany fold over batch shield-buff absorption totals snapshots.</summary>
    public sealed class ShieldBuffServiceBatchAbsorptionTotalsMergeManyFilterTests
    {
        [Test]
        public void MergeMany_Filtered_NullSequence_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ShieldBuffAbsorptionBatchTotals.MergeMany(
                    null,
                    includePredicate: _ => true));
        }

        [Test]
        public void MergeBatchTotalsMany_Filtered_NullSequence_Throws()
        {
            var service = new ShieldBuffService();

            Assert.Throws<ArgumentNullException>(() =>
                service.MergeBatchTotalsMany(null, includePredicate: _ => true));
        }

        [Test]
        public void MergeMany_Filtered_NullElement_ThrowsBeforePredicate()
        {
            var totals = BuildTotalsFromBatch();
            var sequence = new List<ShieldBuffAbsorptionBatchTotals>
            {
                totals,
                null,
                ShieldBuffAbsorptionBatchTotals.Empty,
            };

            var ex = Assert.Throws<ArgumentException>(() =>
                ShieldBuffAbsorptionBatchTotals.MergeMany(
                    sequence,
                    includePredicate: _ => false));
            Assert.That(ex.Message, Does.Contain("index 1"));
        }

        [Test]
        public void MergeBatchTotalsMany_Filtered_NullElement_ThrowsBeforePredicate()
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
                service.MergeBatchTotalsMany(sequence, includePredicate: _ => false));
            Assert.That(ex.Message, Does.Contain("index 2"));
        }

        [Test]
        public void MergeMany_NullPredicate_MatchesUnfilteredOverload()
        {
            var (a, b, c) = BuildThreeDistinctTotals();
            var sequence = new List<ShieldBuffAbsorptionBatchTotals> { a, b, c };

            var unfiltered = ShieldBuffAbsorptionBatchTotals.MergeMany(sequence);
            var nullPredicate = ShieldBuffAbsorptionBatchTotals.MergeMany(
                sequence,
                includePredicate: null);

            AssertTotalsEqual(unfiltered, nullPredicate);
        }

        [Test]
        public void MergeMany_PredicateAlwaysTrue_MatchesUnfilteredOverload()
        {
            var (a, b, c) = BuildThreeDistinctTotals();
            var sequence = new List<ShieldBuffAbsorptionBatchTotals> { a, b, c };

            var unfiltered = ShieldBuffAbsorptionBatchTotals.MergeMany(sequence);
            var alwaysTrue = ShieldBuffAbsorptionBatchTotals.MergeMany(
                sequence,
                includePredicate: _ => true);

            AssertTotalsEqual(unfiltered, alwaysTrue);
        }

        [Test]
        public void MergeMany_PredicateAlwaysFalse_ReturnsEmpty()
        {
            var (a, b, c) = BuildThreeDistinctTotals();

            var merged = ShieldBuffAbsorptionBatchTotals.MergeMany(
                new List<ShieldBuffAbsorptionBatchTotals> { a, b, c },
                includePredicate: _ => false);

            AssertTotalsEqual(ShieldBuffAbsorptionBatchTotals.Empty, merged);
        }

        [Test]
        public void MergeMany_Filtered_EmptySequence_ReturnsEmpty()
        {
            var merged = ShieldBuffAbsorptionBatchTotals.MergeMany(
                new List<ShieldBuffAbsorptionBatchTotals>(),
                includePredicate: _ => true);

            AssertTotalsEqual(ShieldBuffAbsorptionBatchTotals.Empty, merged);
        }

        [Test]
        public void MergeMany_Filtered_MatchesPreFilteredMergeMany()
        {
            var (a, b, c) = BuildThreeDistinctTotals();
            var sequence = new List<ShieldBuffAbsorptionBatchTotals>
            {
                a,
                ShieldBuffAbsorptionBatchTotals.Empty,
                b,
                c,
            };

            // Predicate excludes the explicit Empty entry plus any snapshot with no
            // absorption activity. Pre-filter the same sequence by hand and feed it to
            // the unfiltered MergeMany; both folds must produce the same totals.
            bool Include(ShieldBuffAbsorptionBatchTotals snapshot) =>
                snapshot.ActorsWithAbsorption > 0;

            var preFiltered = new List<ShieldBuffAbsorptionBatchTotals>();
            foreach (var snapshot in sequence)
            {
                if (Include(snapshot))
                    preFiltered.Add(snapshot);
            }

            var filteredFold = ShieldBuffAbsorptionBatchTotals.MergeMany(
                sequence,
                Include);
            var preFilteredFold = ShieldBuffAbsorptionBatchTotals.MergeMany(preFiltered);

            AssertTotalsEqual(preFilteredFold, filteredFold);
        }

        [Test]
        public void MergeMany_Filtered_IsPermutationInvariant()
        {
            var (a, b, c) = BuildThreeDistinctTotals();

            bool Include(ShieldBuffAbsorptionBatchTotals snapshot) =>
                snapshot.ActorCount > 0;

            var forward = ShieldBuffAbsorptionBatchTotals.MergeMany(
                new List<ShieldBuffAbsorptionBatchTotals> { a, b, c },
                Include);
            var reverse = ShieldBuffAbsorptionBatchTotals.MergeMany(
                new List<ShieldBuffAbsorptionBatchTotals> { c, b, a },
                Include);
            var shuffled = ShieldBuffAbsorptionBatchTotals.MergeMany(
                new List<ShieldBuffAbsorptionBatchTotals> { b, a, c },
                Include);

            AssertTotalsEqual(forward, reverse);
            AssertTotalsEqual(forward, shuffled);
        }

        [Test]
        public void MergeBatchTotalsMany_Filtered_DoesNotMutateRegistry()
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

            service.MergeBatchTotalsMany(
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
