using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using EmberCrpg.Presentation.Ember.UI.InGame;
using static EmberCrpg.Presentation.Ember.UI.InGame.IgDesign;

namespace EmberCrpg.Presentation.Ember.UI.InGame.Screens
{
    public sealed class InventoryView
    {
        private readonly VisualElement _overlay;

        public InventoryView(VisualElement stageCanvas, Action onClose, Action<string> onItemAction = null, string activeTab = "Items")
        {
            _overlay = IgModal.BuildTabbed("Inventory", false, new[] { "Items", "Equipment" }, activeTab,
                tab => { Close(); new InventoryView(stageCanvas, onClose, onItemAction, tab); },
                () => { Close(); onClose?.Invoke(); }, out var body);

            body.style.flexDirection = FlexDirection.Row;
            body.style.backgroundColor = VoidWarm;

            var selected = GetSelectedItem();

            body.Add(BuildEquipmentPane());
            body.Add(BuildInventoryGrid(selected));
            body.Add(BuildDetailPane(selected, onItemAction));

            stageCanvas.Add(_overlay);
        }

        public void Close() { _overlay?.RemoveFromHierarchy(); }

        private static VisualElement BuildEquipmentPane()
        {
            var pane = new VisualElement();
            pane.style.width = 260;
            pane.style.borderRightWidth = 1;
            pane.style.borderRightColor = PA(0.10f);
            pane.style.paddingTop = 20;
            pane.style.paddingBottom = 20;
            pane.style.paddingLeft = 20;
            pane.style.paddingRight = 20;
            pane.style.flexShrink = 0;

            var label = Text("EQUIPMENT", Sans, 10, PA(0.35f), FontStyle.Bold);
            label.style.letterSpacing = 1.8f;
            label.style.marginBottom = 8;
            pane.Add(label);

            var dollWrap = new VisualElement();
            dollWrap.style.flexGrow = 1;
            dollWrap.style.position = Position.Relative;
            dollWrap.style.minHeight = 0;
            pane.Add(dollWrap);

            var silhouette = new VisualElement();
            silhouette.style.position = Position.Absolute;
            silhouette.style.left = Length.Percent(50);
            silhouette.style.top = Length.Percent(50);
            silhouette.style.width = 70;
            silhouette.style.height = 170;
            silhouette.style.translate = new Translate(new Length(-50, LengthUnit.Percent), new Length(-52, LengthUnit.Percent));
            silhouette.style.backgroundColor = Alpha(Panel, 0.88f);
            silhouette.style.borderTopLeftRadius = 35;
            silhouette.style.borderTopRightRadius = 35;
            silhouette.style.borderBottomLeftRadius = 28;
            silhouette.style.borderBottomRightRadius = 28;
            Border(silhouette, PA(0.08f), 1);
            dollWrap.Add(silhouette);

            var pos = new Dictionary<string, Vector2>
            {
                ["head"] = new Vector2(50, 8),
                ["neck"] = new Vector2(50, 26),
                ["chest"] = new Vector2(50, 42),
                ["mainhand"] = new Vector2(10, 42),
                ["offhand"] = new Vector2(76, 42),
                ["legs"] = new Vector2(50, 63),
                ["ring"] = new Vector2(12, 63),
                ["feet"] = new Vector2(50, 82),
            };

            for (int i = 0; i < IgMockData.EquipSlots.Length; i++)
            {
                var slotData = IgMockData.EquipSlots[i];
                if (!pos.TryGetValue(slotData.Id, out var p)) continue;
                var slot = BuildItemSlot(slotData.Filled, slotData.Icon, slotData.Item, false, 46);
                slot.style.position = Position.Absolute;
                slot.style.left = Length.Percent(p.x);
                slot.style.top = Length.Percent(p.y);
                slot.style.translate = new Translate(
                    p.x >= 45f && p.x <= 55f ? new Length(-50, LengthUnit.Percent) : 0,
                    0);
                dollWrap.Add(slot);
            }

            var footer = new VisualElement();
            footer.style.borderTopWidth = 1;
            footer.style.borderTopColor = PA(0.10f);
            footer.style.paddingTop = 12;
            pane.Add(footer);
            footer.Add(BuildKeyValue("Weight", "12.7 / 50 kg"));
            footer.Add(BuildKeyValue("Gold", IgMockData.Player.Gold + " gp"));
            return pane;
        }

