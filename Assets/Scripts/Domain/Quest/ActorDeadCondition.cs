using System;
using EmberCrpg.Domain.Core;

// Design note:
// ActorDeadCondition is a Specification over deterministic actor vitality.
// Pattern: Specification.
namespace EmberCrpg.Domain.Quest
{
    /// <summary>Condition satisfied when the referenced actor exists and is dead.</summary>
    public sealed class ActorDeadCondition : IQuestCondition
    {
        public ActorDeadCondition(ActorId actorId)
        {
            if (actorId.IsEmpty)
                throw new ArgumentException("ActorId.Empty cannot back a quest death condition.", nameof(actorId));
            ActorId = actorId;
        }

        /// <summary>Deterministic actor id monitored by this condition.</summary>
        public ActorId ActorId { get; }

        /// <summary>Returns true when the referenced actor exists and is dead.</summary>
        public bool IsMet(in QuestWorldView world, QuestState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            return world.IsActorDead(ActorId);
        }
    }
}
