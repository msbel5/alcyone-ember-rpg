using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Core;

// Design note:
// FactionStore is Faz 1's SOCIETY-seed Core Store. It mirrors ActorStore /
// ItemStore / SiteStore so the four Faz 1 registries share one contract:
// dictionary-backed registry keyed by a value-typed Id, deterministic
// insertion-order enumeration, default-id rejection, no Unity, no I/O.
// Roadmap reference: docs/ROADMAP.md Faz 1 (Core Store reset);
// atom-map row: DOCS/sprint-faz-1-atom-map.md FactionStore sub-area.
namespace EmberCrpg.Domain.World
{
    /// <summary>
    /// Dictionary-backed registry over <see cref="FactionRecord"/> keyed by
    /// <see cref="FactionId"/>. Default ids are rejected; insertion order is
    /// preserved for deterministic enumeration.
    /// </summary>
    public sealed class FactionStore
    {
        private readonly Dictionary<FactionId, FactionRecord> _byId = new Dictionary<FactionId, FactionRecord>();
        private readonly List<FactionId> _order = new List<FactionId>();
        private readonly Dictionary<FactionPair, FactionReputation> _reputation = new Dictionary<FactionPair, FactionReputation>();

        /// <summary>Number of faction records currently held.</summary>
        public int Count => _byId.Count;

        /// <summary>
        /// Adds a record. Throws when the record is null, when its id is the
        /// empty sentinel, or when an id is already registered.
        /// </summary>
        public void Add(FactionRecord record)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));
            if (record.Id.IsEmpty)
                throw new ArgumentException("FactionId.Empty cannot be stored.", nameof(record));
            if (_byId.ContainsKey(record.Id))
                throw new InvalidOperationException($"FactionStore already contains {record.Id}.");

            _byId.Add(record.Id, record);
            _order.Add(record.Id);
        }

        /// <summary>Returns the record for the given id, or throws if missing.</summary>
        public FactionRecord Get(FactionId id)
        {
            if (id.IsEmpty)
                throw new ArgumentException("FactionId.Empty cannot be queried.", nameof(id));
            if (!_byId.TryGetValue(id, out var record))
                throw new KeyNotFoundException($"FactionStore has no record for {id}.");
            return record;
        }

        /// <summary>
        /// Tries to fetch the record for the given id. Returns false (and a
        /// null record) when the id is empty or not registered.
        /// </summary>
        public bool TryGet(FactionId id, out FactionRecord record)
        {
            if (id.IsEmpty)
            {
                record = null;
                return false;
            }
            return _byId.TryGetValue(id, out record);
        }

        /// <summary>True when the id is registered (false for the empty sentinel).</summary>
        public bool Contains(FactionId id)
        {
            return !id.IsEmpty && _byId.ContainsKey(id);
        }

        /// <summary>
        /// Removes the record for the given id. Returns false when the id is
        /// empty or not registered.
        /// </summary>
        public bool Remove(FactionId id)
        {
            if (id.IsEmpty)
                return false;
            if (!_byId.Remove(id))
                return false;
            _order.Remove(id);
            return true;
        }

        /// <summary>Drops every record.</summary>
        public void Clear()
        {
            _byId.Clear();
            _order.Clear();
        }

        /// <summary>
        /// Records in deterministic insertion order. Stable across enumeration
        /// because the underlying list mirrors Add/Remove operations.
        /// </summary>
        public IEnumerable<FactionRecord> Records
        {
            get
            {
                foreach (var id in _order)
                    yield return _byId[id];
            }
        }

        /// <summary>
        /// Sets the reputation between two factions. Symmetric: setting (a, b)
        /// also serves (b, a). Returns the store for chaining. Faz 6 Atom 3.
        /// </summary>
        public FactionStore WithReputation(FactionId a, FactionId b, FactionReputation reputation)
        {
            if (a.IsEmpty || b.IsEmpty)
                throw new ArgumentException("FactionId.Empty cannot participate in reputation.");
            if (a.Equals(b))
                throw new ArgumentException("Reputation must be between two distinct factions.");

            _reputation[FactionPair.Of(a, b)] = reputation;
            return this;
        }

        /// <summary>
        /// Returns the reputation between two factions, or <see cref="FactionReputation.Neutral"/>
        /// when no row exists. Symmetric lookup. Faz 6 Atom 3.
        /// </summary>
        public FactionReputation GetReputation(FactionId a, FactionId b)
        {
            if (a.IsEmpty || b.IsEmpty || a.Equals(b))
                return FactionReputation.Neutral;
            return _reputation.TryGetValue(FactionPair.Of(a, b), out var value)
                ? value
                : FactionReputation.Neutral;
        }

        /// <summary>Ordered pair key for symmetric reputation lookup.</summary>
        private readonly struct FactionPair : IEquatable<FactionPair>
        {
            private readonly FactionId _low;
            private readonly FactionId _high;

            private FactionPair(FactionId low, FactionId high)
            {
                _low = low;
                _high = high;
            }

            public static FactionPair Of(FactionId a, FactionId b)
            {
                return a.Value <= b.Value ? new FactionPair(a, b) : new FactionPair(b, a);
            }

            public bool Equals(FactionPair other) => _low.Equals(other._low) && _high.Equals(other._high);
            public override bool Equals(object obj) => obj is FactionPair other && Equals(other);
            public override int GetHashCode() => unchecked((_low.GetHashCode() * 397) ^ _high.GetHashCode());
        }
    }
}
