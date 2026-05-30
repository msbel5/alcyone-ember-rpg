using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;

// Design note:
// ItemStore is Phase 1's MATTER-box Core Store. It mirrors ActorStore/SiteStore so
// the four Phase 1 registries (ActorStore / ItemStore / SiteStore / FactionStore)
// share one contract: dictionary-backed registry keyed by a value-typed Id,
// deterministic insertion-order enumeration, default-id rejection, no Unity,
// no I/O. Roadmap reference: docs/ROADMAP.md Phase 1 (Core Store reset);
// atom-map row: docs/sprint-phase-1-atom-map.md ItemStore sub-area.
namespace EmberCrpg.Domain.World
{
    /// <summary>
    /// Dictionary-backed registry over <see cref="ItemRecord"/> keyed by
    /// <see cref="ItemId"/>. Default ids are rejected; insertion order is
    /// preserved for deterministic enumeration.
    /// </summary>
    public sealed class ItemStore
    {
        private readonly Dictionary<ItemId, ItemRecord> _byId = new Dictionary<ItemId, ItemRecord>();
        private readonly List<ItemId> _order = new List<ItemId>();

        /// <summary>Number of item records currently held.</summary>
        public int Count => _byId.Count;

        /// <summary>
        /// Adds a record. Throws when the record is null, when its id is the
        /// empty sentinel, or when an id is already registered.
        /// </summary>
        public void Add(ItemRecord record)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));
            if (record.Id.IsEmpty)
                throw new ArgumentException("ItemId.Empty cannot be stored.", nameof(record));
            if (_byId.ContainsKey(record.Id))
                throw new InvalidOperationException($"ItemStore already contains {record.Id}.");

            _byId.Add(record.Id, record);
            _order.Add(record.Id);
        }

        /// <summary>Returns the record for the given id, or throws if missing.</summary>
        public ItemRecord Get(ItemId id)
        {
            if (id.IsEmpty)
                throw new ArgumentException("ItemId.Empty cannot be queried.", nameof(id));
            if (!_byId.TryGetValue(id, out var record))
                throw new KeyNotFoundException($"ItemStore has no record for {id}.");
            return record;
        }

        /// <summary>
        /// Tries to fetch the record for the given id. Returns false (and a
        /// null record) when the id is empty or not registered.
        /// </summary>
        public bool TryGet(ItemId id, out ItemRecord record)
        {
            if (id.IsEmpty)
            {
                record = null;
                return false;
            }
            return _byId.TryGetValue(id, out record);
        }

        /// <summary>True when the id is registered (false for the empty sentinel).</summary>
        public bool Contains(ItemId id)
        {
            return !id.IsEmpty && _byId.ContainsKey(id);
        }

        /// <summary>
        /// Removes the record for the given id. Returns false when the id is
        /// empty or not registered.
        /// </summary>
        public bool Remove(ItemId id)
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
        public IEnumerable<ItemRecord> Records
        {
            get
            {
                foreach (var id in _order)
                    yield return _byId[id];
            }
        }
    }
}
