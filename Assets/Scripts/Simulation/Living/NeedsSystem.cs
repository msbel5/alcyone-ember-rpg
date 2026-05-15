using System;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;

// Design note:
// NeedsSystem is Faz 4's first deterministic needs tick. It advances only
// pressure values and derived mood, then emits a concrete NeedChanged event
// when a caller uses the EventLog overload. Recovery, job refusal, save/load,
// inventory consumption, and sleep/eat actions are later atoms.
// Atom-map ref: DOCS/sprint-faz-4-atom-map.md Needs tick rail.
namespace EmberCrpg.Simulation.Living
{
    /// <summary>Deterministically advances actor needs and recomputes mood.</summary>
    public sealed class NeedsSystem
    {
        public const int HungerIncreasePerTick = 20;
        public const int FatigueIncreasePerTick = 15;

        private readonly NeedMoodEvaluator _moodEvaluator;

        public NeedsSystem()
            : this(new NeedMoodEvaluator())
        {
        }

        public NeedsSystem(NeedMoodEvaluator moodEvaluator)
        {
            _moodEvaluator = moodEvaluator ?? throw new ArgumentNullException(nameof(moodEvaluator));
        }

        public ActorNeeds TickNeeds(ActorNeeds needs, int ticks = 1)
        {
            if (ticks <= 0)
                return needs;

            return needs
                .WithHunger(needs.Hunger.Increase(ScaleRate(HungerIncreasePerTick, ticks)))
                .WithFatigue(needs.Fatigue.Increase(ScaleRate(FatigueIncreasePerTick, ticks)));
        }

        public ActorMood RecomputeMood(ActorRecord actor, NeedValue memoryPressure = default)
        {
            if (actor == null)
                throw new ArgumentNullException(nameof(actor));

            var mood = _moodEvaluator.Evaluate(actor, memoryPressure);
            actor.ApplyMood(mood);
            return mood;
        }

        public bool TickActorNeeds(
            ActorRecord actor,
            WorldEventLog eventLog,
            GameTime now,
            int ticks = 1,
            NeedValue memoryPressure = default)
        {
            if (actor == null)
                throw new ArgumentNullException(nameof(actor));
            if (eventLog == null)
                throw new ArgumentNullException(nameof(eventLog));
            if (ticks <= 0)
                return false;

            var previousNeeds = actor.Needs;
            var nextNeeds = TickNeeds(previousNeeds, ticks);
            actor.ApplyNeeds(nextNeeds);
            var nextMood = RecomputeMood(actor, memoryPressure);

            eventLog.Append(new WorldEvent(
                now,
                WorldEventKind.NeedChanged,
                actor.Id,
                default,
                $"need_changed:{actor.Id.Value}",
                new ReasonTrace(new[]
                {
                    "needs_tick",
                    $"actor:{actor.Id.Value}",
                    $"ticks:{ticks}",
                    $"time:{now.TotalMinutes}",
                    $"hunger:{previousNeeds.Hunger.Value}->{nextNeeds.Hunger.Value}",
                    $"fatigue:{previousNeeds.Fatigue.Value}->{nextNeeds.Fatigue.Value}",
                    $"mood:{nextMood.Value}",
                })));

            return true;
        }

        private static int ScaleRate(int rate, int ticks)
        {
            var amount = (long)rate * ticks;
            return amount > int.MaxValue ? int.MaxValue : (int)amount;
        }
    }
}
