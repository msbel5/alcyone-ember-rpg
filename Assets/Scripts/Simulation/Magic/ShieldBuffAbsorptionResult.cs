using System;
using System.Collections.Generic;

// Design note:
// ShieldBuffAbsorptionResult is the narrow response object for one shield-buff absorption pass
// against a ShieldBuffState container. Inputs: ShieldBuffService.AbsorbDamage incoming damage and
// the deterministic per-buff consume order. Outputs: incoming/absorbed/remaining damage totals,
// the ordered list of spell template ids whose magnitude was reduced this call (consumed), and
// the subset whose magnitude hit zero and were cleared from state (expired). Pure Simulation
// object: no Unity dependency, no presentation coupling, no tick mutation, no save coupling.
// Bible reference: EMBER_VISION_BIBLE.md §3 Layer 3, MASTER_MECHANICS_BIBLE.md §15 Magic effects.
namespace EmberCrpg.Simulation.Magic
{
    /// <summary>Result of one deterministic shield-buff damage-absorption pass.</summary>
    public sealed class ShieldBuffAbsorptionResult
    {
        private ShieldBuffAbsorptionResult(
            int incomingDamage,
            int absorbedDamage,
            int remainingDamage,
            IReadOnlyList<string> consumedSpellTemplateIds,
            IReadOnlyList<string> expiredSpellTemplateIds)
        {
            IncomingDamage = incomingDamage;
            AbsorbedDamage = absorbedDamage;
            RemainingDamage = remainingDamage;
            ConsumedSpellTemplateIds = consumedSpellTemplateIds;
            ExpiredSpellTemplateIds = expiredSpellTemplateIds;
        }

        public int IncomingDamage { get; }
        public int AbsorbedDamage { get; }
        public int RemainingDamage { get; }
        public IReadOnlyList<string> ConsumedSpellTemplateIds { get; }
        public IReadOnlyList<string> ExpiredSpellTemplateIds { get; }

        public static ShieldBuffAbsorptionResult Create(
            int incomingDamage,
            int absorbedDamage,
            int remainingDamage,
            IReadOnlyList<string> consumedSpellTemplateIds,
            IReadOnlyList<string> expiredSpellTemplateIds)
        {
            if (incomingDamage < 0)
                throw new ArgumentOutOfRangeException(nameof(incomingDamage), incomingDamage, "Incoming damage must be zero or positive.");
            if (absorbedDamage < 0)
                throw new ArgumentOutOfRangeException(nameof(absorbedDamage), absorbedDamage, "Absorbed damage must be zero or positive.");
            if (remainingDamage < 0)
                throw new ArgumentOutOfRangeException(nameof(remainingDamage), remainingDamage, "Remaining damage must be zero or positive.");
            if (absorbedDamage + remainingDamage != incomingDamage)
                throw new ArgumentException("Absorbed plus remaining damage must equal incoming damage.", nameof(absorbedDamage));
            if (consumedSpellTemplateIds == null)
                throw new ArgumentNullException(nameof(consumedSpellTemplateIds));
            if (expiredSpellTemplateIds == null)
                throw new ArgumentNullException(nameof(expiredSpellTemplateIds));

            return new ShieldBuffAbsorptionResult(
                incomingDamage,
                absorbedDamage,
                remainingDamage,
                consumedSpellTemplateIds,
                expiredSpellTemplateIds);
        }
    }
}
