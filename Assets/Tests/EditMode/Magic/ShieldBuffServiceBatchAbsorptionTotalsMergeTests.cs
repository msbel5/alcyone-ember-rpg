using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Magic;
using EmberCrpg.Simulation.Magic;
using NUnit.Framework;

// Design note:
// Pins the deterministic Sprint 5 cross-batch shield-buff totals merge seam:
// ShieldBuffService.MergeBatchTotals (and the underlying ShieldBuffAbsorptionBatchTotals.Merge)
// folds two already-computed batch absorption totals snapshots into a single field-wise
// sum. Foundation-only: argument guards, Empty identity, single-pair sums, parity vs
// computing totals over the union of both source result maps, commutativity, associativity,
// and isolation from registry/buff state. No save/load, application, tick-down, or
// combat-pipeline coverage here.
namespace EmberCrpg.Tests.EditMode.Magic
{
    /// <summary>Verifies deterministic merge over batch shield-buff absorption totals snapshots.</summary>
    public sealed class ShieldBuffServiceBatchAbsorptionTotalsMergeTests
    {
        [Test]
        public void Merge_NullLeft_Throws()
        {
            var right = ShieldBuffAbsorptionBatchTotals.Empty;

            Assert.Throws<ArgumentNullException>(() =>
                ShieldBuffAbsorptionBatchTotals.Merge(null, right));
        }

        [Test]
        public void Merge_NullRight_Throws()
        {
            var left = ShieldBuffAbsorptionBatchTotals.Empty;

            Assert.Throws<ArgumentNullException>(() =>
                ShieldBuffAbsorptionBatchTotals.Merge(left, null));
        }

        [Test]
        public void MergeBatchTotals_NullLeft_Throws()
        {
            var service = new ShieldBuffService();
            var right = ShieldBuffAbsorptionBatchTotals.Empty;

            Assert.Throws<ArgumentNullException>(() => service.MergeBatchTotals(null, right));
        }

        [Test]
        public void MergeBatchTotals_NullRight_Throws()
        {
            var service = new ShieldBuffService();
            var left = ShieldBuffAbsorptionBatchTotals.Empty;

            Assert.Throws<ArgumentNullException>(() => service.MergeBatchTotals(left, null));
        }

        [Test]
        public void Empty_AllCountersZero()
        {
            var empty = ShieldBuffAbsorptionBatchTotals.Empty;

            Assert.That(empty.TotalIncomingDamage, Is.EqualTo(0));
            Assert.That(empty.TotalAbsorbedDamage, Is.EqualTo(0));
            Assert.That(empty.TotalRemainingDamage, Is.EqualTo(0));
            Assert.That(empty.ActorCount, Is.EqualTo(0));
            Assert.That(empty.ActorsWithAbsorption, Is.EqualTo(0));
            Assert.That(empty.TotalConsumedBuffEntries, Is.EqualTo(0));
            Assert.That(empty.TotalExpiredBuffEntries, Is.EqualTo(0));
        }

        [Test]
        public void Empty_MatchesFromOverEmptyMap()
        {
            var fromEmpty = ShieldBuffAbsorptionBatchTotals.From(
                new Dictionary<string, ShieldBuffAbsorptionResult>());

            AssertTotalsEqual(ShieldBuffAbsorptionBatchTotals.Empty, fromEmpty);
        }

        [Test]
        public void Merge_EmptyIsLeftIdentity()
        {
            var right = BuildTotalsFromBatch();

            var merged = ShieldBuffAbsorptionBatchTotals.Merge(
                ShieldBuffAbsorptionBatchTotals.Empty, right);

            AssertTotalsEqual(right, merged);
        }

        [Test]
        public void Merge_EmptyIsRightIdentity()
        {
            var left = BuildTotalsFromBatch();

            var merged = ShieldBuffAbsorptionBatchTotals.Merge(
                left, ShieldBuffAbsorptionBatchTotals.Empty);

            AssertTotalsEqual(left, merged);
        }

