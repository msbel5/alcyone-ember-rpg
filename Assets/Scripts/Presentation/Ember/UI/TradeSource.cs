using System.Collections.Generic;

namespace EmberCrpg.Presentation.Ember.UI
{
    public enum TradeActionKind
    {
        Buy = 0,
        Sell = 1,
    }

    public readonly struct TradeActionRequest
    {
        public readonly TradeActionKind Kind;
        public readonly string TemplateId;

        public TradeActionRequest(TradeActionKind kind, string templateId)
        {
            Kind = kind;
            TemplateId = templateId ?? string.Empty;
        }
    }

    public readonly struct TradeActionResult
    {
        public readonly bool Success;
        public readonly string Message;

        public TradeActionResult(bool success, string message)
        {
            Success = success;
            Message = message ?? string.Empty;
        }
    }

    public readonly struct TradeItemRow
    {
        public readonly string TemplateId;
        public readonly string Name;
        public readonly string Category;
        public readonly int Quantity;
        public readonly int UnitPrice;
        public readonly bool CanAfford;
        public readonly bool Equipped;

        public TradeItemRow(string templateId, string name, string category, int quantity, int unitPrice, bool canAfford, bool equipped)
        {
            TemplateId = templateId ?? string.Empty;
            Name = name ?? string.Empty;
            Category = category ?? string.Empty;
            Quantity = quantity;
            UnitPrice = unitPrice;
            CanAfford = canAfford;
            Equipped = equipped;
        }
    }

    public readonly struct TradeLedgerState
    {
        public readonly string MerchantName;
        public readonly string SettlementName;
        public readonly int PlayerGold;
        public readonly int MerchantGold;
        public readonly IReadOnlyList<TradeItemRow> MerchantItems;
        public readonly IReadOnlyList<TradeItemRow> PlayerItems;

        public TradeLedgerState(
            string merchantName,
            string settlementName,
            int playerGold,
            int merchantGold,
            IReadOnlyList<TradeItemRow> merchantItems,
            IReadOnlyList<TradeItemRow> playerItems)
        {
            MerchantName = merchantName ?? string.Empty;
            SettlementName = settlementName ?? string.Empty;
            PlayerGold = playerGold;
            MerchantGold = merchantGold;
            MerchantItems = merchantItems ?? System.Array.Empty<TradeItemRow>();
            PlayerItems = playerItems ?? System.Array.Empty<TradeItemRow>();
        }
    }

    public interface ITradeSource
    {
        TradeLedgerState ReadTradeState();
    }

    public interface ITradeCommandSink
    {
        TradeActionResult ExecuteTrade(TradeActionRequest request);
    }
}
