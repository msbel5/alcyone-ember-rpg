using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.World;

namespace EmberCrpg.Simulation.Inventory
{
    public readonly struct TradeSeedItem
    {
        public readonly string TemplateId;
        public readonly string DisplayName;
        public readonly int Quantity;

        public TradeSeedItem(string templateId, string displayName, int quantity)
        {
            TemplateId = templateId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Quantity = quantity;
        }
    }

    public readonly struct TradeOperationResult
    {
        public readonly bool Success;
        public readonly string Message;

        public TradeOperationResult(bool success, string message)
        {
            Success = success;
            Message = message ?? string.Empty;
        }
    }

    public sealed class SettlementTradeService
    {
        public void EnsureMerchantStock(WorldState world, IReadOnlyList<TradeSeedItem> seedItems)
        {
            if (world == null || world.MerchantStoreSeeded || world.MerchantInventory == null || seedItems == null)
                return;

            ulong nextId = NextInventoryItemId(world.PlayerInventory, world.MerchantInventory);
            for (int i = 0; i < seedItems.Count; i++)
            {
                var seed = seedItems[i];
                if (string.IsNullOrWhiteSpace(seed.TemplateId) || string.IsNullOrWhiteSpace(seed.DisplayName) || seed.Quantity <= 0)
                    continue;
                if (world.MerchantInventory.Contains(seed.TemplateId))
                    continue;
                if (!world.MerchantInventory.TryAdd(new InventoryItem(new ItemId(nextId++), seed.TemplateId, seed.DisplayName, seed.Quantity)))
                    break;
            }

            world.MerchantStoreSeeded = true;
        }

        public int ComputeBuyPrice(int basePrice, int presence)
        {
            return ApplyFactor(basePrice, 1.20 + PresenceDelta(presence));
        }

        public int ComputeSellPrice(int basePrice, int presence)
        {
            return ApplyFactor(basePrice, 0.55 + PresenceDelta(presence) * 0.5);
        }

        public TradeOperationResult TryBuy(WorldState world, string templateId, int unitPrice)
        {
            if (world == null || world.PlayerInventory == null || world.MerchantInventory == null)
                return new TradeOperationResult(false, "Trade state is unavailable.");

            var stockItem = FindFirst(world.MerchantInventory, templateId);
            if (stockItem == null)
                return new TradeOperationResult(false, "That item is no longer in stock.");
            if (world.PlayerGold < unitPrice)
                return new TradeOperationResult(false, "You do not have enough gold.");
            if (!world.MerchantInventory.TryRemove(stockItem.TemplateId, 1))
                return new TradeOperationResult(false, "The merchant's ledger desynced.");

            var moved = CloneSingle(stockItem, NextInventoryItemId(world.PlayerInventory, world.MerchantInventory));
            if (!world.PlayerInventory.TryAdd(moved))
            {
                world.MerchantInventory.TryAdd(moved);
                return new TradeOperationResult(false, "Your pack has no room for that purchase.");
            }

            world.PlayerGold -= unitPrice;
            world.MerchantGold += unitPrice;
            world.LastNarrative = "Bought " + moved.DisplayName + " for " + unitPrice + " gp.";
            return new TradeOperationResult(true, world.LastNarrative);
        }

        public TradeOperationResult TrySell(WorldState world, string templateId, int unitPrice)
        {
            if (world == null || world.PlayerInventory == null || world.MerchantInventory == null)
                return new TradeOperationResult(false, "Trade state is unavailable.");

            var playerItem = FindFirst(world.PlayerInventory, templateId);
            if (playerItem == null)
                return new TradeOperationResult(false, "You are no longer carrying that item.");
            if (playerItem.IsEquipment && world.PlayerEquipment != null && world.PlayerEquipment.IsEquipped(playerItem.Id))
                return new TradeOperationResult(false, "Unequip that item before selling it.");
            if (world.MerchantGold < unitPrice)
                return new TradeOperationResult(false, "The merchant cannot cover that price.");
            if (!world.PlayerInventory.TryRemove(playerItem.TemplateId, 1, world.PlayerEquipment))
                return new TradeOperationResult(false, "The sale could not be completed.");

            var moved = CloneSingle(playerItem, NextInventoryItemId(world.PlayerInventory, world.MerchantInventory));
            if (!world.MerchantInventory.TryAdd(moved))
            {
                world.PlayerInventory.TryAdd(moved);
                return new TradeOperationResult(false, "The merchant has no room for more stock.");
            }

            world.PlayerGold += unitPrice;
            world.MerchantGold -= unitPrice;
            world.LastNarrative = "Sold " + moved.DisplayName + " for " + unitPrice + " gp.";
            return new TradeOperationResult(true, world.LastNarrative);
        }

        private static double PresenceDelta(int presence)
        {
            var normalized = (presence - 50) * 0.004;
            if (normalized < -0.18) return -0.18;
            if (normalized > 0.18) return 0.18;
            return normalized;
        }

        private static int ApplyFactor(int basePrice, double factor)
        {
            var price = (int)Math.Floor(basePrice * factor);
            return price < 1 ? 1 : price;
        }

        private static InventoryItem FindFirst(InventoryState inventory, string templateId)
        {
            if (inventory == null || string.IsNullOrWhiteSpace(templateId))
                return null;
            for (int i = 0; i < inventory.Items.Count; i++)
            {
                var item = inventory.Items[i];
                if (item != null && string.Equals(item.TemplateId, templateId, StringComparison.Ordinal))
                    return item;
            }
            return null;
        }

        private static InventoryItem CloneSingle(InventoryItem item, ulong nextId)
        {
            return new InventoryItem(new ItemId(nextId), item.TemplateId, item.DisplayName, 1, item.EquipmentSlot, item.AccuracyBonus, item.DamageBonus);
        }

        private static ulong NextInventoryItemId(InventoryState first, InventoryState second)
        {
            ulong max = 0UL;
            max = MaxInventoryId(first, max);
            max = MaxInventoryId(second, max);
            return max + 1UL;
        }

        private static ulong MaxInventoryId(InventoryState inventory, ulong max)
        {
            if (inventory == null) return max;
            for (int i = 0; i < inventory.Items.Count; i++)
            {
                var value = inventory.Items[i].Id.Value;
                if (value > max)
                    max = value;
            }
            return max;
        }
    }
}