        private static VisualElement BuildInventoryGrid(InventoryItemData selected)
        {
            var pane = new ScrollView();
            pane.style.flexGrow = 1;
            pane.style.paddingTop = 16;
            pane.style.paddingBottom = 16;
            pane.style.paddingLeft = 20;
            pane.style.paddingRight = 20;

            var wrap = new VisualElement();
            wrap.style.flexDirection = FlexDirection.Row;
            wrap.style.flexWrap = Wrap.Wrap;
            pane.Add(wrap);

            for (int i = 0; i < IgMockData.Inventory.Length; i++)
            {
                var item = IgMockData.Inventory[i];
                bool isSelected = item.Id == selected.Id;
                var card = new VisualElement();
                card.style.width = Length.Percent(23.5f);
                card.style.marginRight = Length.Percent((i % 4) == 3 ? 0f : 2f);
                card.style.marginBottom = 8;
                card.style.height = 78;
                card.style.backgroundColor = isSelected ? GA(0.13f) : Dark(0.65f);
                Border(card, isSelected ? Gold : Alpha(ItemTypeColor(item.Type), 0.27f), isSelected ? 2 : 1);
                Radius(card, 10);
                card.style.alignItems = Align.Center;
                card.style.justifyContent = Justify.Center;
                card.style.position = Position.Relative;
                card.style.paddingTop = 4;
                card.style.paddingBottom = 4;
                card.style.paddingLeft = 4;
                card.style.paddingRight = 4;

                var bar = new VisualElement();
                bar.style.position = Position.Absolute;
                bar.style.top = 0;
                bar.style.left = 10;
                bar.style.right = 10;
                bar.style.height = 2;
                bar.style.backgroundColor = Alpha(ItemTypeColor(item.Type), 0.6f);
                Radius(bar, 1);
                card.Add(bar);

                var type = Text(ShortType(item.Type).ToUpperInvariant(), Sans, 9, ItemTypeColor(item.Type));
                type.style.letterSpacing = 0.6f;
                card.Add(type);

                var name = Text(item.Name, Sans, 10, ParchDim);
                name.style.unityTextAlign = TextAnchor.MiddleCenter;
                name.style.whiteSpace = WhiteSpace.Normal;
                name.style.maxHeight = 26;
                card.Add(name);

                if (item.Quantity > 1)
                {
                    var qty = Text("×" + item.Quantity, Sans, 10, Amber, FontStyle.Bold);
                    card.Add(qty);
                }

                if (item.Equipped)
                {
                    var eq = Text("EQP", Sans, 8, Gold);
                    eq.style.letterSpacing = 0.5f;
                    card.Add(eq);
                }

                wrap.Add(card);
            }

            return pane;
        }

        private static VisualElement BuildDetailPane(InventoryItemData item, Action<string> onItemAction)
        {
            var pane = new VisualElement();
            pane.style.width = 256;
            pane.style.borderLeftWidth = 1;
            pane.style.borderLeftColor = PA(0.10f);
            pane.style.paddingTop = 18;
            pane.style.paddingBottom = 18;
            pane.style.paddingLeft = 16;
            pane.style.paddingRight = 16;
            pane.style.flexShrink = 0;

            var title = Text(item.Name, Serif, 16, Parch);
            title.style.marginBottom = 6;
            pane.Add(title);

            var kind = Text(item.Type.ToUpperInvariant(), Sans, 11, ItemTypeColor(item.Type));
            kind.style.letterSpacing = 0.8f;
            kind.style.marginBottom = 14;
            pane.Add(kind);

            pane.Add(BuildDividerValue("Weight", item.Weight.ToString("0.0") + " kg"));
            pane.Add(BuildDividerValue("Value", item.Value + " gp"));
            pane.Add(BuildDividerValue("Quantity", item.Quantity.ToString()));

            var actions = new VisualElement();
            actions.style.marginTop = 16;
            actions.style.flexDirection = FlexDirection.Column;
            if (item.Type == "Weapon" || item.Type == "Armor")
                actions.Add(BuildSecondaryButton("Equip", true, () => onItemAction?.Invoke("equip:" + item.Name)));
            if (item.Type == "Potion" || item.Type == "Food")
                actions.Add(BuildSecondaryButton("Use", true, () => onItemAction?.Invoke("use:" + item.Name)));
            actions.Add(BuildSecondaryButton("Drop", false, () => onItemAction?.Invoke("drop:" + item.Name)));
            actions.Add(BuildSecondaryButton("Inspect", false, () => onItemAction?.Invoke("inspect:" + item.Name)));
            pane.Add(actions);

            return pane;
        }

