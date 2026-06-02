// Design note:
// IQuestCondition is the narrow Specification-pattern contract for deterministic quest predicates.
// Pattern: Specification.
namespace EmberCrpg.Domain.Quest
{
    /// <summary>Pure deterministic quest predicate evaluated against a read-only world view and quest state.</summary>
    public interface IQuestCondition
    {
        /// <summary>Returns true when this deterministic predicate is currently satisfied.</summary>
        bool IsMet(in QuestWorldView world, QuestState state);
    }
}
