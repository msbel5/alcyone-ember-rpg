using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Magic;
using EmberCrpg.Simulation.Magic;
using NUnit.Framework;

// Design note:
// Pins the deterministic Sprint 5 filtered group-by batch shield-buff absorption totals seam:
// ShieldBuffService.GroupBatchTotals(map, keyExtractor, includePredicate) (and the underlying
// ShieldBuffAbsorptionBatchTotals.GroupBy(map, keyExtractor, includePredicate) overload) walks
// the per-actor absorption result map exactly once and routes only the entries that pass the
// includePredicate into a per-group ShieldBuffAbsorptionBatchTotals bucket keyed by the
// keyExtractor. Foundation-only: argument guards, empty-input emptiness, null-predicate parity
// vs the unfiltered GroupBy, sum-of-buckets parity vs filtered ComputeBatchTotals, per-bucket
// parity vs the equivalent two-condition filtered ComputeBatchTotals, and registry no-mutation.
// No save/load, application, tick-down, or combat-pipeline coverage here.
namespace EmberCrpg.Tests.EditMode.Magic
{
    /// <summary>Verifies deterministic filtered group-by totals over batch shield-buff absorption results.</summary>
    public sealed class ShieldBuffServiceBatchAbsorptionTotalsGroupByFilterTests
    {
        [Test]
        public void GroupByFilter_NullResultMap_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => ShieldBuffAbsorptionBatchTotals.GroupBy(
                    null,
                    (id, result) => "k",
                    (id, result) => true));
        }

        [Test]
        public void GroupByFilter_NullKeyExtractor_Throws()
        {
            var map = new Dictionary<string, ShieldBuffAbsorptionResult>();

            Assert.Throws<ArgumentNullException>(
                () => ShieldBuffAbsorptionBatchTotals.GroupBy(
                    map,
                    keyExtractor: null,
                    includePredicate: (id, result) => true));
        }

        [Test]
        public void GroupBatchTotalsWithFilter_NullResultMap_Throws()
        {
            var service = new ShieldBuffService();

            Assert.Throws<ArgumentNullException>(
                () => service.GroupBatchTotals(null, (id, result) => "k", (id, result) => true));
        }

        [Test]
        public void GroupBatchTotalsWithFilter_NullKeyExtractor_Throws()
        {
            var service = new ShieldBuffService();
            var map = new Dictionary<string, ShieldBuffAbsorptionResult>();

            Assert.Throws<ArgumentNullException>(
                () => service.GroupBatchTotals(map, keyExtractor: null, includePredicate: (id, result) => true));
        }

        [Test]
        public void GroupByFilter_WhitespaceActorKey_StillThrowsBeforePredicate()
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
                    (id, _) => false));
        }

        [Test]
        public void GroupByFilter_NullActorResult_StillThrowsBeforePredicate()
        {
            var map = new Dictionary<string, ShieldBuffAbsorptionResult>
            {
                { "hero_a", null }
            };

            Assert.Throws<ArgumentException>(
                () => ShieldBuffAbsorptionBatchTotals.GroupBy(
                    map,
                    (id, _) => "k",
                    (id, _) => false));
        }

        [Test]
        public void GroupBatchTotalsWithFilter_KeyExtractorReturnsNullForIncluded_Throws()
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
                    (id, _) => null,
                    (id, _) => true));
        }

        [Test]
        public void GroupBatchTotalsWithFilter_EmptyMap_ReturnsEmptyGroupDictionary()
        {
            var service = new ShieldBuffService();
            var groups = service.GroupBatchTotals(
                new Dictionary<string, ShieldBuffAbsorptionResult>(),
                (id, _) => "any",
                (id, _) => true);

            Assert.That(groups, Is.Not.Null);
            Assert.That(groups.Count, Is.EqualTo(0));
        }

        [Test]
        public void GroupBatchTotalsWithFilter_AllExcluded_ReturnsEmptyGroupDictionary()
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
                (id, _) => false);

            Assert.That(groups, Is.Not.Null);
            Assert.That(groups.Count, Is.EqualTo(0));
        }

        [Test]
        public void GroupBatchTotalsWithFilter_NullPredicate_BehavesLikeUnfilteredGroupBy()
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

            var unfiltered = service.GroupBatchTotals(perActor, sideOf);
            var filteredNull = service.GroupBatchTotals(perActor, sideOf, includePredicate: null);

            Assert.That(filteredNull.Count, Is.EqualTo(unfiltered.Count));
            foreach (var pair in unfiltered)
            {
                Assert.That(filteredNull.ContainsKey(pair.Key), Is.True);
                AssertEqualTotals(filteredNull[pair.Key], pair.Value);
            }
        }

        [Test]
        public void GroupBatchTotalsWithFilter_PerBucketParityVsTwoConditionFilteredComputeBatchTotals()
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

            var groups = service.GroupBatchTotals(perActor, sideOf, hasAbsorption);

            foreach (var pair in groups)
            {
                var bucketKey = pair.Key;
                var expected = service.ComputeBatchTotals(
                    perActor,
                    (id, r) => sideOf(id, r) == bucketKey && hasAbsorption(id, r));
                AssertEqualTotals(pair.Value, expected);
            }
        }

        [Test]
        public void GroupBatchTotalsWithFilter_SumOfAllBuckets_EqualsFilteredComputeBatchTotals()
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

            var filteredWhole = service.ComputeBatchTotals(perActor, hasAbsorption);
            var groups = service.GroupBatchTotals(
                perActor,
                (id, _) => id.StartsWith("ally_", StringComparison.Ordinal) ? "ally"
                    : id.StartsWith("enemy_", StringComparison.Ordinal) ? "enemy"
                    : "neutral",
                hasAbsorption);

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

            Assert.That(actorCountSum, Is.EqualTo(filteredWhole.ActorCount));
            Assert.That(incomingSum, Is.EqualTo(filteredWhole.TotalIncomingDamage));
            Assert.That(absorbedSum, Is.EqualTo(filteredWhole.TotalAbsorbedDamage));
            Assert.That(remainingSum, Is.EqualTo(filteredWhole.TotalRemainingDamage));
            Assert.That(withAbsorptionSum, Is.EqualTo(filteredWhole.ActorsWithAbsorption));
            Assert.That(consumedSum, Is.EqualTo(filteredWhole.TotalConsumedBuffEntries));
            Assert.That(expiredSum, Is.EqualTo(filteredWhole.TotalExpiredBuffEntries));
        }

        [Test]
        public void GroupBatchTotalsWithFilter_DoesNotMutateRegistry()
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

            service.GroupBatchTotals(perActor, (id, _) => "any", (id, _) => true);

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
