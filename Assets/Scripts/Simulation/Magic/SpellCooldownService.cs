using System;
using EmberCrpg.Domain.Magic;

// Design note:
// SpellCooldownService is the pure deterministic cooldown seam for Sprint 5 magic.
// Inputs: immutable spell definitions plus mutable cooldown state and elapsed simulation ticks.
// Outputs: remaining cooldown queries, success-only cooldown start, and tick-down expiry.
// Bible reference: EMBER_VISION_BIBLE.md §3 Layer 3 and Sprint 5 cooldown foundation.
namespace EmberCrpg.Simulation.Magic
{
    /// <summary>Pure deterministic cooldown query/update service for spell casts.</summary>
    public sealed class SpellCooldownService
    {
        public int GetRemainingTicks(SpellDefinition spell, SpellCooldownState cooldownState)
        {
            if (spell == null)
                throw new ArgumentNullException(nameof(spell));
            if (cooldownState == null || spell.CooldownTicks <= 0)
                return 0;

            return cooldownState.GetRemainingTicks(spell.TemplateId);
        }

        public bool IsOnCooldown(SpellDefinition spell, SpellCooldownState cooldownState)
        {
            return GetRemainingTicks(spell, cooldownState) > 0;
        }

        public void StartCooldown(SpellDefinition spell, SpellCooldownState cooldownState)
        {
            if (spell == null)
                throw new ArgumentNullException(nameof(spell));
            if (cooldownState == null || spell.CooldownTicks <= 0)
                return;

            cooldownState.SetRemainingTicks(spell.TemplateId, spell.CooldownTicks);
        }

        public void AdvanceTicks(SpellCooldownState cooldownState, int elapsedTicks)
        {
            if (cooldownState == null)
                throw new ArgumentNullException(nameof(cooldownState));
            if (elapsedTicks < 0)
                throw new ArgumentOutOfRangeException(nameof(elapsedTicks), elapsedTicks, "Elapsed ticks must be zero or positive.");
            if (elapsedTicks == 0)
                return;

            foreach (var spellId in cooldownState.GetTrackedSpellTemplateIds())
            {
                var remainingTicks = cooldownState.GetRemainingTicks(spellId);
                if (remainingTicks <= 0)
                    continue;

                var updatedTicks = remainingTicks - elapsedTicks;
                cooldownState.SetRemainingTicks(spellId, updatedTicks > 0 ? updatedTicks : 0);
            }
        }
    }
}
