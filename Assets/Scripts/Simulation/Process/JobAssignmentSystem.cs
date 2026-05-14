using System;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;

// Design note:
// JobAssignmentSystem is Faz 3's first PROCESS/LIVING bridge. This pass only
// claims pending JobBoard entries and writes the actor schedule target. It does
// not start recipes, tick work orders, emit EventLog rows, or persist jobs; those
// are later atoms in DOCS/sprint-faz-3-atom-map.md.
namespace EmberCrpg.Simulation.Process
{
    /// <summary>
    /// Deterministic assignment service that matches idle actors to pending jobs.
    /// </summary>
    public sealed class JobAssignmentSystem
    {
        /// <summary>
        /// Claims at most one eligible job for the best available actor/job pair.
        /// Actor preference priority wins first, then request priority, actor order,
        /// and job insertion order. Returns false without mutation when no pair can
        /// work right now.
        /// </summary>
        public bool TryAssignNext(ActorStore actors, JobBoard board, WorksiteStore worksites, out JobAssignmentResult result)
        {
            if (actors == null)
                throw new ArgumentNullException(nameof(actors));
            if (board == null)
                throw new ArgumentNullException(nameof(board));
            if (worksites == null)
                throw new ArgumentNullException(nameof(worksites));

            result = default;
            Candidate best = null;
            var actorOrder = 0;

            foreach (var actor in actors.Records)
            {
                if (ActorAlreadyHasPendingClaim(actor, board))
                {
                    actorOrder++;
                    continue;
                }

                var jobOrder = 0;
                foreach (var request in board.Requests)
                {
                    if (!board.IsClaimed(request.Id)
                        && TryBuildCandidate(actor, actorOrder, request, jobOrder, worksites, out var candidate)
                        && (best == null || candidate.CompareTo(best) < 0))
                    {
                        best = candidate;
                    }

                    jobOrder++;
                }

                actorOrder++;
            }

            if (best == null)
                return false;

            if (!board.TryClaim(best.Request.Id, best.Actor.Id, out var claimedRequest))
                return false;

            var assigned = ActorScheduleState.Assigned(
                claimedRequest.Id,
                claimedRequest.SiteId,
                claimedRequest.WorksitePosition);
            best.Actor.ApplyScheduleState(assigned);
            result = new JobAssignmentResult(best.Actor.Id, claimedRequest.Id, claimedRequest.SiteId, claimedRequest.WorksitePosition);
            return true;
        }

        /// <summary>
        /// Returns true when an actor is alive, idle, explicitly enabled for the job
        /// kind, and the requested active worksite exists. Recipe input checks belong
        /// to the later StartRecipeForClaim atom so this method stays mutation-free.
        /// </summary>
        public bool CanActorWorkJob(ActorRecord actor, JobRequest request, WorksiteStore worksites)
        {
            if (actor == null)
                throw new ArgumentNullException(nameof(actor));
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (worksites == null)
                throw new ArgumentNullException(nameof(worksites));

            return actor.IsAlive
                && actor.ScheduleState.IsIdle
                && TryGetActivePreference(actor, request.Kind, out _)
                && TryGetActiveMatchingWorksite(request, worksites, out _);
        }

        private static bool ActorAlreadyHasPendingClaim(ActorRecord actor, JobBoard board)
        {
            foreach (var request in board.Requests)
            {
                if (board.GetClaimedBy(request.Id) == actor.Id)
                    return true;
            }

            return false;
        }

        private static bool TryBuildCandidate(
            ActorRecord actor,
            int actorOrder,
            JobRequest request,
            int jobOrder,
            WorksiteStore worksites,
            out Candidate candidate)
        {
            candidate = null;

            if (!actor.IsAlive || !actor.ScheduleState.IsIdle)
                return false;
            if (!TryGetActivePreference(actor, request.Kind, out var preference))
                return false;
            if (!TryGetActiveMatchingWorksite(request, worksites, out _))
                return false;

            candidate = new Candidate(actor, request, preference.Priority, actorOrder, jobOrder);
            return true;
        }

        private static bool TryGetActivePreference(ActorRecord actor, JobKind kind, out ActorJobPreference preference)
        {
            foreach (var candidate in actor.JobPreferences)
            {
                if (candidate.Kind == kind && candidate.IsEnabled)
                {
                    preference = candidate;
                    return true;
                }
            }

            preference = default;
            return false;
        }

        private static bool TryGetActiveMatchingWorksite(JobRequest request, WorksiteStore worksites, out WorksiteRecord worksite)
        {
            if (!worksites.TryGet(request.SiteId, request.WorksitePosition, out worksite))
                return false;

            return worksite.IsActive && worksite.Kind == request.WorksiteKind;
        }

        private sealed class Candidate : IComparable<Candidate>
        {
            public Candidate(ActorRecord actor, JobRequest request, JobPriority actorPriority, int actorOrder, int jobOrder)
            {
                Actor = actor;
                Request = request;
                ActorPriority = actorPriority;
                ActorOrder = actorOrder;
                JobOrder = jobOrder;
            }

            public ActorRecord Actor { get; }
            public JobRequest Request { get; }
            public JobPriority ActorPriority { get; }
            public int ActorOrder { get; }
            public int JobOrder { get; }

            public int CompareTo(Candidate other)
            {
                var actorPriority = ActorPriority.CompareTo(other.ActorPriority);
                if (actorPriority != 0)
                    return actorPriority;

                var requestPriority = Request.Priority.CompareTo(other.Request.Priority);
                if (requestPriority != 0)
                    return requestPriority;

                var actorOrder = ActorOrder.CompareTo(other.ActorOrder);
                if (actorOrder != 0)
                    return actorOrder;

                return JobOrder.CompareTo(other.JobOrder);
            }
        }
    }

    /// <summary>Small immutable result describing one successful job claim.</summary>
    public readonly struct JobAssignmentResult : IEquatable<JobAssignmentResult>
    {
        public JobAssignmentResult(ActorId actorId, JobId jobId, SiteId siteId, GridPosition worksitePosition)
        {
            ActorId = actorId;
            JobId = jobId;
            SiteId = siteId;
            WorksitePosition = worksitePosition;
        }

        /// <summary>Actor that now owns the claim.</summary>
        public ActorId ActorId { get; }

        /// <summary>Claimed pending job id.</summary>
        public JobId JobId { get; }

        /// <summary>Site containing the target worksite.</summary>
        public SiteId SiteId { get; }

        /// <summary>Grid cell the actor should travel toward.</summary>
        public GridPosition WorksitePosition { get; }

        /// <summary>Returns true when all assignment fields match.</summary>
        public bool Equals(JobAssignmentResult other)
        {
            return ActorId == other.ActorId
                && JobId == other.JobId
                && SiteId == other.SiteId
                && WorksitePosition.Equals(other.WorksitePosition);
        }

        /// <summary>Returns true when the object is a matching assignment result.</summary>
        public override bool Equals(object obj)
        {
            return obj is JobAssignmentResult other && Equals(other);
        }

        /// <summary>Returns a hash code derived from stable assignment fields.</summary>
        public override int GetHashCode()
        {
            return HashCode.Combine(ActorId, JobId, SiteId, WorksitePosition);
        }
    }
}
