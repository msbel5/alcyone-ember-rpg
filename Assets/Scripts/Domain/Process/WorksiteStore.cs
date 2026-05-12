using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;

// Design note:
// WorksiteStore is Faz 2's WORLD/PROCESS registry for site-cell worksites.
// It keeps WorksiteRecord lookup deterministic and pure: no Unity references,
// no I/O, no ticking recipes, no inventory mutation, and no EventLog writes.
// RecipeSystem will consume this store in the next visible Faz 2 slice.
// Atom-map ref: DOCS/sprint-faz-2-atom-map.md Worksite state sub-area.
namespace EmberCrpg.Domain.Process
{
    /// <summary>
    /// Dictionary-backed registry over <see cref="WorksiteRecord"/> keyed by
    /// site id plus grid position. Default site ids are rejected; insertion
    /// order is preserved for deterministic enumeration.
    /// </summary>
    public sealed class WorksiteStore
    {
        private readonly Dictionary<WorksiteKey, WorksiteRecord> _byKey = new Dictionary<WorksiteKey, WorksiteRecord>();
        private readonly List<WorksiteKey> _order = new List<WorksiteKey>();

        /// <summary>Number of worksite records currently held.</summary>
        public int Count => _byKey.Count;

        /// <summary>
        /// Adds a worksite. Throws when the record is null, when its site id
        /// is empty, or when another worksite already occupies the same site cell.
        /// </summary>
        public void Add(WorksiteRecord record)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));

            var key = MakeKey(record.SiteId, record.Position, nameof(record));
            if (_byKey.ContainsKey(key))
                throw new InvalidOperationException($"WorksiteStore already contains a worksite at {record.SiteId} {record.Position}.");

            _byKey.Add(key, record);
            _order.Add(key);
        }

        /// <summary>Returns the worksite at the given site cell, or throws if missing.</summary>
        public WorksiteRecord Get(SiteId siteId, GridPosition position)
        {
            var key = MakeKey(siteId, position, nameof(siteId));
            if (!_byKey.TryGetValue(key, out var record))
                throw new KeyNotFoundException($"WorksiteStore has no worksite at {siteId} {position}.");
            return record;
        }

        /// <summary>
        /// Tries to fetch a worksite by site cell. Returns false (and a null
        /// record) when the site id is empty or no worksite is registered.
        /// </summary>
        public bool TryGet(SiteId siteId, GridPosition position, out WorksiteRecord record)
        {
            if (siteId.IsEmpty)
            {
                record = null;
                return false;
            }

            return _byKey.TryGetValue(new WorksiteKey(siteId, position), out record);
        }

        /// <summary>True when a worksite is registered at the given site cell.</summary>
        public bool Contains(SiteId siteId, GridPosition position)
        {
            return !siteId.IsEmpty && _byKey.ContainsKey(new WorksiteKey(siteId, position));
        }

        /// <summary>
        /// Removes a worksite by site cell. Returns false when the site id is
        /// empty or no worksite is registered at that cell.
        /// </summary>
        public bool Remove(SiteId siteId, GridPosition position)
        {
            if (siteId.IsEmpty)
                return false;

            var key = new WorksiteKey(siteId, position);
            if (!_byKey.Remove(key))
                return false;

            _order.Remove(key);
            return true;
        }

        /// <summary>Drops every registered worksite.</summary>
        public void Clear()
        {
            _byKey.Clear();
            _order.Clear();
        }

        /// <summary>
        /// Worksites in deterministic insertion order. Stable across enumeration
        /// because the underlying list mirrors Add/Remove operations.
        /// </summary>
        public IEnumerable<WorksiteRecord> Records
        {
            get
            {
                foreach (var key in _order)
                    yield return _byKey[key];
            }
        }

        private static WorksiteKey MakeKey(SiteId siteId, GridPosition position, string paramName)
        {
            if (siteId.IsEmpty)
                throw new ArgumentException("SiteId.Empty cannot identify a worksite cell.", paramName);
            return new WorksiteKey(siteId, position);
        }

        private readonly struct WorksiteKey : IEquatable<WorksiteKey>
        {
            public WorksiteKey(SiteId siteId, GridPosition position)
            {
                SiteId = siteId;
                Position = position;
            }

            public SiteId SiteId { get; }

            public GridPosition Position { get; }

            public bool Equals(WorksiteKey other)
            {
                return SiteId.Equals(other.SiteId) && Position.Equals(other.Position);
            }

            public override bool Equals(object obj)
            {
                return obj is WorksiteKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(SiteId, Position);
            }
        }
    }
}
