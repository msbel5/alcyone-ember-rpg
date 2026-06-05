using System;
using UnityEngine;
using UnityEngine.UIElements;
using EmberCrpg.Presentation.Ember.UI;
using static EmberCrpg.Presentation.Ember.UI.InGame.IgDesign;

namespace EmberCrpg.Presentation.Ember.UI.InGame.Screens
{
    public sealed class TradeView
    {
        private readonly VisualElement _overlay;
        private readonly VisualElement _content;
        private readonly Action<TradeActionRequest> _onTrade;

        public TradeView(VisualElement stageCanvas, Action onClose, Action<TradeActionRequest> onTrade)
        {
            _onTrade = onTrade;
            _overlay = IgModal.Build("Trade", false, () => { Close(); onClose?.Invoke(); }, out _content);
            stageCanvas.Add(_overlay);
            Refresh();
        }

        public void Close() { _overlay?.RemoveFromHierarchy(); }

        public void Refresh()
        {
            _content.Clear();
            _content.style.flexDirection = FlexDirection.Column;
            var data = IgTradeData.Current ?? IgTradeData.Default;
            _content.Add(BuildHeader(data));

            var body = Row();
            body.style.flexGrow = 1;
            body.style.minHeight = 0;
            body.Add(BuildPane("Merchant Stock", data.MerchantName + " · " + data.SettlementName, data.MerchantOffers));
            body.Add(BuildPane("Sell From Pack", "Your current carry stock", data.PlayerOffers));
            _content.Add(body);

            var status = Text(data.StatusLine ?? string.Empty, Sans, 12, data.StatusLine?.StartsWith("Bought", StringComparison.Ordinal) == true || data.StatusLine?.StartsWith("Sold", StringComparison.Ordinal) == true ? Success : ParchDim);
            status.style.whiteSpace = WhiteSpace.Normal;
            status.style.marginTop = 12;
            _content.Add(status);
        }

        private VisualElement BuildHeader(TradeScreenData data)
        {
            var header = Row();
            header.style.alignItems = Align.Center;
            header.style.paddingTop = 12;
            header.style.paddingBottom = 12;
            header.style.paddingLeft = 22;
            header.style.paddingRight = 22;
            header.style.borderBottomWidth = 1;
            header.style.borderBottomColor = PA(0.10f);
            header.Add(Text(data.MerchantName, Sans, 13, Parch, FontStyle.Bold));
            var gold = Text("⊙ " + data.PlayerGold + " gp", Sans, 13, Amber, FontStyle.Bold);
            gold.style.marginLeft = StyleKeyword.Auto;
            header.Add(gold);
            return header;
        }

        private VisualElement BuildPane(string title, string subtitle, TradeOfferData[] offers)
        {
            var pane = new VisualElement();
            pane.style.flexGrow = 1;
            pane.style.minWidth = 0;
            pane.style.paddingTop = 18;
            pane.style.paddingBottom = 18;
            pane.style.paddingLeft = 18;
            pane.style.paddingRight = 18;
            pane.style.borderRightWidth = title == "Merchant Stock" ? 1 : 0;
            pane.style.borderRightColor = PA(0.10f);

            pane.Add(Text(title.ToUpperInvariant(), Sans, 10, Gold, FontStyle.Bold));
            var note = Text(subtitle, Sans, 11, PA(0.42f));
            note.style.marginTop = 4;
            note.style.marginBottom = 12;
            pane.Add(note);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            scroll.style.minHeight = 0;
            StyleScroll(scroll);
            pane.Add(scroll);

            if (offers == null || offers.Length == 0)
            {
                scroll.Add(EmptyState(title, "No entries are available.", "This ledger will fill when the underlying trade source exposes stock."));
                return pane;
            }

            for (int i = 0; i < offers.Length; i++)
                scroll.Add(BuildOffer(offers[i]));
            return pane;
        }

        private VisualElement BuildOffer(TradeOfferData offer)
        {
            var card = new VisualElement();
            card.style.marginBottom = 10;
            card.style.paddingLeft = 14;
            card.style.paddingRight = 14;
            card.style.paddingTop = 12;
            card.style.paddingBottom = 12;
            card.style.backgroundColor = Dark(0.62f);
            Border(card, offer.ActionKind == TradeActionKind.Buy ? Alpha(Amber, 0.30f) : Alpha(Success, 0.30f), 1);
            Radius(card, 10);

            var top = Row();
            top.style.alignItems = Align.Center;
            top.Add(Text(offer.Name, Sans, 13, Parch, FontStyle.Bold));
            var price = Text(offer.UnitPrice + " gp", Sans, 11, Amber, FontStyle.Bold);
            price.style.marginLeft = StyleKeyword.Auto;
            top.Add(price);
            card.Add(top);

            var meta = Text(offer.Category + " · qty " + offer.Quantity + (offer.Equipped ? " · equipped" : string.Empty), Sans, 10, PA(0.38f));
            meta.style.marginTop = 4;
            card.Add(meta);

            var button = new Button(() => _onTrade?.Invoke(new TradeActionRequest(offer.ActionKind, offer.TemplateId)))
            {
                text = offer.ActionKind == TradeActionKind.Buy ? "BUY" : "SELL"
            };
            ResetButton(button);
            button.SetEnabled(offer.ActionKind == TradeActionKind.Sell ? !offer.Equipped : offer.CanAfford);
            button.style.height = 32;
            button.style.marginTop = 10;
            button.style.backgroundColor = button.enabledSelf ? Gold : Alpha(Panel, 0.62f);
            button.style.color = button.enabledSelf ? Ink : PA(0.55f);
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            button.style.letterSpacing = 0.8f;
            ApplyFont(button, Sans);
            Border(button, button.enabledSelf ? Amber : PA(0.18f), 1);
            Radius(button, 7);
            card.Add(button);
            return card;
        }
    }
}
