using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using EmberCrpg.Presentation.Ember.UI.InGame;
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
            top.Add(Text("Bandit's Pack", Serif, 16, Parch));
            var actions = Row();
            actions.style.marginLeft = StyleKeyword.Auto;
            actions.Add(BuildButton("TAKE ALL", true, () => onTakeAll?.Invoke()));
            var close = new Button(() => { Close(); onClose?.Invoke(); }) { text = "✕" };
            ResetButton(close);
            close.style.fontSize = 16;
            close.style.color = PA(0.35f);
            ApplyFont(close, Sans);
            actions.Add(close);
            top.Add(actions);
            sheet.Add(top);

            var body = Row();
            sheet.Add(body);
            body.Add(BuildLootColumn());
            var divider = new VisualElement();
            divider.style.width = 1;
            divider.style.backgroundColor = PA(0.08f);
            body.Add(divider);
            // TODO(real-data): no host source yet.
            body.Add(BuildPlayerColumn());

            stageCanvas.Add(_overlay);
        }

        public void Close() { _overlay?.RemoveFromHierarchy(); }

        private static VisualElement BuildLootColumn()
        {
            var column = new VisualElement();
            column.style.flexGrow = 1;
            var label = Text("CONTAINER", Sans, 10, PA(0.38f), FontStyle.Bold);
            label.style.letterSpacing = 1.6f;
            label.style.marginBottom = 10;
            column.Add(label);

            var wrap = new VisualElement();
            wrap.style.flexDirection = FlexDirection.Row;
            wrap.style.flexWrap = Wrap.Wrap;
            column.Add(wrap);
            for (int i = 0; i < IgMockData.LootItems.Length; i++)
                wrap.Add(BuildLootTile(IgMockData.LootItems[i], 74, 10));
            return column;
        }

        private static VisualElement BuildPlayerColumn()
        {
            var column = new VisualElement();
            column.style.flexGrow = 1;
            column.style.marginLeft = 20;
            var label = Text($"YOUR PACK · {IgMockData.Player.Gold} GP", Sans, 10, PA(0.38f), FontStyle.Bold);
            label.style.letterSpacing = 1.6f;
            label.style.marginBottom = 10;
            column.Add(label);

            var wrap = new VisualElement();
            wrap.style.flexDirection = FlexDirection.Row;
            wrap.style.flexWrap = Wrap.Wrap;
            column.Add(wrap);
            for (int i = 0; i < Mathf.Min(8, IgMockData.Inventory.Length); i++)
                wrap.Add(BuildPlayerTile(IgMockData.Inventory[i]));
            return column;
        }

        private static VisualElement BuildLootTile(TradeItemData item, int size, int fontSize)
        {
            var tile = new VisualElement();
            tile.style.width = size;
            tile.style.height = size;
            tile.style.marginRight = 8;
            tile.style.marginBottom = 8;
            tile.style.backgroundColor = Alpha(Panel, 0.72f);
            Border(tile, Alpha(TypeColor(item.Type), 0.27f), 1);
            Radius(tile, 10);
            tile.style.alignItems = Align.Center;
            tile.style.justifyContent = Justify.Center;
            tile.style.paddingTop = 4;
            tile.style.paddingBottom = 4;
            tile.style.paddingLeft = 4;
            tile.style.paddingRight = 4;
            var type = Text(Short(item.Type).ToUpperInvariant(), Sans, 9, TypeColor(item.Type));
            type.style.letterSpacing = 0.6f;
            tile.Add(type);
            var name = Text(item.Name, Sans, fontSize, ParchDim);
            name.style.unityTextAlign = TextAnchor.MiddleCenter;
            name.style.whiteSpace = WhiteSpace.Normal;
            tile.Add(name);
            return tile;
        }

        private static VisualElement BuildPlayerTile(InventoryItemData item)
        {
            var tile = new VisualElement();
            tile.style.width = 54;
            tile.style.height = 54;
            tile.style.marginRight = 8;
            tile.style.marginBottom = 8;
            tile.style.backgroundColor = Alpha(InputBg, 0.65f);
            Border(tile, PA(0.10f), 1);
            Radius(tile, 8);
            tile.style.alignItems = Align.Center;
            tile.style.justifyContent = Justify.Center;
            tile.Add(Text(Short(item.Type).ToUpperInvariant(), Sans, 8, PA(0.35f)));
            var name = Text(item.Name, Sans, 8, ParchDim);
            name.style.whiteSpace = WhiteSpace.Normal;
            name.style.unityTextAlign = TextAnchor.MiddleCenter;
            tile.Add(name);
            return tile;
        }

        private static Button BuildButton(string text, bool active, Action onClick)
        {
            var button = new Button(() => onClick?.Invoke()) { text = text };
            ResetButton(button);
            button.style.height = 34;
            button.style.paddingLeft = 16;
            button.style.paddingRight = 16;
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

        private static Color TypeColor(string type)
        {
            switch (type)
            {
                case "Weapon": return Health;
                case "Food": return Orange;
                case "Tool": return Amber;
                case "Currency": return Gold;
                default: return Parch;
            }
        }

        private static string Short(string type) => type.Substring(0, Mathf.Min(4, type.Length));
    }
}
