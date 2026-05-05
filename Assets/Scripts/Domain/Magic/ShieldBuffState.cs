using System;
using System.Collections.Generic;

// Design note:
// ShieldBuffState is the deterministic per-actor active shield-buff bag for Sprint 5 magic.
// Inputs: stable spell template ids mapped to remaining buff ticks plus the absorbed-damage
// magnitude declared by the spell that applied the buff.
// Outputs: mutable pure-Domain state with no Unity dependency. Lives in Domain.Magic so
// world-state aggregates and the JSON save layer can carry it without crossing into Simulation.
// This is the foundation slice: it stores buff state but does not yet apply, tick, or resolve.
// Bible reference: EMBER_VISION_BIBLE.md §3 Layer 3, MASTER_MECHANICS_BIBLE.md §15 Magic effects.
namespace EmberCrpg.Domain.Magic
{
    /// <summary>Mutable timed shield-buff state keyed by stable spell template ids.</summary>
    public sealed class ShieldBuffState
    {
        private readonly Dictionary<string, ShieldBuffEntry> _activeBuffsBySpellId =
            new Dictionary<string, ShieldBuffEntry>(StringComparer.Ordinal);

        public int GetRemainingTicks(string spellTemplateId)
        {
            if (string.IsNullOrWhiteSpace(spellTemplateId))
                return 0;

            return _activeBuffsBySpellId.TryGetValue(spellTemplateId, out var entry)
                ? entry.RemainingTicks
                : 0;
        }

        public int GetMagnitude(string spellTemplateId)
        {
            if (string.IsNullOrWhiteSpace(spellTemplateId))
                return 0;

            return _activeBuffsBySpellId.TryGetValue(spellTemplateId, out var entry)
                ? entry.Magnitude
                : 0;
        }

        public bool IsActive(string spellTemplateId)
        {
            return GetRemainingTicks(spellTemplateId) > 0;
        }

        public IReadOnlyList<string> GetTrackedSpellTemplateIds()
        {
            var trackedSpellIds = new List<string>(_activeBuffsBySpellId.Count);
            foreach (var spellId in _activeBuffsBySpellId.Keys)
            {
                trackedSpellIds.Add(spellId);
            }

            return trackedSpellIds;
        }

        public void SetActiveBuff(string spellTemplateId, int remainingTicks, int magnitude)
        {
            if (string.IsNullOrWhiteSpace(spellTemplateId))
                throw new ArgumentException("Spell templateId must be a non-empty stable id.", nameof(spellTemplateId));
            if (remainingTicks < 0)
                throw new ArgumentOutOfRangeException(nameof(remainingTicks), remainingTicks, "Remaining buff ticks must be zero or positive.");
            if (magnitude < 0)
                throw new ArgumentOutOfRangeException(nameof(magnitude), magnitude, "Buff magnitude must be zero or positive.");

            if (remainingTicks == 0)
            {
                _activeBuffsBySpellId.Remove(spellTemplateId);
                return;
            }

            _activeBuffsBySpellId[spellTemplateId] = new ShieldBuffEntry(remainingTicks, magnitude);
        }

        public void Clear(string spellTemplateId)
        {
            if (string.IsNullOrWhiteSpace(spellTemplateId))
                return;

            _activeBuffsBySpellId.Remove(spellTemplateId);
        }

        private readonly struct ShieldBuffEntry
        {
            public ShieldBuffEntry(int remainingTicks, int magnitude)
            {
                RemainingTicks = remainingTicks;
                Magnitude = magnitude;
            }

            public int RemainingTicks { get; }
            public int Magnitude { get; }
        }
    }
}
