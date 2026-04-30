using System.Linq;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Memory;

// Design note:
// NpcMemoryQueryService is Ember's thin deterministic DM query layer for saved NPC memory.
// Inputs: NpcMemoryStore/ActorMemory plus stable actor/topic/subject ids.
// Outputs: compact interaction contexts and rule states used by live services for player-facing text.
// Bible reference: ARCHITECTURE.md DM query surface + ActorMemory dialogueSeen/events/transactions.
namespace EmberCrpg.Simulation.Narrative
{
    /// <summary>Pure rules over persistent NPC memory; no Unity, no RNG, no narrative invention.</summary>
    public sealed class NpcMemoryQueryService
    {
        public DialogueMemoryContext GetDialogueContext(NpcMemoryStore store, ActorId npcId, string topicId)
        {
            var memory = GetMemory(store, npcId);
            var topicAskCount = CountEvents(memory, ActorMemoryEventTypes.DialogueTopic, topicId);
            var hasSeenTopic = memory != null && memory.HasDialogueSeen(topicId);
            var totalDialogueEvents = memory?.CountEvents(ActorMemoryEventTypes.DialogueTopic) ?? 0;
            var distinctTopicsSeen = memory?.DialogueSeen.Count ?? 0;

            DialogueMemoryState state;
            if (!hasSeenTopic && topicAskCount == 0)
                state = DialogueMemoryState.NewTopic;
            else if (topicAskCount <= 1)
                state = DialogueMemoryState.RememberedTopic;
            else
                state = DialogueMemoryState.WellWornTopic;

            return new DialogueMemoryContext(topicId, topicAskCount, distinctTopicsSeen, totalDialogueEvents, state);
        }

        public GuardMemoryContext GetGuardContext(NpcMemoryStore store, ActorId guardId, string passageId)
        {
            var memory = GetMemory(store, guardId);
            var passageRequestCount = CountEvents(memory, ActorMemoryEventTypes.PassageRequested, passageId);
            var clearanceGranted = memory != null && memory.Events.Any(candidate =>
                candidate.EventType == ActorMemoryEventTypes.ClearanceGranted && candidate.SubjectId == (passageId ?? string.Empty));

            GuardStance stance;
            if (clearanceGranted)
                stance = GuardStance.Cleared;
            else if (passageRequestCount == 0)
                stance = GuardStance.InitialChallenge;
            else if (passageRequestCount == 1)
                stance = GuardStance.FinalWarning;
            else
                stance = GuardStance.PostClosed;

            return new GuardMemoryContext(passageId, passageRequestCount, clearanceGranted, stance);
        }

        public MerchantMemoryContext GetMerchantContext(NpcMemoryStore store, ActorId merchantId)
        {
            var memory = GetMemory(store, merchantId);
            var transactionCount = memory?.Transactions.Count ?? 0;
            var gateWritTransactions = memory?.Transactions.Count(candidate => candidate.TransactionType == "IssueGateWrit") ?? 0;
            var tradeEventCount = memory?.CountEvents(ActorMemoryEventTypes.TradedWith) ?? 0;

            MerchantFamiliarity familiarity;
            if (transactionCount == 0 && tradeEventCount == 0)
                familiarity = MerchantFamiliarity.Stranger;
            else if (transactionCount + tradeEventCount <= 2)
                familiarity = MerchantFamiliarity.Recognized;
            else
                familiarity = MerchantFamiliarity.Trusted;

            return new MerchantMemoryContext(transactionCount, gateWritTransactions, tradeEventCount, familiarity);
        }

        private static ActorMemory GetMemory(NpcMemoryStore store, ActorId actorId)
        {
            if (store == null)
                return null;
            return store.TryGet(actorId, out var memory) ? memory : null;
        }

        private static int CountEvents(ActorMemory memory, string eventType, string subjectId)
        {
            if (memory == null)
                return 0;
            var normalizedSubject = subjectId ?? string.Empty;
            return memory.Events.Count(candidate => candidate.EventType == eventType && candidate.SubjectId == normalizedSubject);
        }
    }

    public readonly struct DialogueMemoryContext
    {
        public DialogueMemoryContext(string topicId, int topicAskCount, int distinctTopicsSeen, int totalDialogueEvents, DialogueMemoryState state)
        {
            TopicId = topicId ?? string.Empty;
            TopicAskCount = topicAskCount;
            DistinctTopicsSeen = distinctTopicsSeen;
            TotalDialogueEvents = totalDialogueEvents;
            State = state;
        }

        public string TopicId { get; }
        public int TopicAskCount { get; }
        public int DistinctTopicsSeen { get; }
        public int TotalDialogueEvents { get; }
        public DialogueMemoryState State { get; }
    }

    public enum DialogueMemoryState
    {
        NewTopic,
        RememberedTopic,
        WellWornTopic,
    }

    public readonly struct GuardMemoryContext
    {
        public GuardMemoryContext(string passageId, int passageRequestCount, bool clearanceGranted, GuardStance stance)
        {
            PassageId = passageId ?? string.Empty;
            PassageRequestCount = passageRequestCount;
            ClearanceGranted = clearanceGranted;
            Stance = stance;
        }

        public string PassageId { get; }
        public int PassageRequestCount { get; }
        public bool ClearanceGranted { get; }
        public GuardStance Stance { get; }
    }

    public enum GuardStance
    {
        InitialChallenge,
        FinalWarning,
        PostClosed,
        Cleared,
    }

    public readonly struct MerchantMemoryContext
    {
        public MerchantMemoryContext(int transactionCount, int gateWritTransactions, int tradeEventCount, MerchantFamiliarity familiarity)
        {
            TransactionCount = transactionCount;
            GateWritTransactions = gateWritTransactions;
            TradeEventCount = tradeEventCount;
            Familiarity = familiarity;
        }

        public int TransactionCount { get; }
        public int GateWritTransactions { get; }
        public int TradeEventCount { get; }
        public MerchantFamiliarity Familiarity { get; }
    }

    public enum MerchantFamiliarity
    {
        Stranger,
        Recognized,
        Trusted,
    }
}
