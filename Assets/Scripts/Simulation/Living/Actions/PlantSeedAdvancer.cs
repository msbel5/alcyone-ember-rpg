using System.Collections.Generic;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Actors.Actions;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;

// Design note:
// W33-01 §7.2 / W33-02 §6.2: ConsumeFood's phase skeleton — validate every step, Advanced()
// until the duration lands, then ONE atomic commit: seed leaves the SITE pile (never the
// player's bag — the B06 lesson), a real PlantComponent is born through PlantingSystem's
// takeSeed seam, the plot claim releases, and the JOB completes. "İş, EYLEMLE biter" — the
// job's Complete lives ONLY here, so the 5101 ghost-cancel loop can never restart.
namespace EmberCrpg.Simulation.Living.Actions
{
    /// <summary>Plants for PlantDurationTicks, then commits seed→plant + job completion atomically.</summary>
    public sealed class PlantSeedAdvancer : ActionAdvancer
    {
        /// <summary>"Ekim 2 tick sürer" — the single home of the constant (W33-01 §5).</summary>
        public const int PlantDurationTicks = 2;

        private readonly IReadOnlyList<PlantSpeciesDef> _species;
        private readonly EmberCrpg.Simulation.Process.PlantingSystem _planting =
            new EmberCrpg.Simulation.Process.PlantingSystem(); // stateless helper

        public PlantSeedAdvancer(ActionLogManager log, IReadOnlyList<PlantSpeciesDef> species)
            : base(log)
        {
            _species = species;
        }

        public override ActorActionType Handles => ActorActionType.PlantSeed;

        protected override void Step(WorldState world, ActorRecord actor, GameTime stamp)
        {
            var state = actor.ActionState;
            if (world.Reservations == null
                || !world.Reservations.TryGetByActor(actor.Id.Value, out var row)
                || row.Id != state.ReservationId.Value
                || !FarmOperations.TryParsePlotKey(row.ItemTag, out var soilId)
                || world.Soils == null || !world.Soils.TryGet(soilId, out var soil))
            {
                Fail(world, actor, ActionFailureReason.ReservationLost, stamp);
                return;
            }
            var species = _species != null && _species.Count > 0 ? _species[0] : null;
            var seedTag = species?.SpeciesId; // seed-corn rule: the crop is its own seed (W33-01 §7.2)
            var pile = FoodOperations.FindPile(world, state.TargetSiteId.Value);
            if (species == null || pile == null || pile.Get(seedTag) <= 0)
            {
                Fail(world, actor, ActionFailureReason.SourceDrained, stamp);
                return;
            }
            if (soil.HasPlant)
            {
                // W33-02 §6.2: a taken plot is PERMANENTLY invalid for this job — action AND
                // job die together, or the claim/cascade pair would oscillate forever.
                DropJob(world, actor);
                Fail(world, actor, ActionFailureReason.PlotTaken, stamp);
                return;
            }
            if (FarmOperations.Chebyshev(actor.Position, soil.Position) > 1)
            {
                Fail(world, actor, ActionFailureReason.Unreachable, stamp);
                return;
            }

            var progressed = state.Advanced();
            if (progressed.ProgressTicks < PlantDurationTicks)
            {
                TransitionTo(world, actor, progressed, ActionLogReason.ProgressTicked, stamp);
                return;
            }

            // ATOMIC COMMIT: seed drops ONLY here; every earlier failure path is release-only.
            var planted = _planting.TryPlant(species, world.Soils, world.Plants, soil.Id,
                FarmOperations.PlantIdFor(soil.Id), () => pile.Remove(seedTag, 1) == 1,
                world.Events, stamp, actor.Id);
            if (!planted)
            {
                DropJob(world, actor);
                Fail(world, actor, ActionFailureReason.PlotTaken, stamp);
                return;
            }
            world.Reservations.Release(row.Id);
            CompleteJob(world, actor, stamp);
            TransitionTo(world, actor, progressed.Succeeded(), ActionLogReason.Completed, stamp);
        }

        /// <summary>The job dies WITH the failed action (no re-decide loop); the daily cascade
        /// reposts against a genuinely free soil (W33-02 §7.3 post gate).</summary>
        private static void DropJob(WorldState world, ActorRecord actor)
        {
            var jobId = actor.ScheduleState.CurrentJobId;
            if (jobId.IsEmpty || world.Jobs == null) return;
            world.Jobs.Cancel(jobId);
            actor.ApplyScheduleState(ActorScheduleState.Idle);
        }

        // "İş ancak eylem zinciri biterse biter" — JobCompleted grammar VERBATIM from
        // JobAssignmentSystem.Tick.cs so chronicle/proof consumers keep reading unchanged.
        private static void CompleteJob(WorldState world, ActorRecord actor, GameTime stamp)
        {
            var jobId = actor.ScheduleState.CurrentJobId;
            if (jobId.IsEmpty || world.Jobs == null || !world.Jobs.TryGet(jobId, out var request))
                return;
            world.Jobs.Complete(jobId);
            world.Events?.Append(new WorldEvent(
                stamp,
                WorldEventKind.JobCompleted,
                actor.Id,
                request.SiteId,
                $"job_completed:{request.Id.Value}",
                new ReasonTrace(new[]
                {
                    $"job:{request.Id.Value}",
                    $"recipe:{request.RecipeId.Value}",
                    $"quantity:{request.Quantity}",
                    $"worksite:{request.WorksiteKind}",
                })));
            actor.ApplyScheduleState(ActorScheduleState.Idle);
        }
    }
}
