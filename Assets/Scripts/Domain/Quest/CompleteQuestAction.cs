// Design note:
// CompleteQuestAction is the Command that flips deterministic quest completion state.
// Pattern: Command.
namespace EmberCrpg.Domain.Quest
{
    /// <summary>Command that marks the quest complete and records whether it succeeded.</summary>
    public sealed class CompleteQuestAction : IQuestAction
    {
        public CompleteQuestAction(bool success)
        {
            Success = success;
        }

        /// <summary>Success flag stored when this completion command fires.</summary>
        public bool Success { get; }

        /// <summary>Marks the quest runtime complete with the configured success result.</summary>
        public void Apply(QuestMutationContext context)
        {
            context.CompleteQuest(Success);
        }
    }
}
