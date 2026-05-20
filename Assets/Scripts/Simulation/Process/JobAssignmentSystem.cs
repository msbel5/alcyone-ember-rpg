using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;

// Design note:
// JobAssignmentSystem is Faz 3's PROCESS/LIVING bridge. It claims pending
// JobBoard entries, writes the actor schedule target, and starts the first
// active RecipeWorkOrder for a claimed job. TickAssignedJobs advances active
// recipe work, emits job-specific EventLog rows, closes completed JobBoard
// entries, and idles the claimed actor. Persistence remains a later atom in
// DOCS/sprint-faz-3-atom-map.md.
namespace EmberCrpg.Simulation.Process
{
    /// <summary>
    /// Deterministic assignment service that matches idle actors to pending jobs.
    /// </summary>
    public sealed class JobAssignmentSystem
    {
        private readonly Dictionary<JobId, RecipeWorkOrder> _activeOrders = new Dictionary<JobId, RecipeWorkOrder>();
        private readonly Dictionary<JobId, int> _completedExecutionCounts = new Dictionary<JobId, int>();

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

            return TryClaimCandidate(board, best, out result);
        }

        /// <summary>
        /// Claims at most one eligible job and appends one JobAssigned event when
        /// the claim succeeds. The caller supplies deterministic game time so the
        /// log can line up with the simulation tick that performed assignment.
        /// This overload also emits JobRefused events when actors are eligible but
        /// refuse to work due to needs/mood.
        /// </summary>
        public bool TryAssignNext(
            ActorStore actors,
            JobBoard board,
            WorksiteStore worksites,
            WorldEventLog eventLog,
            GameTime now,
            out JobAssignmentResult result)
        {
            if (actors == null)
                throw new ArgumentNullException(nameof(actors));
            if (board == null)
                throw new ArgumentNullException(nameof(board));
            if (worksites == null)
                throw new ArgumentNullException(nameof(worksites));
            if (eventLog == null)
                throw new ArgumentNullException(nameof(eventLog));

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
                        && TryBuildCandidate(
                            actor,
                            actorOrder,
                            request,
                            jobOrder,
                            worksites,
                            eventLog,
                            now,
                            out var candidate)
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

            if (!TryClaimCandidate(board, best, out result))
                return false;

            AppendJobAssignedEvent(eventLog, now, result);
            return true;
        }

