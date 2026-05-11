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
    }
}
