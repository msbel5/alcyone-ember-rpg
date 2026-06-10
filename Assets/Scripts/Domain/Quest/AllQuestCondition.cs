using System;
using System.Collections.Generic;

namespace EmberCrpg.Domain.Quest
{
    /// <summary>Specification combinator: every child condition must be met.</summary>
    public sealed class AllQuestCondition : IQuestCondition
    {
        private readonly IQuestCondition[] _conditions;

        public AllQuestCondition(IEnumerable<IQuestCondition> conditions)
        {
            if (conditions == null)
                throw new ArgumentNullException(nameof(conditions));

            var buffer = new List<IQuestCondition>();
            foreach (var condition in conditions)
            {
                if (condition == null)
                    throw new ArgumentException("Composite quest conditions cannot contain null entries.", nameof(conditions));
                buffer.Add(condition);
            }

            if (buffer.Count == 0)
                throw new ArgumentException("Composite quest condition requires at least one child.", nameof(conditions));
            _conditions = buffer.ToArray();
        }

        public bool IsMet(in QuestWorldView world, QuestState state)
        {
            for (int i = 0; i < _conditions.Length; i++)
                if (!_conditions[i].IsMet(in world, state))
                    return false;
            return true;
        }
    }
}
