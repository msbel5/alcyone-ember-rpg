using EmberCrpg.Domain.Core;

// Design note:
// TransactionRecord is a deterministic merchant-memory ledger row.
// Inputs: timestamp plus stable transaction/item/count/currency-delta ids.
// Outputs: pure data that can be serialized and later used by rules or AI narration.
// Bible reference: ARCHITECTURE.md ActorMemory.transactions.
namespace EmberCrpg.Domain.Memory
{
    /// <summary>Saved merchant/innkeeper transaction fact.</summary>
    public readonly struct TransactionRecord
    {
        public TransactionRecord(GameTime timestamp, string transactionType, string itemTemplateId, int count, int goldDelta)
        {
            Timestamp = timestamp;
            TransactionType = transactionType ?? string.Empty;
            ItemTemplateId = itemTemplateId ?? string.Empty;
            Count = count;
            GoldDelta = goldDelta;
        }

        public GameTime Timestamp { get; }
        public string TransactionType { get; }
        public string ItemTemplateId { get; }
        public int Count { get; }
        public int GoldDelta { get; }
    }
}
