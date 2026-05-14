using System;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;

// Design note:
// JobRequest is Faz 3's smallest PROCESS-box pending work row. It binds one
// stable job handle to a concrete recipe, worksite cell, requester, category,
// priority, and quantity without assigning actors, mutating inventory, ticking
// recipes, serializing state, or emitting EventLog lines. JobBoard owns the
// deterministic pending/claimed lifecycle in the sibling atom.
// Atom-map ref: DOCS/sprint-faz-3-atom-map.md Job board state.
namespace EmberCrpg.Domain.Process
{
    /// <summary>
    /// Immutable pure-Domain work request consumed by JobBoard and, later,
    /// JobAssignmentSystem.
    /// </summary>
    public sealed class JobRequest
    {
        public JobRequest(
            JobId id,
            RecipeId recipeId,
            SiteId siteId,
            GridPosition worksitePosition,
            WorksiteKind worksiteKind,
            JobKind kind,
            JobPriority priority,
            int quantity,
            ActorId requesterId)
        {
            if (id.IsEmpty)
                throw new ArgumentException("JobRequest requires a non-empty job id.", nameof(id));
            if (recipeId.IsEmpty)
                throw new ArgumentException("JobRequest requires a non-empty recipe id.", nameof(recipeId));
            if (siteId.IsEmpty)
                throw new ArgumentException("JobRequest requires a non-empty site id.", nameof(siteId));
            if (worksiteKind == WorksiteKind.None)
                throw new ArgumentException("JobRequest requires a concrete worksite kind.", nameof(worksiteKind));
            if (kind == JobKind.None)
                throw new ArgumentException("JobKind.None cannot back a JobRequest.", nameof(kind));
            if (!priority.IsActive)
                throw new ArgumentException("JobRequest priority must be active.", nameof(priority));
            if (quantity <= 0)
                throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "JobRequest quantity must be positive.");
            if (requesterId.IsEmpty)
                throw new ArgumentException("JobRequest requires a non-empty requester id.", nameof(requesterId));

            Id = id;
            RecipeId = recipeId;
            SiteId = siteId;
            WorksitePosition = worksitePosition;
            WorksiteKind = worksiteKind;
            Kind = kind;
            Priority = priority;
            Quantity = quantity;
            RequesterId = requesterId;
        }

        /// <summary>Stable handle for this pending job.</summary>
        public JobId Id { get; }

        /// <summary>Recipe this request should execute once assigned.</summary>
        public RecipeId RecipeId { get; }

        /// <summary>Site containing the target worksite cell.</summary>
        public SiteId SiteId { get; }

        /// <summary>Grid position of the target worksite within the site.</summary>
        public GridPosition WorksitePosition { get; }

        /// <summary>Typed worksite category required by the request.</summary>
        public WorksiteKind WorksiteKind { get; }

        /// <summary>Actor preference category used for later matching.</summary>
        public JobKind Kind { get; }

        /// <summary>Request-level priority; lower active values are handled first.</summary>
        public JobPriority Priority { get; }

        /// <summary>Positive number of recipe executions requested.</summary>
        public int Quantity { get; }

        /// <summary>Actor or system proxy that created the request.</summary>
        public ActorId RequesterId { get; }
    }
}
