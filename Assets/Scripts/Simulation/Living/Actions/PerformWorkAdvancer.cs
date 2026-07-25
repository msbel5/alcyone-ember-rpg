using System;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Actors.Actions;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;

// Design note:
// W34 WORK slice (docs/ruh/w34/02 §7.2): the ONE mover of the order counter — labour finally
// requires a body at the bench. The order row (WorldState.WorkOrders) is born here, funded
// here (inputs leave the SITE pile only at the bench, progress==0 step — the W33 consume-at-
// commit lesson applied to the START), ticked here, and removed here on the final commit.
// CONSTRAINT (funding invariant, §5.2): TryFund and the first counter hit share ONE step so a
// row is never ambiguous about its inputs. Every failure path leaves the row (with progress)
// and the claim in place — "pause" is claim + row + Idle, not a new phase (§2). Batch drain is
// a SourceDrained pause, never the remote strip's "Cannot start next execution" exception (§3).
namespace EmberCrpg.Simulation.Living.Actions
{
    /// <summary>Works the claimed order at the bench: funds, ticks, commits, completes the job.</summary>
    public sealed class PerformWorkAdvancer : ActionAdvancer
    {
        private readonly Func<RecipeId, RecipeDef> _resolveRecipe;
        private readonly EmberCrpg.Simulation.Process.RecipeSystem _recipes =
            new EmberCrpg.Simulation.Process.RecipeSystem(); // stateless helper

        public PerformWorkAdvancer(ActionLogManager log, Func<RecipeId, RecipeDef> resolveRecipe)
            : base(log)
        {
            _resolveRecipe = resolveRecipe;
        }

        public override ActorActionType Handles => ActorActionType.PerformWork;

        protected override void Step(WorldState world, ActorRecord actor, GameTime stamp)
        {
            var state = actor.ActionState;
            // 1-3) MoveToWorksite's gates verbatim: JobLost / WorksiteGone / quitting time.
            if (!WorkOperations.TryGetClaim(world, actor, out var request))
            {
                Fail(world, actor, ActionFailureReason.JobLost, stamp);
                return;
            }
            if (!WorkOperations.TryGetWorksite(world, request, out _))
            {
                Fail(world, actor, ActionFailureReason.WorksiteGone, stamp);
                return;
            }
            if (!ScheduleSystem.IsWorkHour(stamp))
            {
                Fail(world, actor, ActionFailureReason.Interrupted, stamp);
                return;
            }
            // 4) Pushed off the bench (witness-nudge class): remote labour is refused = pause.
            var bench = actor.ScheduleState.TargetWorksitePosition;
            if (FarmOperations.Chebyshev(actor.Position, bench) > WorkOperations.WorkReachCells)
            {
                Fail(world, actor, ActionFailureReason.Unreachable, stamp);
                return;
            }
            // Decide gate 3 already filtered unknown ids; mid-chain unresolvability (resolver
            // torn away) can only mean the job's recipe truth is gone — same class as JobLost.
            var recipe = _resolveRecipe?.Invoke(request.RecipeId);
            if (recipe == null || world.WorkOrders == null)
            {
                Fail(world, actor, ActionFailureReason.JobLost, stamp);
                return;
            }
            var io = WorkOperations.SiteIo(world, request.SiteId);
            if (io == null)
            {
                Fail(world, actor, ActionFailureReason.SourceDrained, stamp);
                return;
            }

            var jobId = request.Id.Value;
            if (!world.WorkOrders.TryGetByJob(jobId, out var row))
            {
                // The work piece lands ON the bench — attribution is the STARTER, forever (§13.4).
                row = new WorkOrderRecord
                {
                    JobId = jobId,
                    RecipeId = request.RecipeId.Value,
                    SiteId = request.SiteId.Value,
                    PositionX = request.WorksitePosition.X,
                    PositionY = request.WorksitePosition.Y,
                    StartedByActorId = actor.Id.Value,
                };
                world.WorkOrders.Add(row);
            }
            if (row.ProgressTicks == 0)
            {
                // Unfunded execution (invariant §5.2): inputs leave the pile ONLY here, at the
                // bench. A dry pile is an honest pause — the claim holds, restock resumes it.
                if (!EmberCrpg.Simulation.Process.RecipeSystem.TryFund(recipe, io))
                {
                    Fail(world, actor, ActionFailureReason.SourceDrained, stamp);
                    return;
                }
            }
            // One bench stroke; on the duration tick the outputs land in the SITE pile and
            // RecipeCompleted carries the REAL boundary stamp with the FINISHING actor (§13.4).
            if (_recipes.Tick(row, recipe, io, world.Events, stamp, actor.Id))
            {
                row.CompletedExecutions++;
                row.ProgressTicks = 0; // next execution starts unfunded — TryFund asks again
                if (row.CompletedExecutions >= request.Quantity)
                {
                    world.WorkOrders.Remove(jobId);            // the piece leaves the bench
                    WorkOperations.CompleteJob(world, actor, stamp); // Complete + JobCompleted + Idle
                    TransitionTo(world, actor, state.Advanced().Succeeded(), ActionLogReason.Completed, stamp);
                    return;
                }
            }
            TransitionTo(world, actor, state.Advanced(), ActionLogReason.ProgressTicked, stamp);
        }
    }
}
