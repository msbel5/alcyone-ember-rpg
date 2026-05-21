using System;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;

// Design note:
// SiteRecord is the Faz 1 pure-Domain payload for a region / settlement / dungeon.
// Inputs: SiteId handle, SiteKind category, display name, and grid bounds (min..max).
// Outputs: immutable record consumed by SiteStore in the next Faz 1 PR; no Unity,
// no I/O, no serialization concerns. Mirrors InventoryItem's defensive constructor
// pattern so invariants are pinned at construction.
// Atom-map ref: docs/sprint-faz-1-atom-map.md SiteStore sub-area.
namespace EmberCrpg.Domain.World
{
    /// <summary>Pure record describing a site (region / settlement / dungeon) by id, kind, name, and bounds.</summary>
    public sealed class SiteRecord
    {
        public SiteRecord(SiteId id, SiteKind kind, string name, GridPosition minBound, GridPosition maxBound)
        {
            if (id.IsEmpty)
                throw new ArgumentException("SiteId.Empty cannot back a SiteRecord.", nameof(id));
            if (kind == SiteKind.None)
                throw new ArgumentException("SiteKind.None is reserved as the empty sentinel.", nameof(kind));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Site name is required.", nameof(name));
            if (maxBound.X < minBound.X || maxBound.Y < minBound.Y)
                throw new ArgumentException("maxBound must be component-wise greater-or-equal to minBound.", nameof(maxBound));

            Id = id;
            Kind = kind;
            Name = name;
            MinBound = minBound;
            MaxBound = maxBound;
        }

        public SiteId Id { get; }
        public SiteKind Kind { get; }
        public string Name { get; }
        public GridPosition MinBound { get; }
        public GridPosition MaxBound { get; }

        /// <summary>True when the supplied grid coordinate falls inside the (inclusive) site bounds.</summary>
        public bool Contains(GridPosition position)
        {
            return position.X >= MinBound.X
                && position.X <= MaxBound.X
                && position.Y >= MinBound.Y
                && position.Y <= MaxBound.Y;
        }
    }
}
