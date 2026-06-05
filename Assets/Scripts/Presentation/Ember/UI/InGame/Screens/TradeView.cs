using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using EmberCrpg.Presentation.Ember.UI.InGame;
using static EmberCrpg.Presentation.Ember.UI.InGame.IgDesign;

namespace EmberCrpg.Presentation.Ember.UI.InGame.Screens
{
    public sealed class TradeView
    {
        private readonly VisualElement _overlay;

        public TradeView(VisualElement stageCanvas, Action onClose, Action<string> onTrade = null)
        {
            _overlay = IgModal.Build("Trade", false, () => { Close(); onClose?.Invoke(); }, out var content);
            content.style.flexDirection = FlexDirection.Column;

            content.Add(BuildHeader());
            var body = Row();
            body.style.flexGrow = 1;
            content.Add(body);
            // TODO(real-data): no host source yet.
            body.Add(BuildMerchantPane(IgMockData.MerchantItems[0].Id));
            body.Add(BuildInventoryPane(IgMockData.Inventory != null && IgMockData.Inventory.Length > 0 ? IgMockData.Inventory[0].Id : -1));
            content.Add(BuildTransactionBar(onClose, onTrade));

            stageCanvas.Add(_overlay);
        }

        public void Close() { _overlay?.RemoveFromHierarchy(); }

        private static VisualElement BuildHeader()
        {
            var bar = Row();
            bar.style.alignItems = Align.Center;
            bar.style.paddingTop = 12;
            bar.style.paddingBottom = 12;
            bar.style.paddingLeft = 22;
            bar.style.paddingRight = 22;
            bar.style.borderBottomWidth = 1;
            bar.style.borderBottomColor = PA(0.10f);
            bar.style.flexShrink = 0;

            var portrait = new VisualElement();
            portrait.style.width = 42;
            portrait.style.height = 42;
            portrait.style.marginRight = 14;
            portrait.style.backgroundColor = Alpha(Panel, 0.72f);
            Border(portrait, PA(0.18f), 1);
            Radius(portrait, 8);
            portrait.style.alignItems = Align.Center;
            portrait.style.justifyContent = Justify.Center;
            portrait.Add(Text("M", Serif, 18, PA(0.40f)));
            bar.Add(portrait);

            var textWrap = new VisualElement();
            textWrap.Add(Text("Mirra's Goods", Sans, 13, Parch, FontStyle.Bold));
            textWrap.Add(Text("General Merchant · Ashton", Serif, 12, PA(0.45f), FontStyle.Italic));
            bar.Add(textWrap);
            var gold = Text("⊙ 420 gp", Sans, 13, Amber, FontStyle.Bold);
            gold.style.marginLeft = StyleKeyword.Auto;
            bar.Add(gold);
            return bar;
        }

        private static ScrollView BuildMerchantPane(string selectedId)
        {
            var pane = new ScrollView();
            pane.style.flexGrow = 1;
            pane.style.borderRightWidth = 1;
            pane.style.borderRightColor = PA(0.08f);
            pane.style.paddingTop = 16;
            pane.style.paddingBottom = 16;
            pane.style.paddingLeft = 18;
            pane.style.paddingRight = 18;
            var label = Text("MERCHANT", Sans, 10, Gold, FontStyle.Bold);
            label.style.letterSpacing = 1.8f;
            label.style.marginBottom = 12;
            pane.Add(label);
            for (int i = 0; i < IgMockData.MerchantItems.Length; i++)
                pane.Add(BuildTradeRow(IgMockData.MerchantItems[i].Name, "×" + IgMockData.MerchantItems[i].Quantity, IgMockData.MerchantItems[i].Value + " gp", selectedId == IgMockData.MerchantItems[i].Id, Gold));
            return pane;
        }

