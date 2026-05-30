// EMB-036: ShieldBuffService batch-totals aggregation (Compute/Group/Merge/Partition), partial.
using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Magic;


namespace EmberCrpg.Simulation.Magic
{
    public sealed partial class ShieldBuffService
    {
        // call without re-walking the result map. Pure delegation — no registry read, no buff
        // mutation, no tick mutation, no save coupling. The aggregation order has no observable
        // effect because the totals are commutative sums and counts.
        public ShieldBuffAbsorptionBatchTotals ComputeBatchTotals(
            IReadOnlyDictionary<string, ShieldBuffAbsorptionResult> resultsByActorId)
        {
            return ShieldBuffAbsorptionBatchTotals.From(resultsByActorId);
        }

        // Subset batch totals seam: forwards a per-actor filter predicate to the underlying
        // ShieldBuffAbsorptionBatchTotals.From overload so a future combat damage-resolution
        // pass or telemetry/UI surface can summarize a side-specific or absorbed-only slice
        // of the same per-actor result map without re-walking it. Pure delegation — no new
        // aggregation rules, no registry read, no buff/tick mutation, no save coupling. The
        // strict input contract is unchanged: every map entry is still validated even when
        // the predicate would otherwise filter it out.
        public ShieldBuffAbsorptionBatchTotals ComputeBatchTotals(
            IReadOnlyDictionary<string, ShieldBuffAbsorptionResult> resultsByActorId,
            Func<string, ShieldBuffAbsorptionResult, bool> includePredicate)
        {
            return ShieldBuffAbsorptionBatchTotals.From(resultsByActorId, includePredicate);
        }

        // Stacked-filter map-level batch totals seam: forwards a per-actor includePredicate
        // that gates kept entries into the running totals and a per-actor filterPredicate
        // that pre-filters the result map outright to the underlying
        // ShieldBuffAbsorptionBatchTotals.From(map, includePredicate, filterPredicate)
        // overload so a future combat damage-resolution pass or telemetry/UI surface can fold a
        // tagged subset of a tagged subset of one batch absorption result map (e.g. only
        // allies that also absorbed damage) in a single deterministic walk without scanning
        // the map twice. Mirrors the stacked-filter seam already established on
        // ComputeBatchTotalsPartition(map, includePredicate, filterPredicate),
        // GroupBatchTotals(map, keyExtractor, includePredicate, filterPredicate), and the
        // cross-batch MergeBatchTotalsMany(seq, includePredicate, filterPredicate) overload.
        // Pure delegation — no new aggregation rules, no registry read, no buff/tick mutation,
        // no save coupling. The strict input contract is unchanged: every map entry is still
        // validated before either predicate is consulted, and filterPredicate is consulted
        // before includePredicate.
        public ShieldBuffAbsorptionBatchTotals ComputeBatchTotals(
            IReadOnlyDictionary<string, ShieldBuffAbsorptionResult> resultsByActorId,
            Func<string, ShieldBuffAbsorptionResult, bool> includePredicate,
            Func<string, ShieldBuffAbsorptionResult, bool> filterPredicate)
        {
            return ShieldBuffAbsorptionBatchTotals.From(
                resultsByActorId, includePredicate, filterPredicate);
        }

        // Partition batch totals seam: forwards a per-actor predicate to the underlying
        // ShieldBuffAbsorptionBatchTotals.PartitionFrom factory so a future combat
        // damage-resolution pass or telemetry/UI surface can compute the side-A vs side-B
        // (e.g. allies vs enemies, absorbed vs untouched) totals of one batch absorption
        // result map in a single deterministic pass instead of two separate filtered
        // ComputeBatchTotals calls. Pure delegation — no new aggregation rules, no
        // registry read, no buff/tick mutation, no save coupling. The strict input
        // contract is unchanged: every map entry is still validated before the predicate
        // is consulted.
        public ShieldBuffAbsorptionBatchTotalsPartition ComputeBatchTotalsPartition(
            IReadOnlyDictionary<string, ShieldBuffAbsorptionResult> resultsByActorId,
            Func<string, ShieldBuffAbsorptionResult, bool> includePredicate)
        {
            return ShieldBuffAbsorptionBatchTotals.PartitionFrom(resultsByActorId, includePredicate);
        }

