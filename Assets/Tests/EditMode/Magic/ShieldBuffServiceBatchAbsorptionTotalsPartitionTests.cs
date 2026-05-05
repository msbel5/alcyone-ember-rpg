using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Magic;
using EmberCrpg.Simulation.Magic;
using NUnit.Framework;

// Design note:
// Pins the deterministic Sprint 5 batch shield-buff absorption totals partition seam:
// ShieldBuffService.ComputeBatchTotalsPartition (and the underlying
// ShieldBuffAbsorptionBatchTotals.PartitionFrom factory) routes each per-actor
// absorption result into either the Included or the Excluded ShieldBuffAbsorptionBatchTotals
// based on a per-actor predicate, in a single deterministic pass. Foundation-only:
// argument guards, empty-input zeros, all-included / all-excluded passthrough,
// included+excluded sums equal the unfiltered batch totals, and parity vs walking the
// map manually. No save/load, application, tick-down, or combat-pipeline coverage here.
namespace EmberCrpg.Tests.EditMode.Magic
{
    /// <summary>Verifies deterministic partition totals over batch shield-buff absorption results.</summary>
    public sealed class ShieldBuffServiceBatchAbsorptionTotalsPartitionTests
    {
        [Test]
        public void PartitionFrom_NullResultMap_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => ShieldBuffAbsorptionBatchTotals.PartitionFrom(null, (id, result) => true));
        }

        [Test]
        public void PartitionFrom_NullPredicate_Throws()
        {
            var map = new Dictionary<string, ShieldBuffAbsorptionResult>();

            Assert.Throws<ArgumentNullException>(
                () => ShieldBuffAbsorptionBatchTotals.PartitionFrom(map, includePredicate: null));
        }

        [Test]
        public void ComputeBatchTotalsPartition_NullResultMap_Throws()
        {
            var service = new ShieldBuffService();

            Assert.Throws<ArgumentNullException>(
                () => service.ComputeBatchTotalsPartition(null, (id, result) => true));
        }

        [Test]
        public void ComputeBatchTotalsPartition_NullPredicate_Throws()
        {
            var service = new ShieldBuffService();
            var map = new Dictionary<string, ShieldBuffAbsorptionResult>();

            Assert.Throws<ArgumentNullException>(
                () => service.ComputeBatchTotalsPartition(map, includePredicate: null));
        }

        [Test]
        public void PartitionFrom_WhitespaceActorKey_StillThrowsBeforeFilter()
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
                () => ShieldBuffAbsorptionBatchTotals.PartitionFrom(map, (id, result) => true));
        }

        [Test]
        public void PartitionFrom_NullActorResult_StillThrowsBeforeFilter()
        {
            var map = new Dictionary<string, ShieldBuffAbsorptionResult>
            {
                { "hero_a", null }
            };

            Assert.Throws<ArgumentException>(
                () => ShieldBuffAbsorptionBatchTotals.PartitionFrom(map, (id, result) => true));
        }

        [Test]
        public void ComputeBatchTotalsPartition_EmptyMap_BothBucketsZero()
        {
            var service = new ShieldBuffService();
            var partition = service.ComputeBatchTotalsPartition(
                new Dictionary<string, ShieldBuffAbsorptionResult>(),
                (id, _) => true);

            AssertAllZero(partition.Included);
            AssertAllZero(partition.Excluded);
        }

        [Test]
        public void ComputeBatchTotalsPartition_PredicateAllTrue_AllInIncludedBucket()
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

            var partition = service.ComputeBatchTotalsPartition(perActor, (id, _) => true);
            var whole = service.ComputeBatchTotals(perActor);

            AssertEqualTotals(partition.Included, whole);
            AssertAllZero(partition.Excluded);
        }

        [Test]
        public void ComputeBatchTotalsPartition_PredicateAllFalse_AllInExcludedBucket()
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

            var partition = service.ComputeBatchTotalsPartition(perActor, (id, _) => false);
            var whole = service.ComputeBatchTotals(perActor);

            AssertAllZero(partition.Included);
            AssertEqualTotals(partition.Excluded, whole);
        }

        [Test]
        public void ComputeBatchTotalsPartition_PredicateBySideKeyPrefix_ReportsBothBucketsFromOnePass()
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

            var partition = service.ComputeBatchTotalsPartition(
                perActor,
                (actorId, _) => actorId.StartsWith("ally_", StringComparison.Ordinal));

            Assert.That(partition.Included.ActorCount, Is.EqualTo(2));
            Assert.That(partition.Included.TotalIncomingDamage, Is.EqualTo(4 + 4));
            Assert.That(partition.Included.TotalAbsorbedDamage, Is.EqualTo(4 + 4));
            Assert.That(partition.Included.TotalRemainingDamage, Is.EqualTo(0));
            Assert.That(partition.Included.ActorsWithAbsorption, Is.EqualTo(2));
            Assert.That(partition.Included.TotalConsumedBuffEntries, Is.EqualTo(2));
            Assert.That(partition.Included.TotalExpiredBuffEntries, Is.EqualTo(1));

            Assert.That(partition.Excluded.ActorCount, Is.EqualTo(1));
            Assert.That(partition.Excluded.TotalIncomingDamage, Is.EqualTo(5));
            Assert.That(partition.Excluded.TotalAbsorbedDamage, Is.EqualTo(3));
            Assert.That(partition.Excluded.TotalRemainingDamage, Is.EqualTo(2));
            Assert.That(partition.Excluded.ActorsWithAbsorption, Is.EqualTo(1));
            Assert.That(partition.Excluded.TotalConsumedBuffEntries, Is.EqualTo(1));
            Assert.That(partition.Excluded.TotalExpiredBuffEntries, Is.EqualTo(1));
        }

        [Test]
        public void ComputeBatchTotalsPartition_IncludedPlusExcluded_EqualsUnfilteredBatchTotals()
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
            var partition = service.ComputeBatchTotalsPartition(
                perActor,
                (id, _) => id.StartsWith("ally_", StringComparison.Ordinal));

            Assert.That(partition.Included.ActorCount + partition.Excluded.ActorCount,
                Is.EqualTo(whole.ActorCount));
            Assert.That(
                partition.Included.TotalIncomingDamage + partition.Excluded.TotalIncomingDamage,
                Is.EqualTo(whole.TotalIncomingDamage));
            Assert.That(
                partition.Included.TotalAbsorbedDamage + partition.Excluded.TotalAbsorbedDamage,
                Is.EqualTo(whole.TotalAbsorbedDamage));
            Assert.That(
                partition.Included.TotalRemainingDamage + partition.Excluded.TotalRemainingDamage,
                Is.EqualTo(whole.TotalRemainingDamage));
            Assert.That(
                partition.Included.ActorsWithAbsorption + partition.Excluded.ActorsWithAbsorption,
                Is.EqualTo(whole.ActorsWithAbsorption));
            Assert.That(
                partition.Included.TotalConsumedBuffEntries + partition.Excluded.TotalConsumedBuffEntries,
                Is.EqualTo(whole.TotalConsumedBuffEntries));
            Assert.That(
                partition.Included.TotalExpiredBuffEntries + partition.Excluded.TotalExpiredBuffEntries,
                Is.EqualTo(whole.TotalExpiredBuffEntries));
        }

        [Test]
        public void ComputeBatchTotalsPartition_ParityWithTwoFilteredComputeBatchTotalsCalls()
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

            var allies = service.ComputeBatchTotals(perActor, isAlly);
            var enemies = service.ComputeBatchTotals(perActor, (id, r) => !isAlly(id, r));
            var partition = service.ComputeBatchTotalsPartition(perActor, isAlly);

            AssertEqualTotals(partition.Included, allies);
            AssertEqualTotals(partition.Excluded, enemies);
        }

        [Test]
        public void ComputeBatchTotalsPartition_DoesNotMutateRegistry()
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

            service.ComputeBatchTotalsPartition(perActor, (id, _) => id == "hero_a");

            var trackedAfter = new List<string>(registry.GetTrackedActorIds());
            var ticksAfter = registry.GetOrNull("hero_a").GetRemainingTicks("spellA");
            var magAfter = registry.GetOrNull("hero_a").GetMagnitude("spellA");

            Assert.That(trackedAfter, Is.EquivalentTo(trackedBefore));
            Assert.That(ticksAfter, Is.EqualTo(ticksBefore));
            Assert.That(magAfter, Is.EqualTo(magBefore));
        }

        private static void AssertAllZero(ShieldBuffAbsorptionBatchTotals totals)
        {
            Assert.That(totals.ActorCount, Is.EqualTo(0));
            Assert.That(totals.TotalIncomingDamage, Is.EqualTo(0));
            Assert.That(totals.TotalAbsorbedDamage, Is.EqualTo(0));
            Assert.That(totals.TotalRemainingDamage, Is.EqualTo(0));
            Assert.That(totals.ActorsWithAbsorption, Is.EqualTo(0));
            Assert.That(totals.TotalConsumedBuffEntries, Is.EqualTo(0));
            Assert.That(totals.TotalExpiredBuffEntries, Is.EqualTo(0));
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
