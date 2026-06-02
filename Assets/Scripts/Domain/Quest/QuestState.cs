using System;
using EmberCrpg.Domain.Core;

// Design note:
// QuestState is the runtime state-machine snapshot for one deterministic quest instance.
// Pattern: State machine runtime state.
namespace EmberCrpg.Domain.Quest
{
    /// <summary>Plain runtime state for a quest instance: triggered tasks, completion flags, and start tick.</summary>
    public sealed class QuestState
    {
        public QuestState(int taskCount, GameTime startTick)
        {
            if (taskCount < 0)
                throw new ArgumentOutOfRangeException(nameof(taskCount), taskCount, "Quest task count cannot be negative.");

            TriggeredTasks = new bool[taskCount];
            StartTick = startTick;
        }

        /// <summary>Per-task triggered flags keyed by the task index in the quest definition.</summary>
        public bool[] TriggeredTasks { get; }
        /// <summary>True when the quest runtime is complete.</summary>
        public bool IsComplete { get; private set; }
        /// <summary>True when the quest runtime completed successfully.</summary>
        public bool IsSuccess { get; private set; }
        /// <summary>Deterministic world tick when the quest runtime started.</summary>
        public GameTime StartTick { get; }

        /// <summary>Returns true when the task index has already fired.</summary>
        public bool IsTaskTriggered(int taskIndex)
        {
            if (taskIndex < 0 || taskIndex >= TriggeredTasks.Length)
                throw new ArgumentOutOfRangeException(nameof(taskIndex), taskIndex, "Quest task index must be within quest state bounds.");
            return TriggeredTasks[taskIndex];
        }

        /// <summary>Marks the task index as triggered.</summary>
        public void MarkTaskTriggered(int taskIndex)
        {
            if (taskIndex < 0 || taskIndex >= TriggeredTasks.Length)
                throw new ArgumentOutOfRangeException(nameof(taskIndex), taskIndex, "Quest task index must be within quest state bounds.");
            TriggeredTasks[taskIndex] = true;
        }

        /// <summary>Marks the quest complete and stores whether it succeeded.</summary>
        public void SetCompleted(bool success)
        {
            IsComplete = true;
            IsSuccess = success;
        }
    }
}
