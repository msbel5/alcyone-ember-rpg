using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Actors.Actions;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;

// Design note:
// W32-03 §4/§6 ConsumeFood (3 ticks): hunger drops ONLY at completion, in one atomic commit
// that is EXACTLY the retired TryEatCached mutation block — including the verbatim meal_eaten
// event line, so RumorMill/Gate meal counters keep reading it unchanged.
namespace EmberCrpg.Simulation.Living.Actions
{
    /// <summary>Chews for ConsumeDurationTicks, then commits benefit + claim release atomically.</summary>
    public sealed class ConsumeFoodAdvancer : ActionAdvancer
    {
        /// <summary>"Yemek 3 tick sürer" — the single home of the constant.</summary>
        public const int ConsumeDurationTicks = 3;

        private readonly NeedMoodEvaluator _mood = new NeedMoodEvaluator(); // stateless helper

        public ConsumeFoodAdvancer(ActionLogManager log) : base(log) { }

        public override ActorActionType Handles => ActorActionType.ConsumeFood;

        protected override void Step(WorldState world, ActorRecord actor, GameTime stamp)
        {
            var state = actor.ActionState;
            if (world.Reservations == null
                || !world.Reservations.TryGetByActor(actor.Id.Value, out var row)
                || row.Id != state.ReservationId.Value)
            {
                // The claim carried the unit's identity; without it the unit cannot be returned.
                // Only a mis-sized TTL can reach this — deterministic, logged, rare by design.
                Fail(world, actor, ActionFailureReason.ReservationLost, stamp);
                return;
            }
            // W32-06 T1: remote chewing is refused by the OP, not by system order — a diner
            // displaced out of reach (witness nudge class) fails here and the unit goes home.
            if (!FoodOperations.WithinEatReach(world, actor, row.SiteId))
            {
                Fail(world, actor, ActionFailureReason.Unreachable, stamp);
                return;
            }
            var progressed = state.Advanced();
            if (progressed.ProgressTicks < ConsumeDurationTicks)
            {
                TransitionTo(world, actor, progressed, ActionLogReason.ProgressTicked, stamp);
                return;
            }
            // Atomic commit — EXACT TryEatCached math (NeedConsumptionSystem.cs:180-188, retired):
            // benefit (hunger floor + drink) and cost (the claim) close in the SAME step.
            var fed = actor.Needs
                .WithHunger(new NeedValue(NeedConsumptionSystem.MealHungerFloor))
                .WithThirst(new NeedValue(actor.Needs.Thirst.Value - NeedConsumptionSystem.MealThirstRecovery));
            actor.ApplyNeeds(fed);
            actor.ApplyMood(_mood.Evaluate(fed));
            world.Events?.Append(new WorldEvent(
                stamp, WorldEventKind.NeedChanged, actor.Id, new SiteId(row.SiteId),
                $"meal_eaten item:{row.ItemTag} hunger:{fed.Hunger.Value}"));
            world.Reservations.Release(row.Id);
            TransitionTo(world, actor, progressed.Succeeded(), ActionLogReason.Completed, stamp);
        }
    }
}
