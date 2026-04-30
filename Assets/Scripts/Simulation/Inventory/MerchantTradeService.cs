using System.Linq;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Memory;
using EmberCrpg.Domain.World;

// Design note:
// MerchantTradeService owns the deterministic Sprint 2 merchant exchange.
// Inputs: world state with player position, inventories, item ids, and merchant memory.
// Outputs: narrow trade results that mutate only pure state and preserve transferred item identity.
// Bible reference: MASTER_MECHANICS_BIBLE.md §31, ARCHITECTURE.md ActorMemory, PRD Sprint 2 FR-03, Sprint 3 hardening.
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

            var playerShard = world.PlayerInventory.Items.FirstOrDefault(item => item.TemplateId == SliceItemCatalog.EmberShardTemplateId);
            if (playerShard != null && playerShard.Quantity > 1 && world.PlayerInventory.IsFull)
                return "Your inventory is too full to carry a sealed gate writ.";

            InventoryItem payment;
            if (!world.PlayerInventory.TryTake(SliceItemCatalog.EmberShardTemplateId, 1, world.ItemIds, out payment))
                return "The trade slips; your Ember Shard never leaves your hand.";

            InventoryItem writ;
            if (!world.MerchantInventory.TryTake(SliceItemCatalog.GateWritTemplateId, 1, world.ItemIds, out writ))
            {
                world.PlayerInventory.TryAdd(payment);
                return "Quartermaster Ivo reaches for a writ that is no longer there.";
            }

            if (!world.PlayerInventory.TryAdd(writ))
            {
                world.PlayerInventory.TryAdd(payment);
                world.MerchantInventory.TryAdd(writ);
                return "Your inventory is too full to carry a sealed gate writ.";
            }

            world.MerchantInventory.TryAdd(payment);
            var merchantMemory = world.NpcMemories == null ? null : world.NpcMemories.GetOrCreate(world.Merchant.Id);
            if (merchantMemory != null)
            {
                merchantMemory.Remember(new ActorMemoryEvent(
                    world.Time,
                    ActorMemoryEventType.TradeCompleted,
                    world.Player.Id,
                    writ.Id,
                    1,
                    string.Empty,
                    "Quartermaster Ivo traded one Ember Shard for a sealed gate writ."));
            }

            return "Quartermaster Ivo trades one Ember Shard for a sealed gate writ.";
        }
    }
}