        // Stacked-filter map-level partition seam: forwards a per-actor includePredicate that
        // buckets kept entries into Included vs Excluded and a per-actor filterPredicate that
        // pre-filters the result map outright to the underlying
        // ShieldBuffAbsorptionBatchTotals.PartitionFrom(map, includePredicate, filterPredicate)
        // overload so a future combat damage-resolution pass or telemetry/UI surface can split a
        // side-versus-side bucket (e.g. allies vs enemies) over a tagged subset (e.g. only
        // actors that absorbed damage) of one batch absorption result map in a single
        // deterministic walk without scanning the map twice. Mirrors the stacked-filter seam
        // already established on the cross-batch PartitionBatchTotalsMany(seq, includePredicate,
        // filterPredicate) overload and on GroupBatchTotals(map, keyExtractor, includePredicate,
        // filterPredicate). Pure delegation — no new aggregation rules, no registry read, no
        // buff/tick mutation, no save coupling. The strict input contract is unchanged: every
        // map entry is still validated before either predicate is consulted, and filterPredicate
        // is consulted before includePredicate.
        public ShieldBuffAbsorptionBatchTotalsPartition ComputeBatchTotalsPartition(
            IReadOnlyDictionary<string, ShieldBuffAbsorptionResult> resultsByActorId,
            Func<string, ShieldBuffAbsorptionResult, bool> includePredicate,
            Func<string, ShieldBuffAbsorptionResult, bool> filterPredicate)
        {
            return ShieldBuffAbsorptionBatchTotals.PartitionFrom(
                resultsByActorId, includePredicate, filterPredicate);
        }

        // Group-by batch totals seam: forwards a per-actor key-extractor to the underlying
        // ShieldBuffAbsorptionBatchTotals.GroupBy factory so a future combat damage-resolution
        // pass or telemetry/UI surface can compute N-way side/faction/region totals (e.g.
        // allies vs neutrals vs enemies) from one batch absorption result map in a single
        // deterministic pass instead of one filtered ComputeBatchTotals call per bucket.
        // Pure delegation — no new aggregation rules, no registry read, no buff/tick mutation,
        // no save coupling. The strict input contract is unchanged: every map entry is still
        // validated before the keyExtractor is consulted, and keyExtractor must return a
        // non-empty stable group key.
        public IReadOnlyDictionary<string, ShieldBuffAbsorptionBatchTotals> GroupBatchTotals(
            IReadOnlyDictionary<string, ShieldBuffAbsorptionResult> resultsByActorId,
            Func<string, ShieldBuffAbsorptionResult, string> keyExtractor)
        {
            return ShieldBuffAbsorptionBatchTotals.GroupBy(resultsByActorId, keyExtractor);
        }

        // Filtered group-by batch totals seam: forwards a per-actor key-extractor and a
        // per-actor includePredicate to the underlying ShieldBuffAbsorptionBatchTotals.GroupBy
        // overload so a future combat damage-resolution pass or telemetry/UI surface can
        // compute N-way faction/region totals over a filtered subset of one batch absorption
        // result map (e.g. allies vs enemies, only over actors that absorbed damage) in a
        // single deterministic pass instead of one filtered ComputeBatchTotals call per bucket
        // or two separate filter-then-group walks. Pure delegation — no new aggregation rules,
        // no registry read, no buff/tick mutation, no save coupling. The strict input contract
        // is unchanged: every map entry is still validated before the predicate and the
        // keyExtractor are consulted, and keyExtractor must return a non-empty stable group key.
        public IReadOnlyDictionary<string, ShieldBuffAbsorptionBatchTotals> GroupBatchTotals(
            IReadOnlyDictionary<string, ShieldBuffAbsorptionResult> resultsByActorId,
            Func<string, ShieldBuffAbsorptionResult, string> keyExtractor,
            Func<string, ShieldBuffAbsorptionResult, bool> includePredicate)
        {
            return ShieldBuffAbsorptionBatchTotals.GroupBy(resultsByActorId, keyExtractor, includePredicate);
        }

