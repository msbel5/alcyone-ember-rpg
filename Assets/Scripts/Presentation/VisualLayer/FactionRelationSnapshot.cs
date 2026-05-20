using System.Collections.Generic;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;

namespace EmberCrpg.Presentation.VisualLayer
{
    /// <summary>
    /// Read-only snapshot of faction-pair reputation for Unity HUD overlays.
    /// Pure C#: no UnityEngine, no mutation. Faz 11 Atom 4.
    /// </summary>
    public sealed class FactionRelationSnapshot
    {
        private readonly IReadOnlyList<FactionRelationRow> _rows;

        public FactionRelationSnapshot(IReadOnlyList<FactionRelationRow> rows)
        {
            _rows = rows ?? new FactionRelationRow[0];
        }

        public IReadOnlyList<FactionRelationRow> Rows => _rows;

        /// <summary>
        /// Builds a snapshot of every (a, b) faction pair from the given store.
        /// Pairs are emitted once per unordered combination, in insertion order
        /// of the FactionStore (deterministic).
        /// </summary>
        public static FactionRelationSnapshot FromStore(FactionStore store)
        {
            var rows = new List<FactionRelationRow>();
            if (store == null)
                return new FactionRelationSnapshot(rows);

            var seen = new HashSet<(ulong, ulong)>();
            var records = new List<FactionRecord>(store.Records);
            for (var i = 0; i < records.Count; i++)
            {
                for (var j = i + 1; j < records.Count; j++)
                {
                    var a = records[i];
                    var b = records[j];
                    var key = a.Id.Value <= b.Id.Value ? (a.Id.Value, b.Id.Value) : (b.Id.Value, a.Id.Value);
                    if (seen.Contains(key))
                        continue;
                    seen.Add(key);

                    var reputation = store.GetReputation(a.Id, b.Id);
                    rows.Add(new FactionRelationRow(
                        a.Id, b.Id,
                        a.Name ?? string.Empty,
                        b.Name ?? string.Empty,
                        reputation.Value,
                        reputation.ToRelationKind().Code));
                }
            }
            return new FactionRelationSnapshot(rows);
        }
    }

    /// <summary>One pair of factions and their current relation for HUD display.</summary>
    public readonly struct FactionRelationRow
    {
        public FactionRelationRow(FactionId a, FactionId b, string aName, string bName, int reputation, string relationCode)
        {
            FactionA = a;
            FactionB = b;
            FactionAName = aName ?? string.Empty;
            FactionBName = bName ?? string.Empty;
            Reputation = reputation;
            RelationCode = relationCode ?? string.Empty;
        }

        public FactionId FactionA { get; }
        public FactionId FactionB { get; }
        public string FactionAName { get; }
        public string FactionBName { get; }
        public int Reputation { get; }
        public string RelationCode { get; }
    }
}
