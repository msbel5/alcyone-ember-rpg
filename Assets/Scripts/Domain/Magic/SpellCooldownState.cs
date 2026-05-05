using System;
using System.Collections.Generic;

// Design note:
// SpellCooldownState is the deterministic per-caster cooldown bag for Sprint 5 magic.
// Inputs: stable spell template ids mapped to remaining cooldown ticks.
// Outputs: mutable pure-Domain state with no Unity dependency. Lives in Domain.Magic so
// world-state aggregates and the JSON save layer can carry it without crossing into Simulation.
// Bible reference: EMBER_VISION_BIBLE.md §3 Layer 3, Sprint 5 cooldown foundation, and Sprint 5
// cooldown persistence increment.
namespace EmberCrpg.Domain.Magic
{
    /// <summary>Mutable cooldown state keyed by stable spell template ids.</summary>
    public sealed class SpellCooldownState
    {
        private readonly Dictionary<string, int> _remainingTicksBySpellId = new Dictionary<string, int>(StringComparer.Ordinal);

        public int GetRemainingTicks(string spellTemplateId)
        {
            if (string.IsNullOrWhiteSpace(spellTemplateId))
                return 0;

            return _remainingTicksBySpellId.TryGetValue(spellTemplateId, out var remainingTicks)
                ? remainingTicks
                : 0;
        }

        public IReadOnlyList<string> GetTrackedSpellTemplateIds()
        {
            var trackedSpellIds = new List<string>(_remainingTicksBySpellId.Count);
            foreach (var spellId in _remainingTicksBySpellId.Keys)
            {
                trackedSpellIds.Add(spellId);
            }

            return trackedSpellIds;
        }

        public void SetRemainingTicks(string spellTemplateId, int remainingTicks)
        {
            if (string.IsNullOrWhiteSpace(spellTemplateId))
                throw new ArgumentException("Spell templateId must be a non-empty stable id.", nameof(spellTemplateId));
            if (remainingTicks < 0)
                throw new ArgumentOutOfRangeException(nameof(remainingTicks), remainingTicks, "Remaining cooldown must be zero or positive.");

            if (remainingTicks == 0)
            {
                _remainingTicksBySpellId.Remove(spellTemplateId);
                return;
            }

            _remainingTicksBySpellId[spellTemplateId] = remainingTicks;
        }
    }
}
