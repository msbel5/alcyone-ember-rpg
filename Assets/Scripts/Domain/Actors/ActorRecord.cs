using System.Collections.Generic;
using EmberCrpg.Domain.Core;

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
            IEnumerable<string> topicIds = null)
        {
            Id = id;
            Name = name;
            Role = role;
            Stats = stats;
            Vitals = vitals;
            Position = position;
            Accuracy = accuracy;
            Dodge = dodge;
            Armor = armor;
            BaseDamage = baseDamage;
            _topicIds = topicIds == null ? new List<string>() : new List<string>(topicIds);
            _askedTopicIds = new List<string>();
        }

        public ActorId Id { get; }
        public string Name { get; }
        public ActorRole Role { get; }
        public EmberStatBlock Stats { get; }
        public GridPosition Position { get; private set; }
        public ActorVitals Vitals { get; private set; }
        public int Accuracy { get; }
        public int Dodge { get; }
        public int Armor { get; }
        public int BaseDamage { get; }
        public bool IsAlive => !Vitals.IsDead;
        public IReadOnlyList<string> TopicIds => _topicIds;
        public IReadOnlyList<string> AskedTopicIds => _askedTopicIds;

        public void MoveTo(GridPosition position)
        {
            Position = position;
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
    }
}
