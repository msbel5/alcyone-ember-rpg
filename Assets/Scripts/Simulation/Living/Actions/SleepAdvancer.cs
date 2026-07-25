using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Actors.Actions;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;

// Design note:
// W34-01 §5.3: the multi-hour Running action that owns night recovery. Fatigue drops ONLY
// here, ONLY while Running — the fiat's positionless hourly subtraction is dead, so an actor
// who never reached its bed stays tired (RUH_TESHIS §10 acceptance). The template's pursuit
// probe runs before every Step: a hunted sleeper WAKES as Failed(Interrupted) for free.
namespace EmberCrpg.Simulation.Living.Actions
{
    /// <summary>Sleeps in place until dawn, recovering fatigue on a fixed tick ladder.</summary>
    public sealed class SleepAdvancer : ActionAdvancer
    {
        // The retired fiat rate kept verbatim: NightSleepFatigueRecovery(40)/hour spread over
        // ticks. 40/60 = 2/3 -> 2 points every 3rd Running tick. Integer-only (determinism
        // constitution — no floats); ProgressTicks lives on ActorActionState, so the ladder is
        // independent of chunk boundaries (stateless-advancer rule).
        public const int RecoveryPerStep = 2;
        public const int TicksPerRecoveryStep = 3;

        private readonly NeedMoodEvaluator _mood = new NeedMoodEvaluator(); // stateless helper

        public SleepAdvancer(ActionLogManager log) : base(log) { }

        public override ActorActionType Handles => ActorActionType.Sleep;

        protected override void Step(WorldState world, ActorRecord actor, GameTime stamp)
        {
            var state = actor.ActionState;
            // Same validation triple as MoveToBed: row exists + matches + still names MY home.
            if (world.Reservations == null
                || !world.Reservations.TryGetByActor(actor.Id.Value, out var row)
                || row.Id != state.ReservationId.Value
                || !SleepOperations.TryParseBedKey(row.ItemTag, out var bed)
                || !bed.Equals(actor.Home))
            {
                Fail(world, actor, ActionFailureReason.ReservationLost, stamp);
                return;
            }
            // Pushed out of bed (witness-nudge class): the ConsumeFood displaced-diner
            // precedent applied to the bedroom — remote sleeping is refused by the OP.
            if (FarmOperations.Chebyshev(actor.Position, actor.Home) > SleepOperations.BedReachCells)
            {
                Fail(world, actor, ActionFailureReason.Unreachable, stamp);
                return;
            }
            // Dawn completes the night. CONSTRAINT (§11 risk 5): the SAME IsNightHour predicate
            // MoveToBed's TimedOut reads — two hour comparisons would fork off-by-one at 06:00.
            if (!SleepOperations.IsNightHour(stamp.Hour))
            {
                world.Reservations.Release(row.Id);
                TransitionTo(world, actor, state.Succeeded(), ActionLogReason.Completed, stamp);
                return;
            }

            var progressed = state.Advanced();
            // Every 3rd Running tick recovers 2 points (fiat parity; NeedValue clamps at 0, as
            // the fiat did). Fatigue 0 does NOT end the night: the actor stays abed until dawn —
            // the fiat-era picture kept, and no aimless 03:00 wanderers are minted.
            if (progressed.ProgressTicks % TicksPerRecoveryStep == 0)
            {
                var rested = actor.Needs.WithFatigue(
                    new NeedValue(actor.Needs.Fatigue.Value - RecoveryPerStep));
                actor.ApplyNeeds(rested);
                actor.ApplyMood(_mood.Evaluate(rested));
            }
            // Phase BOUNDARIES are the log grammar (B21): one Started/one Completed per night —
            // in-phase ticks pass through TransitionTo unlogged. No WorldEvent is published
            // (unlike meal_eaten, sleep has no counter reader; least LOC).
            TransitionTo(world, actor, progressed, ActionLogReason.ProgressTicked, stamp);
        }
    }
}
