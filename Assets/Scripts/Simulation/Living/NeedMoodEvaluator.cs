using System;
using EmberCrpg.Domain.Actors;

// Design note:
// NeedMoodEvaluator is Faz 4's pure LIVING derivation step. It reads current
// need pressure and an explicit memory-pressure value, then returns a mood
// snapshot. It does not mutate ActorRecord, tick needs, persist state, emit
// EventLog rows, or decide job refusal.
// Atom-map ref: DOCS/sprint-faz-4-atom-map.md Mood derivation rail.
namespace EmberCrpg.Simulation.Living
{
    /// <summary>Derives actor mood from needs plus deterministic memory pressure.</summary>
    public sealed class NeedMoodEvaluator
    {
        public ActorMood Evaluate(ActorNeeds needs, NeedValue memoryPressure = default)
        {
            var totalPressure = needs.Hunger.Value
                + needs.Fatigue.Value
                + needs.Thirst.Value
                + memoryPressure.Value;
            var penalty = totalPressure / 4;
            return new ActorMood(ActorMood.NeutralValue - penalty);
        }

        public ActorMood Evaluate(ActorRecord actor, NeedValue memoryPressure = default)
        {
            if (actor == null)
                throw new ArgumentNullException(nameof(actor));

            return Evaluate(actor.Needs, memoryPressure);
        }
    }
}
