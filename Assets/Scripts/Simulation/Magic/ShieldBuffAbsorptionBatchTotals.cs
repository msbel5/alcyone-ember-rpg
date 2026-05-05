using System;
using System.Collections.Generic;

// Design note:
// ShieldBuffAbsorptionBatchTotals is the deterministic aggregate response object for one
// ShieldBuffService.AbsorbDamageForActors batch result. Inputs: the per-actor result map
// returned by the batch absorption dispatcher. Outputs: total incoming/absorbed/remaining
// damage across all actors in the batch, the actor count, the count of actors whose shields
// absorbed any damage this call, and the total number of consumed and expired buff entries
// summed across all actors. Pure Simulation object: no Unity dependency, no presentation
// coupling, no tick mutation, no save coupling, no registry mutation. Bible reference:
// EMBER_VISION_BIBLE.md §3 Layer 3, MASTER_MECHANICS_BIBLE.md §15 Magic effects.
namespace EmberCrpg.Simulation.Magic
{
    /// <summary>Deterministic totals over one batch shield-buff damage-absorption result map.</summary>
    public sealed class ShieldBuffAbsorptionBatchTotals
    {
        private ShieldBuffAbsorptionBatchTotals(
            int totalIncomingDamage,
            int totalAbsorbedDamage,
            int totalRemainingDamage,
            int actorCount,
            int actorsWithAbsorption,
            int totalConsumedBuffEntries,
            int totalExpiredBuffEntries)
        {
            TotalIncomingDamage = totalIncomingDamage;
            TotalAbsorbedDamage = totalAbsorbedDamage;
            TotalRemainingDamage = totalRemainingDamage;
            ActorCount = actorCount;
            ActorsWithAbsorption = actorsWithAbsorption;
            TotalConsumedBuffEntries = totalConsumedBuffEntries;
            TotalExpiredBuffEntries = totalExpiredBuffEntries;
        }

        public int TotalIncomingDamage { get; }
        public int TotalAbsorbedDamage { get; }
        public int TotalRemainingDamage { get; }
        public int ActorCount { get; }
        public int ActorsWithAbsorption { get; }
        public int TotalConsumedBuffEntries { get; }
        public int TotalExpiredBuffEntries { get; }

        public static ShieldBuffAbsorptionBatchTotals From(
            IReadOnlyDictionary<string, ShieldBuffAbsorptionResult> resultsByActorId)
        {
            return From(resultsByActorId, includePredicate: null);
        }

        // Subset-aggregating overload: aggregates only the entries of the per-actor result map
        // for which includePredicate returns true. The predicate is evaluated on every entry
        // after the same actor-key and per-actor-result invariants From(map) enforces, so the
        // strict input contract is preserved regardless of which subset the caller wants. The
        // returned ActorCount is the count of included entries, not the size of the input map,
        // because the totals describe the included subset. Order independence still holds
        // because the predicate is a pure per-entry filter and the totals remain commutative
        // sums and counts.
        public static ShieldBuffAbsorptionBatchTotals From(
            IReadOnlyDictionary<string, ShieldBuffAbsorptionResult> resultsByActorId,
            Func<string, ShieldBuffAbsorptionResult, bool> includePredicate)
        {
            if (resultsByActorId == null)
                throw new ArgumentNullException(nameof(resultsByActorId));

            var totalIncomingDamage = 0;
            var totalAbsorbedDamage = 0;
            var totalRemainingDamage = 0;
            var includedActorCount = 0;
            var actorsWithAbsorption = 0;
            var totalConsumedBuffEntries = 0;
            var totalExpiredBuffEntries = 0;

            foreach (var pair in resultsByActorId)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                    throw new ArgumentException("Actor id keys must be non-empty stable ids.", nameof(resultsByActorId));

                var actorResult = pair.Value;
                if (actorResult == null)
                    throw new ArgumentException("Per-actor absorption result must not be null.", nameof(resultsByActorId));

                if (includePredicate != null && !includePredicate(pair.Key, actorResult))
                    continue;

                includedActorCount++;
                totalIncomingDamage += actorResult.IncomingDamage;
                totalAbsorbedDamage += actorResult.AbsorbedDamage;
                totalRemainingDamage += actorResult.RemainingDamage;
                if (actorResult.AbsorbedDamage > 0)
                    actorsWithAbsorption++;
                totalConsumedBuffEntries += actorResult.ConsumedSpellTemplateIds.Count;
                totalExpiredBuffEntries += actorResult.ExpiredSpellTemplateIds.Count;
            }

