namespace EmberCrpg.Presentation.Ember.UI.InGame.Screens
{
    public static class IgTradeData
    {
        public static readonly TradeScreenData Default = new(
            "Quartermaster",
            "Current Holding",
            0,
            0,
            "No merchant stock is available here yet.",
            System.Array.Empty<TradeOfferData>(),
            System.Array.Empty<TradeOfferData>());

        public static TradeScreenData Current = Default;
    }

    public sealed record TradeScreenData(
        string MerchantName,
        string SettlementName,
        int PlayerGold,
        int MerchantGold,
        string StatusLine,
        TradeOfferData[] MerchantOffers,
        TradeOfferData[] PlayerOffers);

    public sealed record TradeOfferData(
        string TemplateId,
        string Name,
        string Category,
        int Quantity,
        int UnitPrice,
        bool CanAfford,
        bool Equipped,
        EmberCrpg.Presentation.Ember.UI.TradeActionKind ActionKind);
}
