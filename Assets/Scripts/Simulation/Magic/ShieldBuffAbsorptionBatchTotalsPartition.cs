using System;

// Design note:
// ShieldBuffAbsorptionBatchTotalsPartition is the deterministic two-bucket response object
// for one ShieldBuffAbsorptionBatchTotals.PartitionFrom call. It carries the Included totals
// (entries for which the partition predicate returned true) and the Excluded totals (entries
// for which the predicate returned false), each as a complete ShieldBuffAbsorptionBatchTotals
// snapshot. Pure Simulation: no Unity dependency, no presentation coupling, no tick mutation,
// no save coupling, no registry mutation. Bible reference: EMBER_VISION_BIBLE.md §3 Layer 3,
// MASTER_MECHANICS_BIBLE.md §15 Magic effects.
namespace EmberCrpg.Simulation.Magic
{
    /// <summary>Two-bucket totals split deterministically from one batch absorption result map.</summary>
    public sealed class ShieldBuffAbsorptionBatchTotalsPartition
    {
        public ShieldBuffAbsorptionBatchTotalsPartition(
            ShieldBuffAbsorptionBatchTotals included,
            ShieldBuffAbsorptionBatchTotals excluded)
        {
            if (included == null)
                throw new ArgumentNullException(nameof(included));
            if (excluded == null)
                throw new ArgumentNullException(nameof(excluded));

            Included = included;
            Excluded = excluded;
        }

        public ShieldBuffAbsorptionBatchTotals Included { get; }
        public ShieldBuffAbsorptionBatchTotals Excluded { get; }
    }
}
