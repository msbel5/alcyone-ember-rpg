using System;
using EmberCrpg.Domain.World;

namespace EmberCrpg.Domain.Quest
{
    /// <summary>Specification over the deterministic world event chronicle.</summary>
    public sealed class WorldEventOccurredCondition : IQuestCondition
    {
        public WorldEventOccurredCondition(WorldEventKind kind, string reason, bool atOrAfterQuestStart)
        {
            if (kind == WorldEventKind.None)
                throw new ArgumentException("Quest event condition requires a concrete event kind.", nameof(kind));
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Quest event condition requires a reason.", nameof(reason));

            Kind = kind;
            Reason = reason.Trim();
            AtOrAfterQuestStart = atOrAfterQuestStart;
        }

        public WorldEventKind Kind { get; }
        public string Reason { get; }
        public bool AtOrAfterQuestStart { get; }

        public bool IsMet(in QuestWorldView world, QuestState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            return world.HasEvent(Kind, Reason, AtOrAfterQuestStart ? state.StartTick : default);
        }
    }
}
