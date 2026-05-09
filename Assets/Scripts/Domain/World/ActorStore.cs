using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;

// Design note:
// ActorStore is Faz 1's first Core Store. It replaces the named slice fields
// (Player, Talker, Merchant, Guard, Enemy on SliceWorldState) with a single
// dictionary keyed by ActorId. Pure Domain: no Unity references, no I/O,
// deterministic enumeration in insertion order.
// Roadmap reference: docs/ROADMAP.md Faz 1 (Core Store reset).
namespace EmberCrpg.Domain.World
{
    /// <summary>
    /// Dictionary-backed registry over <see cref="ActorRecord"/> keyed by
    /// <see cref="ActorId"/>. Default ids are rejected; insertion order is
    /// preserved for deterministic enumeration.
    /// </summary>
    public sealed class ActorStore
    {
        private readonly Dictionary<ActorId, ActorRecord> _byId = new Dictionary<ActorId, ActorRecord>();
        private readonly List<ActorId> _order = new List<ActorId>();

        /// <summary>Number of actor records currently held.</summary>
        public int Count => _byId.Count;

        /// <summary>
        /// Adds a record. Throws when the id is the empty sentinel, when the
        /// record is null, or when an id is already registered.
        /// </summary>
        public void Add(ActorRecord record)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));
            if (record.Id.IsEmpty)
                throw new ArgumentException("ActorId.Empty cannot be stored.", nameof(record));
            if (_byId.ContainsKey(record.Id))
                throw new InvalidOperationException($"ActorStore already contains {record.Id}.");

            _byId.Add(record.Id, record);
            _order.Add(record.Id);
        }

        /// <summary>Returns the record for the given id, or throws if missing.</summary>
        public ActorRecord Get(ActorId id)
        {
            if (id.IsEmpty)
                throw new ArgumentException("ActorId.Empty cannot be queried.", nameof(id));
            if (!_byId.TryGetValue(id, out var record))
                throw new KeyNotFoundException($"ActorStore has no record for {id}.");
            return record;
        }

        /// <summary>
        /// Tries to fetch the record for the given id. Returns false (and a
        /// null record) when the id is empty or not registered.
        /// </summary>
        public bool TryGet(ActorId id, out ActorRecord record)
        {
            if (id.IsEmpty)
            {
                record = null;
                return false;
            }
            return _byId.TryGetValue(id, out record);
        }

        /// <summary>True when the id is registered (false for the empty sentinel).</summary>
        public bool Contains(ActorId id)
        {
            return !id.IsEmpty && _byId.ContainsKey(id);
        }

        /// <summary>
        /// Removes the record for the given id. Returns false when the id is
        /// empty or not registered.
        /// </summary>
        public bool Remove(ActorId id)
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
        public IEnumerable<ActorRecord> Records
        {
            get
            {
                foreach (var id in _order)
                    yield return _byId[id];
            }
        }

        // Role-view shims: lay the rail for migrating SliceWorldState's
        // Player/Talker/Merchant/Guard/Enemy fields onto ActorStore. Concrete
        // consumer arrives in the next Faz 1 PR (SliceWorldState reads these
        // shims and then marks its named fields [Obsolete]). Per
        // DOCS/agent-rules-v2.md rule 4 (world-store promotion), no new
        // hard-coded slice fields may be added; these lookups are the
        // replacement path.

        /// <summary>
        /// Records carrying the requested role, in deterministic insertion
        /// order. Returns an empty sequence when no record matches; never
        /// allocates a list eagerly.
        /// </summary>
        public IEnumerable<ActorRecord> RecordsByRole(ActorRole role)
        {
            foreach (var id in _order)
            {
                var record = _byId[id];
                if (record.Role == role)
                    yield return record;
            }
        }

        /// <summary>
        /// First record carrying the requested role in insertion order.
        /// Throws <see cref="InvalidOperationException"/> when no record
        /// matches; mirrors the strictness of <see cref="Get"/>.
        /// </summary>
        public ActorRecord FirstByRole(ActorRole role)
        {
            foreach (var id in _order)
            {
                var record = _byId[id];
                if (record.Role == role)
                    return record;
            }
            throw new InvalidOperationException($"ActorStore has no record with role {role}.");
        }

        /// <summary>
        /// Tries to fetch the first record carrying the requested role in
        /// insertion order. Returns false (and a null record) when no record
        /// matches.
        /// </summary>
        public bool TryFirstByRole(ActorRole role, out ActorRecord record)
        {
            foreach (var id in _order)
            {
                var candidate = _byId[id];
                if (candidate.Role == role)
                {
                    record = candidate;
                    return true;
                }
            }
            record = null;
            return false;
        }
    }
}