        private static ScrollView BuildInventoryPane(int selectedId)
        {
            var pane = new ScrollView();
            pane.style.flexGrow = 1;
            pane.style.paddingTop = 16;
            pane.style.paddingBottom = 16;
            pane.style.paddingLeft = 18;
            pane.style.paddingRight = 18;
            var label = Text($"YOUR INVENTORY · {IgMockData.Player.Gold} GP", Sans, 10, Gold, FontStyle.Bold);
            label.style.letterSpacing = 1.8f;
            label.style.marginBottom = 12;
            pane.Add(label);
            for (int i = 0; i < IgMockData.Inventory.Length; i++)
            {
                if (IgMockData.Inventory[i].Type == "Quest") continue;
                string qty = IgMockData.Inventory[i].Quantity > 1 ? "×" + IgMockData.Inventory[i].Quantity : string.Empty;
                pane.Add(BuildTradeRow(IgMockData.Inventory[i].Name, qty, Mathf.FloorToInt(IgMockData.Inventory[i].Value * 0.6f) + " gp", selectedId == IgMockData.Inventory[i].Id, Success));
            }
            return pane;
        }

        private static VisualElement BuildTransactionBar(Action onClose, Action<string> onTrade)
        {
            var bar = Row();
            bar.style.alignItems = Align.Center;
            bar.style.paddingTop = 14;
            bar.style.paddingBottom = 14;
            bar.style.paddingLeft = 22;
            bar.style.paddingRight = 22;
            bar.style.borderTopWidth = 1;
            bar.style.borderTopColor = PA(0.10f);
            bar.style.flexShrink = 0;
            bar.Add(Text("Buy: Iron Sword — 65 gp", Sans, 13, ParchDim));
            var sell = Text("Sell: Iron Shortsword + 24 gp", Sans, 13, ParchDim);
            sell.style.marginLeft = 16;
            bar.Add(sell);
            var actions = Row();
            actions.style.marginLeft = StyleKeyword.Auto;
            actions.Add(BuildButton("BUY", true, () => onTrade?.Invoke("buy")));
            actions.Add(BuildButton("SELL", true, () => onTrade?.Invoke("sell")));
            var done = new Button(() => onClose?.Invoke()) { text = "DONE" };
            ResetButton(done);
            done.style.height = 36;
            done.style.paddingLeft = 18;
            done.style.paddingRight = 18;
            done.style.backgroundColor = Alpha(Panel, 0.62f);
            done.style.color = Parch;
            done.style.fontSize = 12;
            done.style.letterSpacing = 0.8f;
            ApplyFont(done, Sans);
            Border(done, PA(0.18f), 1);
            Radius(done, 7);
            actions.Add(done);
            bar.Add(actions);
            return bar;
        }

        private static VisualElement BuildTradeRow(string name, string qty, string price, bool selected, Color accent)
        {
            var row = Row();
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 6;
            row.style.paddingTop = 10;
            row.style.paddingBottom = 10;
            row.style.paddingLeft = 14;
            row.style.paddingRight = 14;
            row.style.backgroundColor = selected ? GA(0.10f) : Dark(0.55f);
            Border(row, selected ? Gold : PA(0.10f), selected ? 2 : 1);
            Radius(row, 9);
            var title = Text(name, Sans, 13, selected ? Parch : ParchDim, FontStyle.Bold);
            title.style.flexGrow = 1;
            row.Add(title);
            if (!string.IsNullOrEmpty(qty))
            {
                var q = Text(qty, Sans, 11, PA(0.35f));
                q.style.marginLeft = 12;
                row.Add(q);
            }
            var val = Text(price, Sans, 12, accent, FontStyle.Bold);
            val.style.marginLeft = 12;
            row.Add(val);
            return row;
        }

        private static Button BuildButton(string text, bool active, Action onClick)
        {
            var button = new Button(() => onClick?.Invoke()) { text = text };
            ResetButton(button);
            button.style.height = 36;
            button.style.paddingLeft = 18;
            button.style.paddingRight = 18;
            button.style.backgroundColor = active ? Gold : Alpha(Panel, 0.62f);
            button.style.color = active ? Ink : Parch;
            Border(button, active ? Amber : PA(0.18f), 1);
            Radius(button, 7);
            button.style.fontSize = 12;
            button.style.letterSpacing = 0.8f;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            ApplyFont(button, Sans);
            return button;
        }
    }
}
