// Design note:
// ShieldBuffSaveMapper round-trips ShieldBuffState through the JSON save layer.
// Inputs: deterministic timed shield-buff state or its DTO twin.
// Outputs: ordered DTO arrays for stable JSON, and rebuilt mutable state for load paths.
// Bible reference: PRD Sprint 1 FR-06, Sprint 5 shield buff foundation, Sprint 5 shield buff persistence.
using System;
using System.Linq;
using EmberCrpg.Domain.Magic;

namespace EmberCrpg.Data.Save
{
    /// <summary>Pure mapping layer between ShieldBuffState and its serializable DTO.</summary>
    public static class ShieldBuffSaveMapper
    {
        public static ShieldBuffSaveData ToData(ShieldBuffState state)
        {
            if (state == null)
                return new ShieldBuffSaveData { entries = new ShieldBuffEntrySaveData[0] };

            var entries = state.GetTrackedSpellTemplateIds()
                .OrderBy(spellTemplateId => spellTemplateId, StringComparer.Ordinal)
                .Select(spellTemplateId => new ShieldBuffEntrySaveData
                {
                    spellTemplateId = spellTemplateId,
                    remainingTicks = state.GetRemainingTicks(spellTemplateId),
                    magnitude = state.GetMagnitude(spellTemplateId),
                })
                .ToArray();

            return new ShieldBuffSaveData { entries = entries };
        }

        public static ShieldBuffState ToState(ShieldBuffSaveData data)
        {
            var state = new ShieldBuffState();
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
                if (entry.magnitude < 0)
                    continue;

                state.SetActiveBuff(entry.spellTemplateId, entry.remainingTicks, entry.magnitude);
            }

            return state;
        }
    }
}
