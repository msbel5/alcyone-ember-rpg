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

        // Empty totals: the additive identity for Merge. ActorCount, ActorsWithAbsorption,
        // and every damage/buff-entry counter are zero, so Merge(Empty, x) and Merge(x, Empty)
        // both equal x for any totals x. Matches From over an empty result map. A future
        // rolling cross-batch aggregator can seed a multi-tick fold with Empty without
        // having to special-case the first iteration.
        public static ShieldBuffAbsorptionBatchTotals Empty { get; } =
            new ShieldBuffAbsorptionBatchTotals(0, 0, 0, 0, 0, 0, 0);

        // Merge overload: deterministically combines two already-computed
        // ShieldBuffAbsorptionBatchTotals snapshots into a single totals object whose
        // counters are the field-wise sums of left and right. This generalizes the
        // existing From/PartitionFrom/GroupBy aggregations from "one result map" to
        // "many result maps" so a future combat damage-resolution pass or telemetry/UI
        // surface can roll multiple AbsorbDamageForActors batches (e.g. across ticks
        // or across encounter sub-passes) into a single deterministic snapshot without
        // re-walking any original per-actor result map. Pure aggregation: no Unity
        // dependency, no presentation coupling, no registry read, no buff/tick mutation,
        // no save coupling. Order-independent: Merge is commutative and associative
        // because every counter is a commutative integer sum, so Merge(a, b) equals
        // Merge(b, a) and Merge(Merge(a, b), c) equals Merge(a, Merge(b, c)). Empty is
        // the identity element under Merge.
        public static ShieldBuffAbsorptionBatchTotals Merge(
            ShieldBuffAbsorptionBatchTotals left,
            ShieldBuffAbsorptionBatchTotals right)
        {
            if (left == null)
                throw new ArgumentNullException(nameof(left));
            if (right == null)
                throw new ArgumentNullException(nameof(right));

            return new ShieldBuffAbsorptionBatchTotals(
                left.TotalIncomingDamage + right.TotalIncomingDamage,
                left.TotalAbsorbedDamage + right.TotalAbsorbedDamage,
                left.TotalRemainingDamage + right.TotalRemainingDamage,
                left.ActorCount + right.ActorCount,
                left.ActorsWithAbsorption + right.ActorsWithAbsorption,
                left.TotalConsumedBuffEntries + right.TotalConsumedBuffEntries,
                left.TotalExpiredBuffEntries + right.TotalExpiredBuffEntries);
        }

        // MergeMany overload: deterministic fold over an arbitrary sequence of already-computed
        // ShieldBuffAbsorptionBatchTotals snapshots. Generalizes the binary Merge factory from
        // "fold of two snapshots" to "fold of N snapshots" so a future combat damage-resolution
        // pass or telemetry/UI surface can roll a whole list of AbsorbDamageForActors batches
        // (e.g. every tick of an encounter, or every sub-pass of a multi-stage spell volley)
        // into one deterministic snapshot in a single call. Implemented as a left fold seeded
        // with Empty using the binary Merge as the combining operator, so the additive identity
        // and the field-wise integer sums stay consistent with Merge / From / PartitionFrom /
        // GroupBy. Pure aggregation: no Unity dependency, no presentation coupling, no registry
        // read, no buff/tick mutation, no save coupling. Order independence still holds because
        // every counter is a commutative integer sum, so the fold result is invariant under any
        // permutation of the input sequence. An empty sequence returns Empty.
        public static ShieldBuffAbsorptionBatchTotals MergeMany(
            IEnumerable<ShieldBuffAbsorptionBatchTotals> totals)
        {
            if (totals == null)
                throw new ArgumentNullException(nameof(totals));

            var accumulator = Empty;
            var index = 0;
            foreach (var snapshot in totals)
            {
                if (snapshot == null)
                    throw new ArgumentException(
                        $"Totals sequence element at index {index} must not be null.",
                        nameof(totals));
                accumulator = Merge(accumulator, snapshot);
                index++;
            }

            return accumulator;
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

        // Group-by overload: walks the per-actor result map exactly once and routes each
        // entry to a per-group ShieldBuffAbsorptionBatchTotals bucket keyed by a caller
        // supplied keyExtractor. This generalizes PartitionFrom from a binary split to an
        // N-way split, so a future combat damage-resolution pass or telemetry/UI surface
        // can summarize totals broken down by faction, region, or buff family from a
        // single deterministic pass instead of one filtered ComputeBatchTotals call per
        // bucket. Strict input contract is preserved: every entry is validated for non
        // empty actor key and non-null per-actor result before the keyExtractor is
        // consulted, and the keyExtractor itself must return a non-empty stable group
        // key. Group keys use ordinal string comparison for stable lookup independent
        // of culture. The returned dictionary is independent of the input map order
        // because the per-bucket totals are commutative sums and counts, and the union
        // of all bucket totals equals the unfiltered From(map) result.
        public static IReadOnlyDictionary<string, ShieldBuffAbsorptionBatchTotals> GroupBy(
            IReadOnlyDictionary<string, ShieldBuffAbsorptionResult> resultsByActorId,
            Func<string, ShieldBuffAbsorptionResult, string> keyExtractor)
        {
            return GroupBy(resultsByActorId, keyExtractor, includePredicate: null);
        }

        // Filtered group-by overload: walks the per-actor result map exactly once and routes
        // each entry to a per-group ShieldBuffAbsorptionBatchTotals bucket keyed by the
        // caller-supplied keyExtractor, but only after the entry passes the
        // includePredicate. Combines the predicate-filter and N-way group-by surfaces in a
        // single deterministic pass so a future combat damage-resolution pass or
        // telemetry/UI surface can compute "side-by-side faction totals over a tagged
        // subset" (e.g. allies vs enemies, only over actors that absorbed damage) from one
        // batch absorption call without scanning the result map twice. Strict input
        // contract is preserved: every entry is validated for non-empty actor key and
        // non-null per-actor result before the predicate or keyExtractor is consulted, so
        // the guard order matches the unfiltered GroupBy and the filtered From overloads.
        // When includePredicate is null this overload behaves exactly like the unfiltered
        // GroupBy(map, keyExtractor) factory. The union of all per-bucket totals equals
        // the filtered ComputeBatchTotals(map, includePredicate) result, and each bucket
        // matches the filtered ComputeBatchTotals(map, predicate) for the same key
        // membership intersected with includePredicate.
        public static IReadOnlyDictionary<string, ShieldBuffAbsorptionBatchTotals> GroupBy(
            IReadOnlyDictionary<string, ShieldBuffAbsorptionResult> resultsByActorId,
            Func<string, ShieldBuffAbsorptionResult, string> keyExtractor,
            Func<string, ShieldBuffAbsorptionResult, bool> includePredicate)
        {
            if (resultsByActorId == null)
                throw new ArgumentNullException(nameof(resultsByActorId));
            if (keyExtractor == null)
                throw new ArgumentNullException(nameof(keyExtractor));

            var accumulators = new Dictionary<string, GroupAccumulator>(StringComparer.Ordinal);

            foreach (var pair in resultsByActorId)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                    throw new ArgumentException("Actor id keys must be non-empty stable ids.", nameof(resultsByActorId));

                var actorResult = pair.Value;
                if (actorResult == null)
                    throw new ArgumentException("Per-actor absorption result must not be null.", nameof(resultsByActorId));

                if (includePredicate != null && !includePredicate(pair.Key, actorResult))
                    continue;

                var groupKey = keyExtractor(pair.Key, actorResult);
                if (string.IsNullOrWhiteSpace(groupKey))
                    throw new ArgumentException("Group keys returned by keyExtractor must be non-empty stable ids.", nameof(keyExtractor));

                if (!accumulators.TryGetValue(groupKey, out var accumulator))
                {
                    accumulator = new GroupAccumulator();
                    accumulators[groupKey] = accumulator;
                }

                accumulator.ActorCount++;
                accumulator.TotalIncomingDamage += actorResult.IncomingDamage;
                accumulator.TotalAbsorbedDamage += actorResult.AbsorbedDamage;
                accumulator.TotalRemainingDamage += actorResult.RemainingDamage;
                if (actorResult.AbsorbedDamage > 0)
                    accumulator.ActorsWithAbsorption++;
                accumulator.TotalConsumedBuffEntries += actorResult.ConsumedSpellTemplateIds.Count;
                accumulator.TotalExpiredBuffEntries += actorResult.ExpiredSpellTemplateIds.Count;
            }

            var groups = new Dictionary<string, ShieldBuffAbsorptionBatchTotals>(
                accumulators.Count,
                StringComparer.Ordinal);

            foreach (var pair in accumulators)
            {
                var a = pair.Value;
                groups[pair.Key] = new ShieldBuffAbsorptionBatchTotals(
                    a.TotalIncomingDamage,
                    a.TotalAbsorbedDamage,
                    a.TotalRemainingDamage,
                    a.ActorCount,
                    a.ActorsWithAbsorption,
                    a.TotalConsumedBuffEntries,
                    a.TotalExpiredBuffEntries);
            }

            return groups;
        }

        private sealed class GroupAccumulator
        {
            public int TotalIncomingDamage;
            public int TotalAbsorbedDamage;
            public int TotalRemainingDamage;
            public int ActorCount;
            public int ActorsWithAbsorption;
            public int TotalConsumedBuffEntries;
            public int TotalExpiredBuffEntries;
        }
    }
}
