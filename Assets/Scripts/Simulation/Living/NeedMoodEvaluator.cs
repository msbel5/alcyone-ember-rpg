using System;
using EmberCrpg.Domain.Actors;

// Design note:
// NeedMoodEvaluator is Phase 4's pure LIVING derivation step. It reads current
// need pressure, then returns a mood snapshot. It does not mutate ActorRecord,
// tick needs, persist state, emit
// EventLog rows, or decide job refusal.
// Atom-map ref: docs/sprint-phase-4-atom-map.md Mood derivation rail.
namespace EmberCrpg.Simulation.Living
{
    /// <summary>Derives actor mood from deterministic actor needs.</summary>
    public sealed class NeedMoodEvaluator
    {
        public ActorMood Evaluate(ActorNeeds needs)
        {
            var totalPressure = needs.Hunger.Value
                + needs.Fatigue.Value
                + needs.Thirst.Value;
            var penalty = totalPressure / 3;
            return new ActorMood(ActorMood.NeutralValue - penalty);
        }

        public ActorMood Evaluate(ActorRecord actor)
        {
            if (actor == null)
                throw new ArgumentNullException(nameof(actor));

            return Evaluate(actor.Needs);
        }
    }
}
