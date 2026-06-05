using System;
using EmberCrpg.Presentation.Ember.UI.InGame;
using UnityEngine;
using UnityEngine.UIElements;
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

            var header = Row();
            header.style.alignItems = Align.Center;
            header.style.paddingTop = 12;
            header.style.paddingBottom = 12;
            header.style.paddingLeft = 22;
            header.style.paddingRight = 22;
            header.style.borderBottomWidth = 1;
            header.style.borderBottomColor = PA(0.10f);
            header.Add(Text("Trade Ledger", Sans, 13, Parch, FontStyle.Bold));
            var gold = Text("⊙ " + IgMockData.Player.Gold + " gp", Sans, 13, Amber, FontStyle.Bold);
            gold.style.marginLeft = StyleKeyword.Auto;
            header.Add(gold);
            content.Add(header);

            content.Add(EmptyState(
                "No Merchant Stock",
                "No merchant inventory is available here yet.",
                "The trade UI is mounted, but there is no live store or caravan stock source behind it."));

            stageCanvas.Add(_overlay);
        }

        public void Close() { _overlay?.RemoveFromHierarchy(); }
    }
}