            return new ShieldBuffAbsorptionBatchTotals(
                totalIncomingDamage,
                totalAbsorbedDamage,
                totalRemainingDamage,
                includedActorCount,
                actorsWithAbsorption,
                totalConsumedBuffEntries,
                totalExpiredBuffEntries);
        }

        // Partition overload: walks the per-actor result map exactly once and routes each
        // entry to either the Included totals (predicate returns true) or the Excluded
        // totals (predicate returns false), so a future combat damage-resolution pass or
        // telemetry/UI surface can summarize a side-versus-side split (e.g. allies vs
        // enemies) of one batch absorption call without scanning the result map twice.
        // Strict input contract is preserved: every entry is validated for non-empty
        // actor key and non-null per-actor result before the predicate is consulted, so
        // the partition guards match the unfiltered From(map) and the filtered
        // From(map, predicate) overloads exactly. The included-plus-excluded sums are
        // identical to From(map) over the unfiltered total because each entry contributes
        // to exactly one bucket.
        public static ShieldBuffAbsorptionBatchTotalsPartition PartitionFrom(
            IReadOnlyDictionary<string, ShieldBuffAbsorptionResult> resultsByActorId,
            Func<string, ShieldBuffAbsorptionResult, bool> includePredicate)
        {
            if (resultsByActorId == null)
                throw new ArgumentNullException(nameof(resultsByActorId));
            if (includePredicate == null)
                throw new ArgumentNullException(nameof(includePredicate));

            var includedIncoming = 0;
            var includedAbsorbed = 0;
            var includedRemaining = 0;
            var includedActorCount = 0;
            var includedWithAbsorption = 0;
            var includedConsumed = 0;
            var includedExpired = 0;

            var excludedIncoming = 0;
            var excludedAbsorbed = 0;
            var excludedRemaining = 0;
            var excludedActorCount = 0;
            var excludedWithAbsorption = 0;
            var excludedConsumed = 0;
            var excludedExpired = 0;

            foreach (var pair in resultsByActorId)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                    throw new ArgumentException("Actor id keys must be non-empty stable ids.", nameof(resultsByActorId));

                var actorResult = pair.Value;
                if (actorResult == null)
                    throw new ArgumentException("Per-actor absorption result must not be null.", nameof(resultsByActorId));

                if (includePredicate(pair.Key, actorResult))
                {
                    includedActorCount++;
                    includedIncoming += actorResult.IncomingDamage;
                    includedAbsorbed += actorResult.AbsorbedDamage;
                    includedRemaining += actorResult.RemainingDamage;
                    if (actorResult.AbsorbedDamage > 0)
                        includedWithAbsorption++;
                    includedConsumed += actorResult.ConsumedSpellTemplateIds.Count;
                    includedExpired += actorResult.ExpiredSpellTemplateIds.Count;
                }
                else
                {
                    excludedActorCount++;
                    excludedIncoming += actorResult.IncomingDamage;
                    excludedAbsorbed += actorResult.AbsorbedDamage;
                    excludedRemaining += actorResult.RemainingDamage;
                    if (actorResult.AbsorbedDamage > 0)
                        excludedWithAbsorption++;
                    excludedConsumed += actorResult.ConsumedSpellTemplateIds.Count;
                    excludedExpired += actorResult.ExpiredSpellTemplateIds.Count;
                }
            }

            var included = new ShieldBuffAbsorptionBatchTotals(
                includedIncoming,
                includedAbsorbed,
                includedRemaining,
                includedActorCount,
                includedWithAbsorption,
                includedConsumed,
                includedExpired);

            var excluded = new ShieldBuffAbsorptionBatchTotals(
                excludedIncoming,
                excludedAbsorbed,
                excludedRemaining,
                excludedActorCount,
                excludedWithAbsorption,
                excludedConsumed,
                excludedExpired);

            return new ShieldBuffAbsorptionBatchTotalsPartition(included, excluded);
        }
    }
}
