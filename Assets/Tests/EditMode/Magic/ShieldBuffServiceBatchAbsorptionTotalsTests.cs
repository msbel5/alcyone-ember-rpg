using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Magic;
using EmberCrpg.Simulation.Magic;
using NUnit.Framework;

// Design note:
// Pins the deterministic Sprint 5 batch shield-buff absorption totals seam:
// ShieldBuffService.ComputeBatchTotals (and the underlying ShieldBuffAbsorptionBatchTotals.From)
// aggregates the per-actor result map returned by AbsorbDamageForActors into a single
// snapshot. Foundation-only: argument guards, empty-input zeros, single-actor passthrough,
// multi-actor sums, mixed absorbed/non-absorbed counts, and parity vs walking the map
// manually. No save/load, application, tick-down, or combat-pipeline coverage here.
namespace EmberCrpg.Tests.EditMode.Magic
{
    /// <summary>Verifies deterministic totals over batch shield-buff absorption results.</summary>
    public sealed class ShieldBuffServiceBatchAbsorptionTotalsTests
    {
        [Test]
        public void ComputeBatchTotals_NullResultMap_Throws()
        {
            var service = new ShieldBuffService();

            Assert.Throws<ArgumentNullException>(() => service.ComputeBatchTotals(null));
        }

        [Test]
        public void From_NullResultMap_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => ShieldBuffAbsorptionBatchTotals.From(null));
        }

        [Test]
        public void From_WhitespaceActorKey_Throws()
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

            Assert.Throws<ArgumentException>(() => ShieldBuffAbsorptionBatchTotals.From(map));
        }

        [Test]
        public void From_NullActorResult_Throws()
        {
            var map = new Dictionary<string, ShieldBuffAbsorptionResult>
            {
                { "hero_a", null }
            };

            Assert.Throws<ArgumentException>(() => ShieldBuffAbsorptionBatchTotals.From(map));
        }

        [Test]
        public void ComputeBatchTotals_EmptyMap_AllZero()
        {
            var service = new ShieldBuffService();
            var totals = service.ComputeBatchTotals(
                new Dictionary<string, ShieldBuffAbsorptionResult>());

            Assert.That(totals.TotalIncomingDamage, Is.EqualTo(0));
            Assert.That(totals.TotalAbsorbedDamage, Is.EqualTo(0));
            Assert.That(totals.TotalRemainingDamage, Is.EqualTo(0));
            Assert.That(totals.ActorCount, Is.EqualTo(0));
            Assert.That(totals.ActorsWithAbsorption, Is.EqualTo(0));
            Assert.That(totals.TotalConsumedBuffEntries, Is.EqualTo(0));
            Assert.That(totals.TotalExpiredBuffEntries, Is.EqualTo(0));
        }

        [Test]
        public void ComputeBatchTotals_SingleActorFullAbsorption_ReportsSingleActorTotals()
        {
            var registry = new ShieldBuffStateRegistry();
            registry.GetOrCreate("hero_a").SetActiveBuff("spellA", remainingTicks: 4, magnitude: 7);

            var service = new ShieldBuffService();
            var perActor = service.AbsorbDamageForActors(
                registry,
                new Dictionary<string, int> { { "hero_a", 5 } });

            var totals = service.ComputeBatchTotals(perActor);

            Assert.That(totals.ActorCount, Is.EqualTo(1));
            Assert.That(totals.TotalIncomingDamage, Is.EqualTo(5));
            Assert.That(totals.TotalAbsorbedDamage, Is.EqualTo(5));
            Assert.That(totals.TotalRemainingDamage, Is.EqualTo(0));
            Assert.That(totals.ActorsWithAbsorption, Is.EqualTo(1));
            Assert.That(totals.TotalConsumedBuffEntries, Is.EqualTo(1));
            Assert.That(totals.TotalExpiredBuffEntries, Is.EqualTo(0));
        }

        [Test]
        public void ComputeBatchTotals_UntrackedActor_TreatedAsRemainingOnly()
        {
            var registry = new ShieldBuffStateRegistry();

            var service = new ShieldBuffService();
            var perActor = service.AbsorbDamageForActors(
                registry,
                new Dictionary<string, int> { { "hero_b", 9 } });

            var totals = service.ComputeBatchTotals(perActor);

            Assert.That(totals.ActorCount, Is.EqualTo(1));
            Assert.That(totals.TotalIncomingDamage, Is.EqualTo(9));
            Assert.That(totals.TotalAbsorbedDamage, Is.EqualTo(0));
            Assert.That(totals.TotalRemainingDamage, Is.EqualTo(9));
            Assert.That(totals.ActorsWithAbsorption, Is.EqualTo(0));
            Assert.That(totals.TotalConsumedBuffEntries, Is.EqualTo(0));
            Assert.That(totals.TotalExpiredBuffEntries, Is.EqualTo(0));
        }

        [Test]
        public void ComputeBatchTotals_MultiActorMixedAbsorption_AggregatesAcrossActors()
        {
            var registry = new ShieldBuffStateRegistry();
            // hero_a: shield expires this absorption (magnitude 4, damage 4) -> 1 consumed, 1 expired
            registry.GetOrCreate("hero_a").SetActiveBuff("spellA", remainingTicks: 3, magnitude: 4);
            // hero_b: shield consumed but not expired (magnitude 10, damage 4) -> 1 consumed
            registry.GetOrCreate("hero_b").SetActiveBuff("spellB", remainingTicks: 5, magnitude: 10);
            // hero_c: untracked actor -> 0 absorbed, 7 remaining
            // hero_d: tracked but no incoming damage in input map -> not in result

            var service = new ShieldBuffService();
            var input = new Dictionary<string, int>
            {
                { "hero_a", 4 },
                { "hero_b", 4 },
                { "hero_c", 7 }
            };
            var perActor = service.AbsorbDamageForActors(registry, input);

            var totals = service.ComputeBatchTotals(perActor);

            Assert.That(totals.ActorCount, Is.EqualTo(3));
            Assert.That(totals.TotalIncomingDamage, Is.EqualTo(4 + 4 + 7));
            Assert.That(totals.TotalAbsorbedDamage, Is.EqualTo(4 + 4 + 0));
            Assert.That(totals.TotalRemainingDamage, Is.EqualTo(0 + 0 + 7));
            Assert.That(totals.ActorsWithAbsorption, Is.EqualTo(2));
            Assert.That(totals.TotalConsumedBuffEntries, Is.EqualTo(2));
            Assert.That(totals.TotalExpiredBuffEntries, Is.EqualTo(1));
        }

        [Test]
        public void ComputeBatchTotals_ParityWithManualWalk()
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

            var manualIncoming = 0;
            var manualAbsorbed = 0;
            var manualRemaining = 0;
            var manualWithAbsorption = 0;
            var manualConsumed = 0;
            var manualExpired = 0;
            foreach (var pair in perActor)
            {
                manualIncoming += pair.Value.IncomingDamage;
                manualAbsorbed += pair.Value.AbsorbedDamage;
                manualRemaining += pair.Value.RemainingDamage;
                if (pair.Value.AbsorbedDamage > 0)
                    manualWithAbsorption++;
                manualConsumed += pair.Value.ConsumedSpellTemplateIds.Count;
                manualExpired += pair.Value.ExpiredSpellTemplateIds.Count;
            }

            var totals = service.ComputeBatchTotals(perActor);

            Assert.That(totals.TotalIncomingDamage, Is.EqualTo(manualIncoming));
            Assert.That(totals.TotalAbsorbedDamage, Is.EqualTo(manualAbsorbed));
            Assert.That(totals.TotalRemainingDamage, Is.EqualTo(manualRemaining));
            Assert.That(totals.ActorsWithAbsorption, Is.EqualTo(manualWithAbsorption));
            Assert.That(totals.TotalConsumedBuffEntries, Is.EqualTo(manualConsumed));
            Assert.That(totals.TotalExpiredBuffEntries, Is.EqualTo(manualExpired));
            Assert.That(totals.ActorCount, Is.EqualTo(perActor.Count));
        }

        [Test]
        public void From_PredicateNullMap_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => ShieldBuffAbsorptionBatchTotals.From(null, (id, result) => true));
        }

        [Test]
        public void From_PredicateOverloadWithNullPredicate_AggregatesAllEntries()
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

            var unfiltered = ShieldBuffAbsorptionBatchTotals.From(perActor);
            var nullPredicate = ShieldBuffAbsorptionBatchTotals.From(perActor, includePredicate: null);

            Assert.That(nullPredicate.ActorCount, Is.EqualTo(unfiltered.ActorCount));
            Assert.That(nullPredicate.TotalIncomingDamage, Is.EqualTo(unfiltered.TotalIncomingDamage));
            Assert.That(nullPredicate.TotalAbsorbedDamage, Is.EqualTo(unfiltered.TotalAbsorbedDamage));
            Assert.That(nullPredicate.TotalRemainingDamage, Is.EqualTo(unfiltered.TotalRemainingDamage));
            Assert.That(nullPredicate.ActorsWithAbsorption, Is.EqualTo(unfiltered.ActorsWithAbsorption));
            Assert.That(nullPredicate.TotalConsumedBuffEntries, Is.EqualTo(unfiltered.TotalConsumedBuffEntries));
            Assert.That(nullPredicate.TotalExpiredBuffEntries, Is.EqualTo(unfiltered.TotalExpiredBuffEntries));
        }

        [Test]
        public void From_PredicateOverloadWhitespaceActorKey_StillThrowsBeforeFilter()
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
                () => ShieldBuffAbsorptionBatchTotals.From(map, (id, result) => false));
        }

        [Test]
        public void From_PredicateOverloadNullActorResult_StillThrowsBeforeFilter()
        {
            var map = new Dictionary<string, ShieldBuffAbsorptionResult>
            {
                { "hero_a", null }
            };

            Assert.Throws<ArgumentException>(
                () => ShieldBuffAbsorptionBatchTotals.From(map, (id, result) => false));
        }

        [Test]
        public void ComputeBatchTotals_PredicateNullMap_Throws()
        {
            var service = new ShieldBuffService();

            Assert.Throws<ArgumentNullException>(
                () => service.ComputeBatchTotals(null, (id, result) => true));
        }

        [Test]
        public void ComputeBatchTotals_PredicateExcludesAllActors_AllZero()
        {
            var registry = new ShieldBuffStateRegistry();
            registry.GetOrCreate("hero_a").SetActiveBuff("spellA", remainingTicks: 3, magnitude: 4);
            registry.GetOrCreate("hero_b").SetActiveBuff("spellB", remainingTicks: 5, magnitude: 10);

            var service = new ShieldBuffService();
            var perActor = service.AbsorbDamageForActors(
                registry,
                new Dictionary<string, int>
                {
                    { "hero_a", 4 },
                    { "hero_b", 4 }
                });

            var totals = service.ComputeBatchTotals(perActor, (id, result) => false);

            Assert.That(totals.ActorCount, Is.EqualTo(0));
            Assert.That(totals.TotalIncomingDamage, Is.EqualTo(0));
            Assert.That(totals.TotalAbsorbedDamage, Is.EqualTo(0));
            Assert.That(totals.TotalRemainingDamage, Is.EqualTo(0));
            Assert.That(totals.ActorsWithAbsorption, Is.EqualTo(0));
            Assert.That(totals.TotalConsumedBuffEntries, Is.EqualTo(0));
            Assert.That(totals.TotalExpiredBuffEntries, Is.EqualTo(0));
        }

        [Test]
        public void ComputeBatchTotals_PredicateBySideKeyPrefix_ReportsOnlyMatchingSubset()
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

            var alliesOnly = service.ComputeBatchTotals(
                perActor,
                (actorId, _) => actorId.StartsWith("ally_", StringComparison.Ordinal));

            Assert.That(alliesOnly.ActorCount, Is.EqualTo(2));
            Assert.That(alliesOnly.TotalIncomingDamage, Is.EqualTo(4 + 4));
            Assert.That(alliesOnly.TotalAbsorbedDamage, Is.EqualTo(4 + 4));
            Assert.That(alliesOnly.TotalRemainingDamage, Is.EqualTo(0));
            Assert.That(alliesOnly.ActorsWithAbsorption, Is.EqualTo(2));
            Assert.That(alliesOnly.TotalConsumedBuffEntries, Is.EqualTo(2));
            Assert.That(alliesOnly.TotalExpiredBuffEntries, Is.EqualTo(1));

            var enemiesOnly = service.ComputeBatchTotals(
                perActor,
                (actorId, _) => actorId.StartsWith("enemy_", StringComparison.Ordinal));

            Assert.That(enemiesOnly.ActorCount, Is.EqualTo(1));
            Assert.That(enemiesOnly.TotalIncomingDamage, Is.EqualTo(5));
            Assert.That(enemiesOnly.TotalAbsorbedDamage, Is.EqualTo(3));
            Assert.That(enemiesOnly.TotalRemainingDamage, Is.EqualTo(2));
            Assert.That(enemiesOnly.ActorsWithAbsorption, Is.EqualTo(1));
            Assert.That(enemiesOnly.TotalConsumedBuffEntries, Is.EqualTo(1));
            Assert.That(enemiesOnly.TotalExpiredBuffEntries, Is.EqualTo(1));
        }

        [Test]
        public void ComputeBatchTotals_PredicateByAbsorptionFlag_ExcludesNonAbsorbers()
        {
            var registry = new ShieldBuffStateRegistry();
            registry.GetOrCreate("hero_a").SetActiveBuff("spellA", remainingTicks: 4, magnitude: 7);
            // hero_b is untracked -> 0 absorbed, full remaining
            // hero_c is untracked too -> 0 absorbed, full remaining

            var service = new ShieldBuffService();
            var perActor = service.AbsorbDamageForActors(
                registry,
                new Dictionary<string, int>
                {
                    { "hero_a", 5 },
                    { "hero_b", 9 },
                    { "hero_c", 6 }
                });

            var absorbersOnly = service.ComputeBatchTotals(
                perActor,
                (_, result) => result.AbsorbedDamage > 0);

            Assert.That(absorbersOnly.ActorCount, Is.EqualTo(1));
            Assert.That(absorbersOnly.TotalIncomingDamage, Is.EqualTo(5));
            Assert.That(absorbersOnly.TotalAbsorbedDamage, Is.EqualTo(5));
            Assert.That(absorbersOnly.TotalRemainingDamage, Is.EqualTo(0));
            Assert.That(absorbersOnly.ActorsWithAbsorption, Is.EqualTo(1));
            Assert.That(absorbersOnly.TotalConsumedBuffEntries, Is.EqualTo(1));
            Assert.That(absorbersOnly.TotalExpiredBuffEntries, Is.EqualTo(0));
        }

        [Test]
        public void ComputeBatchTotals_PredicateSubsetSumsAddBackToWholeBatch()
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

            var whole = service.ComputeBatchTotals(perActor);
            var allies = service.ComputeBatchTotals(
                perActor,
                (id, _) => id.StartsWith("ally_", StringComparison.Ordinal));
            var enemies = service.ComputeBatchTotals(
                perActor,
                (id, _) => id.StartsWith("enemy_", StringComparison.Ordinal));

            Assert.That(allies.ActorCount + enemies.ActorCount, Is.EqualTo(whole.ActorCount));
            Assert.That(allies.TotalIncomingDamage + enemies.TotalIncomingDamage,
                Is.EqualTo(whole.TotalIncomingDamage));
            Assert.That(allies.TotalAbsorbedDamage + enemies.TotalAbsorbedDamage,
                Is.EqualTo(whole.TotalAbsorbedDamage));
            Assert.That(allies.TotalRemainingDamage + enemies.TotalRemainingDamage,
                Is.EqualTo(whole.TotalRemainingDamage));
            Assert.That(allies.ActorsWithAbsorption + enemies.ActorsWithAbsorption,
                Is.EqualTo(whole.ActorsWithAbsorption));
            Assert.That(allies.TotalConsumedBuffEntries + enemies.TotalConsumedBuffEntries,
                Is.EqualTo(whole.TotalConsumedBuffEntries));
            Assert.That(allies.TotalExpiredBuffEntries + enemies.TotalExpiredBuffEntries,
                Is.EqualTo(whole.TotalExpiredBuffEntries));
        }

        [Test]
        public void ComputeBatchTotals_PredicateDoesNotMutateRegistry()
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

            service.ComputeBatchTotals(perActor, (id, _) => id == "hero_a");

            var trackedAfter = new List<string>(registry.GetTrackedActorIds());
            var ticksAfter = registry.GetOrNull("hero_a").GetRemainingTicks("spellA");
            var magAfter = registry.GetOrNull("hero_a").GetMagnitude("spellA");

            Assert.That(trackedAfter, Is.EquivalentTo(trackedBefore));
            Assert.That(ticksAfter, Is.EqualTo(ticksBefore));
            Assert.That(magAfter, Is.EqualTo(magBefore));
        }

        [Test]
        public void ComputeBatchTotals_DoesNotMutateRegistry()
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

            service.ComputeBatchTotals(perActor);

            var trackedAfter = new List<string>(registry.GetTrackedActorIds());
            var ticksAfter = registry.GetOrNull("hero_a").GetRemainingTicks("spellA");
            var magAfter = registry.GetOrNull("hero_a").GetMagnitude("spellA");

            Assert.That(trackedAfter, Is.EquivalentTo(trackedBefore));
            Assert.That(ticksAfter, Is.EqualTo(ticksBefore));
            Assert.That(magAfter, Is.EqualTo(magBefore));
        }
    }
}
