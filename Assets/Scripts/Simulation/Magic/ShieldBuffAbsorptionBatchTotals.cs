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
            if (resultsByActorId == null)
                throw new ArgumentNullException(nameof(resultsByActorId));

            var totalIncomingDamage = 0;
            var totalAbsorbedDamage = 0;
            var totalRemainingDamage = 0;
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
                resultsByActorId.Count,
                actorsWithAbsorption,
                totalConsumedBuffEntries,
                totalExpiredBuffEntries);
        }
    }
}
