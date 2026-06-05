using System;
using EmberCrpg.Presentation.Ember.UI.InGame;
using UnityEngine;
using UnityEngine.UIElements;
using static EmberCrpg.Presentation.Ember.UI.InGame.IgDesign;

namespace EmberCrpg.Presentation.Ember.UI.InGame.Screens
{
    public sealed class LootView
    {
        private readonly VisualElement _overlay;

        public LootView(VisualElement stageCanvas, Action onClose, Action onTakeAll = null)
        {
            _overlay = new VisualElement();
            _overlay.style.position = Position.Absolute;
            _overlay.style.left = 0;
            _overlay.style.right = 0;
            _overlay.style.top = 0;
            _overlay.style.bottom = 0;
            _overlay.style.backgroundColor = C(4, 3, 2, 0.58f);
            _overlay.style.alignItems = Align.Center;
            _overlay.style.justifyContent = Justify.FlexEnd;
            _overlay.style.paddingBottom = Length.Percent(5);
            _overlay.pickingMode = PickingMode.Position;

            var sheet = new VisualElement();
            sheet.style.width = Length.Percent(70);
            sheet.style.maxWidth = 900;
            sheet.style.backgroundColor = Alpha(VoidWarm, 0.95f);
            Border(sheet, PA(0.22f), 1);
            Radius(sheet, 18);
            sheet.style.paddingTop = 22;
            sheet.style.paddingBottom = 22;
            sheet.style.paddingLeft = 26;
            sheet.style.paddingRight = 26;
            _overlay.Add(sheet);

            var top = Row();
            top.style.alignItems = Align.Center;
            top.style.paddingBottom = 12;
            top.style.marginBottom = 18;
            top.style.borderBottomWidth = 1;
            top.style.borderBottomColor = PA(0.10f);
            top.Add(Text("Loot", Serif, 16, Parch));
            var close = new Button(() => { Close(); onClose?.Invoke(); }) { text = "✕" };
            ResetButton(close);
            close.style.marginLeft = StyleKeyword.Auto;
            close.style.fontSize = 16;
            close.style.color = PA(0.35f);
            ApplyFont(close, Sans);
            top.Add(close);
            sheet.Add(top);

            sheet.Add(EmptyState(
                "Container Empty",
                "Nothing here to take.",
                "No live loot source is available for this screen right now."));

            stageCanvas.Add(_overlay);
        }

        public void Close() { _overlay?.RemoveFromHierarchy(); }
    }
}
