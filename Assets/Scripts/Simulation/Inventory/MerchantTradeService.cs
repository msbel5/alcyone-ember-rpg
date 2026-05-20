using EmberCrpg.Domain.Memory;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Narrative;
using EmberCrpg.Domain.Actors;

// Design note:
// MerchantTradeService owns the deterministic Sprint 2 merchant exchange.
// Inputs: world state with player position, player inventory, and merchant stock.
// Outputs: narrow trade results that mutate only pure inventories and status-driving state.
// Bible reference: MASTER_MECHANICS_BIBLE.md §31, PRD Sprint 2 FR-03.
namespace EmberCrpg.Simulation.Inventory
{
    /// <summary>Rules-based one-item merchant interaction for the slice.</summary>
    public sealed class MerchantTradeService
    {
        private readonly NpcMemoryQueryService _memoryQueries = new NpcMemoryQueryService();

        public string TradeGateWrit(SliceWorldState world)
        {
            if (world.Actors.FirstByRole(ActorRole.Player).Position.ManhattanDistanceTo(world.Actors.FirstByRole(ActorRole.Merchant).Position) > 2)
                return "Stand closer to Quartermaster Ivo before trying to trade.";

            var context = _memoryQueries.GetMerchantContext(world.NpcMemory, world.Actors.FirstByRole(ActorRole.Merchant).Id);
            if (!world.MerchantInventory.Contains(SliceItemCatalog.GateWritTemplateId))
                return context.Familiarity == MerchantFamiliarity.Stranger
                    ? "Quartermaster Ivo has no gate writs left to issue."
                    : "Quartermaster Ivo checks his ledger and remembers your earlier writ. No sealed gate writs remain.";
            if (!world.PlayerInventory.Contains(SliceItemCatalog.EmberShardTemplateId))
                return context.Familiarity == MerchantFamiliarity.Stranger
                    ? "Bring Quartermaster Ivo one Ember Shard and he will issue a gate writ."
                    : "Quartermaster Ivo recognizes you, but still needs one Ember Shard before issuing another writ.";

            if (!world.PlayerInventory.TryRemove(SliceItemCatalog.EmberShardTemplateId, 1))
                return "The trade slips; your Ember Shard never leaves your hand.";
            if (!world.MerchantInventory.TryRemove(SliceItemCatalog.GateWritTemplateId, 1))
            {
                world.PlayerInventory.TryAdd(SliceItemCatalog.CreateEmberShard());
                return "Quartermaster Ivo reaches for a writ that is no longer there.";
            }
            if (!world.PlayerInventory.TryAdd(SliceItemCatalog.CreateGateWrit()))
            {
                world.PlayerInventory.TryAdd(SliceItemCatalog.CreateEmberShard());
                world.MerchantInventory.TryAdd(SliceItemCatalog.CreateGateWrit());
                return "Your inventory is too full to carry a sealed gate writ.";
            }

            world.MerchantInventory.TryAdd(SliceItemCatalog.CreateEmberShard());
            var memory = world.NpcMemory.GetOrCreate(world.Actors.FirstByRole(ActorRole.Merchant).Id);
            memory.RecordEvent(new InteractionEvent(
                world.Time,
                ActorMemoryEventTypes.TradedWith,
                world.Actors.FirstByRole(ActorRole.Player).Id,
                "gate_writ_trade",
                SliceItemCatalog.GateWritTemplateId,
                1,
                world.Actors.FirstByRole(ActorRole.Merchant).Position));
            memory.RecordTransaction(new TransactionRecord(
                world.Time,
                "IssueGateWrit",
                SliceItemCatalog.GateWritTemplateId,
                1,
                0));
            switch (context.Familiarity)
            {
                case MerchantFamiliarity.Stranger:
                    return "Quartermaster Ivo trades one Ember Shard for a sealed gate writ.";
                case MerchantFamiliarity.Recognized:
                    return "Quartermaster Ivo recognizes your useful trade and exchanges another Ember Shard for a sealed gate writ.";
                default:
                    return "Quartermaster Ivo trusts your steady Ember Shard supply and has a sealed gate writ ready.";
            }
        }
    }
}