        // Stacked-filter map-level group-by batch totals seam: forwards a per-actor key-extractor,
        // a per-actor includePredicate, and a per-actor filterPredicate to the underlying
        // ShieldBuffAbsorptionBatchTotals.GroupBy(map, keyExtractor, includePredicate,
        // filterPredicate) overload so a future combat damage-resolution pass or telemetry/UI
        // surface can compute N-way per-faction totals over a tagged subset of a tagged subset
        // (e.g. only allies that survived AND only actors that absorbed damage) from one batch
        // absorption result map in a single deterministic pass instead of one filtered
        // ComputeBatchTotals call per bucket or two separate filter-then-group walks. Mirrors
        // the stacked-filter seam already established on the cross-batch
        // GroupBatchTotalsByMany(seq, keyExtractor, includePredicate, filterPredicate) overload
        // and on PartitionBatchTotalsMany(seq, includePredicate, filterPredicate). Pure
        // delegation — no new aggregation rules, no registry read, no buff/tick mutation, no
        // save coupling. The strict input contract is unchanged: every map entry is still
        // validated before either predicate or the keyExtractor is consulted, filterPredicate
        // is consulted before includePredicate, and keyExtractor must return a non-empty stable
        // group key.
        public IReadOnlyDictionary<string, ShieldBuffAbsorptionBatchTotals> GroupBatchTotals(
            IReadOnlyDictionary<string, ShieldBuffAbsorptionResult> resultsByActorId,
            Func<string, ShieldBuffAbsorptionResult, string> keyExtractor,
            Func<string, ShieldBuffAbsorptionResult, bool> includePredicate,
            Func<string, ShieldBuffAbsorptionResult, bool> filterPredicate)
        {
            return ShieldBuffAbsorptionBatchTotals.GroupBy(
                resultsByActorId, keyExtractor, includePredicate, filterPredicate);
        }

        // Cross-batch merge seam: forwards two already-computed
        // ShieldBuffAbsorptionBatchTotals snapshots to the underlying
        // ShieldBuffAbsorptionBatchTotals.Merge factory so a future combat
        // damage-resolution pass or telemetry/UI surface can fold the totals
        // of multiple AbsorbDamageForActors batches (e.g. across ticks or across
        // encounter sub-passes) into a single deterministic snapshot without
        // re-walking any original per-actor result map. Pure delegation — no new
        // aggregation rules, no registry read, no buff/tick mutation, no save
        // coupling. Commutative and associative under Merge because every
        // counter is a commutative integer sum.
        public ShieldBuffAbsorptionBatchTotals MergeBatchTotals(
            ShieldBuffAbsorptionBatchTotals left,
            ShieldBuffAbsorptionBatchTotals right)
        {
            return ShieldBuffAbsorptionBatchTotals.Merge(left, right);
        }

        // Cross-batch fold seam: forwards an arbitrary sequence of already-computed
        // ShieldBuffAbsorptionBatchTotals snapshots to the underlying
        // ShieldBuffAbsorptionBatchTotals.MergeMany factory so a future combat
        // damage-resolution pass or telemetry/UI surface can fold a whole list of
        // AbsorbDamageForActors batches (e.g. every tick or every spell sub-pass)
        // into one deterministic snapshot in a single call instead of chaining
        // pairwise MergeBatchTotals calls. Pure delegation — no new aggregation
        // rules, no registry read, no buff/tick mutation, no save coupling.
        public ShieldBuffAbsorptionBatchTotals MergeBatchTotalsMany(
            System.Collections.Generic.IEnumerable<ShieldBuffAbsorptionBatchTotals> totals)
        {
            return ShieldBuffAbsorptionBatchTotals.MergeMany(totals);
        }

        // Filtered cross-batch fold seam: forwards an arbitrary sequence of already-computed
        // ShieldBuffAbsorptionBatchTotals snapshots and a per-snapshot includePredicate to the
        // underlying ShieldBuffAbsorptionBatchTotals.MergeMany(totals, predicate) factory so a
        // future combat damage-resolution pass or telemetry/UI surface can fold a tagged
        // subset of cross-batch snapshots (e.g. only ticks where any actor absorbed damage)
        // without rebuilding the sequence first. Pure delegation — no new aggregation rules,
        // no registry read, no buff/tick mutation, no save coupling.
        public ShieldBuffAbsorptionBatchTotals MergeBatchTotalsMany(
            System.Collections.Generic.IEnumerable<ShieldBuffAbsorptionBatchTotals> totals,
            System.Func<ShieldBuffAbsorptionBatchTotals, bool> includePredicate)
        {
            return ShieldBuffAbsorptionBatchTotals.MergeMany(totals, includePredicate);
        }

