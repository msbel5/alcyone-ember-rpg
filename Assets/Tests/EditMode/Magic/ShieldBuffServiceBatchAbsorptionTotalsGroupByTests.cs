using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Magic;
using EmberCrpg.Simulation.Magic;
using NUnit.Framework;

// Design note:
// Pins the deterministic Sprint 5 batch shield-buff absorption totals group-by seam:
// ShieldBuffService.GroupBatchTotals (and the underlying
// ShieldBuffAbsorptionBatchTotals.GroupBy factory) routes each per-actor absorption
// result into a per-group ShieldBuffAbsorptionBatchTotals bucket keyed by a caller
// supplied keyExtractor, in a single deterministic pass. Foundation-only:
// argument guards, empty-input emptiness, all-into-one-key passthrough, sum-of-buckets
// equal to unfiltered totals, parity vs filtered ComputeBatchTotals per bucket, and
// parity vs the binary PartitionFrom contract. No save/load, application, tick-down,
// or combat-pipeline coverage here.
namespace EmberCrpg.Tests.EditMode.Magic
{
    /// <summary>Verifies deterministic group-by totals over batch shield-buff absorption results.</summary>
    public sealed class ShieldBuffServiceBatchAbsorptionTotalsGroupByTests
    {
        [Test]
        public void GroupBy_NullResultMap_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => ShieldBuffAbsorptionBatchTotals.GroupBy(null, (id, result) => "k"));
        }

        [Test]
        public void GroupBy_NullKeyExtractor_Throws()
        {
            var map = new Dictionary<string, ShieldBuffAbsorptionResult>();

            Assert.Throws<ArgumentNullException>(
                () => ShieldBuffAbsorptionBatchTotals.GroupBy(map, keyExtractor: null));
        }

        [Test]
        public void GroupBatchTotals_NullResultMap_Throws()
        {
            var service = new ShieldBuffService();

            Assert.Throws<ArgumentNullException>(
                () => service.GroupBatchTotals(null, (id, result) => "k"));
        }

        [Test]
        public void GroupBatchTotals_NullKeyExtractor_Throws()
        {
            var service = new ShieldBuffService();
            var map = new Dictionary<string, ShieldBuffAbsorptionResult>();

            Assert.Throws<ArgumentNullException>(
                () => service.GroupBatchTotals(map, keyExtractor: null));
        }

        [Test]
        public void GroupBy_WhitespaceActorKey_StillThrowsBeforeKeyExtractor()
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
                () => ShieldBuffAbsorptionBatchTotals.GroupBy(map, (id, _) => "k"));
        }

        [Test]
        public void GroupBy_NullActorResult_StillThrowsBeforeKeyExtractor()
        {
            var map = new Dictionary<string, ShieldBuffAbsorptionResult>
            {
                { "hero_a", null }
            };

            Assert.Throws<ArgumentException>(
                () => ShieldBuffAbsorptionBatchTotals.GroupBy(map, (id, _) => "k"));
        }

        [Test]
        public void GroupBatchTotals_KeyExtractorReturnsNull_Throws()
        {
            var registry = new ShieldBuffStateRegistry();
            registry.GetOrCreate("hero_a").SetActiveBuff("spellA", remainingTicks: 4, magnitude: 6);

            var service = new ShieldBuffService();
            var perActor = service.AbsorbDamageForActors(
                registry,
                new Dictionary<string, int> { { "hero_a", 4 } });

            Assert.Throws<ArgumentException>(
                () => service.GroupBatchTotals(perActor, (id, _) => null));
        }

        [Test]
        public void GroupBatchTotals_KeyExtractorReturnsWhitespace_Throws()
        {
            var registry = new ShieldBuffStateRegistry();
            registry.GetOrCreate("hero_a").SetActiveBuff("spellA", remainingTicks: 4, magnitude: 6);

            var service = new ShieldBuffService();
            var perActor = service.AbsorbDamageForActors(
                registry,
                new Dictionary<string, int> { { "hero_a", 4 } });

            Assert.Throws<ArgumentException>(
                () => service.GroupBatchTotals(perActor, (id, _) => "   "));
        }

        [Test]
        public void GroupBatchTotals_EmptyMap_ReturnsEmptyGroupDictionary()
        {
            var service = new ShieldBuffService();
            var groups = service.GroupBatchTotals(
                new Dictionary<string, ShieldBuffAbsorptionResult>(),
                (id, _) => "any");

            Assert.That(groups, Is.Not.Null);
            Assert.That(groups.Count, Is.EqualTo(0));
        }

        [Test]
        public void GroupBatchTotals_AllIntoOneKey_SingleBucketEqualsUnfilteredTotals()
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

            var groups = service.GroupBatchTotals(perActor, (id, _) => "all");
            var whole = service.ComputeBatchTotals(perActor);

            Assert.That(groups.Count, Is.EqualTo(1));
            Assert.That(groups.ContainsKey("all"), Is.True);
            AssertEqualTotals(groups["all"], whole);
        }

        [Test]
        public void GroupBatchTotals_BySidePrefix_BucketsMatchFilteredComputeBatchTotals()
        {
            var registry = new ShieldBuffStateRegistry();
            // ally_a: shield expires this absorption (mag 4, dmg 4) -> 1 consumed, 1 expired
            registry.GetOrCreate("ally_a").SetActiveBuff("spellA", remainingTicks: 3, magnitude: 4);
            // ally_b: shield consumed but not expired (mag 10, dmg 4) -> 1 consumed
            registry.GetOrCreate("ally_b").SetActiveBuff("spellB", remainingTicks: 5, magnitude: 10);
            // enemy_a: tracked, absorbs partially (mag 3, dmg 5) -> 1 consumed, 1 expired
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

            var groups = service.GroupBatchTotals(perActor, sideOf);
            var allies = service.ComputeBatchTotals(
                perActor,
                (id, _) => id.StartsWith("ally_", StringComparison.Ordinal));
            var enemies = service.ComputeBatchTotals(
                perActor,
                (id, _) => !id.StartsWith("ally_", StringComparison.Ordinal));

            Assert.That(groups.Count, Is.EqualTo(2));
            Assert.That(groups.ContainsKey("ally"), Is.True);
            Assert.That(groups.ContainsKey("enemy"), Is.True);
            AssertEqualTotals(groups["ally"], allies);
            AssertEqualTotals(groups["enemy"], enemies);
        }

        [Test]
        public void GroupBatchTotals_SumOfAllBuckets_EqualsUnfilteredBatchTotals()
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

            var whole = service.ComputeBatchTotals(perActor);
            var groups = service.GroupBatchTotals(
                perActor,
                (id, _) => id.StartsWith("ally_", StringComparison.Ordinal) ? "ally"
                    : id.StartsWith("enemy_", StringComparison.Ordinal) ? "enemy"
                    : "neutral");

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

            Assert.That(actorCountSum, Is.EqualTo(whole.ActorCount));
            Assert.That(incomingSum, Is.EqualTo(whole.TotalIncomingDamage));
            Assert.That(absorbedSum, Is.EqualTo(whole.TotalAbsorbedDamage));
            Assert.That(remainingSum, Is.EqualTo(whole.TotalRemainingDamage));
            Assert.That(withAbsorptionSum, Is.EqualTo(whole.ActorsWithAbsorption));
            Assert.That(consumedSum, Is.EqualTo(whole.TotalConsumedBuffEntries));
            Assert.That(expiredSum, Is.EqualTo(whole.TotalExpiredBuffEntries));
        }

        [Test]
        public void GroupBatchTotals_ParityWithBinaryPartitionFrom()
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

            Func<string, ShieldBuffAbsorptionResult, bool> isAlly =
                (id, _) => id.StartsWith("ally_", StringComparison.Ordinal);

            var partition = service.ComputeBatchTotalsPartition(perActor, isAlly);
            var groups = service.GroupBatchTotals(
                perActor,
                (id, r) => isAlly(id, r) ? "in" : "out");

            Assert.That(groups.Count, Is.EqualTo(2));
            AssertEqualTotals(groups["in"], partition.Included);
            AssertEqualTotals(groups["out"], partition.Excluded);
        }

        [Test]
        public void GroupBatchTotals_DoesNotMutateRegistry()
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

            service.GroupBatchTotals(perActor, (id, _) => "any");

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
