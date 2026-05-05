using System;
using EmberCrpg.Domain.Magic;

// Design note:
// ShieldBuffService is the deterministic Sprint 5 shield-buff tick-down seam.
// Inputs: a ShieldBuffState container (filled by SpellEffectResolutionService.ApplyShieldBuffs)
// and elapsed simulation ticks. Outputs: in-place tick-down that expires entries when their
// remaining ticks reach zero, preserving each entry's magnitude until expiry.
// This slice is decay-only. It does not key buffs to specific actors, does not reduce shield
// magnitude per absorbed damage, and does not call into combat resolution. Bible reference:
// EMBER_VISION_BIBLE.md §3 Layer 3, MASTER_MECHANICS_BIBLE.md §15 Magic effects, mirroring the
// SpellCooldownService.AdvanceTicks pattern from the cooldown foundation slice.
namespace EmberCrpg.Simulation.Magic
{
    /// <summary>Pure deterministic tick-down service for active shield buff state.</summary>
    public sealed class ShieldBuffService
    {
        public void AdvanceTicks(ShieldBuffState shieldBuffState, int elapsedTicks)
        {
            if (shieldBuffState == null)
                throw new ArgumentNullException(nameof(shieldBuffState));
            if (elapsedTicks < 0)
                throw new ArgumentOutOfRangeException(nameof(elapsedTicks), elapsedTicks, "Elapsed ticks must be zero or positive.");
            if (elapsedTicks == 0)
                return;

            foreach (var spellTemplateId in shieldBuffState.GetTrackedSpellTemplateIds())
            {
                var remainingTicks = shieldBuffState.GetRemainingTicks(spellTemplateId);
                if (remainingTicks <= 0)
                    continue;

                var magnitude = shieldBuffState.GetMagnitude(spellTemplateId);
                var updatedTicks = remainingTicks - elapsedTicks;
                shieldBuffState.SetActiveBuff(spellTemplateId, updatedTicks > 0 ? updatedTicks : 0, magnitude);
            }
        }
    }
}