        // Stacked-filter cross-batch fold seam: forwards an arbitrary sequence of
        // already-computed ShieldBuffAbsorptionBatchTotals snapshots, a per-snapshot
        // includePredicate that gates kept snapshots into the running Merge fold, and
        // a per-snapshot filterPredicate that pre-filters the sequence to the underlying
        // ShieldBuffAbsorptionBatchTotals.MergeMany(totals, includePredicate, filterPredicate)
        // factory so a future combat damage-resolution pass or telemetry/UI surface can
        // fold a tagged subset of a tagged subset of cross-batch snapshots (e.g. only
        // sub-passes flagged offensive that also registered any absorption) in one
        // deterministic walk without rebuilding the sequence first. Mirrors the
        // stacked-filter seam already established on
        // PartitionBatchTotalsMany(seq, includePredicate, filterPredicate),
        // GroupBatchTotalsByMany(seq, keyExtractor, includePredicate, filterPredicate),
        // GroupBatchTotals(map, keyExtractor, includePredicate, filterPredicate), and
        // ComputeBatchTotalsPartition(map, includePredicate, filterPredicate). Pure
        // delegation — no new aggregation rules, no registry read, no buff/tick mutation,
        // no save coupling. The strict input contract is unchanged: every element is
        // still validated for non-null before either predicate is consulted, and
        // filterPredicate is consulted before includePredicate.
        public ShieldBuffAbsorptionBatchTotals MergeBatchTotalsMany(
            System.Collections.Generic.IEnumerable<ShieldBuffAbsorptionBatchTotals> totals,
            System.Func<ShieldBuffAbsorptionBatchTotals, bool> includePredicate,
            System.Func<ShieldBuffAbsorptionBatchTotals, bool> filterPredicate)
        {
            return ShieldBuffAbsorptionBatchTotals.MergeMany(
                totals, includePredicate, filterPredicate);
        }

        // Cross-batch partition seam: forwards an arbitrary sequence of already-computed
        // ShieldBuffAbsorptionBatchTotals snapshots and a per-snapshot includePredicate to
        // the underlying ShieldBuffAbsorptionBatchTotals.PartitionMany factory so a future
        // combat damage-resolution pass or telemetry/UI surface can split a tagged subset
        // versus its complement (e.g. ticks where any actor absorbed damage versus ticks
        // where none did) in a single deterministic walk and read both buckets as complete
        // batch totals snapshots. Pure delegation — no new aggregation rules, no registry
        // read, no buff/tick mutation, no save coupling.
        public ShieldBuffAbsorptionBatchTotalsPartition PartitionBatchTotalsMany(
            System.Collections.Generic.IEnumerable<ShieldBuffAbsorptionBatchTotals> totals,
            System.Func<ShieldBuffAbsorptionBatchTotals, bool> includePredicate)
        {
            return ShieldBuffAbsorptionBatchTotals.PartitionMany(totals, includePredicate);
        }

        // Filtered cross-batch partition seam: forwards an arbitrary sequence of already
        // computed ShieldBuffAbsorptionBatchTotals snapshots, a per-snapshot
        // includePredicate that buckets kept snapshots into Included vs Excluded, and a
        // per-snapshot filterPredicate that pre-filters the sequence to the underlying
        // ShieldBuffAbsorptionBatchTotals.PartitionMany(totals, includePredicate,
        // filterPredicate) factory so a future combat damage-resolution pass or
        // telemetry/UI surface can split a side-versus-side bucket (e.g. offensive vs
        // defensive) over a tagged subset (e.g. only sub-passes that registered any
        // absorption) in one deterministic walk without rebuilding the sequence first.
        // Pure delegation — no new aggregation rules, no registry read, no buff/tick
        // mutation, no save coupling.
        public ShieldBuffAbsorptionBatchTotalsPartition PartitionBatchTotalsMany(
            System.Collections.Generic.IEnumerable<ShieldBuffAbsorptionBatchTotals> totals,
            System.Func<ShieldBuffAbsorptionBatchTotals, bool> includePredicate,
            System.Func<ShieldBuffAbsorptionBatchTotals, bool> filterPredicate)
        {
            return ShieldBuffAbsorptionBatchTotals.PartitionMany(totals, includePredicate, filterPredicate);
        }

