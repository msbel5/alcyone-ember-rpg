using System;
using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Memory;
using EmberCrpg.Domain.Process;

// Design note:
// ActorRecord is Sprint 1's deterministic actor state bag for movement, combat, dialogue, and saves.
// Inputs: core identity, role, stats, vitals, position, and narrow combat fields.
// Outputs: mutable pure-Domain actor state with no Unity dependency.
// Bible reference: ARCHITECTURE.md Part 1, PRD FR-01/FR-02/FR-04.
namespace EmberCrpg.Domain.Actors
{
    /// <summary>Pure actor record used by slice simulation and save/load mapping.</summary>
    public sealed class ActorRecord
    {
        private readonly List<string> _topicIds;
        private readonly List<string> _askedTopicIds;
        private readonly List<ActorJobPreference> _jobPreferences;

        public ActorRecord(
            ActorId id,
            string name,
            ActorRole role,
            EmberStatBlock stats,
            ActorVitals vitals,
            GridPosition position,
            int accuracy,
            int dodge,
            int armor,
            int baseDamage,
            IEnumerable<string> topicIds = null,
            IEnumerable<ActorJobPreference> jobPreferences = null,
            ActorScheduleState scheduleState = default,
            ActorNeeds needs = default,
            ActorMood mood = default,
            MemoryComponent memory = null,
            GridPosition? home = null,
            GridPosition? dayAnchor = null)
        {
            Id = id;
            Name = name;
            Role = role;
            Stats = stats;
            Vitals = vitals;
            Position = position;
            Home = home ?? position;
            DayAnchor = dayAnchor ?? position;
            Accuracy = accuracy;
            Dodge = dodge;
            Armor = armor;
            BaseDamage = baseDamage;
            _topicIds = topicIds == null ? new List<string>() : new List<string>(topicIds);
            _askedTopicIds = new List<string>();
            _jobPreferences = new List<ActorJobPreference>();
            ApplyJobPreferences(jobPreferences);
            ScheduleState = scheduleState;
            Needs = needs;
            Mood = mood;
            Memory = memory ?? (id.IsEmpty ? null : new MemoryComponent(id));
            if (Memory != null && !Memory.OwnerId.Equals(id))
                throw new ArgumentException("Actor memory owner must match the actor id.", nameof(memory));
        }

        public ActorId Id { get; }
        public string Name { get; }
        public ActorRole Role { get; }
        public EmberStatBlock Stats { get; }
        public GridPosition Position { get; private set; }
        public GridPosition Home { get; private set; }
        public GridPosition DayAnchor { get; private set; }
        public ActorVitals Vitals { get; private set; }
        public int Accuracy { get; }
        public int Dodge { get; }
        public int Armor { get; }
        public int BaseDamage { get; }
        public bool IsAlive => !Vitals.IsDead;
        public IReadOnlyList<string> TopicIds => _topicIds;
        public IReadOnlyList<string> AskedTopicIds => _askedTopicIds;
        public IReadOnlyList<ActorJobPreference> JobPreferences => _jobPreferences;
        public ActorScheduleState ScheduleState { get; private set; }
        public ActorNeeds Needs { get; private set; }
        public ActorMood Mood { get; private set; }
        public MemoryComponent Memory { get; private set; }

        public void MoveTo(GridPosition position)
        {
            Position = position;
        }

        public ActorRecord WithHomeAndAnchor(GridPosition home, GridPosition dayAnchor)
        {
            var copy = new ActorRecord(
                Id,
                Name,
                Role,
                Stats,
                Vitals,
                Position,
                Accuracy,
                Dodge,
                Armor,
                BaseDamage,
                _topicIds,
                _jobPreferences,
                ScheduleState,
                Needs,
                Mood,
                Memory,
                home,
                dayAnchor);
            copy.ReplaceAskedTopics(_askedTopicIds);
            return copy;
        }

        public void ApplyVitals(ActorVitals vitals)
        {
            Vitals = vitals;
        }

        public void RecordTopic(string topicId)
        {
            if (!_askedTopicIds.Contains(topicId))
                _askedTopicIds.Add(topicId);
        }

        public void ReplaceAskedTopics(IEnumerable<string> topicIds)
        {
            _askedTopicIds.Clear();
            if (topicIds == null)
                return;

            foreach (var topicId in topicIds)
            {
                RecordTopic(topicId);
            }
        }

        public void ApplyJobPreferences(IEnumerable<ActorJobPreference> preferences)
        {
            if (preferences == null)
            {
                _jobPreferences.Clear();
                return;
            }

            var replacement = new List<ActorJobPreference>();

            foreach (var preference in preferences)
            {
                if (replacement.Any(existing => existing.Kind == preference.Kind))
                    throw new InvalidOperationException($"ActorRecord already has a preference for {preference.Kind}.");

                replacement.Add(preference);
            }

            _jobPreferences.Clear();
            _jobPreferences.AddRange(replacement);
        }

        public void ApplyScheduleState(ActorScheduleState scheduleState)
        {
            ScheduleState = scheduleState;
        }

        public void ApplyNeeds(ActorNeeds needs)
        {
            Needs = needs;
        }

        public void ApplyMood(ActorMood mood)
        {
            Mood = mood;
        }

        public void ApplyMemory(MemoryComponent memory)
        {
            if (memory == null)
                throw new ArgumentNullException(nameof(memory));
            if (!memory.OwnerId.Equals(Id))
                throw new ArgumentException("Actor memory owner must match the actor id.", nameof(memory));

            Memory = memory;
        }
    }
}