        private static VisualElement BuildItemSlot(bool filled, string icon, string item, bool selected, int size)
        {
            var slot = new VisualElement();
            slot.style.width = size;
            slot.style.height = size;
            slot.style.backgroundColor = filled ? Alpha(Panel, 0.72f) : Dark(0.55f);
            Border(slot, selected ? Gold : filled ? PA(0.28f) : PA(0.12f), selected ? 2 : 1);
            Radius(slot, 10);
            slot.style.alignItems = Align.Center;
            slot.style.justifyContent = Justify.Center;

            var glyph = Text(icon, Sans, Mathf.RoundToInt(size * 0.28f), WA(filled ? 0.8f : 0.2f));
            slot.Add(glyph);
            if (filled && !string.IsNullOrEmpty(item))
            {
                var shortName = Text(FirstWord(item), Sans, 8, ParchDim);
                shortName.style.marginTop = 2;
                slot.Add(shortName);
            }

            return slot;
        }

        private static VisualElement BuildKeyValue(string key, string value)
        {
            var row = Row();
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.paddingTop = 4;
            row.style.paddingBottom = 4;
            var k = Text(key, Sans, 12, PA(0.38f));
            var v = Text(value, Sans, 12, Parch, FontStyle.Bold);
            row.Add(k);
            row.Add(v);
            return row;
        }

        private static VisualElement BuildDividerValue(string key, string value)
        {
            var row = BuildKeyValue(key, value);
            row.style.borderBottomWidth = 1;
            row.style.borderBottomColor = PA(0.07f);
            row.style.paddingTop = 5;
            row.style.paddingBottom = 5;
            return row;
        }

        private static Button BuildSecondaryButton(string text, bool active, Action onClick)
        {
            var button = new Button(() => onClick?.Invoke()) { text = text.ToUpperInvariant() };
            ResetButton(button);
            button.style.height = 36;
            button.style.fontSize = 12;
            button.style.letterSpacing = 0.8f;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            ApplyFont(button, Sans);
            button.style.color = active ? Ink : PA(0.55f);
            button.style.backgroundColor = active ? Gold : Alpha(Panel, 0.62f);
            Border(button, active ? Amber : PA(0.18f), 1);
            Radius(button, 7);
            return button;
        }

        private static InventoryItemData GetSelectedItem()
        {
            if (IgMockData.Inventory != null && IgMockData.Inventory.Length > 0)
                return IgMockData.Inventory[0];
            return new InventoryItemData(0, "No Items", "Pack", 0f, 0, 0, false);
        }

        private static Color ItemTypeColor(string type)
        {
            switch (type)
            {
                case "Weapon": return Health;
                case "Armor": return Success;
                case "Potion": return Mana;
                case "Scroll": return Violet;
                case "Tool": return Amber;
                case "Food": return Orange;
                case "Currency": return Gold;
                default: return Parch;
            }
        }

        private static string ShortType(string type) => string.IsNullOrEmpty(type) ? string.Empty : type.Substring(0, Mathf.Min(4, type.Length));
        private static string FirstWord(string s) { int idx = s.IndexOf(' '); return idx > 0 ? s.Substring(0, idx) : s; }
    }
}
