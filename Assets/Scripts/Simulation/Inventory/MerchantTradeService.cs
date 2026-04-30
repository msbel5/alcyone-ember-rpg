using EmberCrpg.Domain.Memory;
using EmberCrpg.Domain.World;

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
        public string TradeGateWrit(SliceWorldState world)
        {
            if (world.Player.Position.ManhattanDistanceTo(world.Merchant.Position) > 2)
                return "Stand closer to Quartermaster Ivo before trying to trade.";
            if (!world.MerchantInventory.Contains(SliceItemCatalog.GateWritTemplateId))
                return "Quartermaster Ivo has no gate writs left to issue.";
            if (!world.PlayerInventory.Contains(SliceItemCatalog.EmberShardTemplateId))
                return "Bring Quartermaster Ivo one Ember Shard and he will issue a gate writ.";

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
            var memory = world.NpcMemory.GetOrCreate(world.Merchant.Id);
            memory.RecordEvent(new InteractionEvent(
                world.Time,
                ActorMemoryEventTypes.TradedWith,
                world.Player.Id,
                "gate_writ_trade",
                SliceItemCatalog.GateWritTemplateId,
                1,
                world.Merchant.Position));
            memory.RecordTransaction(new TransactionRecord(
                world.Time,
                "IssueGateWrit",
                SliceItemCatalog.GateWritTemplateId,
                1,
                0));
            return "Quartermaster Ivo trades one Ember Shard for a sealed gate writ.";
        }
    }
}
