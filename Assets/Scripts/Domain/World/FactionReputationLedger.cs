using System;
using System.Collections.Generic;
using System.Linq;

// Design note:
// FactionReputationLedger keeps Sprint 3's first persistent reputation hooks tiny and deterministic.
// Inputs: faction id + delta mutations from pure simulation services.
// Outputs: clamped saveable standings that can influence NPC attitude and warning logic.
// Bible reference: ARCHITECTURE.md NPC reputation fields, Sprint 3 faction hooks.
namespace EmberCrpg.Domain.World
{
    /// <summary>Serializable key/value reputation entry for one faction.</summary>
    public sealed class FactionReputationEntry
    {
        public FactionReputationEntry(string factionId, int score)
        {
            FactionId = factionId ?? string.Empty;
            Score = score;
        }

        public string FactionId { get; }
        public int Score { get; }
    }

    /// <summary>Mutable faction reputation ledger with small deterministic clamps.</summary>
    public sealed class FactionReputationLedger
    {
        private const int MinScore = -3;
        private const int MaxScore = 3;
        private readonly Dictionary<string, int> _scores = new Dictionary<string, int>(StringComparer.Ordinal);

        public IReadOnlyList<FactionReputationEntry> Entries => _scores
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => new FactionReputationEntry(pair.Key, pair.Value))
            .ToArray();

        public int Get(string factionId)
        {
            int score;
            return !string.IsNullOrWhiteSpace(factionId) && _scores.TryGetValue(factionId, out score)
                ? score
                : 0;
        }

        public int Adjust(string factionId, int delta)
        {
            return Set(factionId, Get(factionId) + delta);
        }

        public int Set(string factionId, int score)
        {
            if (string.IsNullOrWhiteSpace(factionId))
                return 0;

            var clamped = Math.Max(MinScore, Math.Min(MaxScore, score));
            if (clamped == 0)
                _scores.Remove(factionId);
            else
                _scores[factionId] = clamped;
            return clamped;
        }

        public void Replace(IEnumerable<FactionReputationEntry> entries)
        {
            _scores.Clear();
            if (entries == null)
                return;

            foreach (var entry in entries)
                Set(entry.FactionId, entry.Score);
        }
    }
}