        /// <summary>
        /// Returns true when an actor is alive, idle, explicitly enabled for the job
        /// kind, and the requested active worksite exists. This legacy overload keeps
        /// assignment-only callsites stable; recipe-aware callers should use the
        /// overload that also receives a recipe and inventory.
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
                && TryGetActiveMatchingWorksite(request, worksites, out _)
                && !IsRefusing(actor);
        }

        /// <summary>
        /// Returns true when the actor/job/worksite checks pass and the requested
        /// recipe can start with the current inventory. The inventory is cloned for
        /// the recipe preflight so eligibility checks never consume player stock.
        /// </summary>
        public bool CanActorWorkJob(
            ActorRecord actor,
            JobRequest request,
            WorksiteStore worksites,
            RecipeDef recipe,
            InventoryState inventory)
        {
            if (recipe == null)
                throw new ArgumentNullException(nameof(recipe));
            if (inventory == null)
                throw new ArgumentNullException(nameof(inventory));

            if (!CanActorWorkJob(actor, request, worksites))
                return false;
            if (recipe.Id != request.RecipeId)
                return false;

            var recipeSystem = new RecipeSystem();
            return CanStartRequestedQuantity(recipeSystem, request, worksites, recipe, inventory, actor.Id);
        }

        /// <summary>
        /// Starts the recipe for an already-claimed pending job and tracks the
        /// resulting active work order by job id. Returns false without mutation
        /// when the job is missing, unclaimed, already active, has no live actor,
        /// mismatches the recipe, or cannot consume the recipe inputs.
        /// </summary>
        public bool StartRecipeForClaim(
            ActorStore actors,
            JobBoard board,
            WorksiteStore worksites,
            RecipeDef recipe,
            InventoryState inventory,
            JobId jobId,
            out JobRecipeStartResult result)
        {
            if (actors == null)
                throw new ArgumentNullException(nameof(actors));
            if (board == null)
                throw new ArgumentNullException(nameof(board));
            if (worksites == null)
                throw new ArgumentNullException(nameof(worksites));
            if (recipe == null)
                throw new ArgumentNullException(nameof(recipe));
            if (inventory == null)
                throw new ArgumentNullException(nameof(inventory));

            result = default;

            if (jobId.IsEmpty || _activeOrders.ContainsKey(jobId))
                return false;
            if (!board.TryGet(jobId, out var request))
                return false;
            if (request.RecipeId != recipe.Id)
                return false;

            var actorId = board.GetClaimedBy(jobId);
            if (actorId.IsEmpty || !actors.TryGet(actorId, out var actor) || !actor.IsAlive)
                return false;
            if (!actor.ScheduleState.IsIdle && actor.ScheduleState.CurrentJobId != jobId)
                return false;
            if (!TryGetActivePreference(actor, request.Kind, out _))
                return false;
            if (!TryGetActiveMatchingWorksite(request, worksites, out _))
                return false;

            var recipeSystem = new RecipeSystem();
            if (!CanStartRequestedQuantity(recipeSystem, request, worksites, recipe, inventory, actorId))
                return false;

            if (!recipeSystem.TryStart(
                recipe,
                worksites,
                request.SiteId,
                request.WorksitePosition,
                inventory,
                actorId,
                out var order))
            {
                return false;
            }

            _activeOrders.Add(jobId, order);
            _completedExecutionCounts[jobId] = 0;
            result = new JobRecipeStartResult(actorId, jobId, request.SiteId, request.WorksitePosition, order);
            return true;
        }

        /// <summary>Returns the active recipe work order for a claimed job.</summary>
        public bool TryGetActiveWorkOrder(JobId jobId, out RecipeWorkOrder order)
        {
            if (jobId.IsEmpty)
            {
                order = null;
                return false;
            }

            return _activeOrders.TryGetValue(jobId, out order);
        }

        /// <summary>
        /// Advances every active recipe work order by one tick. When RecipeSystem
        /// emits RecipeCompleted for an order, multi-quantity jobs immediately
        /// start their next execution and stay claimed; the JobBoard row is only
        /// completed, removed, and idled after the requested quantity finishes.
        /// </summary>
        public int TickAssignedJobs(
            ActorStore actors,
            JobBoard board,
            WorksiteStore worksites,
            InventoryState inventory,
            WorldEventLog eventLog,
            Func<RecipeOutput, InventoryItem> createOutput)
        {
            return TickAssignedJobs(
                actors,
                board,
                worksites,
                inventory,
                eventLog,
                default(GameTime),
                emitJobEvents: false,
                createOutput: createOutput);
        }

        /// <summary>
        /// Advances active recipe work orders and appends JobCompleted rows for
        /// board entries whose requested quantity has fully completed.
        /// </summary>
        public int TickAssignedJobs(
            ActorStore actors,
            JobBoard board,
            WorksiteStore worksites,
            InventoryState inventory,
            WorldEventLog eventLog,
            GameTime now,
            Func<RecipeOutput, InventoryItem> createOutput)
        {
            return TickAssignedJobs(
                actors,
                board,
                worksites,
                inventory,
                eventLog,
                now,
                emitJobEvents: true,
                createOutput: createOutput);
        }

        private int TickAssignedJobs(
            ActorStore actors,
            JobBoard board,
            WorksiteStore worksites,
            InventoryState inventory,
            WorldEventLog eventLog,
            GameTime now,
            bool emitJobEvents,
            Func<RecipeOutput, InventoryItem> createOutput)
        {
            if (actors == null)
                throw new ArgumentNullException(nameof(actors));
            if (board == null)
                throw new ArgumentNullException(nameof(board));
            if (worksites == null)
                throw new ArgumentNullException(nameof(worksites));
            if (inventory == null)
                throw new ArgumentNullException(nameof(inventory));
            if (eventLog == null)
                throw new ArgumentNullException(nameof(eventLog));
            if (createOutput == null)
                throw new ArgumentNullException(nameof(createOutput));

            if (_activeOrders.Count == 0)
                return 0;

            var recipeSystem = new RecipeSystem();
            var completed = new List<JobId>();

            foreach (var pair in _activeOrders)
            {
                if (recipeSystem.Tick(pair.Value, inventory, eventLog, createOutput))
                    completed.Add(pair.Key);
            }

            var completedJobs = 0;
            foreach (var jobId in completed)
            {
                if (!board.TryGet(jobId, out var request))
                {
                    _activeOrders.Remove(jobId);
                    _completedExecutionCounts.Remove(jobId);
                    continue;
                }

                var claimedBy = board.GetClaimedBy(jobId);
                var completedExecutions = GetCompletedExecutionCount(jobId) + 1;
                if (completedExecutions < request.Quantity)
                {
                    if (claimedBy.IsEmpty)
                        throw new InvalidOperationException($"Cannot continue unclaimed batch job {jobId.Value}.");
                    if (!_activeOrders.TryGetValue(jobId, out var finishedOrder))
                        throw new InvalidOperationException($"Missing completed work order for batch job {jobId.Value}.");
                    if (!recipeSystem.TryStart(
                        finishedOrder.Recipe,
                        worksites,
                        request.SiteId,
                        request.WorksitePosition,
                        inventory,
                        claimedBy,
                        out var nextOrder))
                    {
                        throw new InvalidOperationException($"Cannot start next execution for batch job {jobId.Value}.");
                    }

                    _activeOrders[jobId] = nextOrder;
                    _completedExecutionCounts[jobId] = completedExecutions;
                    continue;
                }

                board.Complete(jobId);
                if (emitJobEvents)
                {
                    eventLog.Append(new WorldEvent(
                        now,
                        WorldEventKind.JobCompleted,
                        claimedBy,
                        request.SiteId,
                        $"job_completed:{request.Id.Value}",
                        new ReasonTrace(new[]
                        {
                            $"job:{request.Id.Value}",
                            $"recipe:{request.RecipeId.Value}",
                            $"quantity:{request.Quantity}",
                            $"worksite:{request.WorksiteKind}",
                        })));
                }
                _activeOrders.Remove(jobId);
                _completedExecutionCounts.Remove(jobId);
                completedJobs++;

                if (!claimedBy.IsEmpty
                    && actors.TryGet(claimedBy, out var actor)
                    && actor.ScheduleState.CurrentJobId == jobId)
                {
                    actor.ApplyScheduleState(ActorScheduleState.Idle);
                }
            }

            return completedJobs;
        }

        /// <summary>
        /// Compatibility overload for single-execution jobs. Batch jobs should call
        /// the WorksiteStore overload so the next execution can safely consume inputs.
        /// </summary>
        public int TickAssignedJobs(
            ActorStore actors,
            JobBoard board,
            InventoryState inventory,
            WorldEventLog eventLog,
            Func<RecipeOutput, InventoryItem> createOutput)
        {
            foreach (var pair in _activeOrders)
            {
                if (board.TryGet(pair.Key, out var request) && GetCompletedExecutionCount(pair.Key) + 1 < request.Quantity)
                    throw new InvalidOperationException("Batch jobs require the TickAssignedJobs overload that receives WorksiteStore.");
            }

            return TickAssignedJobs(actors, board, new WorksiteStore(), inventory, eventLog, createOutput);
        }

        private int GetCompletedExecutionCount(JobId jobId)
        {
            return _completedExecutionCounts.TryGetValue(jobId, out var count) ? count : 0;
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
            return TryBuildCandidate(
                actor,
                actorOrder,
                request,
                jobOrder,
                worksites,
                null,
                default(GameTime),
                out candidate);
        }

        private static bool TryBuildCandidate(
            ActorRecord actor,
            int actorOrder,
            JobRequest request,
            int jobOrder,
            WorksiteStore worksites,
            WorldEventLog eventLog,
            GameTime now,
            out Candidate candidate)
        {
            candidate = null;

            if (!actor.IsAlive || !actor.ScheduleState.IsIdle)
                return false;
            if (!TryGetActivePreference(actor, request.Kind, out var preference))
                return false;
            if (!TryGetActiveMatchingWorksite(request, worksites, out _))
                return false;

            // refusal check: hungry or low-mood actors do not become candidates
            if (IsRefusing(actor))
            {
                if (eventLog != null)
                    AppendJobRefusedEvent(eventLog, now, actor, request);

                return false;
            }

            candidate = new Candidate(actor, request, preference.Priority, actorOrder, jobOrder);
            return true;
        }

        private static bool TryClaimCandidate(JobBoard board, Candidate candidate, out JobAssignmentResult result)
        {
            result = default;
            if (!board.TryClaim(candidate.Request.Id, candidate.Actor.Id, out var claimedRequest))
                return false;

            var assigned = ActorScheduleState.Assigned(
                claimedRequest.Id,
                claimedRequest.SiteId,
                claimedRequest.WorksitePosition);
            candidate.Actor.ApplyScheduleState(assigned);
            result = new JobAssignmentResult(
                candidate.Actor.Id,
                claimedRequest.Id,
                claimedRequest.SiteId,
                claimedRequest.WorksitePosition);
            return true;
        }

        private static void AppendJobAssignedEvent(
            WorldEventLog eventLog,
            GameTime now,
            JobAssignmentResult result)
        {
            eventLog.Append(new WorldEvent(
                now,
                WorldEventKind.JobAssigned,
                result.ActorId,
                result.SiteId,
                $"job_assigned:{result.JobId.Value}",
                new ReasonTrace(new[]
                {
                    $"job:{result.JobId.Value}",
                    $"actor:{result.ActorId.Value}",
                    $"site:{result.SiteId.Value}",
                    $"worksite:{result.WorksitePosition.X},{result.WorksitePosition.Y}",
                })));
        }

        private static void AppendJobRefusedEvent(
            WorldEventLog eventLog,
            GameTime now,
            ActorRecord actor,
            JobRequest request)
        {
            eventLog.Append(new WorldEvent(
                now,
                WorldEventKind.JobRefused,
                actor.Id,
                request.SiteId,
                $"job_refused:{request.Id.Value}",
                new ReasonTrace(new[]
                {
                    $"job:{request.Id.Value}",
                    $"actor:{actor.Id.Value}",
                    $"reason:hunger_or_low_mood",
                })));
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

        private static bool CanStartRequestedQuantity(
            RecipeSystem recipeSystem,
            JobRequest request,
            WorksiteStore worksites,
            RecipeDef recipe,
            InventoryState inventory,
            ActorId actorId)
        {
            var inventoryPreflight = inventory.Clone();
            for (var execution = 0; execution < request.Quantity; execution++)
            {
                if (!recipeSystem.TryStart(
                    recipe,
                    worksites,
                    request.SiteId,
                    request.WorksitePosition,
                    inventoryPreflight,
                    actorId,
                    out _))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryGetActiveMatchingWorksite(JobRequest request, WorksiteStore worksites, out WorksiteRecord worksite)
        {
            if (!worksites.TryGet(request.SiteId, request.WorksitePosition, out worksite))
                return false;

            return worksite.IsActive && worksite.Kind == request.WorksiteKind;
        }

        // Refusal policy: simple, deterministic guard used by both candidate selection
        // and query-style public checks. This keeps the policy local to JobAssignment
        // so later phase work (config rows or data-driven rules) can replace it.
        private static bool IsRefusing(ActorRecord actor)
        {
            if (actor == null)
                return false;

            // Refuse when hunger is severe or mood is low. Thresholds are conservative
            // for this atom; future faz may expose them as data rows.
            const int hungerRefusalThreshold = 80;

            var hunger = actor.Needs.Hunger;
            if (hunger.Value >= hungerRefusalThreshold)
                return true;

            if (actor.Mood.IsLow)
                return true;

            return false;
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

    /// <summary>Small immutable result describing one started recipe claim.</summary>
    public readonly struct JobRecipeStartResult : IEquatable<JobRecipeStartResult>
    {
        public JobRecipeStartResult(
            ActorId actorId,
            JobId jobId,
            SiteId siteId,
            GridPosition worksitePosition,
            RecipeWorkOrder workOrder)
        {
            ActorId = actorId;
            JobId = jobId;
            SiteId = siteId;
            WorksitePosition = worksitePosition;
            WorkOrder = workOrder ?? throw new ArgumentNullException(nameof(workOrder));
        }

        /// <summary>Actor whose claim started the recipe.</summary>
        public ActorId ActorId { get; }

        /// <summary>Claimed pending job id now bound to the active work order.</summary>
        public JobId JobId { get; }

        /// <summary>Site containing the active worksite.</summary>
        public SiteId SiteId { get; }

        /// <summary>Grid cell of the active worksite.</summary>
        public GridPosition WorksitePosition { get; }

        /// <summary>Runtime recipe work order created by RecipeSystem.TryStart.</summary>
        public RecipeWorkOrder WorkOrder { get; }

        /// <summary>Returns true when the start result carries the same stable ids and work order reference.</summary>
        public bool Equals(JobRecipeStartResult other)
        {
            return ActorId == other.ActorId
                && JobId == other.JobId
                && SiteId == other.SiteId
                && WorksitePosition.Equals(other.WorksitePosition)
                && ReferenceEquals(WorkOrder, other.WorkOrder);
        }

        /// <summary>Returns true when the object is a matching start result.</summary>
        public override bool Equals(object obj)
        {
            return obj is JobRecipeStartResult other && Equals(other);
        }

        /// <summary>Returns a hash code derived from stable ids and work order reference.</summary>
        public override int GetHashCode()
        {
            return HashCode.Combine(ActorId, JobId, SiteId, WorksitePosition, WorkOrder);
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