        // Cross-batch group-by seam: forwards an arbitrary sequence of already-computed
        // ShieldBuffAbsorptionBatchTotals snapshots and a per-snapshot keyExtractor to the
        // underlying ShieldBuffAbsorptionBatchTotals.GroupByMany factory so a future combat
        // damage-resolution pass or telemetry/UI surface can bucket cross-batch snapshots
        // by an N-way categorical tag (e.g. by tick phase, by encounter id, by faction
        // label) in a single deterministic walk and read each bucket as a complete batch
        // totals snapshot. Pure delegation — no new aggregation rules, no registry read,
        // no buff/tick mutation, no save coupling.
        public System.Collections.Generic.IReadOnlyDictionary<string, ShieldBuffAbsorptionBatchTotals> GroupBatchTotalsByMany(
            System.Collections.Generic.IEnumerable<ShieldBuffAbsorptionBatchTotals> totals,
            System.Func<ShieldBuffAbsorptionBatchTotals, string> keyExtractor)
        {
            return ShieldBuffAbsorptionBatchTotals.GroupByMany(totals, keyExtractor);
        }

        // Filtered cross-batch group-by seam: forwards an arbitrary sequence of already-computed
        // ShieldBuffAbsorptionBatchTotals snapshots, a per-snapshot keyExtractor, and a
        // per-snapshot includePredicate to the underlying
        // ShieldBuffAbsorptionBatchTotals.GroupByMany(totals, keyExtractor, includePredicate)
        // factory so a future combat damage-resolution pass or telemetry/UI surface can compute
        // N-way per-group totals (e.g. by tick phase, by encounter id) over a tagged subset of
        // cross-batch snapshots (e.g. only sub-passes flagged offensive, or only ticks where
        // any actor absorbed damage) in a single deterministic walk without rebuilding the
        // sequence first. Pure delegation — no new aggregation rules, no registry read, no
        // buff/tick mutation, no save coupling.
        public System.Collections.Generic.IReadOnlyDictionary<string, ShieldBuffAbsorptionBatchTotals> GroupBatchTotalsByMany(
            System.Collections.Generic.IEnumerable<ShieldBuffAbsorptionBatchTotals> totals,
            System.Func<ShieldBuffAbsorptionBatchTotals, string> keyExtractor,
            System.Func<ShieldBuffAbsorptionBatchTotals, bool> includePredicate)
        {
            return ShieldBuffAbsorptionBatchTotals.GroupByMany(totals, keyExtractor, includePredicate);
        }

        // Stacked-filter cross-batch group-by seam: forwards an arbitrary sequence of
        // already-computed ShieldBuffAbsorptionBatchTotals snapshots, a per-snapshot
        // keyExtractor, a per-snapshot includePredicate, and a per-snapshot filterPredicate
        // to the underlying ShieldBuffAbsorptionBatchTotals.GroupByMany(totals, keyExtractor,
        // includePredicate, filterPredicate) factory so a future combat damage-resolution
        // pass or telemetry/UI surface can compute N-way per-group totals (e.g. by tick
        // phase, by encounter id) over a tagged subset of cross-batch snapshots (e.g. only
        // sub-passes flagged offensive that also registered any absorption) in a single
        // deterministic walk without rebuilding the sequence first. Pure delegation — no new
        // aggregation rules, no registry read, no buff/tick mutation, no save coupling.
        public System.Collections.Generic.IReadOnlyDictionary<string, ShieldBuffAbsorptionBatchTotals> GroupBatchTotalsByMany(
            System.Collections.Generic.IEnumerable<ShieldBuffAbsorptionBatchTotals> totals,
            System.Func<ShieldBuffAbsorptionBatchTotals, string> keyExtractor,
            System.Func<ShieldBuffAbsorptionBatchTotals, bool> includePredicate,
            System.Func<ShieldBuffAbsorptionBatchTotals, bool> filterPredicate)
        {
            return ShieldBuffAbsorptionBatchTotals.GroupByMany(
                totals, keyExtractor, includePredicate, filterPredicate);
        }
    }
}
