using System;
using System.Collections.Generic;
using EmberCrpg.Data.Content;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Simulation.Inventory;

namespace EmberCrpg.Presentation.Ember.Adapters
{
    public sealed partial class DomainSimulationAdapter : ITradeSource, ITradeCommandSink
    {
        private readonly SettlementTradeService _tradeService = new SettlementTradeService();

        public TradeLedgerState ReadTradeState()
        {
            EnsureTradeStock();
            var merchantItems = BuildTradeRows(_world?.MerchantInventory, isSellSide: false);
            var playerItems = BuildTradeRows(_world?.PlayerInventory, isSellSide: true);
            return new TradeLedgerState(
                ResolveMerchantName(),
                ResolveStartingSettlementName() ?? "Current Holding",
                _world?.PlayerGold ?? 0,
                _world?.MerchantGold ?? 0,
                merchantItems,
                playerItems);
        }

        public TradeActionResult ExecuteTrade(TradeActionRequest request)
        {
            var templateId = request.TemplateId ?? string.Empty;
            var meta = ResolveTradeMeta(templateId);
            if (meta.BasePrice <= 0)
                return new TradeActionResult(false, "That item has no trade value yet.");

            var pre = _world?.Actors?.FirstByRole(ActorRole.Player)?.Stats.Pre ?? 50;
            int basis = LivePriceOr(meta.BasePrice, templateId);
            TradeOperationResult result = request.Kind == TradeActionKind.Buy
                ? _tradeService.TryBuy(_world, templateId, _tradeService.ComputeBuyPrice(basis, pre))
                : _tradeService.TrySell(_world, templateId, _tradeService.ComputeSellPrice(basis, pre));
            if (result.Success && basis != meta.BasePrice)
                UnityEngine.Debug.Log($"[Trade] live market price used for '{templateId}': {basis} (base {meta.BasePrice}).");
            return new TradeActionResult(result.Success, result.Message);
        }

        private TradeItemRow[] BuildTradeRows(EmberCrpg.Domain.Inventory.InventoryState inventory, bool isSellSide)
        {
            if (inventory == null || inventory.Items.Count == 0)
                return Array.Empty<TradeItemRow>();

            var rows = new List<TradeItemRow>(inventory.Items.Count);
            var pre = _world?.Actors?.FirstByRole(ActorRole.Player)?.Stats.Pre ?? 50;
            for (int i = 0; i < inventory.Items.Count; i++)
            {
                var item = inventory.Items[i];
                if (item == null) continue;
                var meta = ResolveTradeMeta(item.TemplateId, item.DisplayName);
                int basis = LivePriceOr(meta.BasePrice, item.TemplateId);
                int price = isSellSide
                    ? _tradeService.ComputeSellPrice(basis, pre)
                    : _tradeService.ComputeBuyPrice(basis, pre);
                bool canAfford = isSellSide || (_world?.PlayerGold ?? 0) >= price;
                bool equipped = item.IsEquipment && _world?.PlayerEquipment != null && _world.PlayerEquipment.IsEquipped(item.Id);
                rows.Add(new TradeItemRow(item.TemplateId, meta.DisplayName, meta.Category, item.Quantity, price, canAfford, equipped));
            }

            rows.Sort(CompareTradeRows);
            return rows.ToArray();
        }

        // F2/live economy: the sim's PriceSystem writes per-site prices into WorldState.Prices daily. When
        // the home site lists the item, that LIVE market price replaces the static content base price; an
        // unlisted item keeps the base (the ledger returns 0 for unknown tags — safe fallback).
        private int LivePriceOr(int basePrice, string templateId)
        {
            var ledger = _world?.Prices;
            if (ledger == null || string.IsNullOrWhiteSpace(templateId)) return basePrice;
            int live = ledger.GetPrice(SettlementSiteId(CurrentSettlementOrStart), templateId);
            return live > 0 ? live : basePrice;
        }

        private void EnsureTradeStock()
        {
            if (_world == null || _world.MerchantInventory == null)
                return;

            var content = ContentDatabaseProvider.Current;
            var defaults = content?.EconomyConfig?.default_store_inventory;
            if (defaults == null || defaults.Count == 0)
                return;

            var seed = new List<TradeSeedItem>(defaults.Count);
            for (int i = 0; i < defaults.Count; i++)
            {
                var row = defaults[i];
                if (row == null || string.IsNullOrWhiteSpace(row.item_def_id) || row.quantity <= 0)
                    continue;
                var meta = ResolveTradeMeta(row.item_def_id);
                if (meta.BasePrice <= 0 || string.IsNullOrWhiteSpace(meta.DisplayName))
                    continue;
                seed.Add(new TradeSeedItem(row.item_def_id, meta.DisplayName, row.quantity));
            }

            _tradeService.EnsureMerchantStock(_world, seed);
        }

        private TradeMeta ResolveTradeMeta(string templateId, string fallbackName = null)
        {
            if (string.IsNullOrWhiteSpace(templateId))
                return new TradeMeta(fallbackName ?? "Unknown Item", "Misc", 0);

            var content = ContentDatabaseProvider.Current;
            if (content != null && content.Items.TryGetValue(templateId, out var item) && item != null)
                return new TradeMeta(item.name, item.type, item.value > 0 ? item.value : CommodityPrice(content, templateId));

            int fallbackPrice = SpecialPrice(templateId);
            return new TradeMeta(
                string.IsNullOrWhiteSpace(fallbackName) ? HumanizeToken(templateId) : fallbackName,
                "Misc",
                fallbackPrice);
        }

        private static int CommodityPrice(ContentDatabase content, string templateId)
        {
            var commodities = content?.EconomyConfig?.commodities;
            if (commodities == null) return 0;
            for (int i = 0; i < commodities.Count; i++)
            {
                var commodity = commodities[i];
                if (commodity != null && string.Equals(commodity.item_id, templateId, StringComparison.Ordinal))
                    return commodity.base_price;
            }
            return 0;
        }

        private static int SpecialPrice(string templateId)
        {
            switch (templateId)
            {
                case WorldItemCatalog.GateWritTemplateId: return 18;
                case WorldItemCatalog.EmberShardTemplateId: return 12;
                case WorldItemCatalog.AshTrainingBladeTemplateId: return 24;
                default: return 0;
            }
        }

        private string ResolveMerchantName()
        {
            var merchant = _world?.Actors?.FirstByRole(ActorRole.Merchant);
            return merchant == null ? "Quartermaster" : merchant.Name;
        }

        private static int CompareTradeRows(TradeItemRow left, TradeItemRow right)
        {
            int byName = string.CompareOrdinal(left.Name, right.Name);
            if (byName != 0) return byName;
            return string.CompareOrdinal(left.TemplateId, right.TemplateId);
        }

        private static string HumanizeToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var parts = value.Split('_');
            for (int i = 0; i < parts.Length; i++)
            {
                if (string.IsNullOrEmpty(parts[i])) continue;
                var part = parts[i];
                parts[i] = char.ToUpperInvariant(part[0]) + part.Substring(1);
            }
            return string.Join(" ", parts);
        }

        private readonly struct TradeMeta
        {
            public readonly string DisplayName;
            public readonly string Category;
            public readonly int BasePrice;

            public TradeMeta(string displayName, string category, int basePrice)
            {
                DisplayName = displayName ?? string.Empty;
                Category = category ?? string.Empty;
                BasePrice = basePrice;
            }
        }
    }
}
