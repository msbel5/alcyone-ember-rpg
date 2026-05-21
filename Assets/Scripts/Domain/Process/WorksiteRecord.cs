using System;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;

// Design note:
// WorksiteRecord is a pure site-cell component for Faz 2. It identifies a
// worksite by site id, grid position, typed kind, and active flag without
// mutating inventory, ticking recipes, logging events, or serializing state.
// WorksiteStore and RecipeSystem will own lookup/progress behaviour later.
// Atom-map ref: docs/sprint-faz-2-atom-map.md Worksite state sub-area.
namespace EmberCrpg.Domain.Process
{
    /// <summary>
    /// Immutable pure-Domain worksite component attached to a site grid position.
    /// </summary>
    public sealed class WorksiteRecord
    {
        public WorksiteRecord(SiteId siteId, GridPosition position, WorksiteKind kind, bool isActive)
        {
            if (siteId.IsEmpty)
                throw new ArgumentException("SiteId.Empty cannot back a WorksiteRecord.", nameof(siteId));
            if (kind == WorksiteKind.None)
                throw new ArgumentException("WorksiteKind.None is reserved as the empty sentinel.", nameof(kind));

            SiteId = siteId;
            Position = position;
            Kind = kind;
            IsActive = isActive;
        }

        /// <summary>Site containing this worksite cell.</summary>
        public SiteId SiteId { get; }

        /// <summary>Grid position of this worksite within the site.</summary>
        public GridPosition Position { get; }

        /// <summary>Typed worksite category used by recipe matching.</summary>
        public WorksiteKind Kind { get; }

        /// <summary>Whether this worksite is currently eligible for recipe work.</summary>
        public bool IsActive { get; }

        /// <summary>
        /// Returns an equivalent record with a different active flag for store-level replacement.
        /// </summary>
        public WorksiteRecord WithActive(bool isActive)
        {
            return new WorksiteRecord(SiteId, Position, Kind, isActive);
        }
    }
}
