// Design note:
// SpellCooldownSaveMapper round-trips SpellCooldownState through the JSON save layer.
// Inputs: deterministic per-caster cooldown state or its DTO twin.
// Outputs: ordered DTO arrays for stable JSON, and rebuilt mutable state for load paths.
// Bible reference: PRD Sprint 1 FR-06, Sprint 5 cooldown foundation, Sprint 5 cooldown persistence.
using System;
using System.Linq;
using EmberCrpg.Domain.Magic;

namespace EmberCrpg.Data.Save
{
    /// <summary>Pure mapping layer between SpellCooldownState and its serializable DTO.</summary>
    public static class SpellCooldownSaveMapper
    {
        public static SpellCooldownSaveData ToData(SpellCooldownState state)
        {
            if (state == null)
                return new SpellCooldownSaveData { entries = new SpellCooldownEntrySaveData[0] };

            var entries = state.GetTrackedSpellTemplateIds()
                .OrderBy(spellTemplateId => spellTemplateId, StringComparer.Ordinal)
                .Select(spellTemplateId => new SpellCooldownEntrySaveData
                {
                    spellTemplateId = spellTemplateId,
                    remainingTicks = state.GetRemainingTicks(spellTemplateId),
                })
                .ToArray();

            return new SpellCooldownSaveData { entries = entries };
        }

        public static SpellCooldownState ToState(SpellCooldownSaveData data)
        {
            var state = new SpellCooldownState();
            if (data?.entries == null)
                return state;

            foreach (var entry in data.entries)
            {
                if (entry == null)
                    continue;
                if (string.IsNullOrWhiteSpace(entry.spellTemplateId))
                    continue;
                if (entry.remainingTicks <= 0)
                    continue;

                state.SetRemainingTicks(entry.spellTemplateId, entry.remainingTicks);
            }

            return state;
        }
    }
}
