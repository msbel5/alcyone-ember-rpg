using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using EmberCrpg.Domain.Core;

// Design note:
// FactionRecord is the Phase 1 SOCIETY-seed payload for a faction known to the world:
// a stable FactionId handle, a display name, and an ordered, immutable tag bag.
// Inputs: FactionId handle, display name, tag enumerable (defensive copy at construction).
// Outputs: immutable record consumed by FactionStore in a follow-up Phase 1 PR; no Unity,
// no I/O, no serialization concerns. Mirrors SiteRecord / ItemRecord's defensive
// constructor pattern so invariants are pinned at construction.
// Atom-map ref: docs/sprint-phase-1-atom-map.md FactionStore sub-area.
namespace EmberCrpg.Domain.World
{
    /// <summary>Pure record describing a faction registry entry by id, name, and tags.</summary>
    public sealed class FactionRecord
    {
        private readonly string[] _tags;
        // ReadOnlyCollection wrapper prevents callers from casting Tags back to string[]
        // and mutating the internal tag bag — IReadOnlyList<string> alone does not.
        private readonly ReadOnlyCollection<string> _tagsView;

        public FactionRecord(FactionId id, string name, IEnumerable<string> tags)
        {
            if (id.IsEmpty)
                throw new ArgumentException("FactionId.Empty cannot back a FactionRecord.", nameof(id));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Faction name is required.", nameof(name));
            if (tags == null)
                throw new ArgumentNullException(nameof(tags));

            var buffer = new List<string>();
            foreach (var tag in tags)
            {
                if (string.IsNullOrWhiteSpace(tag))
                    throw new ArgumentException("Faction tags cannot be blank or whitespace.", nameof(tags));
                buffer.Add(tag);
            }

            Id = id;
            Name = name;
            _tags = buffer.ToArray();
            _tagsView = new ReadOnlyCollection<string>(_tags);
        }

        public FactionId Id { get; }
        public string Name { get; }

        /// <summary>Insertion-ordered snapshot of the tags supplied at construction.</summary>
        public IReadOnlyList<string> Tags
        {
            get { return _tagsView; }
        }

        /// <summary>True when the supplied tag was provided at construction (case-sensitive match).</summary>
        public bool HasTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return false;

            for (int i = 0; i < _tags.Length; i++)
            {
                if (string.Equals(_tags[i], tag, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }
}
