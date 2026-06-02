using System;
using EmberCrpg.Domain.Core;

// Design note:
// TicksElapsedCondition is a Specification over deterministic game time deltas.
// Pattern: Specification.
namespace EmberCrpg.Domain.Quest
{
    /// <summary>Condition satisfied when the required number of deterministic world ticks has elapsed.</summary>
    public sealed class TicksElapsedCondition : IQuestCondition
    {
        public TicksElapsedCondition(GameTime sinceTick, long count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), count, "Elapsed quest tick count cannot be negative.");

            SinceTick = sinceTick;
            Count = count;
        }

        /// <summary>Inclusive starting tick for this elapsed-time check.</summary>
        public GameTime SinceTick { get; }
        /// <summary>Required deterministic tick delta.</summary>
        public long Count { get; }

        /// <summary>Returns true when the world time is at least <see cref="Count"/> ticks past <see cref="SinceTick"/>.</summary>
        public bool IsMet(in QuestWorldView world, QuestState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            return world.Time >= SinceTick && (world.Time - SinceTick) >= Count;
        }
    }
}
