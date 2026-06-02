using System;
using System.Collections.Generic;

// Design note:
// QuestDefinition is the immutable DFU-style resource-plus-tasks template for Ember's deterministic quest model.
// Pattern: State machine template.
namespace EmberCrpg.Domain.Quest
{
    /// <summary>Immutable quest template composed of resource bindings, ordered tasks, and a terminal task index.</summary>
    public sealed class QuestDefinition
    {
        private readonly QuestTask[] _tasks;

        public QuestDefinition(
            QuestId id,
            string displayName,
            bool oneTime,
            QuestResourceBinding resourceBindings,
            IEnumerable<QuestTask> tasks,
            int completionTaskIndex)
        {
            if (id.IsEmpty)
                throw new ArgumentException("QuestId.Empty cannot back a quest definition.", nameof(id));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Quest display name is required.", nameof(displayName));
            if (resourceBindings == null)
                throw new ArgumentNullException(nameof(resourceBindings));
            if (tasks == null)
                throw new ArgumentNullException(nameof(tasks));

            var buffer = new List<QuestTask>();
            foreach (var task in tasks)
            {
                if (task == null)
                    throw new ArgumentException("Quest definition tasks cannot contain null entries.", nameof(tasks));
                buffer.Add(task.Clone());
            }

            if (buffer.Count == 0)
                throw new ArgumentException("Quest definition requires at least one task.", nameof(tasks));
            if (completionTaskIndex < 0 || completionTaskIndex >= buffer.Count)
                throw new ArgumentOutOfRangeException(nameof(completionTaskIndex), completionTaskIndex, "Quest completion task index must reference an existing task.");

            Id = id;
            DisplayName = displayName.Trim();
            OneTime = oneTime;
            ResourceBindings = resourceBindings;
            _tasks = buffer.ToArray();
            CompletionTaskIndex = completionTaskIndex;
        }

        /// <summary>Stable deterministic quest definition id.</summary>
        public QuestId Id { get; }
        /// <summary>Human-readable deterministic display name for this quest template.</summary>
        public string DisplayName { get; }
        /// <summary>True when this quest definition should only be started once.</summary>
        public bool OneTime { get; }
        /// <summary>Immutable DFU-style resource bindings for this quest template.</summary>
        public QuestResourceBinding ResourceBindings { get; }
        /// <summary>Ordered deterministic task templates for this quest.</summary>
        public IReadOnlyList<QuestTask> Tasks
        {
            get { return _tasks; }
        }

        /// <summary>Task index that marks this quest as terminal for the runtime system.</summary>
        public int CompletionTaskIndex { get; }

        /// <summary>Creates fresh runtime task instances so shared definitions stay reusable.</summary>
        public IReadOnlyList<QuestTask> CreateTaskInstances()
        {
            var clones = new QuestTask[_tasks.Length];
            for (var i = 0; i < _tasks.Length; i++)
                clones[i] = _tasks[i].Clone();
            return clones;
        }
    }
}
