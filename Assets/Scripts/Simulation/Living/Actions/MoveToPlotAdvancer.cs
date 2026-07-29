using System.Collections.Generic;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Actors.Actions;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;

// Design note:
// W33-01 §7.3: MoveToFoodAdvancer's skeleton with a soil-cell target. The soil identity is
// parsed from the plot reservation row each step (the struct carries ids only, W32 rule).
// Mid-route validations mirror MoveToFood's drain checks: the chain fails EARLY and honestly,
// never on arrival surprise. Arrival = standing ON the plot cell (plots are claimed 1:1, so
// the Gate8 seat-stacking problem cannot arise — no seat ring needed).
namespace EmberCrpg.Simulation.Living.Actions
{
    /// <summary>Walks the actor one step per tick toward its reserved plot cell.</summary>
    public sealed class MoveToPlotAdvancer : ActionAdvancer
    {
        private readonly IReadOnlyList<PlantSpeciesDef> _species;

        public MoveToPlotAdvancer(ActionLogManager log, IReadOnlyList<PlantSpeciesDef> species)
            : base(log)
        {
            _species = species;
        }

        public override ActorActionType Handles => ActorActionType.MoveToPlot;

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
            if (state.CurrentIntent == ActorIntent.Plant)
            {
                if (soil.HasPlant)
                {
                    Fail(world, actor, ActionFailureReason.PlotTaken, stamp);
                    return;
                }
                // Seed-corn rule (W33-01 §7.2): the seed is the crop's own tag and is NOT
                // reserved — mid-route drain fails softly here, exactly like MoveToFood.
                var species = FarmOperations.SpeciesFor(_species, SeedSpeciesId());
                var pile = FoodOperations.FindPile(world, state.TargetSiteId.Value);
                if (species == null || pile == null || pile.Get(species.SpeciesId) <= 0)
                {
                    Fail(world, actor, ActionFailureReason.SourceDrained, stamp);
                    return;
                }
            }
            else
            {
                // Harvest leg: the claim locks the plot, but a save-era orphan or an
                // out-of-band mutation could still empty it — cheap and loud beats silent.
                if (!soil.HasPlant || world.Plants == null
                    || !world.Plants.TryGet(soil.PlantId, out var plant)
                    || !FarmOperations.IsHarvestable(_species, plant))
                {
                    Fail(world, actor, ActionFailureReason.CropGone, stamp);
                    return;
                }
            }

            if (!actor.Position.Equals(soil.Position))
            {
                var movement = MovementService.RouteToward(actor.Position, soil.Position, world?.NavView);
                if (!movement.Moved)
                {
                    Fail(world, actor, ActionFailureReason.Unreachable, stamp);
                    return;
                }
                actor.MoveTo(movement.Position);
            }
            if (actor.Position.Equals(soil.Position))
                TransitionTo(world, actor, state.Succeeded(), ActionLogReason.Arrived, stamp);
            else
                TransitionTo(world, actor, state.Advanced(), ActionLogReason.ProgressTicked, stamp);
        }

        // Single-species slice: the catalog's first row IS the plantable crop (W33-02 §5).
        private string SeedSpeciesId()
            => _species != null && _species.Count > 0 && _species[0] != null ? _species[0].SpeciesId : null;
    }
}
