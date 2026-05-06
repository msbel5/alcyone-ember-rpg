using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Magic;
using EmberCrpg.Simulation.Magic;
using NUnit.Framework;

// Design note:
// Pins the deterministic Sprint 5 stacked-filter map-level fold seam:
// ShieldBuffService.ComputeBatchTotals(map, includePredicate, filterPredicate)
// (and the underlying ShieldBuffAbsorptionBatchTotals.From 3-arg factory) folds each
// per-actor absorption result into one ShieldBuffAbsorptionBatchTotals snapshot, but only
// after the entry first survives a per-actor filterPredicate (a pre-filter that drops
// entries outright) and then includePredicate (the existing semantic gate). Foundation
// only: argument guards (null map even with predicates non-null, whitespace key and null
// per-actor value still throw before either predicate is consulted), null-filter
// passthrough, both-null parity with the unfiltered factory, all-filter-rejected zeros,
// all-filter-accepted parity with the predicate-only From overload, parity vs
// From(filteredMap, includePredicate), filter-before-include guard order, and
// registry-non-mutation. No save/load, application, tick-down, or combat-pipeline coverage
// here.
namespace EmberCrpg.Tests.EditMode.Magic
{
    /// <summary>Verifies deterministic stacked-filter fold over batch shield-buff absorption results.</summary>
    public sealed class ShieldBuffServiceBatchAbsorptionTotalsFromStackedFilterTests
    {
        [Test]
        public void From_StackedFilter_NullResultMap_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => ShieldBuffAbsorptionBatchTotals.From(
                    null,
                    (id, r) => true,
                    (id, r) => true));
        }

        [Test]
        public void ComputeBatchTotals_StackedFilter_NullResultMap_Throws()
        {
            var service = new ShieldBuffService();

            Assert.Throws<ArgumentNullException>(
                () => service.ComputeBatchTotals(
                    null,
                    (id, _) => true,
                    (id, _) => true));
        }

        [Test]
        public void From_StackedFilter_WhitespaceActorKey_StillThrowsBeforeFilters()
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
                () => ShieldBuffAbsorptionBatchTotals.From(
                    map,
                    (id, r) => true,
                    (id, r) => false));
        }

        [Test]
        public void From_StackedFilter_NullActorResult_StillThrowsBeforeFilters()
        {
            var map = new Dictionary<string, ShieldBuffAbsorptionResult>
            {
                { "hero_a", null }
            };

            Assert.Throws<ArgumentException>(
                () => ShieldBuffAbsorptionBatchTotals.From(
                    map,
                    (id, r) => true,
                    (id, r) => false));
        }

        [Test]
        public void ComputeBatchTotals_StackedFilter_NullFilter_BehavesLikePredicateOnlyOverload()
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

            var predicateOnly = service.ComputeBatchTotals(perActor, isAlly);
            var stackedNullFilter = service.ComputeBatchTotals(perActor, isAlly, filterPredicate: null);

            AssertEqualTotals(stackedNullFilter, predicateOnly);
        }

        [Test]
        public void From_StackedFilter_BothPredicatesNull_MatchesUnfilteredFrom()
        {
            var registry = new ShieldBuffStateRegistry();
            registry.GetOrCreate("ally_a").SetActiveBuff("spellA", remainingTicks: 3, magnitude: 4);
            registry.GetOrCreate("enemy_a").SetActiveBuff("spellC", remainingTicks: 2, magnitude: 3);

            var service = new ShieldBuffService();
            var perActor = service.AbsorbDamageForActors(
                registry,
                new Dictionary<string, int>
                {
                    { "ally_a", 4 },
                    { "enemy_a", 5 }
                });

            var unfiltered = ShieldBuffAbsorptionBatchTotals.From(perActor);
            var stackedBothNull = ShieldBuffAbsorptionBatchTotals.From(
                perActor,
                includePredicate: null,
                filterPredicate: null);

            AssertEqualTotals(stackedBothNull, unfiltered);
        }

        [Test]
        public void ComputeBatchTotals_StackedFilter_FilterRejectsAll_TotalsAllZero()
        {
            var registry = new ShieldBuffStateRegistry();
            registry.GetOrCreate("ally_a").SetActiveBuff("spellA", remainingTicks: 3, magnitude: 4);
            registry.GetOrCreate("enemy_a").SetActiveBuff("spellC", remainingTicks: 2, magnitude: 3);

            var service = new ShieldBuffService();
            var perActor = service.AbsorbDamageForActors(
                registry,
                new Dictionary<string, int>
                {
                    { "ally_a", 4 },
                    { "enemy_a", 5 }
                });

            var totals = service.ComputeBatchTotals(
                perActor,
                (id, _) => id.StartsWith("ally_", StringComparison.Ordinal),
                (id, _) => false);

            AssertAllZero(totals);
        }

        [Test]
        public void ComputeBatchTotals_StackedFilter_FilterAcceptsAll_ParityWithPredicateOnlyOverload()
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

            var stackedAllTrue = service.ComputeBatchTotals(perActor, isAlly, (id, _) => true);
            var predicateOnly = service.ComputeBatchTotals(perActor, isAlly);

            AssertEqualTotals(stackedAllTrue, predicateOnly);
        }

        [Test]
        public void ComputeBatchTotals_StackedFilter_OnlyAbsorbingAllies_ParityWithFilteredThenIncluded()
        {
            var registry = new ShieldBuffStateRegistry();
            // ally_a: shield expires this absorption (mag 4, dmg 4) -> 1 consumed, 1 expired
            registry.GetOrCreate("ally_a").SetActiveBuff("spellA", remainingTicks: 3, magnitude: 4);
            // ally_b: shield consumed but not expired (mag 10, dmg 4) -> 1 consumed
            registry.GetOrCreate("ally_b").SetActiveBuff("spellB", remainingTicks: 5, magnitude: 10);
            // enemy_a: tracked, partial absorb (mag 3, dmg 5) -> 1 consumed, 1 expired
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

            Func<string, ShieldBuffAbsorptionResult, bool> isAlly =
                (id, _) => id.StartsWith("ally_", StringComparison.Ordinal);
            Func<string, ShieldBuffAbsorptionResult, bool> absorbedAny =
                (_, r) => r.AbsorbedDamage > 0;

            var stacked = service.ComputeBatchTotals(perActor, isAlly, absorbedAny);

            // Manual reference: filter to only absorbed entries first, then include allies.
            var filtered = new Dictionary<string, ShieldBuffAbsorptionResult>();
            foreach (var pair in perActor)
            {
                if (pair.Value.AbsorbedDamage > 0)
                    filtered[pair.Key] = pair.Value;
            }
            var reference = service.ComputeBatchTotals(filtered, isAlly);

            AssertEqualTotals(stacked, reference);
        }

        [Test]
        public void ComputeBatchTotals_StackedFilter_FilterConsultedBeforeIncludePredicate()
        {
            var registry = new ShieldBuffStateRegistry();
            registry.GetOrCreate("ally_a").SetActiveBuff("spellA", remainingTicks: 3, magnitude: 4);

            var service = new ShieldBuffService();
            var perActor = service.AbsorbDamageForActors(
                registry,
                new Dictionary<string, int> { { "ally_a", 4 } });

            var includeCalls = 0;
            var filterCalls = 0;
            Func<string, ShieldBuffAbsorptionResult, bool> include = (id, r) =>
            {
                includeCalls++;
                return true;
            };
            Func<string, ShieldBuffAbsorptionResult, bool> filter = (id, r) =>
            {
                filterCalls++;
                return false;
            };

            var totals = service.ComputeBatchTotals(perActor, include, filter);

            Assert.That(filterCalls, Is.EqualTo(1), "filter must be consulted exactly once per kept entry");
            Assert.That(includeCalls, Is.EqualTo(0), "include must not be consulted when filter rejects the entry");
            AssertAllZero(totals);
        }

        [Test]
        public void ComputeBatchTotals_StackedFilter_OrderIndependent()
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

            // Build a reordered map (insertion order should not matter for output totals).
            var reordered = new Dictionary<string, ShieldBuffAbsorptionResult>
            {
                { "enemy_a", perActor["enemy_a"] },
                { "ally_b", perActor["ally_b"] },
                { "ally_a", perActor["ally_a"] }
            };

            Func<string, ShieldBuffAbsorptionResult, bool> isAlly =
                (id, _) => id.StartsWith("ally_", StringComparison.Ordinal);
            Func<string, ShieldBuffAbsorptionResult, bool> absorbedAny =
                (_, r) => r.AbsorbedDamage > 0;

            var first = service.ComputeBatchTotals(perActor, isAlly, absorbedAny);
            var second = service.ComputeBatchTotals(reordered, isAlly, absorbedAny);

            AssertEqualTotals(second, first);
        }

        [Test]
        public void ComputeBatchTotals_StackedFilter_DoesNotMutateRegistry()
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

            service.ComputeBatchTotals(
                perActor,
                (id, _) => id == "hero_a",
                (id, _) => true);

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
