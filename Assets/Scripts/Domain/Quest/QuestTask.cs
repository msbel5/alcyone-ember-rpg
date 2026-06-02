using System;
using System.Collections.Generic;

// Design note:
// QuestTask is the minimal deterministic quest state-machine step: one Specification and ordered Commands.
// Pattern: State machine step.
namespace EmberCrpg.Domain.Quest
{
    /// <summary>Quest step that fires once when its condition becomes true, then applies its actions in order.</summary>
    public sealed class QuestTask
    {
        private readonly IQuestAction[] _actions;

        public QuestTask(IQuestCondition condition, IEnumerable<IQuestAction> actions, bool triggered = false)
        {
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));
            if (actions == null)
                throw new ArgumentNullException(nameof(actions));

            var buffer = new List<IQuestAction>();
            foreach (var action in actions)
            {
                if (action == null)
                    throw new ArgumentException("Quest task actions cannot contain null entries.", nameof(actions));
                buffer.Add(action);
            }

            Condition = condition;
            _actions = buffer.ToArray();
            Triggered = triggered;
        }

        /// <summary>Deterministic trigger predicate for this task.</summary>
        public IQuestCondition Condition { get; }
        /// <summary>Ordered deterministic commands applied when this task fires.</summary>
        public IReadOnlyList<IQuestAction> Actions
        {
            get { return _actions; }
        }

        /// <summary>True after this task has fired once.</summary>
        public bool Triggered { get; private set; }

        /// <summary>Attempts to fire this task once for the supplied task index.</summary>
        public bool TryTrigger(int taskIndex, in QuestWorldView world, QuestMutationContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (taskIndex < 0 || taskIndex >= context.State.TriggeredTasks.Length)
                throw new ArgumentOutOfRangeException(nameof(taskIndex), taskIndex, "Quest task index must be within quest state bounds.");
            if (Triggered || context.State.IsTaskTriggered(taskIndex) || !Condition.IsMet(in world, context.State))
                return false;

            for (var i = 0; i < _actions.Length; i++)
                _actions[i].Apply(context);

            Triggered = true;
            context.State.MarkTaskTriggered(taskIndex);
            return true;
        }

        /// <summary>Creates a copy of this task preserving its current triggered flag.</summary>
        public QuestTask Clone()
        {
            return new QuestTask(Condition, _actions, Triggered);
        }
    }
}
