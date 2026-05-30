// EMB-035: JobAssignmentSystem job-tick/execution phase (partial).
using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;

namespace EmberCrpg.Simulation.Process
{
    public sealed partial class JobAssignmentSystem
    {
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
            // for this atom; future phase may expose them as data rows.
            const int hungerRefusalThreshold = 80;

            var hunger = actor.Needs.Hunger;
            if (hunger.Value >= hungerRefusalThreshold)
                return true;

            if (actor.Mood.IsLow)
                return true;

            return false;
        }

    }
}
