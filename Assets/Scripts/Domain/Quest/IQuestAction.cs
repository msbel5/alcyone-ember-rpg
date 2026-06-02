// Design note:
// IQuestAction is the narrow Command-pattern contract for deterministic quest mutations.
// Pattern: Command.
namespace EmberCrpg.Domain.Quest
{
    /// <summary>Deterministic quest command that mutates quest state and/or the world through a constrained context.</summary>
    public interface IQuestAction
    {
        /// <summary>Applies this deterministic command to the provided quest mutation context.</summary>
        void Apply(QuestMutationContext context);
    }
}