        [Test]
        public void Merge_SumsEveryCounterFieldWise()
        {
            var registryA = new ShieldBuffStateRegistry();
            registryA.GetOrCreate("ally_a").SetActiveBuff("spellA", remainingTicks: 3, magnitude: 4);
            registryA.GetOrCreate("ally_b").SetActiveBuff("spellB", remainingTicks: 5, magnitude: 10);

            var registryB = new ShieldBuffStateRegistry();
            registryB.GetOrCreate("enemy_a").SetActiveBuff("spellC", remainingTicks: 2, magnitude: 3);

            var service = new ShieldBuffService();
            var totalsA = service.ComputeBatchTotals(service.AbsorbDamageForActors(
                registryA,
                new Dictionary<string, int> { { "ally_a", 4 }, { "ally_b", 4 } }));
            var totalsB = service.ComputeBatchTotals(service.AbsorbDamageForActors(
                registryB,
                new Dictionary<string, int> { { "enemy_a", 5 } }));

            var merged = service.MergeBatchTotals(totalsA, totalsB);

            Assert.That(merged.TotalIncomingDamage,
                Is.EqualTo(totalsA.TotalIncomingDamage + totalsB.TotalIncomingDamage));
            Assert.That(merged.TotalAbsorbedDamage,
                Is.EqualTo(totalsA.TotalAbsorbedDamage + totalsB.TotalAbsorbedDamage));
            Assert.That(merged.TotalRemainingDamage,
                Is.EqualTo(totalsA.TotalRemainingDamage + totalsB.TotalRemainingDamage));
            Assert.That(merged.ActorCount,
                Is.EqualTo(totalsA.ActorCount + totalsB.ActorCount));
            Assert.That(merged.ActorsWithAbsorption,
                Is.EqualTo(totalsA.ActorsWithAbsorption + totalsB.ActorsWithAbsorption));
            Assert.That(merged.TotalConsumedBuffEntries,
                Is.EqualTo(totalsA.TotalConsumedBuffEntries + totalsB.TotalConsumedBuffEntries));
            Assert.That(merged.TotalExpiredBuffEntries,
                Is.EqualTo(totalsA.TotalExpiredBuffEntries + totalsB.TotalExpiredBuffEntries));
        }

        [Test]
        public void Merge_MatchesFromOverUnionOfBothBatches()
        {
            var registryA = new ShieldBuffStateRegistry();
            registryA.GetOrCreate("ally_a").SetActiveBuff("spellA", remainingTicks: 3, magnitude: 4);
            registryA.GetOrCreate("ally_b").SetActiveBuff("spellB", remainingTicks: 5, magnitude: 10);

            var registryB = new ShieldBuffStateRegistry();
            registryB.GetOrCreate("enemy_a").SetActiveBuff("spellC", remainingTicks: 2, magnitude: 3);

            var service = new ShieldBuffService();
            var batchA = service.AbsorbDamageForActors(
                registryA,
                new Dictionary<string, int> { { "ally_a", 4 }, { "ally_b", 4 } });
            var batchB = service.AbsorbDamageForActors(
                registryB,
                new Dictionary<string, int> { { "enemy_a", 5 } });

            var union = new Dictionary<string, ShieldBuffAbsorptionResult>(StringComparer.Ordinal);
            foreach (var pair in batchA) union[pair.Key] = pair.Value;
            foreach (var pair in batchB) union[pair.Key] = pair.Value;

            var totalsA = service.ComputeBatchTotals(batchA);
            var totalsB = service.ComputeBatchTotals(batchB);
            var merged = ShieldBuffAbsorptionBatchTotals.Merge(totalsA, totalsB);

            var unionTotals = service.ComputeBatchTotals(union);

            AssertTotalsEqual(unionTotals, merged);
        }

        [Test]
        public void Merge_IsCommutative()
        {
            var registryA = new ShieldBuffStateRegistry();
            registryA.GetOrCreate("hero_a").SetActiveBuff("spellA", remainingTicks: 3, magnitude: 4);

            var registryB = new ShieldBuffStateRegistry();
            registryB.GetOrCreate("hero_b").SetActiveBuff("spellB", remainingTicks: 1, magnitude: 7);

            var service = new ShieldBuffService();
            var totalsA = service.ComputeBatchTotals(service.AbsorbDamageForActors(
                registryA, new Dictionary<string, int> { { "hero_a", 4 } }));
            var totalsB = service.ComputeBatchTotals(service.AbsorbDamageForActors(
                registryB, new Dictionary<string, int> { { "hero_b", 9 } }));

            var leftThenRight = ShieldBuffAbsorptionBatchTotals.Merge(totalsA, totalsB);
            var rightThenLeft = ShieldBuffAbsorptionBatchTotals.Merge(totalsB, totalsA);

            AssertTotalsEqual(leftThenRight, rightThenLeft);
        }

        [Test]
        public void Merge_IsAssociative()
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

            var leftFold = ShieldBuffAbsorptionBatchTotals.Merge(
                ShieldBuffAbsorptionBatchTotals.Merge(a, b), c);
            var rightFold = ShieldBuffAbsorptionBatchTotals.Merge(
                a, ShieldBuffAbsorptionBatchTotals.Merge(b, c));

            AssertTotalsEqual(leftFold, rightFold);
        }

        [Test]
        public void MergeBatchTotals_DoesNotMutateRegistry()
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

            service.MergeBatchTotals(totals, ShieldBuffAbsorptionBatchTotals.Empty);
            service.MergeBatchTotals(ShieldBuffAbsorptionBatchTotals.Empty, totals);

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
