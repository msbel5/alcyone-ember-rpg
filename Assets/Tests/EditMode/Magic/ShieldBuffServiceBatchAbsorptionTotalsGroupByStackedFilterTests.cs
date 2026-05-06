using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Magic;
using EmberCrpg.Simulation.Magic;
using NUnit.Framework;

// Design note:
// Pins the deterministic Sprint 5 stacked-filter map-level group-by batch shield-buff
// absorption totals seam: ShieldBuffService.GroupBatchTotals(map, keyExtractor,
// includePredicate, filterPredicate) (and the underlying
// ShieldBuffAbsorptionBatchTotals.GroupBy(map, keyExtractor, includePredicate, filterPredicate)
// overload) walks the per-actor absorption result map exactly once, applies filterPredicate
// before includePredicate, and routes each kept entry into a per-group bucket keyed by the
// keyExtractor. Foundation only: argument guards (null map, null keyExtractor, whitespace
// actor key and null actor result both still throw before either predicate is consulted),
// null filterPredicate behaves as the 3-arg overload, both predicates null behaves as the
// unfiltered overload, fully filtered-out and empty maps return empty dictionaries,
// filterPredicate is consulted before includePredicate, per-bucket parity with the equivalent
// two-condition filtered ComputeBatchTotals, sum-of-buckets parity with the
// (filterPredicate AND includePredicate) ComputeBatchTotals, and registry no-mutation. No
// save/load, application, tick-down, or combat-pipeline coverage here.
namespace EmberCrpg.Tests.EditMode.Magic
{
    /// <summary>Verifies deterministic stacked-filter (filter + key + secondary filter) map-level group-by totals over batch shield-buff absorption results.</summary>
    public sealed class ShieldBuffServiceBatchAbsorptionTotalsGroupByStackedFilterTests
    {
        [Test]
        public void GroupByStackedFilter_NullResultMap_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => ShieldBuffAbsorptionBatchTotals.GroupBy(
                    null,
                    (id, _) => "k",
                    (id, _) => true,
                    (id, _) => true));
        }

        [Test]
        public void GroupBatchTotalsWithStackedFilter_NullResultMap_Throws()
        {
            var service = new ShieldBuffService();

            Assert.Throws<ArgumentNullException>(
                () => service.GroupBatchTotals(
                    null,
                    (id, _) => "k",
                    (id, _) => true,
                    (id, _) => true));
        }

        [Test]
        public void GroupByStackedFilter_NullKeyExtractor_Throws()
        {
            var map = new Dictionary<string, ShieldBuffAbsorptionResult>();

            Assert.Throws<ArgumentNullException>(
                () => ShieldBuffAbsorptionBatchTotals.GroupBy(
                    map,
                    keyExtractor: null,
                    includePredicate: (id, _) => true,
                    filterPredicate: (id, _) => true));
        }

        [Test]
        public void GroupBatchTotalsWithStackedFilter_NullKeyExtractor_Throws()
        {
            var service = new ShieldBuffService();
            var map = new Dictionary<string, ShieldBuffAbsorptionResult>();

            Assert.Throws<ArgumentNullException>(
                () => service.GroupBatchTotals(
                    map,
                    keyExtractor: null,
                    includePredicate: (id, _) => true,
                    filterPredicate: (id, _) => true));
        }

        [Test]
        public void GroupByStackedFilter_WhitespaceActorKey_StillThrowsBeforePredicates()
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

            Assert.Throws<ArgumentException>(
                () => ShieldBuffAbsorptionBatchTotals.GroupBy(
                    map,
                    (id, _) => "k",
                    (id, _) => false,
                    (id, _) => false));
        }

        [Test]
        public void GroupByStackedFilter_NullActorResult_StillThrowsBeforePredicates()
        {
            var map = new Dictionary<string, ShieldBuffAbsorptionResult>
            {
                { "hero_a", null }
            };

            Assert.Throws<ArgumentException>(
                () => ShieldBuffAbsorptionBatchTotals.GroupBy(
                    map,
                    (id, _) => "k",
                    (id, _) => false,
                    (id, _) => false));
        }

        [Test]
        public void GroupByStackedFilter_WhitespaceGroupKeyOnKeptEntry_Throws()
        {
            var registry = new ShieldBuffStateRegistry();
            registry.GetOrCreate("hero_a").SetActiveBuff("spellA", remainingTicks: 4, magnitude: 6);

            var service = new ShieldBuffService();
            var perActor = service.AbsorbDamageForActors(
                registry,
                new Dictionary<string, int> { { "hero_a", 4 } });

            Assert.Throws<ArgumentException>(
                () => service.GroupBatchTotals(
                    perActor,
                    (id, _) => "  ",
                    (id, _) => true,
                    (id, _) => true));
        }

        [Test]
        public void GroupByStackedFilter_WhitespaceGroupKeyOnFilterRejectedEntry_DoesNotThrow()
        {
            var registry = new ShieldBuffStateRegistry();
            registry.GetOrCreate("hero_a").SetActiveBuff("spellA", remainingTicks: 4, magnitude: 6);

            var service = new ShieldBuffService();
            var perActor = service.AbsorbDamageForActors(
                registry,
                new Dictionary<string, int> { { "hero_a", 4 } });

            var groups = service.GroupBatchTotals(
                perActor,
                (id, _) => "  ",
                (id, _) => true,
                (id, _) => false);

            Assert.That(groups, Is.Not.Null);
            Assert.That(groups.Count, Is.EqualTo(0));
        }

        [Test]
        public void GroupByStackedFilter_WhitespaceGroupKeyOnIncludeRejectedEntry_DoesNotThrow()
        {
            var registry = new ShieldBuffStateRegistry();
            registry.GetOrCreate("hero_a").SetActiveBuff("spellA", remainingTicks: 4, magnitude: 6);

            var service = new ShieldBuffService();
            var perActor = service.AbsorbDamageForActors(
                registry,
                new Dictionary<string, int> { { "hero_a", 4 } });

            var groups = service.GroupBatchTotals(
                perActor,
                (id, _) => "  ",
                (id, _) => false,
                (id, _) => true);

            Assert.That(groups, Is.Not.Null);
            Assert.That(groups.Count, Is.EqualTo(0));
        }

        [Test]
        public void GroupByStackedFilter_NullFilterPredicate_BehavesAsThreeArgOverload()
        {
            var registry = new ShieldBuffStateRegistry();
            registry.GetOrCreate("ally_a").SetActiveBuff("spellA", remainingTicks: 3, magnitude: 4);
            registry.GetOrCreate("ally_b").SetActiveBuff("spellB", remainingTicks: 5, magnitude: 10);
            registry.GetOrCreate("enemy_a").SetActiveBuff("spellC", remainingTicks: 2, magnitude: 3);

            var service = new ShieldBuffService();
            var perActor = service.AbsorbDamageForActors(
                registry,
                new Dictionary<string, int>
                {
                    { "ally_a", 4 },
                    { "ally_b", 4 },
                    { "enemy_a", 5 }
                });

            Func<string, ShieldBuffAbsorptionResult, string> sideOf =
                (actorId, _) => actorId.StartsWith("ally_", StringComparison.Ordinal) ? "ally" : "enemy";
            Func<string, ShieldBuffAbsorptionResult, bool> hasAbsorption =
                (_, r) => r.AbsorbedDamage > 0;

            var threeArg = service.GroupBatchTotals(perActor, sideOf, hasAbsorption);
            var stackedNullFilter = service.GroupBatchTotals(perActor, sideOf, hasAbsorption, filterPredicate: null);

            Assert.That(stackedNullFilter.Count, Is.EqualTo(threeArg.Count));
            foreach (var pair in threeArg)
            {
                Assert.That(stackedNullFilter.ContainsKey(pair.Key), Is.True);
                AssertEqualTotals(stackedNullFilter[pair.Key], pair.Value);
            }
        }

        [Test]
        public void GroupByStackedFilter_BothPredicatesNull_BehavesAsUnfilteredOverload()
        {
            var registry = new ShieldBuffStateRegistry();
            registry.GetOrCreate("ally_a").SetActiveBuff("spellA", remainingTicks: 3, magnitude: 4);
            registry.GetOrCreate("enemy_a").SetActiveBuff("spellB", remainingTicks: 2, magnitude: 3);

            var service = new ShieldBuffService();
            var perActor = service.AbsorbDamageForActors(
                registry,
                new Dictionary<string, int>
                {
                    { "ally_a", 4 },
                    { "enemy_a", 5 }
                });

            Func<string, ShieldBuffAbsorptionResult, string> sideOf =
                (actorId, _) => actorId.StartsWith("ally_", StringComparison.Ordinal) ? "ally" : "enemy";

            var unfiltered = service.GroupBatchTotals(perActor, sideOf);
            var stackedBothNull = service.GroupBatchTotals(
                perActor, sideOf, includePredicate: null, filterPredicate: null);

            Assert.That(stackedBothNull.Count, Is.EqualTo(unfiltered.Count));
            foreach (var pair in unfiltered)
            {
                Assert.That(stackedBothNull.ContainsKey(pair.Key), Is.True);
                AssertEqualTotals(stackedBothNull[pair.Key], pair.Value);
            }
        }

        [Test]
        public void GroupByStackedFilter_EmptyMap_ReturnsEmptyDictionary()
        {
            var service = new ShieldBuffService();
            var groups = service.GroupBatchTotals(
                new Dictionary<string, ShieldBuffAbsorptionResult>(),
                (id, _) => "any",
                (id, _) => true,
                (id, _) => true);

            Assert.That(groups, Is.Not.Null);
            Assert.That(groups.Count, Is.EqualTo(0));
        }

        [Test]
        public void GroupByStackedFilter_FilterRejectsAll_ReturnsEmptyDictionary()
        {
            var registry = new ShieldBuffStateRegistry();
            registry.GetOrCreate("ally_a").SetActiveBuff("spellA", remainingTicks: 3, magnitude: 4);
            registry.GetOrCreate("enemy_a").SetActiveBuff("spellB", remainingTicks: 2, magnitude: 3);

            var service = new ShieldBuffService();
            var perActor = service.AbsorbDamageForActors(
                registry,
                new Dictionary<string, int>
                {
                    { "ally_a", 4 },
                    { "enemy_a", 5 }
                });

            var groups = service.GroupBatchTotals(
                perActor,
                (id, _) => "any",
                (id, _) => true,
                (id, _) => false);

            Assert.That(groups, Is.Not.Null);
            Assert.That(groups.Count, Is.EqualTo(0));
        }

        [Test]
        public void GroupByStackedFilter_IncludeRejectsAll_ReturnsEmptyDictionary()
        {
            var registry = new ShieldBuffStateRegistry();
            registry.GetOrCreate("ally_a").SetActiveBuff("spellA", remainingTicks: 3, magnitude: 4);
            registry.GetOrCreate("enemy_a").SetActiveBuff("spellB", remainingTicks: 2, magnitude: 3);

            var service = new ShieldBuffService();
            var perActor = service.AbsorbDamageForActors(
                registry,
                new Dictionary<string, int>
                {
                    { "ally_a", 4 },
                    { "enemy_a", 5 }
                });

            var groups = service.GroupBatchTotals(
                perActor,
                (id, _) => "any",
                (id, _) => false,
                (id, _) => true);

            Assert.That(groups, Is.Not.Null);
            Assert.That(groups.Count, Is.EqualTo(0));
        }

        [Test]
        public void GroupByStackedFilter_FilterConsultedBeforeIncludePredicate()
        {
            var registry = new ShieldBuffStateRegistry();
            registry.GetOrCreate("ally_a").SetActiveBuff("spellA", remainingTicks: 3, magnitude: 4);
            registry.GetOrCreate("ally_b").SetActiveBuff("spellB", remainingTicks: 5, magnitude: 10);

            var service = new ShieldBuffService();
            var perActor = service.AbsorbDamageForActors(
                registry,
                new Dictionary<string, int>
                {
                    { "ally_a", 4 },
                    { "ally_b", 4 }
                });

            var filterCalls = new List<string>();
            var includeCalls = new List<string>();

            service.GroupBatchTotals(
                perActor,
                (id, _) => "ally",
                includePredicate: (id, _) =>
                {
                    includeCalls.Add(id);
                    return true;
                },
                filterPredicate: (id, _) =>
                {
                    filterCalls.Add(id);
                    return false;
                });

            Assert.That(filterCalls.Count, Is.EqualTo(perActor.Count));
            Assert.That(includeCalls, Is.Empty);
        }

        [Test]
        public void GroupByStackedFilter_PerBucketParityVsTwoConditionFilteredComputeBatchTotals()
        {
            var registry = new ShieldBuffStateRegistry();
            registry.GetOrCreate("ally_a").SetActiveBuff("spellA", remainingTicks: 3, magnitude: 4);
            registry.GetOrCreate("ally_b").SetActiveBuff("spellB", remainingTicks: 5, magnitude: 10);
            registry.GetOrCreate("enemy_a").SetActiveBuff("spellC", remainingTicks: 2, magnitude: 3);

            var service = new ShieldBuffService();
            var perActor = service.AbsorbDamageForActors(
                registry,
                new Dictionary<string, int>
                {
                    { "ally_a", 4 },
                    { "ally_b", 4 },
                    { "enemy_a", 5 },
                    { "no_shield_npc", 6 }
                });

            Func<string, ShieldBuffAbsorptionResult, string> sideOf =
                (actorId, _) => actorId.StartsWith("ally_", StringComparison.Ordinal) ? "ally"
                    : actorId.StartsWith("enemy_", StringComparison.Ordinal) ? "enemy"
                    : "neutral";

            Func<string, ShieldBuffAbsorptionResult, bool> hasAbsorption =
                (_, r) => r.AbsorbedDamage > 0;
            Func<string, ShieldBuffAbsorptionResult, bool> notNeutral =
                (id, _) => !id.StartsWith("no_", StringComparison.Ordinal);

            var groups = service.GroupBatchTotals(perActor, sideOf, hasAbsorption, notNeutral);

            foreach (var pair in groups)
            {
                var bucketKey = pair.Key;
                var expected = service.ComputeBatchTotals(
                    perActor,
                    (id, r) => notNeutral(id, r) && hasAbsorption(id, r) && sideOf(id, r) == bucketKey);
                AssertEqualTotals(pair.Value, expected);
            }
        }

        [Test]
        public void GroupByStackedFilter_SumOfAllBuckets_EqualsStackedFilterComputeBatchTotals()
        {
            var registry = new ShieldBuffStateRegistry();
            registry.GetOrCreate("ally_a").SetActiveBuff("spellA", remainingTicks: 3, magnitude: 4);
            registry.GetOrCreate("ally_b").SetActiveBuff("spellB", remainingTicks: 5, magnitude: 10);
            registry.GetOrCreate("enemy_a").SetActiveBuff("spellC", remainingTicks: 2, magnitude: 3);

            var service = new ShieldBuffService();
            var perActor = service.AbsorbDamageForActors(
                registry,
                new Dictionary<string, int>
                {
                    { "ally_a", 4 },
                    { "ally_b", 4 },
                    { "enemy_a", 5 },
                    { "no_shield_npc", 6 }
                });

            Func<string, ShieldBuffAbsorptionResult, bool> hasAbsorption =
                (_, r) => r.AbsorbedDamage > 0;
            Func<string, ShieldBuffAbsorptionResult, bool> notNeutral =
                (id, _) => !id.StartsWith("no_", StringComparison.Ordinal);

            var stackedWhole = service.ComputeBatchTotals(
                perActor,
                (id, r) => notNeutral(id, r) && hasAbsorption(id, r));
            var groups = service.GroupBatchTotals(
                perActor,
                (id, _) => id.StartsWith("ally_", StringComparison.Ordinal) ? "ally"
                    : id.StartsWith("enemy_", StringComparison.Ordinal) ? "enemy"
                    : "neutral",
                hasAbsorption,
                notNeutral);

            var actorCountSum = 0;
            var incomingSum = 0;
            var absorbedSum = 0;
            var remainingSum = 0;
            var withAbsorptionSum = 0;
            var consumedSum = 0;
            var expiredSum = 0;
            foreach (var pair in groups)
            {
                actorCountSum += pair.Value.ActorCount;
                incomingSum += pair.Value.TotalIncomingDamage;
                absorbedSum += pair.Value.TotalAbsorbedDamage;
                remainingSum += pair.Value.TotalRemainingDamage;
                withAbsorptionSum += pair.Value.ActorsWithAbsorption;
                consumedSum += pair.Value.TotalConsumedBuffEntries;
                expiredSum += pair.Value.TotalExpiredBuffEntries;
            }

            Assert.That(actorCountSum, Is.EqualTo(stackedWhole.ActorCount));
            Assert.That(incomingSum, Is.EqualTo(stackedWhole.TotalIncomingDamage));
            Assert.That(absorbedSum, Is.EqualTo(stackedWhole.TotalAbsorbedDamage));
            Assert.That(remainingSum, Is.EqualTo(stackedWhole.TotalRemainingDamage));
            Assert.That(withAbsorptionSum, Is.EqualTo(stackedWhole.ActorsWithAbsorption));
            Assert.That(consumedSum, Is.EqualTo(stackedWhole.TotalConsumedBuffEntries));
            Assert.That(expiredSum, Is.EqualTo(stackedWhole.TotalExpiredBuffEntries));
        }

        [Test]
        public void GroupBatchTotalsWithStackedFilter_DoesNotMutateRegistry()
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

            service.GroupBatchTotals(perActor, (id, _) => "any", (id, _) => true, (id, _) => true);

            var trackedAfter = new List<string>(registry.GetTrackedActorIds());
            var ticksAfter = registry.GetOrNull("hero_a").GetRemainingTicks("spellA");
            var magAfter = registry.GetOrNull("hero_a").GetMagnitude("spellA");

            Assert.That(trackedAfter, Is.EquivalentTo(trackedBefore));
            Assert.That(ticksAfter, Is.EqualTo(ticksBefore));
            Assert.That(magAfter, Is.EqualTo(magBefore));
        }

        private static void AssertEqualTotals(
            ShieldBuffAbsorptionBatchTotals actual,
            ShieldBuffAbsorptionBatchTotals expected)
        {
            Assert.That(actual.ActorCount, Is.EqualTo(expected.ActorCount));
            Assert.That(actual.TotalIncomingDamage, Is.EqualTo(expected.TotalIncomingDamage));
            Assert.That(actual.TotalAbsorbedDamage, Is.EqualTo(expected.TotalAbsorbedDamage));
            Assert.That(actual.TotalRemainingDamage, Is.EqualTo(expected.TotalRemainingDamage));
            Assert.That(actual.ActorsWithAbsorption, Is.EqualTo(expected.ActorsWithAbsorption));
            Assert.That(actual.TotalConsumedBuffEntries, Is.EqualTo(expected.TotalConsumedBuffEntries));
            Assert.That(actual.TotalExpiredBuffEntries, Is.EqualTo(expected.TotalExpiredBuffEntries));
        }
    }
}
