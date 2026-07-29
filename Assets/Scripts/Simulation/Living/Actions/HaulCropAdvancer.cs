using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Actors.Actions;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;

// Design note:
// W33-01 §5/§6: walk + terminal deposit in ONE advancer (a separate DepositCrop member would
// cost an enum value + advancer + registration for a 1-tick body; precedent: ConsumeFood also
// commits its benefit on its last tick). The carry row is a tag+TTL carrier — the tag truth
// lives there because the struct stores ids only (W32 rule). First reach-contact delivers:
// deposit is Add-only (no capacity, no reservation), so arrival order cannot matter.
namespace EmberCrpg.Simulation.Living.Actions
{
    /// <summary>Hauls the carried yield to the site pile; deposits on first reach-contact.</summary>
    public sealed class HaulCropAdvancer : ActionAdvancer
    {
        public HaulCropAdvancer(ActionLogManager log) : base(log) { }

        public override ActorActionType Handles => ActorActionType.HaulCrop;

        protected override void Step(WorldState world, ActorRecord actor, GameTime stamp)
        {
            var state = actor.ActionState;
            // Empty hands can only come from a corrupt save (TryRestore normalizes) — loud, cheap.
            if (state.CarriedUnits <= 0)
            {
                Fail(world, actor, ActionFailureReason.SourceDrained, stamp);
                return;
            }
            // The carry row remains the live claim; ActorActionState also owns the matter tag
            // so a missing/expired row can refund the load instead of erasing it.
            if (world.Reservations == null
                || !world.Reservations.TryGetByActor(actor.Id.Value, out var row)
                || row.Id != state.ReservationId.Value
                || !FarmOperations.TryParseCarryKey(row.ItemTag, out var cropTag))
            {
                Fail(world, actor, ActionFailureReason.ReservationLost, stamp);
                return;
            }
            if (!string.IsNullOrWhiteSpace(state.CarriedMatterTag)
                && !string.Equals(state.CarriedMatterTag, cropTag, System.StringComparison.Ordinal))
            {
                Fail(world, actor, ActionFailureReason.ReservationLost, stamp);
                return;
            }
            if (string.IsNullOrWhiteSpace(state.CarriedMatterTag))
            {
                state = state.WithCarriedMatter(cropTag, state.CarriedUnits);
                TransitionTo(world, actor, state, ActionLogReason.ProgressTicked, stamp);
            }

            // Siteless worlds (bare tests) have no centre: stay permissive and deliver in place,
            // matching FoodOperations.WithinEatReach's contract.
            if (NeedConsumptionSystem.TryGetSiteCentre(world, state.TargetSiteId, out var centre)
                && !FoodOperations.WithinEatReach(world, actor, state.TargetSiteId.Value))
            {
                var movement = MovementService.RouteToward(
                    actor.Position, centre, world?.NavView, NeedConsumptionSystem.EatReachCells);
                if (!movement.Moved)
                {
                    Fail(world, actor, ActionFailureReason.Unreachable, stamp);
                    return;
                }
                actor.MoveTo(movement.Position);
            }

            if (FoodOperations.WithinEatReach(world, actor, state.TargetSiteId.Value))
            {
                // Deposit commit: hands → pile and hands-empty in the SAME step (W33-03 §4.3).
                var pile = FarmOperations.FindOrCreatePile(world, state.TargetSiteId);
                if (pile == null)
                {
                    Fail(world, actor, ActionFailureReason.ReservationLost, stamp);
                    return;
                }
                pile.Add(cropTag, state.CarriedUnits);
                world.Reservations.Release(row.Id);
                TransitionTo(world, actor,
                    state.WithCarriedUnits(0).Succeeded(), ActionLogReason.Completed, stamp);
            }
            else
            {
                TransitionTo(world, actor, state.Advanced(), ActionLogReason.ProgressTicked, stamp);
            }
        }
    }
}
