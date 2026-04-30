using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Domain.Core;

// Design note:
// ActorMemory is Ember's deterministic per-NPC persistent memory module.
// Inputs: mechanical interaction events, dialogue topic ids, and transaction records.
// Outputs: bounded saveable memory facts for rules/DM queries; it never generates flavor text.
// Bible reference: ARCHITECTURE.md ActorMemory serialized per NPC.
namespace EmberCrpg.Domain.Memory
{
    /// <summary>Persistent mechanical memory attached to one NPC actor.</summary>
    public sealed class ActorMemory
    {
        public const int MaxEvents = 64;

        private readonly List<InteractionEvent> _events = new List<InteractionEvent>();
        private readonly HashSet<string> _dialogueSeen = new HashSet<string>();
        private readonly List<TransactionRecord> _transactions = new List<TransactionRecord>();

        public ActorMemory(ActorId actorId)
        {
            ActorId = actorId;
        }

        public ActorId ActorId { get; }
        public IReadOnlyList<InteractionEvent> Events => _events;
        public IReadOnlyCollection<string> DialogueSeen => _dialogueSeen;
        public IReadOnlyList<TransactionRecord> Transactions => _transactions;

        public void RecordEvent(InteractionEvent interactionEvent)
        {
            if (_events.Count == MaxEvents)
                _events.RemoveAt(0);
            _events.Add(interactionEvent);
        }

        public void MarkDialogueSeen(string topicId)
        {
            if (!string.IsNullOrEmpty(topicId))
                _dialogueSeen.Add(topicId);
        }

        public bool HasDialogueSeen(string topicId)
        {
            return !string.IsNullOrEmpty(topicId) && _dialogueSeen.Contains(topicId);
        }

        public int CountEvents(string eventType)
        {
            return _events.Count(candidate => candidate.EventType == eventType);
        }

        public void RecordTransaction(TransactionRecord transaction)
        {
            _transactions.Add(transaction);
        }

        public void ReplaceEvents(IEnumerable<InteractionEvent> events)
        {
            _events.Clear();
            if (events == null)
                return;

            foreach (var interactionEvent in events)
                RecordEvent(interactionEvent);
        }

        public void ReplaceDialogueSeen(IEnumerable<string> topicIds)
        {
            _dialogueSeen.Clear();
            if (topicIds == null)
                return;

            foreach (var topicId in topicIds)
                MarkDialogueSeen(topicId);
        }

        public void ReplaceTransactions(IEnumerable<TransactionRecord> transactions)
        {
            _transactions.Clear();
            if (transactions == null)
                return;

            foreach (var transaction in transactions)
                RecordTransaction(transaction);
        }
    }
}
