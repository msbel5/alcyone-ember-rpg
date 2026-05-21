using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Core;

// Design note:
// SiteStore is Faz 1's WORLD-box Core Store. It mirrors ActorStore's shape so
// the four Faz 1 registries (ActorStore / ItemStore / SiteStore / FactionStore)
// share one contract: dictionary-backed registry keyed by a value-typed Id,
// deterministic insertion-order enumeration, default-id rejection, no Unity,
// no I/O. Roadmap reference: docs/ROADMAP.md Faz 1 (Core Store reset);
// atom-map row: docs/sprint-faz-1-atom-map.md SiteStore sub-area.
namespace EmberCrpg.Domain.World
{
    /// <summary>
    /// Dictionary-backed registry over <see cref="SiteRecord"/> keyed by
    /// <see cref="SiteId"/>. Default ids are rejected; insertion order is
    /// preserved for deterministic enumeration.
    /// </summary>
    public sealed class SiteStore
    {
        private readonly Dictionary<SiteId, SiteRecord> _byId = new Dictionary<SiteId, SiteRecord>();
        private readonly List<SiteId> _order = new List<SiteId>();

        /// <summary>Number of site records currently held.</summary>
        public int Count => _byId.Count;

        /// <summary>
        /// Adds a record. Throws when the record is null, when its id is the
        /// empty sentinel, or when an id is already registered.
        /// </summary>
        public void Add(SiteRecord record)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));
            if (record.Id.IsEmpty)
                throw new ArgumentException("SiteId.Empty cannot be stored.", nameof(record));
            if (_byId.ContainsKey(record.Id))
                throw new InvalidOperationException($"SiteStore already contains {record.Id}.");

            _byId.Add(record.Id, record);
            _order.Add(record.Id);
        }

        /// <summary>Returns the record for the given id, or throws if missing.</summary>
        public SiteRecord Get(SiteId id)
        {
            if (id.IsEmpty)
                throw new ArgumentException("SiteId.Empty cannot be queried.", nameof(id));
            if (!_byId.TryGetValue(id, out var record))
                throw new KeyNotFoundException($"SiteStore has no record for {id}.");
            return record;
        }

        /// <summary>
        /// Tries to fetch the record for the given id. Returns false (and a
        /// null record) when the id is empty or not registered.
        /// </summary>
        public bool TryGet(SiteId id, out SiteRecord record)
        {
            if (id.IsEmpty)
            {
                record = null;
                return false;
            }
            return _byId.TryGetValue(id, out record);
        }

        /// <summary>True when the id is registered (false for the empty sentinel).</summary>
        public bool Contains(SiteId id)
        {
            return !id.IsEmpty && _byId.ContainsKey(id);
        }

        /// <summary>
        /// Removes the record for the given id. Returns false when the id is
        /// empty or not registered.
        /// </summary>
        public bool Remove(SiteId id)
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
        public IEnumerable<SiteRecord> Records
        {
            get
            {
                foreach (var id in _order)
                    yield return _byId[id];
            }
        }
    }
}
