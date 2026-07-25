using System.Collections.Generic;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Actors.Actions;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;

// Design note:
// W33-01 §6: the slice's heart — the ONLY code that turns a ripe plant into carried units.
// CONSTRAINT (atomicity): unplant + yield-to-HANDS + event + plot→carry row swap close in the
// SAME step; a chunk boundary can never observe "yield minted but plant still standing" or the
// reverse. CONSTRAINT (single writer): with the fiat HarvestStep retired, this commit is the
// only plant-removal in the live loop — replanting is the shortage cascade's (5101's) real job.
namespace EmberCrpg.Simulation.Living.Actions
{
    /// <summary>Harvests for HarvestDurationTicks, then commits plant→hands atomically.</summary>
    public sealed class HarvestCropAdvancer : ActionAdvancer
    {
        /// <summary>"Hasat 2 tick sürer" — the single home of the constant (W33-01 §5).</summary>
        public const int HarvestDurationTicks = 2;

        /// <summary>Verbatim the retired HarvestStep's "+2" — economy calibration unchanged.</summary>
        public const int HarvestYieldUnits = 2;

        private readonly IReadOnlyList<PlantSpeciesDef> _species;

        public HarvestCropAdvancer(ActionLogManager log, IReadOnlyList<PlantSpeciesDef> species)
            : base(log)
        {
            _species = species;
        }

        public override ActorActionType Handles => ActorActionType.HarvestCrop;

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
            if (!soil.HasPlant || world.Plants == null
                || !world.Plants.TryGet(soil.PlantId, out var plant)
                || !FarmOperations.IsHarvestable(_species, plant))
            {
                Fail(world, actor, ActionFailureReason.CropGone, stamp);
                return;
            }
            // Nudged out of reach (witness shy-step class): fail and replan — no remote harvest.
            if (FarmOperations.Chebyshev(actor.Position, plant.Position) > FarmOperations.HarvestReachCells)
            {
                Fail(world, actor, ActionFailureReason.Unreachable, stamp);
                return;
            }

            var progressed = state.Advanced();
            if (progressed.ProgressTicks < HarvestDurationTicks)
            {
                TransitionTo(world, actor, progressed, ActionLogReason.ProgressTicked, stamp);
                return;
            }

            // ATOMIC COMMIT — nothing was minted before this line, so every earlier failure
            // path is release-only (matter conservation is free, W33-02 §6.3).
            world.Plants.Remove(plant.Id);
            world.Soils.Replace(soil.Id, soil.WithoutPlant());
            // Event grammar VERBATIM from the retired HarvestStep, now authored by real hands.
            world.Events?.Append(new WorldEvent(
                stamp, WorldEventKind.PlantHarvested, actor.Id, plant.SiteId,
                $"harvested species:{plant.SpeciesId} qty:{HarvestYieldUnits} by:{actor.Id.Value}"));
            // Row swap (W33-01 §6 step 4): release FIRST (one-row-per-actor rule), then the
            // carry row — both inside this step, so no system can observe the gap.
            world.Reservations.Release(row.Id);
            long haulWalk = 0L;
            if (NeedConsumptionSystem.TryGetSiteCentre(world, state.TargetSiteId, out var centre))
                haulWalk = FarmOperations.Chebyshev(actor.Position, centre);
            long until = stamp.TotalMinutes + haulWalk + 60; // W32-02 §4.3 TTL family
            if (!world.Reservations.TryReserve(state.TargetSiteId.Value,
                    FarmOperations.CarryKey(plant.SpeciesId), actor.Id.Value, until,
                    int.MaxValue, out var carryRowId))
                throw new System.InvalidOperationException(
                    "HarvestCrop invariant: the carry row must be reservable right after the plot release.");
            var next = progressed
                .WithReservation(new ReservationId(carryRowId))
                .WithCarriedUnits(HarvestYieldUnits)
                .Succeeded();
            TransitionTo(world, actor, next, ActionLogReason.ProgressTicked, stamp);
        }
    }
}
