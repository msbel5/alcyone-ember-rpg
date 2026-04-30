using System.Collections.Generic;

// Design note:
// ActorMemory stores one NPC's persistent recent events and seen dialogue topics.
// Inputs: remembered interaction events from deterministic simulation services.
// Outputs: bounded, saveable memory state for DM queries and future simulation.
// Bible reference: ARCHITECTURE.md §1.4 ActorMemory, §3 DM query surface.
namespace EmberCrpg.Domain.Memory
{
    /// <summary>Persistent per-NPC memory record for the slice.</summary>
    public sealed class ActorMemory
    {
        private const int EventCapacity = 64;
        private readonly List<ActorMemoryEvent> _events = new List<ActorMemoryEvent>();
        private readonly HashSet<string> _dialogueSeen = new HashSet<string>();

        public ActorMemory(EmberCrpg.Domain.Core.ActorId ownerId)
        {
            OwnerId = ownerId;
        }

        public EmberCrpg.Domain.Core.ActorId OwnerId { get; }
        public IReadOnlyList<ActorMemoryEvent> Events => _events;
        public IReadOnlyCollection<string> DialogueSeen => _dialogueSeen;

        public void Remember(ActorMemoryEvent entry)
        {
            if (_events.Count == EventCapacity)
                _events.RemoveAt(0);

            _events.Add(entry);
            if (!string.IsNullOrEmpty(entry.TopicId))
                _dialogueSeen.Add(entry.TopicId);
        }

        public void Replace(IEnumerable<ActorMemoryEvent> events, IEnumerable<string> dialogueSeen)
        {
            _events.Clear();
            _dialogueSeen.Clear();

            if (dialogueSeen != null)
            {
                foreach (var topicId in dialogueSeen)
                {
                    if (!string.IsNullOrWhiteSpace(topicId))
                        _dialogueSeen.Add(topicId);
                }
            }

            if (events == null)
                return;

            foreach (var entry in events)
                Remember(entry);
        }
    }
}
